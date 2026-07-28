using namespace System.IO.Compression
Add-Type -AssemblyName System.IO.Compression.FileSystem

$docxPath = $args[0]
$tempDir = Join-Path $env:TEMP ([guid]::NewGuid().ToString())
New-Item -ItemType Directory -Path $tempDir | Out-Null

try {
    [ZipFile]::ExtractToDirectory($docxPath, $tempDir)
    $xmlPath = Join-Path $tempDir "word\document.xml"
    if (Test-Path $xmlPath) {
        $xmlContent = Get-Content $xmlPath -Raw
        # Simple regex to strip XML tags
        $text = $xmlContent -replace '<[^>]+>', ' '
        # Remove extra spaces
        $text = $text -replace '\s+', ' '
        Write-Output $text.Trim()
    } else {
        Write-Output "Could not find word\document.xml in $docxPath"
    }
} finally {
    Remove-Item -Path $tempDir -Recurse -Force
}
