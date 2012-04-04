using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DCPU.VM
{
	/// <summary>
	/// The basic opcodes.
	/// </summary>
	public enum BasicOpcodes : ushort
	{
		NonBasic = 0x0,
		SET = 0x1,
		ADD = 0x2,
		SUB = 0x3,
		MUL = 0x4,
		DIV = 0x5,
		MOD = 0x6,
		SHL = 0x7,
		SHR = 0x8,
		AND = 0x9,
		BOR = 0xA,
		XOR = 0xB,
		IFE = 0xC,
		IFN = 0xD,
		IFG = 0xE,
		IFB = 0xF
	}
}
