using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DCPU.VM
{
	/// <summary>
	/// The non-basic opcodes.
	/// </summary>
	public enum ExtendedOpcodes : ushort
	{
		Reserved = 0x00,
		JSR = 0x01
	}
}
