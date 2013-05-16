SC=gmcs	# source compiler
RC=resgen # resource compiler
CFG=Debug
DBGFLG=-debug+
#CFG=Release
#DBGFLG=-debug-
AI=CSharp/AInScanWithCallback

all: *.cs 
	$(RC) AInScanForm.resx
	mkdir -p bin/$(CFG)
	chmod -R u=rwx,g=rwx,o=rwx bin
	$(SC) -t:winexe $(DBGFLG) -recurse:'*.cs'\
 	-resource:AInScanForm.resources,AInScanWithCallback.AInScanForm.resources \
 	-r:System,System.Drawing,System.Windows.Forms,/usr/lib/daqflex/DAQFlex.dll \
	-out:bin/$(CFG)/AInScanWithCallback.exe
	chmod -R u=rwx,g=rwx,o=rwx bin

install:
	-install -c --mode=0755 bin/$(CFG)/AInScanWithCallback.exe /usr/lib/daqflex
	-install -c --mode=0755 ainscanwithcallback /usr/bin
	dos2unix /usr/bin/ainscanwithcallback

uninstall:
	-rm -f /usr/lib/daqflex/AInScanWithCallback*
	-rm -f /usr/bin/ainscanwithcallback

