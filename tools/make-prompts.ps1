# Generate the mascot's prompt lines (zh-TW + en-US) with Windows SAPI into Assets/BabyApp/Audio/Prompts.
# Existing files are skipped so hand-recorded replacements survive a re-run.
param([string]$Project = "D:\Project\BabyApp")
Add-Type -AssemblyName System.Speech
$synth = New-Object System.Speech.Synthesis.SpeechSynthesizer
$dir = Join-Path $Project "Assets\BabyApp\Audio\Prompts"
New-Item -ItemType Directory -Force -Path $dir | Out-Null
$lines = Import-Csv -Path (Join-Path $Project "tools\prompts.csv") -Encoding UTF8
$made = 0
foreach ($l in $lines) {
    $path = Join-Path $dir ($l.file + ".wav")
    if (Test-Path $path) { continue }
    if ($l.lang -eq "en") { $synth.SelectVoice("Microsoft Zira Desktop") } else { $synth.SelectVoice("Microsoft Hanhan Desktop") }
    $synth.Rate = -2
    $synth.SetOutputToWaveFile($path)
    $synth.Speak($l.text)
    $made += 1
}
$synth.SetOutputToNull(); $synth.Dispose()
"made $made prompt files"
