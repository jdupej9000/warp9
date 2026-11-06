$ThirdPartyDir = "../thirdparty"

$OpenBlasUrl = "https://github.com/OpenMathLib/OpenBLAS/releases/download/v0.3.34/OpenBLAS-0.3.34-x64.zip"
$OpenBlasDir = ([System.IO.Path]::Combine($ThirdPartyDir, 'openblas'))
New-Item -Path $ThirdPartyDir -Name "openblas" -ItemType "directory"
Invoke-WebRequest $OpenBlasUrl -OutFile "openblas.zip"
Expand-Archive 'openblas.zip' -DestinationPath $OpenBlasDir -Force
Remove-Item 'openblas.zip'

$EigenUrl = "https://gitlab.com/libeigen/eigen/-/archive/5.0.1/eigen-5.0.1.zip"
$EigenDir = ([System.IO.Path]::Combine($ThirdPartyDir, 'eigen'))
New-Item -Path $ThirdPartyDir -Name "eigen" -ItemType "directory"
Invoke-WebRequest $EigenUrl -OutFile "eigen.zip"
Expand-Archive 'eigen.zip' -DestinationPath $EigenDir -Force
Remove-Item 'eigen.zip'
