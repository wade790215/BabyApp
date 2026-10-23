# Generate card name voices (zh-TW + en-US) with Windows SAPI into the project's audio folders.
param([string]$Project = "D:\Project\BabyApp")
Add-Type -AssemblyName System.Speech
$synth = New-Object System.Speech.Synthesis.SpeechSynthesizer
$csv = Import-Csv -Path (Join-Path $Project "Assets\BabyApp\CardManifest.csv") -Encoding UTF8
$made = 0
foreach ($row in $csv) {
    $folder = $row.deckId.Substring(0,1).ToUpper() + $row.deckId.Substring(1)
    $dir = Join-Path $Project ("Assets\BabyApp\Audio\Cards\" + $folder)
    New-Item -ItemType Directory -Force -Path $dir | Out-Null

    $zh = Join-Path $dir ($row.cardId + ".wav")
    if (-not (Test-Path $zh)) {
        $synth.SelectVoice("Microsoft Hanhan Desktop"); $synth.Rate = -2
        $synth.SetOutputToWaveFile($zh); $synth.Speak($row.cardName); $made += 1
    }
    $en = Join-Path $dir ($row.cardId + "_en.wav")
    if (-not (Test-Path $en)) {
        $synth.SelectVoice("Microsoft Zira Desktop"); $synth.Rate = -2
        $synth.SetOutputToWaveFile($en); $synth.Speak($row.cardNameEn); $made += 1
    }
}
$synth.SetOutputToNull()
$synth.Dispose()
"made $made voice files"
