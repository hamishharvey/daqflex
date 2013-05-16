SC=gmcs	# source compiler
RC=resgen # resource compiler
CFG=Debug
DBGFLG=-debug+
#CFG=Release
#DBGFLG=-debug-

all: *.cs 
	$(RC) CtrInForm.resx
	mkdir -p bin/$(CFG)
	chmod -R u=rwx,g=rwx,o=rwx bin
	$(SC) -t:winexe $(DBGFLG) -recurse:'*.cs'\
 	-resource:CtrInForm.resources,CtrIn.CtrInForm.resources \
 	-r:System,System.Drawing,System.Windows.Forms,/usr/lib/daqflex/DAQFlex.dll \
	-out:bin/$(CFG)/CtrIn.exe
	chmod -R u=rwx,g=rwx,o=rwx bin

install:
	-install -c --mode=0755 bin/$(CFG)/CtrIn.exe /usr/lib/daqflex
	-install -c --mode=0755 ctrin /usr/bin
	dos2unix /usr/bin/ctrin

uninstall:
	-rm -f /usr/lib/daqflex/CtrIn*
	-rm -f /usr/bin/ctrin
	
