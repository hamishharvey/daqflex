SC=gmcs	# source compiler
RC=resgen # resource compiler
CFG=Debug
DBGFLG=-debug+
#CFG=Release
#DBGFLG=-debug-

all: *.cs 
	$(RC) AOutScanForm.resx
	mkdir -p bin/$(CFG)
	chmod -R u=rwx,g=rwx,o=rwx bin
	$(SC) -t:winexe $(DBGFLG) -recurse:'*.cs'\
 	-resource:AOutScanForm.resources,AOutScan.AOutScanForm.resources \
 	-r:System,System.Drawing,System.Windows.Forms,/usr/lib/daqflex/DAQFlex.dll \
	-out:bin/$(CFG)/AOutScan.exe
	chmod -R u=rwx,g=rwx,o=rwx bin

install:
	-install -c --mode=0755 bin/$(CFG)/AOutScan.exe /usr/lib/daqflex
	-install -c --mode=0755 aoutscan /usr/bin
	dos2unix /usr/bin/aoutscan

uninstall:
	-rm -f /usr/lib/daqflex/AOutScan*
	-rm -f /usr/bin/aoutscan
	
