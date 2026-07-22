# Run this ONCE to save your GitHub token
Write-Host ""
Write-Host "Paste your GitHub token and press Enter:" -ForegroundColor Cyan
$token = Read-Host
$token.Trim() | Set-Content "$PSScriptRoot\.github-token"
Write-Host "Token saved. Now run .\publish.ps1 to publish." -ForegroundColor Green
