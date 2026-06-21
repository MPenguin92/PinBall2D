# Fix Unity csproj for OmniSharp/Cursor.
# - Replace Rider placeholder paths
# - Restore broken XML header if needed
# - Save as UTF-8 WITHOUT BOM (BOM breaks OmniSharp)
$refPath = 'C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.8.1'
$placeholder = 'non_empty_path_generated_by_unity.rider.package'
$utf8NoBom = New-Object System.Text.UTF8Encoding $false

if (-not (Test-Path $refPath)) {
    Write-Error "Missing .NET Framework reference assemblies: $refPath"
    exit 1
}

$files = @(
    'Assembly-CSharp.csproj',
    'Assembly-CSharp-firstpass.csproj',
    'Assembly-CSharp-Editor.csproj',
    'Assembly-CSharp-Editor-firstpass.csproj',
    'EasySave3.csproj'
)

foreach ($f in $files) {
    $path = Join-Path $PSScriptRoot $f
    if (-not (Test-Path $path)) { continue }

    $content = $utf8NoBom.GetString([System.IO.File]::ReadAllBytes($path))
    if ($content.StartsWith([char]0xFEFF)) {
        $content = $content.Substring(1)
    }
    if ($content.StartsWith('?xml version')) {
        $content = '<' + $content
    }
    if ($content.StartsWith('xml version')) {
        $content = '<?' + $content
    }

    $content = $content -replace [regex]::Escape($placeholder), $refPath
    [System.IO.File]::WriteAllText($path, $content, $utf8NoBom)
    Write-Host "Fixed: $f"
}

Write-Host "Done. Reload Cursor and check OmniSharp Log for 'Successfully loaded project'."
