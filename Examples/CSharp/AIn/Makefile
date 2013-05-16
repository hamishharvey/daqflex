SC=gmcs	# source compiler
RC=resgen # resource compiler
CFG=Debug
DBGFLG=-debug+
#CFG=Release
#DBGFLG=-debug-
AI=CSharp/AIn

all: *.cs 
	$(RC) AInForm.resx
	mkdir -p bin/$(CFG)
	chmod -R u=rwx,g=rwx,o=rwx bin
	$(SC) -t:winexe $(DBGFLG) -recurse:'*.cs'\
 	-resource:AInForm.resources,AIn.AInForm.resources \
 	-r:System,System.Drawing,System.Windows.Forms,/usr/lib/daqflex/DAQFlex.dll \
	-out:bin/$(CFG)/AIn.exe
	chmod -R u=rwx,g=rwx,o=rwx bin

install:
	-install -c --mode=0755 bin/$(CFG)/AIn.exe /usr/lib/daqflex
	-install -c --mode=0755 ain /usr/bin
	dos2unix /usr/bin/ain

uninstall:
	-rm -f /usr/lib/daqflex/AIn*
	-rm -f /usr/bin/ain
	
