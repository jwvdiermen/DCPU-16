using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DCPU.VM
{
	/// <summary>
	/// A DCPU container for all its states.
	/// All variables are put in arrays for easy referencing in the virtual machine.
	/// </summary>
	public class DCPU
	{
		#region Fields
		
		/// <summary>
		/// The array containing the registers.
		/// </summary>
		/// <remarks>The registers are: A, B, C, X, Y, Z, I and J.</remarks>
		public ushort[] Registers = new ushort[8];

		/// <summary>
		/// The buffer containing the miscellaneous variables.
		/// </summary>
		public ushort[] Misc = new ushort[3] { 0xFFFF, 0, 0 };

		/// <summary>
		/// The memory buffer.
		/// </summary>
		public ushort[] Memory = new ushort[0x10000];

		#endregion

		#region Properties

		/// <summary>
		/// Gets or sets the stack pointer.
		/// </summary>
		public ushort StackPointer
		{
			get { return Misc[0]; }
			set { Misc[0] = value; }
		}

		/// <summary>
		/// Gets or sets the program counter.
		/// </summary>
		public ushort ProgramCounter 
		{ 
			get { return Misc[1]; } 
			set { Misc[1] = value; } 
		}

		/// <summary>
		/// Gets or sets the overflow.
		/// </summary>
		public ushort Overflow
		{
			get { return Misc[2]; }
			set { Misc[2] = value; }
		}

		#endregion

		#region Methods

		public override string ToString()
		{
			return String.Format("{0:x4} {1:x4} {2:x4} {3:x4} {4:x4} {5:x4} {6:x4} {7:x4} {8:x4} {9:x4} {10:x4}",
				ProgramCounter, StackPointer, Overflow, 
				Registers[0], Registers[1], Registers[2], 
				Registers[3], Registers[4], Registers[5], 
				Registers[6], Registers[7]);
		}

		#endregion
	}
}
