SC=gmcs	# source compiler
RC=resgen # resource compiler
CFG=Debug
DBGFLG=-debug+
#CFG=Release
#DBGFLG=-debug-

all: MainForm.cs 
	$(RC) MainForm.resx
	$(RC) MessageLog.resx
	mkdir -p bin/$(CFG)
	chmod 777 bin
	chmod 777 bin/$(CFG)
	$(SC) -t:exe $(DBGFLG) -recurse:'*.cs' \
	-resource:MainForm.resources,MeasurementComputing.DAQFlex.Test.MainForm.resources \
	-resource:MessageLog.resources,MeasurementComputing.DAQFlex.Test.MessageLog.resources \
	-r:System,System.Drawing,System.Windows.Forms,/usr/lib/daqflex/DAQFlex.dll \
	-out:bin/$(CFG)/FlexTest.exe

install:
	chmod 777 bin/$(CFG)/*.*
	-install -c --mode=0755 bin/$(CFG)/FlexTest.exe /usr/lib/daqflex
	-install -c --mode=0755 ../../flextest /usr/bin
	dos2unix /usr/bin/flextest

uninstall:
	-rm -f /usr/lib/daqflex/FlexTest*
	-rm -f /usr/bin/flextest

