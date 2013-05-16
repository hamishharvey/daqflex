SC=gmcs	# source compiler
RC=resgen # resource compiler
CFG=Debug
DBGFLG=-debug+
#CFG=Release
#DBGFLG=-debug-
AI=CSharp/AInScanWithQueue

all: *.cs 
	$(RC) AInScanFormWithQueue.resx
	mkdir -p bin/$(CFG)
	chmod -R u=rwx,g=rwx,o=rwx bin
	$(SC) -t:winexe $(DBGFLG) -recurse:'*.cs'\
 	-resource:AInScanFormWithQueue.resources,AInScan.AInScanFormWithQueue.resources \
 	-r:System,System.Drawing,System.Windows.Forms,/usr/lib/daqflex/DAQFlex.dll \
	-out:bin/$(CFG)/AInScanWithQueue.exe
	chmod -R u=rwx,g=rwx,o=rwx bin

install:
	-install -c --mode=0755 bin/$(CFG)/AInScanWithQueue.exe /usr/lib/daqflex
	-install -c --mode=0755 ainscanwithqueue /usr/bin
	dos2unix /usr/bin/ainscanwithqueue

uninstall:
	-rm -f /usr/lib/daqflex/AInScanWithQueue*
	-rm -f /usr/bin/ainscanwithcallqueue

