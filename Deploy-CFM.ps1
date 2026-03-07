# Church Facility Management - Deploy to Google Cloud Run
Write-Host "=" -ForegroundColor Cyan -NoNewline
Write-Host "=" * 60 -ForegroundColor Cyan
Write-Host "  Deploy Church Facility Management (CFM) to Google Cloud" -ForegroundColor Cyan
Write-Host "=" -ForegroundColor Cyan -NoNewline
Write-Host "=" * 60 -ForegroundColor Cyan
Write-Host ""

# Navigate to project directory
$scriptPath = Split-Path -Parent $MyInvocation.MyCommand.Path
Set-Location $scriptPath

Write-Host "Project Directory: $scriptPath" -ForegroundColor Yellow
Write-Host ""

# Verify credentials file exists
Write-Host "[1/2] Verifying Google credentials file..." -ForegroundColor Green
if (Test-Path "credentials.json") {
    Write-Host "      credentials.json found" -ForegroundColor Gray
} 
else {
    Write-Host "      ERROR: credentials.json not found!" -ForegroundColor Red
    Write-Host ""
    Write-Host "Press any key to exit..."
    $null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
    exit 1
}
Write-Host ""

# Dropbox access token
Write-Host "[2/2] Preparing environment variables..." -ForegroundColor Green
$dropboxToken = "sl.u.AGXQbBm0uxDK19U1r5qJpE5P0odZek15njKeGjfxJkL8PCX64vkQXh4LODRELhNodvQKbRzO5CJHFp9K0l2xJk0DIZ9nRv7oNIUF8Kv4CBIFs4UxQNvH1jrlUYBsZ6vshdojC9x-MxXc_Vj002pbQ1EwE5BRLYdt20b8NJBZd7pjjLy415iikNTincn0E_bGJtij3cDvpjp3YDHgcidTMBDvUOLle5uHsDO3IraEYh195t9Tlutr-fPUIpflp4z-hpvHzdgMRkwGDd3yLej4tqDZrzP7GV5aDK1OKkA9aa8muBeBkZYDBaaxAjRovx1iJBsMaWu00JQSPe8Ej8ie9AaitGAui97GE2GB8BleqUL0LYUufnszI5DZgXs-Wnap7nJjuDU0R3aK4PLu61N0cujMVKUyjgU6GeY5UaDRH24jH16kTuR3ckMr6zGUd9Y9JzB6YDBTRyQxni3AKGCqCMB5aN0YXc59qe9byDtX9EwTRmAVOLQR50-r_1ksAbd4pfugSTB21OeJCD7EzFVsoT4uO1xlulTBwjAh1-I3V3CcQSpIkWHO3IY7lNAu874ERkgNPQHJcv1ym-0UiVSU0wjL0N_0xZGqpzLilRWMpB25PB2DEsnX9R_J2sfH7Hc1lRnyPqrLty2C_nBkaRMNT5nPzqDL8KfpBuQsWbHvyVe0Ww-pUmgErXHNFjOxt-L_UFtAoWd6o4d5GuTG9EP4nRzylGOvajTCk69MquZPCwxvhAVDsaCG2916goh1mIXoOgqoVQyTe4Hlp_YGoI5lEsoKQmZdk04ucyAEaStpUTG6dqaza9CvBOoM8MzSbDARbb6L_A2sLwy2QIJkE01vFGcnbr5KGZWwx8Td9VulfNiD71pHxcf9feh4FEL4b0g8NhDXeEgU0yvJiXKkJMoMEm6SJ_xxBqqM1o1BDC3_NUAtPhchxEpZVV7gO7ikozKF0y8W7_TfAOpVOxSdAGjAe0J8Oyr7FSy5jWzY21CMCPJiHfw9HFBXldvbSMlxESbF5-SLLOml37fptpOdxnQVtGfifQpq5VRm7M3miekHrwulMf7sk2s2Zh3-4TcfJskys5FEw28-Ugb4Kuys5Nms8D55aHkiTl7g9eDL6HH8L_inDYve8U7ejmUW_b8k9uKgcvl1Dz7tqzF7TVXAspRfLGxh6Q56_hWxC-m7pjjBgv4wn6mi3RTU9Lwc3fg1df8zvj5rAZxThRhtF-317Y0OgdCo3pUlAvV5l81HXJqiaPY9e9UWSvEChqrNY7TGBnmjNeqIEblYxlcKy4i9n4N6tCy0zs2dKa7b9Kh94XbQ0LpN1w"
Write-Host "      Dropbox token configured" -ForegroundColor Gray
Write-Host ""

# Deploy to Cloud Run
Write-Host "Deploying to Google Cloud Run..." -ForegroundColor Green
Write-Host "This may take 3-5 minutes..." -ForegroundColor Gray
Write-Host ""

# Deploy - credentials.json will be copied by Dockerfile, only pass Dropbox token as env var
gcloud run deploy church-facility-management `
  --source . `
  --platform managed `
  --region us-central1 `
  --allow-unauthenticated `
  --set-env-vars "DROPBOX_ACCESS_TOKEN=$dropboxToken"

if ($LASTEXITCODE -eq 0) {
    Write-Host ""
    Write-Host "=" -ForegroundColor Green -NoNewline
    Write-Host "=" * 60 -ForegroundColor Green
    Write-Host "  DEPLOYMENT SUCCESSFUL!" -ForegroundColor Green
    Write-Host "=" -ForegroundColor Green -NoNewline
    Write-Host "=" * 60 -ForegroundColor Green
    Write-Host ""
    Write-Host "Your application is now live at:" -ForegroundColor Yellow
    Write-Host "https://church-facility-management-902794624514.us-central1.run.app" -ForegroundColor Cyan
} else {
    Write-Host ""
    Write-Host "  X DEPLOYMENT FAILED - Check errors above" -ForegroundColor Red
}

Write-Host ""
Write-Host "Press any key to exit..."
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
