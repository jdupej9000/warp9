Name "warp9 Setup"
BrandingText " "
OutFile "warp9-setup.exe"
Caption "$(^Name)"
XPStyle on
RequestExecutionLevel user

!define SrcDir "..\bin\Release"
InstallDir "$ProgramFiles\warp9"

Page Directory
Page InstFiles

Section
	SetOutPath $INSTDIR	

	File ${SrcDir}\Warp9.exe
	File ${SrcDir}\Warp9.deps.json
	File ${SrcDir}\Warp9.dll	
	File ${SrcDir}\Warp9.runtimeconfig.json
	File ${SrcDir}\CHANGELOG.md

	File ${SrcDir}\libopenblas.dll		
	File ${SrcDir}\WarpCore.dll
	File ${SrcDir}\WarpProcessing.dll
	File ${SrcDir}\WarpViewer.dll

	File ${SrcDir}\Material.Icons.dll
	File ${SrcDir}\Material.Icons.WPF.dll
	File ${SrcDir}\SharpDX.D3DCompiler.dll
	File ${SrcDir}\SharpDX.Direct3D11.dll
	File ${SrcDir}\SharpDX.dll
	File ${SrcDir}\SharpDX.DXGI.dll
	File ${SrcDir}\TqkLibrary.Wpf.Interop.DirectX.dll
	File ${SrcDir}\TqkLibrary.Wpf.Interop.DirectX.Native.dll

	SetOutPath $INSTDIR\Assets
	File /r ${SrcDir}\Assets\*.*

	WriteUninstaller "$INSTDIR\uninstall.exe"
	
	SetOutPath $INSTDIR	
	CreateShortCut "$DESKTOP\warp9.lnk" "$INSTDIR\Warp9.exe"
SectionEnd


Section "uninstall"
	Delete "$INSTDIR\uninstall.exe"
	Delete "$DESKTOP\warp9.lnk"
	RMDir $INSTDIR
SectionEnd