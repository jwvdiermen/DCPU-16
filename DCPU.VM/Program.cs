using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace DCPU.VM
{
	class Program
	{
		static void Main(string[] args)
		{
			// Load a simple file.
			// Every line in the file represents a Word.

			// Read all the lines, and remove empty lines.
			var lines = File.ReadAllLines(@"Samples\Test.hex").Where(e => String.IsNullOrWhiteSpace(e) == false).ToArray();

			// Convert to lines to Words.
			ushort[] program = new ushort[lines.Length];
			for (int i = 0; i < lines.Length; ++i)
			{
				var code = Convert.ToUInt16(lines[i].Trim(), 16);
				program[i] = code;
			}

			// Create a virtual machine and start executing.
			var vm = new VirtualMachine();
			vm.Load(program);

			// Run the program step by step.
			//RunStepByStep(vm);

			// Run the program in its own thread.
			RunThreaded(vm);
		}

		private static void RunStepByStep(VirtualMachine vm)
		{
			Console.WriteLine("Press any key to step through the program.");
			Console.WriteLine("Press Q to quit.");
			Console.WriteLine();

			int counter = 0;
			while (true)
			{
				// Wait for a key press.
				// Quit the program when the Q key has been pressed.
				if ((System.Diagnostics.Debugger.IsAttached == false && Console.ReadKey(true).Key == ConsoleKey.Q) ||
					vm.DCPU.Halt == true)
				{
					break;
				}

				// Step the DCPU.
				vm.Step();
				Console.WriteLine(vm.DCPU.ToString((counter++ % 15) == 0));
			}

			if (vm.DCPU.Halt == true)
			{
				Console.WriteLine("The DCPU has halted.");
			}
		}

		private static void RunThreaded(VirtualMachine vm)
		{
			vm.Halted += (_vm, dcpu) =>
				{
					Console.WriteLine("The DCPU has halted.");
					Console.WriteLine();
					Console.WriteLine(dcpu.ToString(true));
				};

			vm.Start();
			Console.ReadKey(true);
			vm.Stop();
		}
	}
}
