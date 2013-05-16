SC=gmcs	# source compiler
RC=resgen # resource compiler
CFG=Debug
DBGFLG=-debug+
#CFG=Release
#DBGFLG=-debug-

all: ErrorMessages.resx \
     API \
     DaqDeviceClasses \
     DriverInterfaceClasses \
     General \
     PlatformInteropClasses \
     Properties
	$(RC) ErrorMessages.resx
	mkdir -p bin/$(CFG)
	chmod -R 777 bin
	$(SC) -t:library -unsafe $(DBGFLG) -recurse:'*.cs' -resource:ErrorMessages.resources,MeasurementComputing.DAQFlex.ErrorMessages.resources -r:System,System.Drawing,System.Windows.Forms -out:bin/$(CFG)/DAQFlex.dll

install:
	chmod 777 bin/$(CFG)/*.*
	cp -f ../../60-daqflex.rules /etc/udev/rules.d
	cp -f ../../daqflex.sh /etc/profile.d
	-install -d /usr/lib/daqflex
	cp -f ../../*.rbf /usr/lib/daqflex
	-install -c --mode=0755 bin/$(CFG)/DAQFlex.dll /usr/lib/daqflex
	-install -c --mode=0755 ../../DAQFlex.dll.config /usr/lib/daqflex
	-install -c --mode=0755 ../../DAQFlex.dll.config ../lib

uninstall:
	-rm -f /etc/udev/rules.d/60-daqflex.rules
	-rm -f /usr/lib/daqflex/DAQFlex*
	-rm -f /usr/lib/daqflex/*.rbf
	-rmdir /usr/lib/daqflex
	-rm -f /etc/profile.d/daqflex.sh
	
