$ThirdPartyDir = "../thirdparty"

$EigenUrl = "https://gitlab.com/libeigen/eigen/-/archive/5.0.1/eigen-5.0.1.zip"
$EigenDir = ([System.IO.Path]::Combine($ThirdPartyDir, 'eigen'))
New-Item -Path $ThirdPartyDir -Name "eigen" -ItemType "directory"
Invoke-WebRequest $EigenUrl -OutFile "eigen.zip"
Expand-Archive 'eigen.zip' -DestinationPath $EigenDir -Force
Remove-Item 'eigen.zip'

$SpectraUrl = "https://github.com/yixuan/spectra/archive/refs/tags/v1.2.0.zip"
$SpectraDir = ([System.IO.Path]::Combine($ThirdPartyDir, 'spectra'))
New-Item -Path $ThirdPartyDir -Name "spectra" -ItemType "directory"
Invoke-WebRequest $SpectraUrl -OutFile "spectra.zip"
Expand-Archive 'spectra.zip' -DestinationPath $SpectraDir -Force
Remove-Item 'spectra.zip'
