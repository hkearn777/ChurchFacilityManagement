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

# Try to retrieve Google Credentials JSON from Secret Manager
Write-Host "Checking for Google Sheets credentials in Secret Manager..." -ForegroundColor Cyan
$googleCredentialsJson = $null
try {
    $googleCredentialsJson = gcloud secrets versions access latest --secret="cfm-google-credentials" 2>$null
    if ($LASTEXITCODE -eq 0 -and ![string]::IsNullOrWhiteSpace($googleCredentialsJson)) {
        Write-Host "      Google credentials retrieved from Secret Manager" -ForegroundColor Green
    } else {
        throw "Secret not found"
    }
} catch {
    Write-Host "      Secret not found. Let's set it up now!" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "==================================================================" -ForegroundColor Cyan
    Write-Host "  ONE-TIME SETUP: Store Google Sheets Credentials" -ForegroundColor Cyan
    Write-Host "==================================================================" -ForegroundColor Cyan
    Write-Host ""

    if (-not (Test-Path "credentials.json")) {
        Write-Host "      ERROR: credentials.json not found in project directory!" -ForegroundColor Red
        Write-Host "      Please ensure credentials.json exists before deployment." -ForegroundColor Red
        Write-Host ""
        Write-Host "Press any key to exit..."
        $null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
        exit 1
    }

    Write-Host "Reading credentials.json and storing in Secret Manager..." -ForegroundColor Cyan
    Get-Content "credentials.json" -Raw | gcloud secrets create cfm-google-credentials --data-file=-

    if ($LASTEXITCODE -eq 0) {
        Write-Host "      Google credentials stored successfully!" -ForegroundColor Green
        $googleCredentialsJson = Get-Content "credentials.json" -Raw
    } else {
        Write-Host "      ERROR: Failed to store credentials" -ForegroundColor Red
        Write-Host ""
        Write-Host "Press any key to exit..."
        $null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
        exit 1
    }
}

Write-Host ""

# Try to retrieve Dropbox App Key from Secret Manager
Write-Host "Checking for Dropbox App Key in Secret Manager..." -ForegroundColor Cyan
$dropboxAppKey = $null
try {
    $dropboxAppKey = gcloud secrets versions access latest --secret="cfm-dropbox-app-key" 2>$null
    if ($LASTEXITCODE -eq 0 -and ![string]::IsNullOrWhiteSpace($dropboxAppKey)) {
        Write-Host "      Dropbox App Key retrieved from Secret Manager" -ForegroundColor Green
    } else {
        throw "Secret not found"
    }
} catch {
    Write-Host "      Secret not found. Let's set it up now!" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "==================================================================" -ForegroundColor Cyan
    Write-Host "  ONE-TIME SETUP: Store Dropbox App Key in Secret Manager" -ForegroundColor Cyan
    Write-Host "==================================================================" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "Paste your Dropbox App Key:" -ForegroundColor White
    Write-Host "(Found in your Dropbox App Console under 'App key')" -ForegroundColor Gray
    $dropboxAppKey = Read-Host

    Write-Host ""
    Write-Host "Storing App Key in Secret Manager..." -ForegroundColor Cyan
    $dropboxAppKey | gcloud secrets create cfm-dropbox-app-key --data-file=-

    if ($LASTEXITCODE -eq 0) {
        Write-Host "      App Key stored successfully!" -ForegroundColor Green
    } else {
        Write-Host "      ERROR: Failed to store App Key" -ForegroundColor Red
        Write-Host ""
        Write-Host "Press any key to exit..."
        $null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
        exit 1
    }
}

Write-Host ""
Write-Host "Checking for Dropbox App Secret in Secret Manager..." -ForegroundColor Cyan
$dropboxAppSecret = $null
try {
    $dropboxAppSecret = gcloud secrets versions access latest --secret="cfm-dropbox-app-secret" 2>$null
    if ($LASTEXITCODE -eq 0 -and ![string]::IsNullOrWhiteSpace($dropboxAppSecret)) {
        Write-Host "      Dropbox App Secret retrieved from Secret Manager" -ForegroundColor Green
    } else {
        throw "Secret not found"
    }
} catch {
    Write-Host "      Secret not found. Let's set it up now!" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "Paste your Dropbox App Secret:" -ForegroundColor White
    Write-Host "(Found in your Dropbox App Console - click 'Show' next to 'App secret')" -ForegroundColor Gray
    $dropboxAppSecret = Read-Host -AsSecureString
    $dropboxAppSecretPlain = [Runtime.InteropServices.Marshal]::PtrToStringAuto([Runtime.InteropServices.Marshal]::SecureStringToBSTR($dropboxAppSecret))

    Write-Host ""
    Write-Host "Storing App Secret in Secret Manager..." -ForegroundColor Cyan
    $dropboxAppSecretPlain | gcloud secrets create cfm-dropbox-app-secret --data-file=-

    if ($LASTEXITCODE -eq 0) {
        Write-Host "      App Secret stored successfully!" -ForegroundColor Green
        $dropboxAppSecret = $dropboxAppSecretPlain
    } else {
        Write-Host "      ERROR: Failed to store App Secret" -ForegroundColor Red
        Write-Host ""
        Write-Host "Press any key to exit..."
        $null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
        exit 1
    }
}

Write-Host ""
Write-Host "Checking for Dropbox Refresh Token in Secret Manager..." -ForegroundColor Cyan
$dropboxRefreshToken = $null
try {
    $dropboxRefreshToken = gcloud secrets versions access latest --secret="cfm-dropbox-refresh-token" 2>$null
    if ($LASTEXITCODE -eq 0 -and ![string]::IsNullOrWhiteSpace($dropboxRefreshToken)) {
        Write-Host "      Dropbox Refresh Token retrieved from Secret Manager" -ForegroundColor Green
    } else {
        Write-Host "      No refresh token found (will be configured after first OAuth setup)" -ForegroundColor Yellow
    }
} catch {
    Write-Host "      No refresh token found (will be configured after first OAuth setup)" -ForegroundColor Yellow
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
Write-Host "      - Google Sheets credentials: Ready" -ForegroundColor Gray
Write-Host "      - Dropbox App Key & Secret: Ready" -ForegroundColor Gray
Write-Host "      - Dropbox Refresh Token: $(if ($dropboxRefreshToken) { 'Ready' } else { 'Will be configured via OAuth' })" -ForegroundColor Gray
Write-Host "      - Email password: Ready" -ForegroundColor Gray
Write-Host ""

# Deploy to Cloud Run
Write-Host "Deploying to Google Cloud Run..." -ForegroundColor Green
Write-Host "This may take 3-5 minutes..." -ForegroundColor Gray
Write-Host ""

# Build environment variables
$envVars = "Email__FromPassword=$emailPasswordPlain,DROPBOX_APP_KEY=$dropboxAppKey,DROPBOX_APP_SECRET=$dropboxAppSecret,DROPBOX_REDIRECT_URI=https://church-facility-management-902794624514.us-central1.run.app/dropbox/callback"

# Add Google credentials as environment variable (base64 encoded to handle special characters)
$googleCredentialsBase64 = [Convert]::ToBase64String([System.Text.Encoding]::UTF8.GetBytes($googleCredentialsJson))
$envVars += ",GOOGLE_CREDENTIALS_JSON_BASE64=$googleCredentialsBase64"

# Add Spreadsheet ID from appsettings.Development.json (or prompt if not found)
$spreadsheetId = "1wAqKmvqa7rQoqttMBP1IQPB0G3465QNs5SsQXq5oTk4"
$envVars += ",GoogleSheets__SpreadsheetId=$spreadsheetId"

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
