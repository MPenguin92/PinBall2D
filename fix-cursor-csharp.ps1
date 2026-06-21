# Run this script after closing Cursor completely.
$ErrorActionPreference = "Stop"

$extRoot = Join-Path $env:USERPROFILE ".cursor\extensions"
$toDisable = @(
    "ms-dotnettools.csharp-2.84.19",
    "ms-dotnettools.csdevkit-1.16.6"
)

Write-Host "Step 1: Disable conflicting C# extensions"
foreach ($name in $toDisable) {
    $path = Join-Path $extRoot $name
    $disabled = "$path.disabled"
    if (Test-Path $path) {
        if (Test-Path $disabled) { Remove-Item $disabled -Recurse -Force }
        Rename-Item $path $disabled
        Write-Host "Disabled: $name"
    } elseif (Test-Path $disabled) {
        Write-Host "Already disabled: $name"
    } else {
        Write-Host "Not found: $name"
    }
}

Write-Host ""
Write-Host "Step 2: Fix keyboard.dispatch in Cursor settings"
$settingsPath = Join-Path $env:APPDATA "Cursor\User\settings.json"
if (Test-Path $settingsPath) {
    $content = Get-Content $settingsPath -Raw
    $content = $content -replace '"keyboard\.dispatch":\s*"keyCode"', '"keyboard.dispatch": "code"'
    Set-Content -Path $settingsPath -Value $content -Encoding UTF8
    Write-Host "Updated keyboard.dispatch to code"
}

Write-Host ""
Write-Host "Step 3: Disable Ctrl+Shift IME hotkeys"
reg add "HKCU\Keyboard Layout\Toggle" /v "Language Hotkey" /t REG_SZ /d "3" /f | Out-Null
reg add "HKCU\Keyboard Layout\Toggle" /v "Layout Hotkey" /t REG_SZ /d "3" /f | Out-Null
reg add "HKCU\Keyboard Layout\Toggle" /v "Hotkey" /t REG_SZ /d "3" /f | Out-Null
foreach ($id in @("00000011", "00000071")) {
    reg add "HKCU\Control Panel\Input Method\Hot Keys\$id" /v "Key Modifiers" /t REG_BINARY /d 00000000 /f | Out-Null
    reg add "HKCU\Control Panel\Input Method\Hot Keys\$id" /v "Virtual Key" /t REG_BINARY /d 00000000 /f | Out-Null
}
Write-Host "IME hotkeys updated"

Write-Host ""
Write-Host "Done. Reopen Cursor and press F1, search OmniSharp."
