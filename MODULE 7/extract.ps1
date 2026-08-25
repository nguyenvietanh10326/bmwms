$text = Get-Content "d:\bmwms\MODULE 7\ucs.txt" -Raw
$matches = [regex]::Matches($text, 'UC-\d+:\s*([^U]+)UI-type block.*?Allowed Roles\s*(.*?)\s*Verification Criteria', [System.Text.RegularExpressions.RegexOptions]::Singleline)
foreach ($match in $matches) {
    $uc = $match.Groups[1].Value.Trim()
    $roles = $match.Groups[2].Value.Trim() -replace '\s+', ' '
    Write-Output "$uc => $roles"
}
