# C# / .NET DCPU-16 implementation

A C# / .NET based implementation of DCPU-16, a CPU used by the new game [0x10c](http://0x10c.com/) written by Notch.

See: [http://0x10c.com/doc/dcpu-16.txt](http://0x10c.com/doc/dcpu-16.txt)

## About

This project will contain a simple IDE for use with the DCPU-16 from [0x10c](http://0x10c.com/).

So what can you expect?

* A code editor for the assembly code.
* A compiler and linker for working with multiple files.
* A testing environment with virtual screen and keyboard.
* Additional I/O when released by Notch.
* And much more...

## ToDo ##

Below is a very rough list of things to do, in appearing order:

* Decouple the Virtual Machine into a class library. This is currently a prototype.
* Create the basic user interface.
* Add a compiler.
* Create a simple debugger, allowing to go step by step through the code. You'll have access to all the processor values and see the current executing line.
* Add functionality to easy add memory mapped I/O.
* Implement a virtual screen.
* Implement a virtual keyboard.
* Make a more fancy code editor.
* More to follow...