# 🔍 Image Upload Debugging - Summary

## What I've Done

### 1. ✅ Added Comprehensive Logging
- Added detailed logging to all image upload endpoints:
  - `/requestor/create` (requestor form)
  - `/proxy/create` (proxy form)
  - `/request/create` (admin form)
  - `/request/{id}/update` (edit form)
  
- Logs now track:
  - Number of files received
  - Valid files being uploaded
  - Success/failure of Dropbox uploads
  - Specific error messages

### 2. ✅ Created Debugging Tools
- **check-production-config.md** - Complete diagnostic guide
- **view-production-logs.ps1** - PowerShell script to view logs
- **view-production-logs.bat** - Windows batch script
- **view-production-logs.sh** - Linux/Mac bash script

---

## Why It Works Locally But Not in Production

### Most Likely Cause: Missing Dropbox Refresh Token

**Locally:** 
- You have `dropbox_tokens.json` file with the refresh token
- File is read by `DropboxOAuthService`

**Production (Cloud Run):**
- No local file system
- Needs refresh token in **Google Cloud Secret Manager**
- Secret must be named: `cfm-dropbox-refresh-token`
- Cloud Run service account needs permission to read the secret

---

## Quick Fix Steps

### Step 1: Redeploy with Logging
```bash
gcloud run deploy cfm-api --source . --allow-unauthenticated
```

### Step 2: View Logs to See the Error
**Option A - PowerShell (Recommended for Windows):**
```powershell
.\view-production-logs.ps1
# Select option 3 (Upload errors)
```

**Option B - Direct Command (PowerShell):**
```powershell
gcloud logging read --filter 'textPayload=~"upload"' --limit 50 --format "table(timestamp,severity,textPayload)"
```

### Step 3: Fix Based on Error Message

#### If you see: "Dropbox not configured" or "No valid access token"

**Solution:** Set up Dropbox OAuth in production

1. Go to: `https://YOUR_PRODUCTION_URL/dropbox/setup`
2. Click "Authorize Dropbox"
3. Complete the OAuth flow
4. Copy the refresh token shown
5. Save it to Secret Manager:
   ```bash
   echo "YOUR_REFRESH_TOKEN" | gcloud secrets create cfm-dropbox-refresh-token --data-file=-
   ```
6. Grant Cloud Run access:
   ```bash
   # Get service account
   SERVICE_ACCOUNT=$(gcloud run services describe cfm-api --format="value(spec.template.spec.serviceAccountName)")
   
   # Grant access
   gcloud secrets add-iam-policy-binding cfm-dropbox-refresh-token \
     --member="serviceAccount:${SERVICE_ACCOUNT}" \
     --role="roles/secretmanager.secretAccessor"
   ```

#### If you see: "Timeout" or request takes too long

**Solution:** Increase Cloud Run timeout
```bash
gcloud run services update cfm-api --timeout=300
```

#### If you see: File size or memory errors

**Solution:** Increase memory
```bash
gcloud run services update cfm-api --memory=512Mi
```

---

## Expected Log Output

### Success Case:
```
INFO: Requestor upload: Received 1 file(s) for request 123
INFO: Requestor upload: Attempting to upload 1 valid file(s) to Dropbox for request 123
INFO: Uploading file 123_photo.jpg to Dropbox at /MaintenanceImages/123_photo.jpg
INFO: File uploaded successfully. Dropbox ID: id:xxxxx
INFO: Created shareable link: https://www.dropbox.com/...
```

### Failure Case (Missing Token):
```
ERROR: Failed to get valid tokens via OAuth service
ERROR: Dropbox not configured. Please run OAuth setup...
ERROR: Requestor upload: Image upload failed for request 123
```

---

## Verification Checklist

After fixing:

- [ ] Redeploy with logging changes
- [ ] Try uploading an image in production
- [ ] Check logs using the PowerShell script
- [ ] Verify Dropbox secret exists: `gcloud secrets describe cfm-dropbox-refresh-token`
- [ ] Verify Cloud Run has secret access
- [ ] Test image upload again
- [ ] Verify link appears in Google Sheet

---

## Need More Help?

The logs will now show you EXACTLY what's failing. Run the PowerShell script and share the error message you see!

Common scenarios:
1. **"Secret not found"** → Need to create the Dropbox refresh token secret
2. **"Permission denied"** → Cloud Run needs access to the secret
3. **"Access token invalid"** → Refresh token expired, re-run OAuth
4. **"Timeout"** → Increase Cloud Run timeout setting
5. **"File too large"** → File exceeds 5MB limit (already validated in code)

---

## Files Created
- ✅ `Program.cs` - Updated with logging
- ✅ `check-production-config.md` - Full diagnostic guide
- ✅ `view-production-logs.ps1` - PowerShell log viewer
- ✅ `view-production-logs.bat` - Windows batch log viewer  
- ✅ `view-production-logs.sh` - Linux/Mac log viewer
- ✅ This summary file

---

## Next Action

**Run this now:**
```powershell
.\view-production-logs.ps1
```

Then select option 3 (Upload errors) after trying an image upload in production.

The logs will tell you exactly what's wrong! 🎯
