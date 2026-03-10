# Church Facility Management - Local Development Setup
Write-Host "=" -ForegroundColor Cyan -NoNewline
Write-Host "=" * 60 -ForegroundColor Cyan
Write-Host "  Local Development Environment Setup" -ForegroundColor Cyan
Write-Host "=" -ForegroundColor Cyan -NoNewline
Write-Host "=" * 60 -ForegroundColor Cyan
Write-Host ""

Write-Host "This script will help you set up environment variables for local development." -ForegroundColor White
Write-Host ""

# Check if running as Administrator
$isAdmin = ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)

if (-not $isAdmin) {
    Write-Host "WARNING: Not running as Administrator" -ForegroundColor Yellow
    Write-Host "Environment variables will only be set for the current PowerShell session." -ForegroundColor Yellow
    Write-Host "To set them permanently, run PowerShell as Administrator." -ForegroundColor Yellow
    Write-Host ""
}

# Dropbox OAuth Configuration
Write-Host "[1/3] Dropbox OAuth Configuration" -ForegroundColor Green
Write-Host ""
Write-Host "To get your Dropbox App credentials:" -ForegroundColor White
Write-Host "1. Go to https://www.dropbox.com/developers/apps" -ForegroundColor Gray
Write-Host "2. Select your app (or create one)" -ForegroundColor Gray
Write-Host "3. Go to 'Settings' tab" -ForegroundColor Gray
Write-Host ""
Write-Host "Paste your Dropbox App Key (or press Enter to skip):" -ForegroundColor White
$dropboxAppKey = Read-Host

if (-not [string]::IsNullOrWhiteSpace($dropboxAppKey)) {
    $env:DROPBOX_APP_KEY = $dropboxAppKey

    if ($isAdmin) {
        [System.Environment]::SetEnvironmentVariable("DROPBOX_APP_KEY", $dropboxAppKey, [System.EnvironmentVariableTarget]::User)
    }

    Write-Host "Paste your Dropbox App Secret:" -ForegroundColor White
    $dropboxAppSecret = Read-Host -AsSecureString
    $dropboxAppSecretPlain = [Runtime.InteropServices.Marshal]::PtrToStringAuto([Runtime.InteropServices.Marshal]::SecureStringToBSTR($dropboxAppSecret))

    $env:DROPBOX_APP_SECRET = $dropboxAppSecretPlain
    $env:DROPBOX_REDIRECT_URI = "http://localhost:5118/dropbox/callback"

    if ($isAdmin) {
        [System.Environment]::SetEnvironmentVariable("DROPBOX_APP_SECRET", $dropboxAppSecretPlain, [System.EnvironmentVariableTarget]::User)
        [System.Environment]::SetEnvironmentVariable("DROPBOX_REDIRECT_URI", "http://localhost:5118/dropbox/callback", [System.EnvironmentVariableTarget]::User)
        Write-Host "      Dropbox OAuth configured (permanent)" -ForegroundColor Green
    } else {
        Write-Host "      Dropbox OAuth configured (session only)" -ForegroundColor Yellow
    }

    Write-Host ""
    Write-Host "      After running your app, visit http://localhost:5118/dropbox/setup" -ForegroundColor Cyan
    Write-Host "      to complete the OAuth authorization flow." -ForegroundColor Cyan
} else {
    Write-Host "      Skipped" -ForegroundColor Gray
}

Write-Host ""

# Email Password
Write-Host "[2/3] Email Password" -ForegroundColor Green
Write-Host ""
Write-Host "Enter your Gmail App Password (or press Enter to skip):" -ForegroundColor White
Write-Host "(This is the 16-character password from Google Account settings)" -ForegroundColor Gray
$emailPassword = Read-Host -AsSecureString
$emailPasswordPlain = [Runtime.InteropServices.Marshal]::PtrToStringAuto([Runtime.InteropServices.Marshal]::SecureStringToBSTR($emailPassword))

if (-not [string]::IsNullOrWhiteSpace($emailPasswordPlain)) {
    $env:Email__FromPassword = $emailPasswordPlain
    
    if ($isAdmin) {
        [System.Environment]::SetEnvironmentVariable("Email__FromPassword", $emailPasswordPlain, [System.EnvironmentVariableTarget]::User)
        Write-Host "      Email password set (permanent)" -ForegroundColor Green
    } else {
        Write-Host "      Email password set (session only)" -ForegroundColor Yellow
    }
} else {
    Write-Host "      Skipped" -ForegroundColor Gray
}

Write-Host ""
Write-Host "=" -ForegroundColor Green -NoNewline
Write-Host "=" * 60 -ForegroundColor Green
Write-Host "  Setup Complete!" -ForegroundColor Green
Write-Host "=" -ForegroundColor Green -NoNewline
Write-Host "=" * 60 -ForegroundColor Green
Write-Host ""

if ($isAdmin) {
    Write-Host "Environment variables have been set permanently." -ForegroundColor White
    Write-Host "You may need to restart Visual Studio for changes to take effect." -ForegroundColor Yellow
} else {
    Write-Host "Environment variables are set for this PowerShell session only." -ForegroundColor Yellow
    Write-Host "To make them permanent, re-run this script as Administrator." -ForegroundColor Yellow
}

Write-Host ""
Write-Host "Press any key to exit..."
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
