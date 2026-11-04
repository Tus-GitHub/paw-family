param([string]$DotNet = 'dotnet')
$ErrorActionPreference = 'Stop'
$sourceRoot = $PSScriptRoot
$runRoot = Join-Path $sourceRoot ('build/' + [Guid]::NewGuid().ToString('N'))
$published = Join-Path $runRoot 'published'
$release = Join-Path $runRoot 'PawFamily'
$dist = Join-Path $sourceRoot 'dist'
$version = ([xml](Get-Content -LiteralPath (Join-Path $sourceRoot 'PawFamily.csproj'))).Project.PropertyGroup.Version
New-Item -ItemType Directory -Path $published,$release,$dist -Force | Out-Null
& $DotNet publish (Join-Path $sourceRoot 'PawFamily.csproj') -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o $published
if ($LASTEXITCODE -ne 0) { throw 'Publish failed.' }
$test = Start-Process -FilePath (Join-Path $published 'PawFamily.exe') -ArgumentList '--self-test' -WindowStyle Hidden -Wait -PassThru
$result = Join-Path $published 'self-test-result.txt'
if ($test.ExitCode -ne 0 -or !(Test-Path -LiteralPath $result) -or !(Get-Content -LiteralPath $result -Raw).StartsWith('PASS:')) { throw 'Self-test failed.' }
Get-Content -LiteralPath $result
Copy-Item -LiteralPath (Join-Path $published 'PawFamily.exe') -Destination $release
foreach ($name in @('README.txt','LICENSE','THIRD-PARTY-NOTICES.txt')) { Copy-Item -LiteralPath (Join-Path $sourceRoot $name) -Destination $release }
$zip = Join-Path $dist "PawFamily-$version-win-x64.zip"
Compress-Archive -LiteralPath $release -DestinationPath $zip -Force
$hash = Get-FileHash -LiteralPath $zip -Algorithm SHA256
Set-Content -LiteralPath "$zip.sha256" -Value ($hash.Hash.ToLowerInvariant() + '  ' + [IO.Path]::GetFileName($zip)) -Encoding ascii
Write-Output "Windows package: $zip"
