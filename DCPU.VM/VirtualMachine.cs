using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Threading;

namespace DCPU.VM
{
	/// <summary>
	/// Used for executing code with a <see cref="DCPU" />.
	/// </summary>
	public class VirtualMachine
	{
		#region Fields

		private ushort[] m_program;
		private DCPU m_dcpu;

		private long m_frequency = 100000;
		private volatile int m_cycleTicks = (int)(Stopwatch.Frequency / 100000);

		private object m_executionLock = new object();
		private Thread m_executionThread;

		private static readonly ushort[] ms_literals = new ushort[]
		{
			0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x0A, 0x0B, 0x0C, 0x0D, 0x0E, 0x0F,
			0x10, 0x11, 0x12, 0x13, 0x14, 0x15, 0x16, 0x17, 0x18, 0x19, 0x1A, 0x1B, 0x1C, 0x1D, 0x1E, 0x1F
		};

		#endregion

		#region Properties

		/// <summary>
		/// Gets the DCPU.
		/// </summary>
		public DCPU DCPU
		{
			get { return m_dcpu; }
		}

		/// <summary>
		/// Gets or sets the frequency of execution.
		/// </summary>
		/// <remarks>The default value is 100.000 (100 MHz).</remarks>
		public long Frequency
		{
			get { return m_frequency; }
			set 
			{
				m_frequency = value;
				m_cycleTicks = (int)(Stopwatch.Frequency / m_frequency);
			}
		}

		public bool IsRunning
		{
			get
			{
				lock (m_executionLock)
				{
					return m_executionThread != null;
				}
			}
		}

		#endregion

		#region Methods

		/// <summary>
		/// Loads the given program into the memory of the DCPU.
		/// </summary>
		/// <param name="program">The program buffer.</param>
		public void Load(ushort[] program)
		{
			if (program.Length > 0x1000)
			{
				throw new ArgumentException("The length of the program exceeds the total amount of available memory.");
			}

			lock (m_executionLock)
			{
				// Store a copy of the program.
				m_program = new ushort[program.Length];
				Array.Copy(program, m_program, program.Length);

				// Create a new instance of the DCPU and load the program into its memory.
				m_dcpu = new DCPU();
				Array.Copy(program, m_dcpu.Memory, program.Length);
			}
		}

		/// <summary>
		/// Resets the state of the DCPU if a program is loaded.
		/// </summary>
		public void Reset()
		{
			if (this.IsRunning == true)
			{
				throw new InvalidOperationException("Failed to reset the virtual machine. Can not reset the virtual machine while it is running.");
			}

			// Create a new instance of the DCPU and load the program into its memory.
			lock (m_executionLock)
			{
				if (m_dcpu != null)
				{
					m_dcpu = new DCPU();
					Array.Copy(m_program, m_dcpu.Memory, m_program.Length);
				}
			}
		}

		private int IncrementProgramCounter()
		{
			return m_dcpu.Memory[m_dcpu.ProgramCounter++];
		}

		private int InstructionLength(int code)
		{
			// Check if there are instruction to read the next word,
			// which can make the instruction 2 or 3 words long. Else,
			// the instruction is simply 1 word.

			int result = 1;
			if ((code & 0xF) == 0)
			{
				result += IsLongValue(code >> 10) == true ? 1 : 0;
			}
			else
			{
				result += IsLongValue((code >> 4) & 0x3F) == true ? 1 : 0;
				result += IsLongValue(code >> 10) == true ? 1 : 0;
			}

			return result;
		}

		private bool IsLongValue(int value)
		{
			return (value >= 0x10 && value <= 0x17) ||
				value == 0x1E || value == 0x1F;
		}

		private int GetValue(int code, out ushort[] buffer, out int offset, out bool isLiteral, ref int cycles)
		{
			isLiteral = false;

			if (code < 0x08)
			{
				buffer = m_dcpu.Registers;
				offset = code;
			}
			else if (code < 0x10)
			{
				buffer = m_dcpu.Memory;
				offset = m_dcpu.Registers[code - 0x08];
			}
			else if (code < 0x18)
			{
				buffer = m_dcpu.Memory;
				offset = m_dcpu.Registers[code - 0x10] + IncrementProgramCounter();

				cycles++;
			}
			else if (code == 0x18)
			{
				buffer = m_dcpu.Memory;
				offset = m_dcpu.StackPointer++;
			}
			else if (code == 0x19)
			{
				buffer = m_dcpu.Memory;
				offset = m_dcpu.StackPointer;
			}
			else if (code == 0x1A)
			{
				buffer = m_dcpu.Memory;
				offset = --m_dcpu.StackPointer;
			}
			else if (code == 0x1B)
			{
				buffer = m_dcpu.Misc;
				offset = 0; // StackPointer
			}
			else if (code == 0x1C)
			{
				buffer = m_dcpu.Misc;
				offset = 1; // ProgramCounter
			}
			else if (code == 0x1D)
			{
				buffer = m_dcpu.Misc;
				offset = 2; // Overflow
			}
			else if (code == 0x1E)
			{
				buffer = m_dcpu.Memory;
				offset = IncrementProgramCounter();

				cycles++;
			}
			else if (code == 0x1F)
			{
				buffer = m_dcpu.Memory;
				offset = m_dcpu.ProgramCounter++;
				isLiteral = true;

				cycles++;
			}
			else
			{
				buffer = ms_literals;
				offset = code - 0x20;
				isLiteral = true;
			}

			return buffer[offset];
		}

		private ushort CheckOverflow(int va, int vb, Func<int, int, int> operationFn, Func<int, int, int> overflowFn)
		{
			int value = operationFn(va, vb);
			m_dcpu.Overflow = (ushort)overflowFn(va, vb);

			return (ushort)(value & 0xFFFF);
		}

		/// <summary>
		/// Executes a single step.
		/// </summary>
		/// <returns>The number of necessary cycles to complete the step</returns>
		public int Step()
		{
			bool d;
			int cycles = 0;

			// Determine the opcode.
			BasicOpcodes basicOpcode;
			ExtendedOpcodes extendedOpcode = ExtendedOpcodes.Reserved;

			int firstWord = IncrementProgramCounter();
			basicOpcode = (BasicOpcodes)(firstWord & 0xF);

			if (basicOpcode == BasicOpcodes.NonBasic)
			{
				extendedOpcode = (ExtendedOpcodes)((firstWord >> 4) & 0x3F);

				int ca = firstWord >> 10;
				ushort[] ba; int oa;
				var va = GetValue(ca, out ba, out oa, out d, ref cycles);

				switch (extendedOpcode)
				{
					case ExtendedOpcodes.JSR:
						{
							cycles += 2;

							//SetValue(0x1A, GetValue(0x1C, ref cycles), ref cycles); // Simulate "SET PUSH, PC".
							//SetValue(0x1C, pa, ref cycles); // Simulate "SET PC, a".
							
							// Do this inline instead.
							m_dcpu.Memory[--m_dcpu.StackPointer] = m_dcpu.ProgramCounter;
							m_dcpu.ProgramCounter = (ushort)va;
						}
						break;

					default:
						throw new InvalidOperationException(String.Format("Unkown extended opcode 0x{0:x4}.", (int)extendedOpcode));
				}
			}
			else
			{				
				int ca = (firstWord >> 4) & 0x3F;
				ushort[] ba; int oa; bool skipAssign;
				var va = GetValue(ca, out ba, out oa, out skipAssign, ref cycles);

				int cb = firstWord >> 10;
				ushort[] bb; int ob;
				var vb = GetValue(cb, out bb, out ob, out d, ref cycles);

				bool res = true;
				switch (basicOpcode)
				{
					case BasicOpcodes.SET:
						cycles++;
						if (skipAssign == false) ba[oa] = bb[ob];
						break;

					case BasicOpcodes.ADD:
						cycles++;
						if (skipAssign == false) ba[oa] = CheckOverflow(ba[oa], bb[ob], (pa, pb) => pa + pb, (pa, pb) => (pa + pb) > 0xFFFF ? 0x0001 : 0x0);
						break;

					case BasicOpcodes.SUB:
						cycles++;
						if (skipAssign == false) ba[oa] = CheckOverflow(ba[oa], bb[ob], (pa, pb) => pa - pb, (pa, pb) => (pa + pb) < 0 ? 0xFFFF : 0x0);
						break;

					case BasicOpcodes.MUL:
						cycles += 2;
						if (skipAssign == false) ba[oa] = CheckOverflow(ba[oa], bb[ob], (pa, pb) => pa * pb, (pa, pb) => ((pa * pb) >> 16) & 0xFFFF);
						break;

					case BasicOpcodes.DIV:
						cycles += 3;
						if (skipAssign == false) ba[oa] = bb[ob] == 0 ? (m_dcpu.Overflow = 0) : CheckOverflow(ba[oa], bb[ob], (pa, pb) => pa / pb, (pa, pb) => ((pa << 16) / pb) & 0xFFFF);
						break;

					case BasicOpcodes.MOD:
						cycles += 3;
						if (skipAssign == false) ba[oa] = bb[ob] == 0 ? (ushort)0 : (ushort)(ba[oa] % bb[ob]);
						break;

					case BasicOpcodes.SHL:
						cycles += 2;
						if (skipAssign == false) ba[oa] = CheckOverflow(ba[oa], bb[ob], (pa, pb) => pa << pb, (pa, pb) => ((pa << pb) >> 16) & 0xFFFF);
						break;

					case BasicOpcodes.SHR:
						cycles += 2;
						if (skipAssign == false) ba[oa] = CheckOverflow(ba[oa], bb[ob], (pa, pb) => pa >> pb, (pa, pb) => ((pa << 16) >> pb) & 0xFFFF);
						break;

					case BasicOpcodes.AND:
						cycles++;
						if (skipAssign == false) ba[oa] = (ushort)(ba[oa] & bb[ob]);
						break;

					case BasicOpcodes.BOR:
						cycles++;
						if (skipAssign == false) ba[oa] = (ushort)(ba[oa] | bb[ob]);
						break;

					case BasicOpcodes.XOR:
						cycles++;
						if (skipAssign == false) ba[oa] = (ushort)(ba[oa] ^ bb[ob]);
						break;

					case BasicOpcodes.IFE:
						cycles += 2;
						res = ba[oa] == bb[ob];
						break;

					case BasicOpcodes.IFN:
						cycles += 2;
						res = ba[oa] != bb[ob];
						break;

					case BasicOpcodes.IFG:
						cycles += 2;
						res = ba[oa] > bb[ob];
						break;

					case BasicOpcodes.IFB:
						cycles += 2;
						res = (ba[oa] & bb[ob]) != 0;
						break;

					default:
						throw new InvalidOperationException(String.Format("Unkown basic opcode 0x{0:x1}.", (int)basicOpcode));
				}

				// Check if we need to skip the next instruction.
				if (res == false)
				{
					var skipCount = (ushort)(InstructionLength(IncrementProgramCounter()) - 1);
					m_dcpu.ProgramCounter += skipCount;
				}
			}

			return cycles;
		}

		private void ThreadEntry()
		{
			// Start the stopwatch and cycle loop.
			var stopwatch = new Stopwatch();
			try
			{
				stopwatch.Start();

				long lastTime = 0, currentTime = 0, waitTime = 0;
				while (Thread.CurrentThread.ThreadState == System.Threading.ThreadState.Running)
				{
					currentTime = stopwatch.ElapsedTicks;
					if (currentTime - lastTime > waitTime)
					{
						lastTime = currentTime;
						waitTime = Step() * m_cycleTicks;
					}
					else
					{
						Thread.Yield();
					}
				}
			}
			finally
			{
				stopwatch.Stop();
			}
		}

		/// <summary>
		/// Starts executing the loaded program using the given frequency.
		/// This method starts execution on a new thread and returns immediately.
		/// </summary>
		/// <remarks>
		/// Use the <see cref="M:VirtualMachine.Step" /> method instead to execute the
		/// the program step by step.
		/// </remarks>
		public void Start()
		{
			if (m_dcpu == null)
			{
				throw new InvalidOperationException("Failed to start the virtual machine. No program has been loaded.");
			}

			lock (m_executionLock)
			{
				if (m_executionThread == null)
				{
					m_executionThread = new Thread(ThreadEntry);
					m_executionThread.IsBackground = true;
					m_executionThread.Start();
				}
				else
				{
					throw new InvalidOperationException("Failed to start the virtual machine. It is already started.");
				}
			}
		}

		/// <summary>
		/// Stops the current executing program.
		/// </summary>
		public void Stop()
		{
			lock (m_executionLock)
			{
				if (m_executionThread != null)
				{
					m_executionThread.Abort();
					m_executionThread.Join();
					m_executionThread = null;
				}
			}
		}

		#endregion
	}
}
