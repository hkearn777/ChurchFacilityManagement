# Production Image Upload Debugging Guide

## Why Images Upload Locally But Not in Production

### Most Likely Causes:
1. **Missing Dropbox Refresh Token** in Cloud Run environment
2. **Missing or incorrect environment variables**
3. **Timeout issues** in production
4. **File size limits** in Cloud Run

---

## How to View Production Logs (CRITICAL)

Since you've added logging, you can now see what's failing:

### View logs in Google Cloud Console:
```bash
# Via gcloud CLI
gcloud logging read "resource.type=cloud_run_revision AND resource.labels.service_name=cfm-api" --limit=50 --format=json

# Or simplified
gcloud logging read "resource.type=cloud_run_revision" --limit=50
```

### Or use Cloud Console:
1. Go to https://console.cloud.google.com/logs
2. Select your Cloud Run service
3. Look for errors with keywords: "Dropbox", "upload", "Image upload failed"

---

## Step-by-Step Diagnostic Checklist

### 1. Verify Dropbox Refresh Token in Google Cloud Secret Manager

```bash
# Check if secret exists
gcloud secrets describe cfm-dropbox-refresh-token

# View the secret value (be careful - this is sensitive!)
gcloud secrets versions access latest --secret="cfm-dropbox-refresh-token"
```

**If the secret doesn't exist:**
- You need to run the OAuth flow in production first
- Go to https://YOUR_PRODUCTION_URL/dropbox/setup
- Complete the authorization
- The refresh token will be displayed - save it to Secret Manager

**Create the secret:**
```bash
echo "YOUR_REFRESH_TOKEN_HERE" | gcloud secrets create cfm-dropbox-refresh-token --data-file=-
```

---

### 2. Verify Cloud Run Environment Variables

Check your Cloud Run service has these environment variables:

```bash
gcloud run services describe cfm-api --format="value(spec.template.spec.containers[0].env)"
```

**Required environment variables:**
- `DROPBOX_APP_KEY` - Your Dropbox app key
- `DROPBOX_APP_SECRET` - Your Dropbox app secret  
- `DROPBOX_REDIRECT_URI` - Should be `https://YOUR_DOMAIN/dropbox/callback`

**Add missing variables:**
```bash
gcloud run services update cfm-api \
  --set-env-vars="DROPBOX_APP_KEY=YOUR_KEY,DROPBOX_APP_SECRET=YOUR_SECRET,DROPBOX_REDIRECT_URI=https://YOUR_DOMAIN/dropbox/callback"
```

---

### 3. Grant Cloud Run Access to Secret Manager

```bash
# Get your Cloud Run service account
SERVICE_ACCOUNT=$(gcloud run services describe cfm-api --format="value(spec.template.spec.serviceAccountName)")

# Grant access to the Dropbox refresh token secret
gcloud secrets add-iam-policy-binding cfm-dropbox-refresh-token \
  --member="serviceAccount:${SERVICE_ACCOUNT}" \
  --role="roles/secretmanager.secretAccessor"
```

---

### 4. Check Cloud Run Request Limits

Your Cloud Run service might have request size or timeout limits:

```bash
# Check current configuration
gcloud run services describe cfm-api --format="yaml(spec.template.spec)"
```

**Update if needed:**
```bash
# Increase timeout to 5 minutes (for image uploads)
gcloud run services update cfm-api --timeout=300

# Increase memory if needed
gcloud run services update cfm-api --memory=512Mi

# Increase max request size (if using Cloud Run with Cloud Load Balancer)
# This is typically 32MB by default
```

---

### 5. Test OAuth Flow in Production

1. Go to `https://YOUR_PRODUCTION_URL/dropbox/setup`
2. Click "Authorize Dropbox"
3. Complete OAuth flow
4. Verify you see the refresh token
5. Check if it's automatically saved or if you need to add it manually to Secret Manager

---

### 6. Monitor Logs After Deployment

After redeploying with logging:

**PowerShell:**
```powershell
# Stream live logs
gcloud logging tail --format "table(timestamp,severity,textPayload)"

# Filter for Dropbox-related logs
gcloud logging read --filter 'textPayload=~"Dropbox"' --limit 20 --format "table(timestamp,severity,textPayload)"

# Filter for upload errors
gcloud logging read --filter 'textPayload=~"upload"' --limit 20 --format "table(timestamp,severity,textPayload)"

# View all recent logs (simplest!)
gcloud logging read --limit 50 --format "table(timestamp,severity,textPayload)"
```

**Bash/Linux/Mac:**
```bash
# Stream live logs
gcloud logging tail "resource.type=cloud_run_revision AND resource.labels.service_name=cfm-api"

# Filter for Dropbox-related logs
gcloud logging read 'resource.type=cloud_run_revision AND textPayload=~"Dropbox"' --limit=20

# Filter for upload errors
gcloud logging read 'resource.type=cloud_run_revision AND textPayload=~"upload"' --limit=20
```

---

## Quick Test Checklist

After fixing the configuration:

1. ✅ Redeploy the app with new logging
2. ✅ Verify environment variables are set in Cloud Run
3. ✅ Verify Dropbox refresh token exists in Secret Manager
4. ✅ Verify Cloud Run has access to the secret
5. ✅ Try uploading an image via production URL
6. ✅ Check logs for errors: `gcloud logging tail`
7. ✅ If still failing, look at the specific error message in logs

---

## Common Error Messages and Solutions

### "Dropbox not configured"
- Missing `DROPBOX_REFRESH_TOKEN` secret or environment variables
- Solution: Complete OAuth setup or set environment variables

### "Failed to get valid tokens via OAuth service"
- Expired or invalid refresh token
- Solution: Re-run OAuth flow at `/dropbox/setup`

### "Could not create or retrieve shared link"
- File uploaded but sharing failed
- Check Dropbox app permissions include "files.content.write" and "sharing.write"

### "No images were uploaded successfully"
- Files might exceed 5MB limit
- Network timeout
- Check file size validation in logs

---

## Redeploy Command

After adding logging, redeploy:

```bash
gcloud run deploy cfm-api \
  --source . \
  --region=YOUR_REGION \
  --allow-unauthenticated \
  --timeout=300
```

---

## Next Steps

1. **Deploy the updated code** (with logging)
2. **Check production logs** immediately after trying an upload
3. **Look for the specific error message** in the logs
4. **Share the error message** if you need more help

The logging will tell you EXACTLY where it's failing!
