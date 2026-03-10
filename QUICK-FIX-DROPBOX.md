# 🔧 Dropbox OAuth 2.0 Setup (Permanent Fix)

## Why This Fix?

Dropbox now only generates short-lived tokens (starting with "sl.") that expire after 4 hours. The permanent solution is to use **OAuth 2.0 with refresh tokens**, which:
- ✅ Never expire
- ✅ Automatically refresh access tokens
- ✅ More secure
- ✅ Industry standard

## Solution: OAuth 2.0 Implementation

Your app has been updated to use OAuth 2.0! Here's how to set it up:

### Step 1: Get Dropbox App Credentials

1. Go to [Dropbox App Console](https://www.dropbox.com/developers/apps)
2. Select your app (or create a new "Scoped access" app)
3. Click the **Permissions** tab:
   - Enable `files.content.write`
   - Enable `sharing.write`
   - Click **Submit**
4. Click the **Settings** tab
5. Under **OAuth 2**, find **Redirect URIs** and add:
   - For local: `http://localhost:5118/dropbox/callback`
   - For production: `https://church-facility-management-902794624514.us-central1.run.app/dropbox/callback`
6. Copy your **App key** and **App secret** (you'll need these)

### Step 2: Local Development Setup

Run the setup script:

```powershell
# Run PowerShell as Administrator for permanent setup
.\Setup-LocalEnvironment.ps1
```

When prompted:
1. Paste your **Dropbox App Key**
2. Paste your **Dropbox App Secret**
3. Complete the email password setup

### Step 3: Authorize Dropbox Access

1. Start your app in Visual Studio (F5)
2. Open browser and go to: `http://localhost:5118/dropbox/setup`
3. Click **"Authorize Dropbox"**
4. Log in to Dropbox and approve the authorization
5. You'll be redirected back with a success message
6. Your refresh token is now saved in `dropbox_tokens.json`

🎉 **Done!** Your app now has permanent Dropbox access.

### Step 4: Cloud Deployment

When you're ready to deploy:

```powershell
.\Deploy-CFM.ps1
```

The script will:
1. Prompt you for your App Key and App Secret (one-time)
2. Deploy your app to Cloud Run
3. Show you a link to complete OAuth authorization
4. Display the refresh token to save

After first deployment:
1. Visit: `https://your-app-url/dropbox/setup`
2. Authorize Dropbox
3. Copy the refresh token shown on the success page
4. Run the command provided to save it:
   ```bash
   echo "YOUR_REFRESH_TOKEN" | gcloud secrets create cfm-dropbox-refresh-token --data-file=-
   ```
5. Re-run `.\Deploy-CFM.ps1` to update the deployment

## What Changed in Your Code?

✅ **New Service**: `DropboxOAuthService.cs` - Handles OAuth flow and token refresh  
✅ **Updated**: `DropboxService.cs` - Now uses refresh tokens automatically  
✅ **New Endpoints**: `/dropbox/setup` and `/dropbox/callback` for OAuth flow  
✅ **Updated Scripts**: Both deployment and local setup scripts support OAuth  
✅ **Configuration**: `appsettings.json` now includes OAuth settings  

## How It Works

1. **One-time authorization**: User authorizes the app via Dropbox OAuth
2. **Refresh token stored**: A never-expiring refresh token is saved securely
3. **Auto-refresh**: When access token expires (4 hours), it's automatically refreshed
4. **Seamless operation**: Your app always has valid Dropbox access

## Troubleshooting

### "Dropbox not configured" Error

Make sure environment variables are set:
```powershell
# Check variables
echo $env:DROPBOX_APP_KEY
echo $env:DROPBOX_APP_SECRET
echo $env:DROPBOX_REDIRECT_URI

# If not set, run:
.\Setup-LocalEnvironment.ps1
```

### OAuth Authorization Fails

1. Check that your redirect URI matches exactly in Dropbox app settings
2. Make sure permissions are enabled (files.content.write, sharing.write)
3. Try clearing your browser cookies and authorizing again

### Tokens Not Saving

- **Local**: Check that `dropbox_tokens.json` is created in your project folder
- **Production**: Verify the refresh token is saved in Google Cloud Secret Manager:
  ```bash
  gcloud secrets versions access latest --secret="cfm-dropbox-refresh-token"
  ```

## Benefits of This Approach

✅ **No more expired tokens** - Refresh tokens don't expire  
✅ **Automatic renewal** - Access tokens refresh transparently  
✅ **More secure** - Follows OAuth 2.0 best practices  
✅ **Production-ready** - Works seamlessly in cloud deployment  

---

**You're all set!** 🚀 This is a one-time setup. Once configured, your app will maintain Dropbox access indefinitely.
