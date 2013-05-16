
     ============================================================
                    DAQFlex Software Readme for Linux
                    Measurement Computing Corporation
     ============================================================

IMPORTANT: PLEASE READ THE DAQFlex END USER LICENSE AGREEMENT (DAQFlex EULA.rtf)

1. Introduction
2. Requirements
3. Installation
4. Support policy and how to contact us

=======================
1. Introduction
=======================
The DAQFlex Software installation includes the DAQFlex API Library, a ready to run test application 
and programming examples to use with your DAQFlex device. 
The DAQFlex devices implement a message-based protocol which makes programming DAQ devices extremely simple. 
All the source code for the DAQFlex software is included with the installation. 
Please read the following sections for important information regarding the DAQFlex software.

=======================
2. Requirements
=======================
Linux (Kernel 2.6)
Mono 2.4 or later framework and runtime 
Libusb 1.0 
MonoDevelop 2.0 (optional)
Intel Pentium IV class processor, 800 MHz or higher 
Video display—800 x 600, 256 colors or greater
Minimum of 512MB of RAM 
Minimum of 25MB of free hard disk space 
Microsoft-compatible mouse 

=======================
3. Installation
=======================

If not already installed, download and install the the Mono packages
for your distributions. The Mono packages may already be pre-installed 
on various distributions. You can check this using the following commands.

	$ mono -V

This will show the version to verify the core mono package is installed.

	$ resgen -V
	
This will show the version to verify the mono resource generator is installed
which is part of the mono-devel package.
	
The Mono packages are readily available from software repositories for 
most Linux distributions.

	libgdiplus
	mono-core
	mono-debugger
	mono-devel
	mono-tools
	mono-winforms
	monodoc-core

Note: On some distributions such as Ubuntu, simply installing the MonoDevelop package will
install all the necessary components. (http://monodevelop.com/Download)

Next, download and install the Libusb user mode driver library. Libusb can be downloaded from
http://sourceforge.net/projects/libusb/files/ (libusb-1.0). Build/install Libusb following 
the install documentation included. 

The DAQFlex API will try to load a shared object file named "libusb-1.0.so" so you may need
to create a symbolic link to the shared object file as a root user. 

	$ ln -s /usr/lib/libusb-1.0.x.x.x /usr/lib/libusb-1.0.so

Building the DAQFlex shared library:

	Extract the DAQFlex source files onto your system.

	In a terminal set the current directory to DAQFlex/Source/DAQFlexAPI directory.

	Run the following commands as a root user.

	$ make all
	$ make install

	Restart the sytem

Building the FlexTest application:

	In a terminal set the current directory to DAQFlex/Source/DAQFlexTest directory.

	Run the following commands as a root user.

	$ make all
	$ make install

	now you can run the FlexTest application using the following command

	$ flextest

Building the DAQFlex example applications:

	In a terminal set the current directory to DAQFlex/Examples directory.

	Run the following commands as a root user.

	$ make all
	$ make install

	now you can run the example programs as a non-root user using the 
	following commands.

	$ ain
	$ ainscan
	$ ainscanwithcallback
	$ aout
	$ aoutscan
	$ ctrin
	$ din
	$ dout
	$ tempview

Note: The DAQFlex test and example project files are configured to look for the DAQFlex API assembly
in the /usr/lib/daqflex folder. If you make changes to the DAQFlex API assembly and rebuild it,
you'll need to copy DAQFlex.dll (and DAQFlex.dll.mdb) to this folder as a root user.

==========================================
4. Support policy and how to contact us 
==========================================
Please contact Measurement Computing for information 
on technical support.

Measurement Computing
10 Commerce Way
Norton, MA 02766
Phone: (508) 946-5100
Fax: (508) 946-9500
www.mccdaq.com

 
