param(
    [string] $EditorPath = 'C:\Program Files\Unity\Hub\Editor\6000.6.7f1\Editor\Unity.exe',
    [switch] $VerifyOnly
)
$ErrorActionPreference = 'Stop'
$taskRoot = Split-Path -Parent $PSScriptRoot
$projectPath = Join-Path $taskRoot 'LastTransmission'
if (!(Test-Path -LiteralPath $EditorPath)) { throw 'Unity Editor not found. Supply -EditorPath.' }
$outputPath = Join-Path $taskRoot 'Artifacts'
New-Item -ItemType Directory -Path $outputPath -Force | Out-Null
$logPath = Join-Path $outputPath 'unity-build.log'
$method = if ($VerifyOnly) { 'LastTransmission.Editor.Verification.Run' } else { 'LastTransmission.Editor.BuildGame.Windows' }
$arguments = @('-batchmode','-nographics','-projectPath',('"'+$projectPath+'"'),'-executeMethod',$method,'-quit','-logFile',('"'+$logPath+'"'))
$buildProcess = Start-Process -FilePath $EditorPath -ArgumentList $arguments -WindowStyle Hidden -PassThru -Wait
if ($buildProcess.ExitCode -ne 0) { Get-Content -LiteralPath $logPath -Tail 70; throw ('Unity failed: '+$buildProcess.ExitCode) }
Select-String -LiteralPath $logPath -Pattern 'EDITOR_VERIFICATION_PASS|LAST_TRANSMISSION_BUILD|error CS|Exception' | ForEach-Object Line
Write-Output ('Log: '+$logPath)
