$ThirdPartyDir = "../thirdparty"

$OpenBlasUrl = 'https://github.com/OpenMathLib/OpenBLAS/releases/download/v0.3.28/OpenBLAS-0.3.28-x64.zip'
$OpenBlasDir = ([System.IO.Path]::Combine($ThirdPartyDir, 'openblas'))
New-Item -Path $ThirdPartyDir -Name "openblas" -ItemType "directory"
Invoke-WebRequest $OpenBlasUrl -OutFile "openblas.zip"
Expand-Archive 'openblas.zip' -DestinationPath $OpenBlasDir -Force
Remove-Item 'openblas.zip'
