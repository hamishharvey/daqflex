SC=gmcs	# source compiler
RC=resgen # resource compiler
CFG=Debug
DBGFLG=-debug+
#CFG=Release
#DBGFLG=-debug-

all: *.cs 
	$(RC) DInForm.resx
	mkdir -p bin/$(CFG)
	chmod -R u=rwx,g=rwx,o=rwx bin
	$(SC) -t:winexe $(DBGFLG) -recurse:'*.cs'\
 	-resource:DInForm.resources,DIn.DInForm.resources \
 	-r:System,System.Drawing,System.Windows.Forms,/usr/lib/daqflex/DAQFlex.dll \
	-out:bin/$(CFG)/DIn.exe
	chmod -R u=rwx,g=rwx,o=rwx bin

install:
	-install -c --mode=0755 bin/$(CFG)/DIn.exe /usr/lib/daqflex
	-install -c --mode=0755 din /usr/bin
	dos2unix /usr/bin/din

uninstall:
	-rm -f /usr/lib/daqflex/DIn*
	-rm -f /usr/bin/din
	
