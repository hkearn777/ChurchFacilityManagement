# PowerShell Script to Update Dropbox Refresh Token in Google Cloud Secret Manager
# This script handles encoding correctly to avoid BOM issues

param(
    [Parameter(Mandatory=$true)]
    [string]$RefreshToken
)

Write-Host "Updating Dropbox refresh token in Secret Manager..." -ForegroundColor Green

# Write token to temp file with ASCII encoding (no BOM)
$tempFile = "dropbox-token-temp.txt"
[System.IO.File]::WriteAllText($tempFile, $RefreshToken, [System.Text.Encoding]::ASCII)

Write-Host "Token written to temporary file: $tempFile" -ForegroundColor Yellow

# Add new version to Secret Manager
Write-Host "Adding new version to Secret Manager..." -ForegroundColor Yellow
gcloud secrets versions add cfm-dropbox-refresh-token --data-file="$tempFile"

if ($LASTEXITCODE -eq 0) {
    Write-Host "✅ Success! Token added to Secret Manager" -ForegroundColor Green
    
    # Verify it was saved correctly
    Write-Host "`nVerifying token (first 20 characters)..." -ForegroundColor Yellow
    $savedToken = gcloud secrets versions access latest --secret="cfm-dropbox-refresh-token"
    Write-Host "Saved token starts with: $($savedToken.Substring(0, [Math]::Min(20, $savedToken.Length)))..." -ForegroundColor Cyan
    
    # Clean up temp file
    Remove-Item $tempFile
    Write-Host "`nTemp file cleaned up." -ForegroundColor Yellow
    
    Write-Host "`n✅ NEXT STEPS:" -ForegroundColor Green
    Write-Host "1. Redeploy Cloud Run to pick up the new token:" -ForegroundColor White
    Write-Host "   gcloud run deploy church-facility-management --region=us-central1" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "2. Try uploading an image again" -ForegroundColor White
    Write-Host ""
    Write-Host "3. Check logs for success:" -ForegroundColor White
    Write-Host "   gcloud logging read --filter 'textPayload=~`"Dropbox`"' --limit 20" -ForegroundColor Cyan
} else {
    Write-Host "❌ Failed to update secret. Error code: $LASTEXITCODE" -ForegroundColor Red
    Remove-Item $tempFile -ErrorAction SilentlyContinue
}
