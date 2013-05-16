SC=gmcs	# source compiler
RC=resgen # resource compiler
CFG=Debug
DBGFLG=-debug+
#CFG=Release
#DBGFLG=-debug-

all: *.cs 
	$(RC) PulseTimerForm.resx
	mkdir -p bin/$(CFG)
	chmod -R u=rwx,g=rwx,o=rwx bin
	$(SC) -t:winexe $(DBGFLG) -recurse:'*.cs'\
 	-resource:PulseTimerForm.resources,PulseTimer.PulseTimerForm.resources \
 	-r:System,System.Drawing,System.Windows.Forms,/usr/lib/daqflex/DAQFlex.dll \
	-out:bin/$(CFG)/PulseTimer.exe
	chmod -R u=rwx,g=rwx,o=rwx bin

install:
	-install -c --mode=0755 bin/$(CFG)/PulseTimer.exe /usr/lib/daqflex
	-install -c --mode=0755 pulsetimer /usr/bin
	dos2unix /usr/bin/pulsetimer

uninstall:
	-rm -f /usr/lib/daqflex/PulseTimer*
	-rm -f /usr/bin/pulsetimer
	
