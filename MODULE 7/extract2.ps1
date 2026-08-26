$lines = Get-Content 'd:\bmwms\MODULE 7\ucs.txt'
$uc = ''
foreach ($line in $lines) {
    if ($line -match 'UC-\d+:') {
        $uc = $line.Trim()
    }
    if ($line -match 'Allowed Roles') {
        Write-Output "$uc -> $line"
    }
}
