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

		private delegate void GetValueDelegate(ref ushort value);
		private void GetValue(int code, ref int cycles, GetValueDelegate del)
		{
			switch (code)
			{
				case 0x00:
					del(ref m_dcpu.A);
					break;
				case 0x01:
					del(ref m_dcpu.B);
					break;
				case 0x02:
					del(ref m_dcpu.C);
					break;
				case 0x03:
					del(ref m_dcpu.X);
					break;
				case 0x04:
					del(ref m_dcpu.Y);
					break;
				case 0x05:
					del(ref m_dcpu.Z);
					break;
				case 0x06:
					del(ref m_dcpu.I);
					break;
				case 0x07:
					del(ref m_dcpu.J);
					break;

				case 0x08:
					del(ref m_dcpu.Memory[m_dcpu.A]);
					break;
				case 0x09:
					del(ref m_dcpu.Memory[m_dcpu.B]);
					break;
				case 0x0A:
					del(ref m_dcpu.Memory[m_dcpu.C]);
					break;
				case 0x0B:
					del(ref m_dcpu.Memory[m_dcpu.X]);
					break;
				case 0x0C:
					del(ref m_dcpu.Memory[m_dcpu.Y]);
					break;
				case 0x0D:
					del(ref m_dcpu.Memory[m_dcpu.Z]);
					break;
				case 0x0E:
					del(ref m_dcpu.Memory[m_dcpu.I]);
					break;
				case 0x0F:
					del(ref m_dcpu.Memory[m_dcpu.J]);
					break;

				case 0x10:
					del(ref m_dcpu.Memory[m_dcpu.A + m_dcpu.Memory[m_dcpu.ProgramCounter++]]);
					break;
				case 0x11:
					del(ref m_dcpu.Memory[m_dcpu.B + m_dcpu.Memory[m_dcpu.ProgramCounter++]]);
					break;
				case 0x12:
					del(ref m_dcpu.Memory[m_dcpu.C + m_dcpu.Memory[m_dcpu.ProgramCounter++]]);
					break;
				case 0x13:
					del(ref m_dcpu.Memory[m_dcpu.X + m_dcpu.Memory[m_dcpu.ProgramCounter++]]);
					break;
				case 0x14:
					del(ref m_dcpu.Memory[m_dcpu.Y + m_dcpu.Memory[m_dcpu.ProgramCounter++]]);
					break;
				case 0x15:
					del(ref m_dcpu.Memory[m_dcpu.Z + m_dcpu.Memory[m_dcpu.ProgramCounter++]]);
					break;
				case 0x16:
					del(ref m_dcpu.Memory[m_dcpu.I + m_dcpu.Memory[m_dcpu.ProgramCounter++]]);
					break;
				case 0x17:
					del(ref m_dcpu.Memory[m_dcpu.J + m_dcpu.Memory[m_dcpu.ProgramCounter++]]);
					break;

				case 0x18:
					del(ref m_dcpu.Memory[m_dcpu.StackPointer++]);
					break;
				case 0x19:
					del(ref m_dcpu.Memory[m_dcpu.StackPointer]);
					break;
				case 0x1A:
					del(ref m_dcpu.Memory[--m_dcpu.StackPointer]);
					break;

				case 0x1B:
					del(ref m_dcpu.StackPointer);
					break;
				case 0x1C:
					del(ref m_dcpu.ProgramCounter);
					break;
				case 0x1D:
					del(ref m_dcpu.Overflow);
					break;

				case 0x1E:
					del(ref m_dcpu.Memory[m_dcpu.Memory[m_dcpu.ProgramCounter++]]);
					break;
				case 0x1F:
					{
						// Use a temporary value here because it is literal, which should not be writable.
						ushort tmp = m_dcpu.Memory[m_dcpu.ProgramCounter++];
						del(ref tmp);
					}
					break;

				default:
					{
						ushort tmp = (ushort)(code - 0x20);
						del(ref tmp);
					}
					break;
			}
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
			int cycles = 0;

			// Determine the opcode.
			BasicOpcodes basicOpcode;
			ExtendedOpcodes extendedOpcode = ExtendedOpcodes.Reserved;

			int firstWord = m_dcpu.Memory[m_dcpu.ProgramCounter++];
			basicOpcode = (BasicOpcodes)(firstWord & 0xF);

			if (basicOpcode == BasicOpcodes.NonBasic)
			{
				extendedOpcode = (ExtendedOpcodes)((firstWord >> 4) & 0x3F);

				int ca = firstWord >> 10;
				GetValue(ca, ref cycles, (ref ushort va) =>
					{
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
					});				
			}
			else
			{
				int ca = (firstWord >> 4) & 0x3F;
				GetValue(ca, ref cycles, (ref ushort va) =>
					{
						int cb = firstWord >> 10;
						ushort vb = 0;
						GetValue(cb, ref cycles, (ref ushort tvb) => vb = tvb);

						bool res = true;
						switch (basicOpcode)
						{
							case BasicOpcodes.SET:
								cycles++;
								va = vb;
								break;

							case BasicOpcodes.ADD:
								cycles++;
								va = CheckOverflow(va, vb, (pa, pb) => pa + pb, (pa, pb) => (pa + pb) > 0xFFFF ? 0x0001 : 0x0);
								break;

							case BasicOpcodes.SUB:
								cycles++;
								va = CheckOverflow(va, vb, (pa, pb) => pa - pb, (pa, pb) => (pa + pb) < 0 ? 0xFFFF : 0x0);
								break;

							case BasicOpcodes.MUL:
								cycles += 2;
								va = CheckOverflow(va, vb, (pa, pb) => pa * pb, (pa, pb) => ((pa * pb) >> 16) & 0xFFFF);
								break;

							case BasicOpcodes.DIV:
								cycles += 3;
								va = vb == 0 ? (m_dcpu.Overflow = 0) : CheckOverflow(va, vb, (pa, pb) => pa / pb, (pa, pb) => ((pa << 16) / pb) & 0xFFFF);
								break;

							case BasicOpcodes.MOD:
								cycles += 3;
								va = vb == 0 ? (ushort)0 : (ushort)(va % vb);
								break;

							case BasicOpcodes.SHL:
								cycles += 2;
								va = CheckOverflow(va, vb, (pa, pb) => pa << pb, (pa, pb) => ((pa << pb) >> 16) & 0xFFFF);
								break;

							case BasicOpcodes.SHR:
								cycles += 2;
								va = CheckOverflow(va, vb, (pa, pb) => pa >> pb, (pa, pb) => ((pa << 16) >> pb) & 0xFFFF);
								break;

							case BasicOpcodes.AND:
								cycles++;
								va = (ushort)(va & vb);
								break;

							case BasicOpcodes.BOR:
								cycles++;
								va = (ushort)(va | vb);
								break;

							case BasicOpcodes.XOR:
								cycles++;
								va = (ushort)(va ^ vb);
								break;

							case BasicOpcodes.IFE:
								cycles += 2;
								res = va == vb;
								break;

							case BasicOpcodes.IFN:
								cycles += 2;
								res = va != vb;
								break;

							case BasicOpcodes.IFG:
								cycles += 2;
								res = va > vb;
								break;

							case BasicOpcodes.IFB:
								cycles += 2;
								res = (va & vb) != 0;
								break;

							default:
								throw new InvalidOperationException(String.Format("Unkown basic opcode 0x{0:x1}.", (int)basicOpcode));
						}

						// Check if we need to skip the next instruction.
						if (res == false)
						{
							var skipCount = (ushort)(InstructionLength(m_dcpu.Memory[m_dcpu.ProgramCounter++]) - 1);
							m_dcpu.ProgramCounter += skipCount;
						}
					});
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
				while (Thread.CurrentThread.ThreadState == System.Threading.ThreadState.Background)
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
