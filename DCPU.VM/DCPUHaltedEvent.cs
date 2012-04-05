using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DCPU.VM
{
	/// <summary>
	/// Used for listening the DCPU halts.
	/// </summary>
	/// <param name="vm">The virtual machine.</param>
	/// <param name="dcpu">The DCPU.</param>
	public delegate void DCPUHaltedEvent(VirtualMachine vm, DCPU dcpu);
}
