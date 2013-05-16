SC=gmcs	# source compiler
RC=resgen # resource compiler
CFG=Debug
DBGFLG=-debug+
#CFG=Release
#DBGFLG=-debug-
AI=CSharp/AInScan

all: *.cs 
	$(RC) AInScanForm.resx
	mkdir -p bin/$(CFG)
	chmod -R u=rwx,g=rwx,o=rwx bin
	$(SC) -t:winexe $(DBGFLG) -recurse:'*.cs'\
 	-resource:AInScanForm.resources,AInScan.AInScanForm.resources \
 	-r:System,System.Drawing,System.Windows.Forms,/usr/lib/daqflex/DAQFlex.dll \
	-out:bin/$(CFG)/AInScan.exe
	chmod -R u=rwx,g=rwx,o=rwx bin

install:
	-install -c --mode=0755 bin/$(CFG)/AInScan.exe /usr/lib/daqflex
	-install -c --mode=0755 ainscan /usr/bin
	dos2unix /usr/bin/ainscan

uninstall:
	-rm -f /usr/lib/daqflex/AInScan*
	-rm -f /usr/bin/ainscan

