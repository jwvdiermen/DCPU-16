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
		/// The A register.
		/// </summary>
		public ushort A;

		/// <summary>
		/// The B register.
		/// </summary>
		public ushort B;

		/// <summary>
		/// The C register.
		/// </summary>
		public ushort C;

		/// <summary>
		/// The Xregister.
		/// </summary>
		public ushort X;

		/// <summary>
		/// The Y register.
		/// </summary>
		public ushort Y;

		/// <summary>
		/// The Z register.
		/// </summary>
		public ushort Z;

		/// <summary>
		/// The I register.
		/// </summary>
		public ushort I;

		/// <summary>
		/// The J register.
		/// </summary>
		public ushort J;

		/// <summary>
		/// Gets or sets the stack pointer.
		/// </summary>
		public ushort StackPointer;

		/// <summary>
		/// Gets or sets the program counter.
		/// </summary>
		public ushort ProgramCounter;

		/// <summary>
		/// Gets or sets the overflow.
		/// </summary>
		public ushort Overflow;

		/// <summary>
		/// The memory buffer.
		/// </summary>
		public ushort[] Memory = new ushort[0x10000];

		#endregion

		#region Properties

		

		#endregion

		#region Methods

		public override string ToString()
		{
			return String.Format("{0:x4} {1:x4} {2:x4} {3:x4} {4:x4} {5:x4} {6:x4} {7:x4} {8:x4} {9:x4} {10:x4}",
				ProgramCounter, StackPointer, Overflow, A, B, C, X, Y, Z, I, J);
		}

		#endregion
	}
}
