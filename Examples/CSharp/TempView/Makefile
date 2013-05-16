SC=gmcs	# source compiler
RC=resgen # resource compiler
CFG=Debug
DBGFLG=-debug+
#CFG=Release
#DBGFLG=-debug-

all: *.cs 
	$(RC) TempViewForm.resx
	$(RC) SetupDlg.resx
	mkdir -p bin/$(CFG)
	chmod -R u=rwx,g=rwx,o=rwx bin
	$(SC) -t:winexe $(DBGFLG) -recurse:'*.cs'\
 	-resource:TempViewForm.resources,TempView.TempViewForm.resources \
 	-resource:SetupDlg.resources,TempView.SetupDlg.resources \
 	-r:System,System.Drawing,System.Windows.Forms,/usr/lib/daqflex/DAQFlex.dll \
	-out:bin/$(CFG)/TempView.exe
	chmod -R u=rwx,g=rwx,o=rwx bin

install:
	-install -c --mode=0755 bin/$(CFG)/TempView.exe /usr/lib/daqflex
	-install -c --mode=0755 tempview /usr/bin
	dos2unix /usr/bin/tempview

uninstall:
	-rm -f /usr/lib/daqflex/TempView*
	-rm -f /usr/bin/tempview
	
