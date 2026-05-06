# Deployment Steps for Approver Email Response Feature

## Pre-Deployment Checklist
- [x] Code changes completed
- [x] Build successful
- [x] New config setting added: `Email:ManagerEmail`

## Deployment to Cloud Run

### Step 1: Verify Configuration
Check that your Cloud Run environment has the following environment variables set:
- `Email__ManagerEmail` = `facilities@fbcmandeville.org`

If not already set, you'll need to add it during deployment.

### Step 2: Deploy via PowerShell Script
You likely have a deployment script. If using the `Deploy-CFM.ps1` script in your repo:

```powershell
# From your project directory
.\Deploy-CFM.ps1
```

### Step 3: Add ManagerEmail Environment Variable (if needed)
If the deployment script doesn't include `Email:ManagerEmail`, you can add it via Google Cloud Console:

1. Go to Cloud Run console
2. Select your service
3. Click "EDIT & DEPLOY NEW REVISION"
4. Under "Variables & Secrets" tab
5. Add environment variable:
   - Name: `Email__ManagerEmail`
   - Value: `facilities@fbcmandeville.org`
6. Deploy

Or via gcloud CLI:
```bash
gcloud run services update church-facility-management \
  --update-env-vars Email__ManagerEmail=facilities@fbcmandeville.org \
  --region us-central1
```

### Step 4: Test in Production
1. Go to your production URL `/approver`
2. Find a request with "Need Approval" status
3. Click "✅ Approve" (or "❌ Not Approve" or "⏸️ Defer")
4. Check the Google Sheets - Notes column should update
5. Check `facilities@fbcmandeville.org` inbox for the email

### Step 5: Verify Logs
Check Cloud Run logs for successful email send:
```
Approve endpoint called for request ID: X
Manager email from config: facilities@fbcmandeville.org
Sending approval notification email to manager: facilities@fbcmandeville.org
Approver response notification email sent for request #X to facilities@fbcmandeville.org
Email send result: True
```

## Troubleshooting

### Email Not Received
1. Check Cloud Run logs for errors
2. Verify `Email__FromPassword` environment variable is set
3. Check spam folder
4. Verify `Email__ManagerEmail` is correctly set

### Notes Not Updating
1. Check Google Sheets permissions
2. Verify `GOOGLE_CREDENTIALS_JSON_BASE64` environment variable is set
3. Check Cloud Run logs for Google Sheets API errors

## Rollback Plan
If issues occur, you can quickly rollback in Cloud Run:
1. Go to Cloud Run console
2. Select "REVISIONS" tab
3. Click on previous working revision
4. Click "MANAGE TRAFFIC"
5. Route 100% traffic to previous revision
