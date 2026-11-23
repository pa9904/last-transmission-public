$ErrorActionPreference = 'Stop'
$taskRoot = Split-Path -Parent $PSScriptRoot
$buildPath = Join-Path $taskRoot 'Builds\LastTransmission-0.12.0-Windows'
$zipPath = Join-Path $taskRoot 'Builds\LastTransmission-0.12.0-Windows-x64.zip'
foreach ($needed in @('LastTransmission.exe','UnityPlayer.dll','LastTransmission_Data','ThirdPartyNotices\SUIT-OFL.txt','ThirdPartyNotices\Kenney-CC0.txt','시작하기.txt')) {
    if (!(Test-Path -LiteralPath (Join-Path $buildPath $needed))) { throw ('Missing distribution file: '+$needed) }
}
$sourceCommit = (& git -C $taskRoot rev-parse HEAD).Trim()
if ($LASTEXITCODE -ne 0) { throw 'Cannot identify source commit.' }
$pendingChanges = @(& git -C $taskRoot status --porcelain)
if ($LASTEXITCODE -ne 0 -or $pendingChanges.Count -gt 0) { throw 'Commit the final source changes before packaging a release.' }
$buildInfo = [ordered]@{
    version = '0.12.0'
    unity = '6000.6.7f1'
    platform = 'Windows x64 Mono'
    sourceCommit = $sourceCommit
    narrativeSHA256 = (Get-FileHash -LiteralPath (Join-Path $taskRoot 'LastTransmission\Assets\Resources\story.json') -Algorithm SHA256).Hash
    playerAssemblySHA256 = (Get-FileHash -LiteralPath (Join-Path $buildPath 'LastTransmission_Data\Managed\Assembly-CSharp.dll') -Algorithm SHA256).Hash
    note = 'See Docs/VERIFICATION.md in the source repository for exact validation scope.'
}
$buildInfo | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $buildPath 'BuildInfo.json') -Encoding utf8
Copy-Item -LiteralPath (Join-Path $taskRoot 'Docs\ASSETS.md'),(Join-Path $taskRoot 'Docs\CREDITS.md') -Destination (Join-Path $buildPath 'ThirdPartyNotices') -Force
Copy-Item -LiteralPath (Join-Path $taskRoot 'Docs\ART_PROMPTS.md') -Destination (Join-Path $buildPath 'ThirdPartyNotices\ART_PROMPTS.md') -Force
Copy-Item -LiteralPath (Join-Path $taskRoot 'Docs\MAP_ART.md') -Destination (Join-Path $buildPath 'ThirdPartyNotices\MAP_ART.md') -Force
Copy-Item -LiteralPath (Join-Path $taskRoot 'Docs\UI_REFERENCES.md') -Destination (Join-Path $buildPath 'ThirdPartyNotices\UI_REFERENCES.md') -Force
Copy-Item -LiteralPath (Join-Path $taskRoot 'Docs\VERIFICATION.md') -Destination (Join-Path $buildPath '검증기록.md') -Force
Compress-Archive -LiteralPath $buildPath -DestinationPath $zipPath -Force
$archiveHash = Get-FileHash -LiteralPath $zipPath -Algorithm SHA256
$archiveHash | Format-List Path,Hash
($archiveHash.Hash + '  ' + (Split-Path -Leaf $zipPath)) | Set-Content -LiteralPath ($zipPath+'.sha256') -Encoding ascii
$archive = [System.IO.Compression.ZipFile]::OpenRead($zipPath)
try {
    if (!($archive.Entries.FullName -match 'LastTransmission.exe$')) { throw 'ZIP does not contain the player.' }
    if (!($archive.Entries.FullName -match 'LastTransmission_Data/globalgamemanagers$')) { throw 'ZIP does not contain game data.' }
    Write-Output ('ZIP entries: '+$archive.Entries.Count)
} finally { $archive.Dispose() }
Get-Item -LiteralPath $zipPath | Select-Object Name,Length
