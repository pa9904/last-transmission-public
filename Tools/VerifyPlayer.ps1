param([int] $Width = 1600, [int] $Height = 900)
$ErrorActionPreference = 'Stop'
function Assert-FreshFile {
    param([string] $Path, [DateTime] $After)
    if (!(Test-Path -LiteralPath $Path)) { throw ('Missing verification output: '+$Path) }
    if ((Get-Item -LiteralPath $Path).LastWriteTimeUtc -lt $After) { throw ('Stale verification output: '+$Path) }
}
$taskRoot = Split-Path -Parent $PSScriptRoot
$playerPath = Join-Path $taskRoot 'Builds\LastTransmission-0.12.0-Windows\LastTransmission.exe'
if (!(Test-Path -LiteralPath $playerPath)) { throw 'Build the Windows player first.' }
$outputPath = Join-Path $taskRoot ('Artifacts\Player-v0120-'+$Width+'x'+$Height)
New-Item -ItemType Directory -Force -Path $outputPath | Out-Null
$profilePath = Join-Path $outputPath ('MainProfile-'+[guid]::NewGuid().ToString('N'))
$logPath = Join-Path $outputPath 'player.log'
$arguments = @('--verify-player','--profile-dir',('"'+$profilePath+'"'),'--verification-output',('"'+$outputPath+'"'),'-screen-width',$Width,'-screen-height',$Height,'-screen-fullscreen','0','-logFile',('"'+$logPath+'"'))
# This is an interactive rendering check: keep the game visible so Windows does
# not suppress its rendering/OnGUI callbacks, as happens for hidden windows.
$runStarted = [DateTime]::UtcNow
$playerProcess = Start-Process -FilePath $playerPath -ArgumentList $arguments -WindowStyle Normal -PassThru
if (!$playerProcess.WaitForExit(90000)) { throw 'Player verification timed out. Inspect the player window and log.' }
if ($playerProcess.ExitCode -ne 0) { throw ('Player failed: '+$playerProcess.ExitCode) }
$reportPath = Join-Path $outputPath 'player-report.txt'
Assert-FreshFile -Path $reportPath -After $runStarted
if ((Get-Content -LiteralPath $reportPath -First 1) -ne 'PLAYER_VERIFICATION_PASS') { throw 'Player did not report a successful run.' }
Get-Content -LiteralPath $reportPath
foreach ($capture in @('00-room.png','01-title.png','02-map.png','03-radio.png','04-door.png','05-ending.png','06-voice.png','07-silence.png','08-casework.png','09-approvals.png','10-archive.png','11-collection.png','12-collected-ending.png','13-settings.png','14-map-bridge.png','15-map-detail.png','16-map-large-text.png','17-records.png','18-radio-received.png','19-radio-choices.png','20-rescue.png','21-echo.png','22-door.png','23-comparison.png','24-board-filed.png','25-map-entry.png','26-radio-needle.png','27-radio-echo.png')) {
    Assert-FreshFile -Path (Join-Path $outputPath $capture) -After $runStarted
}
$resumePath = Join-Path $outputPath 'ResumeProfile'
$resumeLog = Join-Path $outputPath 'resume.log'
$resumeArguments = @('--verify-resume','--profile-dir',('"'+$resumePath+'"'),'-screen-width',$Width,'-screen-height',$Height,'-screen-fullscreen','0','-logFile',('"'+$resumeLog+'"'))
$resumeStarted = [DateTime]::UtcNow
$resumeProcess = Start-Process -FilePath $playerPath -ArgumentList $resumeArguments -WindowStyle Normal -PassThru
if (!$resumeProcess.WaitForExit(30000)) { throw 'Fresh-process resume timed out.' }
if ($resumeProcess.ExitCode -ne 0) { throw 'Fresh-process resume failed.' }
$resumeReport = Join-Path $resumePath 'resume-report.txt'
Assert-FreshFile -Path $resumeReport -After $resumeStarted
if ((Get-Content -LiteralPath $resumeReport -First 1) -ne 'RESUME_VERIFICATION_PASS') { throw 'Fresh-process resume did not report success.' }
Get-Content -LiteralPath $resumeReport
$errors = Select-String -Path $logPath,$resumeLog -Pattern 'Exception|NullReference|MissingReference|error CS|Assertion failed|Failed to capture screen shot'
if ($errors) { $errors | ForEach-Object Line; throw 'Player log contains errors.' }
