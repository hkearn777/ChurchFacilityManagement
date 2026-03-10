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

# Retrieve secrets from Google Cloud Secret Manager
Write-Host "[2/2] Retrieving secrets from Google Cloud Secret Manager..." -ForegroundColor Green
Write-Host ""

# Try to retrieve Dropbox access token from Secret Manager
Write-Host "Checking for Dropbox access token in Secret Manager..." -ForegroundColor Cyan
$dropboxToken = $null
try {
    $dropboxToken = gcloud secrets versions access latest --secret="cfm-dropbox-token" 2>$null
    if ($LASTEXITCODE -eq 0 -and ![string]::IsNullOrWhiteSpace($dropboxToken)) {
        Write-Host "      Dropbox token retrieved from Secret Manager" -ForegroundColor Green
    } else {
        throw "Secret not found"
    }
} catch {
    Write-Host "      Secret not found. Let's set it up now!" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "==================================================================" -ForegroundColor Cyan
    Write-Host "  ONE-TIME SETUP: Store Dropbox Access Token in Secret Manager" -ForegroundColor Cyan
    Write-Host "==================================================================" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "To get a Dropbox access token:" -ForegroundColor White
    Write-Host "1. Go to https://www.dropbox.com/developers/apps" -ForegroundColor Gray
    Write-Host "2. Select your app (or create one)" -ForegroundColor Gray
    Write-Host "3. Go to 'Permissions' tab and enable 'files.content.write' and 'sharing.write'" -ForegroundColor Gray
    Write-Host "4. Go to 'Settings' tab and generate an access token" -ForegroundColor Gray
    Write-Host "   NOTE: Use 'Generate access token' in the OAuth 2 section for a long-lived token" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "Paste your Dropbox access token:" -ForegroundColor White
    Write-Host "(This will be stored securely in Google Cloud)" -ForegroundColor Gray
    $dropboxTokenSecure = Read-Host -AsSecureString
    $dropboxToken = [Runtime.InteropServices.Marshal]::PtrToStringAuto([Runtime.InteropServices.Marshal]::SecureStringToBSTR($dropboxTokenSecure))

    Write-Host ""
    Write-Host "Storing token in Secret Manager..." -ForegroundColor Cyan
    $dropboxToken | gcloud secrets create cfm-dropbox-token --data-file=-

    if ($LASTEXITCODE -eq 0) {
        Write-Host "      Token stored successfully!" -ForegroundColor Green
        Write-Host "      Future deployments will use this automatically" -ForegroundColor Gray
    } else {
        Write-Host "      ERROR: Failed to store token" -ForegroundColor Red
        Write-Host ""
        Write-Host "Press any key to exit..."
        $null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
        exit 1
    }
}

Write-Host ""
Write-Host "Checking for email password in Secret Manager..." -ForegroundColor Cyan

# Try to retrieve email password from Secret Manager
$emailPasswordPlain = $null
try {
    $emailPasswordPlain = gcloud secrets versions access latest --secret="cfm-email-password" 2>$null
    if ($LASTEXITCODE -eq 0 -and ![string]::IsNullOrWhiteSpace($emailPasswordPlain)) {
        Write-Host "      Email password retrieved from Secret Manager" -ForegroundColor Green
    } else {
        throw "Secret not found"
    }
} catch {
    Write-Host "      Secret not found. Let's set it up now!" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "==================================================================" -ForegroundColor Cyan
    Write-Host "  ONE-TIME SETUP: Store Gmail App Password in Secret Manager" -ForegroundColor Cyan
    Write-Host "==================================================================" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "Paste your 16-character Gmail App Password:" -ForegroundColor White
    Write-Host "(This will be stored securely in Google Cloud)" -ForegroundColor Gray
    $emailPassword = Read-Host -AsSecureString
    $emailPasswordPlain = [Runtime.InteropServices.Marshal]::PtrToStringAuto([Runtime.InteropServices.Marshal]::SecureStringToBSTR($emailPassword))

    Write-Host ""
    Write-Host "Storing password in Secret Manager..." -ForegroundColor Cyan
    $emailPasswordPlain | gcloud secrets create cfm-email-password --data-file=-

    if ($LASTEXITCODE -eq 0) {
        Write-Host "      Password stored successfully!" -ForegroundColor Green
        Write-Host "      Future deployments will use this automatically" -ForegroundColor Gray
    } else {
        Write-Host "      ERROR: Failed to store password" -ForegroundColor Red
        Write-Host ""
        Write-Host "Press any key to exit..."
        $null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
        exit 1
    }
}

Write-Host ""
Write-Host "      All secrets configured!" -ForegroundColor Green
Write-Host "      - Dropbox OAuth: Ready" -ForegroundColor Gray
Write-Host "      - Email password: Ready" -ForegroundColor Gray
Write-Host ""

# Deploy to Cloud Run
Write-Host "Deploying to Google Cloud Run..." -ForegroundColor Green
Write-Host "This may take 3-5 minutes..." -ForegroundColor Gray
Write-Host ""

# Build environment variables
$envVars = "Email__FromPassword=$emailPasswordPlain,DROPBOX_APP_KEY=$dropboxAppKey,DROPBOX_APP_SECRET=$dropboxAppSecret,DROPBOX_REDIRECT_URI=https://church-facility-management-902794624514.us-central1.run.app/dropbox/callback"

if (![string]::IsNullOrWhiteSpace($dropboxRefreshToken)) {
    $envVars += ",DROPBOX_REFRESH_TOKEN=$dropboxRefreshToken"
}

# Deploy - credentials.json will be copied by Dockerfile
gcloud run deploy church-facility-management `
  --source . `
  --platform managed `
  --region us-central1 `
  --allow-unauthenticated `
  --set-env-vars $envVars

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

    if ([string]::IsNullOrWhiteSpace($dropboxRefreshToken)) {
        Write-Host ""
        Write-Host "==================================================================" -ForegroundColor Yellow
        Write-Host "  NEXT STEP: Complete Dropbox OAuth Setup" -ForegroundColor Yellow
        Write-Host "==================================================================" -ForegroundColor Yellow
        Write-Host ""
        Write-Host "1. Visit: https://church-facility-management-902794624514.us-central1.run.app/dropbox/setup" -ForegroundColor Cyan
        Write-Host "2. Click 'Authorize Dropbox'" -ForegroundColor White
        Write-Host "3. Follow the instructions to save the refresh token" -ForegroundColor White
        Write-Host "4. Re-run this deployment script" -ForegroundColor White
        Write-Host ""
    }
} else {
    Write-Host ""
    Write-Host "  X DEPLOYMENT FAILED - Check errors above" -ForegroundColor Red
}

Write-Host ""
Write-Host "Press any key to exit..."
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
