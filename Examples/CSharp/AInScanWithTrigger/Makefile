SC=gmcs	# source compiler
RC=resgen # resource compiler
CFG=Debug
DBGFLG=-debug+
#CFG=Release
#DBGFLG=-debug-
AI=CSharp/AInScanWithTrigger

all: *.cs 
	$(RC) AInScanWithTriggerForm.resx
	mkdir -p bin/$(CFG)
	chmod -R u=rwx,g=rwx,o=rwx bin
	$(SC) -t:winexe $(DBGFLG) -recurse:'*.cs'\
 	-resource:AInScanWithTriggerForm.resources,AInScan.AInScanWithTriggerForm.resources \
 	-r:System,System.Drawing,System.Windows.Forms,/usr/lib/daqflex/DAQFlex.dll \
	-out:bin/$(CFG)/AInScanWithTrigger.exe
	chmod -R u=rwx,g=rwx,o=rwx bin

install:
	-install -c --mode=0755 bin/$(CFG)/AInScanWithTrigger.exe /usr/lib/daqflex
	-install -c --mode=0755 ainscanwithtrigger /usr/bin
	dos2unix /usr/bin/ainscanwithtrigger

uninstall:
	-rm -f /usr/lib/daqflex/AInScanWithTrigger*
	-rm -f /usr/bin/ainscanwithtrigger

