$ThirdPartyDir = "../thirdparty"

$EigenUrl = "https://gitlab.com/libeigen/eigen/-/archive/5.0.1/eigen-5.0.1.zip"
$EigenDir = ([System.IO.Path]::Combine($ThirdPartyDir, 'eigen'))
New-Item -Path $ThirdPartyDir -Name "eigen" -ItemType "directory"
Invoke-WebRequest $EigenUrl -OutFile "eigen.zip"
Expand-Archive 'eigen.zip' -DestinationPath $EigenDir -Force
Remove-Item 'eigen.zip'
