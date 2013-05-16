SC=gmcs	# source compiler
RC=resgen # resource compiler
CFG=Debug
DBGFLG=-debug+
#CFG=Release
#DBGFLG=-debug-

all: *.cs 
	$(RC) DOutForm.resx
	mkdir -p bin/$(CFG)
	chmod -R u=rwx,g=rwx,o=rwx bin
	$(SC) -t:winexe $(DBGFLG) -recurse:'*.cs'\
 	-resource:DOutForm.resources,DOut.DOutForm.resources \
 	-r:System,System.Drawing,System.Windows.Forms,/usr/lib/daqflex/DAQFlex.dll \
	-out:bin/$(CFG)/DOut.exe
	chmod -R u=rwx,g=rwx,o=rwx bin

install:
	-install -c --mode=0755 bin/$(CFG)/DOut.exe /usr/lib/daqflex
	-install -c --mode=0755 dout /usr/bin
	dos2unix /usr/bin/dout

uninstall:
	-rm -f /usr/lib/daqflex/DOut*
	-rm -f /usr/bin/dout
	
