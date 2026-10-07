[Setup]
AppName=Stock Manager
AppVersion=1.0
DefaultDirName={autopf}\StockManager
DefaultGroupName=Stock Manager
OutputDir=installer
OutputBaseFilename=StockManagerSetup
Compression=lzma
SolidCompression=yes
ArchitecturesInstallIn64BitMode=x64compatible

[Files]
Source: "publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs

[Icons]
Name: "{group}\Stock Manager"; Filename: "{app}\StockManager.App.exe"
Name: "{autodesktop}\Stock Manager"; Filename: "{app}\StockManager.App.exe"

[Run]
Filename: "{app}\StockManager.App.exe"; Description: "Launch Stock Manager"; Flags: nowait postinstall skipifsilent