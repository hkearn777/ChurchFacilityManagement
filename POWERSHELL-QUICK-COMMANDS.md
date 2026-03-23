# PowerShell Quick Commands for Production Logs

## ✅ CORRECT SYNTAX FOR POWERSHELL

**Important:** In PowerShell, use SPACE after `--filter` and `--limit` (not `=`)

```powershell
# View upload-related logs
gcloud logging read --filter 'textPayload=~"upload"' --limit 50 --format "table(timestamp,severity,textPayload)"

# View Dropbox-related logs
gcloud logging read --filter 'textPayload=~"Dropbox"' --limit 50 --format "table(timestamp,severity,textPayload)"

# View all errors
gcloud logging read --filter 'severity>=ERROR' --limit 50 --format "table(timestamp,severity,textPayload)"

# View ALL recent logs (simplest - no filter needed!)
gcloud logging read --limit 50 --format "table(timestamp,severity,textPayload)"

# Stream live logs
gcloud logging tail --format "table(timestamp,severity,textPayload)"
```

---

## ❌ WHAT DOESN'T WORK IN POWERSHELL

```powershell
# DON'T USE = with --filter or --limit
gcloud logging read --filter='...' --limit=50   # ❌ FAILS IN POWERSHELL
```

---

## 🎯 RECOMMENDED: Just Use the Script!

Instead of typing these commands, run:

```powershell
.\view-production-logs.ps1
```

Then select option 2 or 3 from the menu. The script handles all the quoting for you! ✅

---

## What You Need Right Now

**Copy and paste this in PowerShell:**

```powershell
gcloud logging read --filter 'textPayload=~"upload"' --limit 50 --format "table(timestamp,severity,textPayload)"
```

This will show you any upload-related logs from production.

---

## If You See "No entries found"

That means either:
1. No one has tried uploading an image yet in production
2. The logs haven't synced yet (wait 1-2 minutes)
3. Your service might not be named `cfm-api`

**Try viewing ALL logs to see what's there:**
```powershell
gcloud logging read --limit 50 --format "table(timestamp,severity,textPayload)"
```

---

## Alternative: Use Cloud Console (Easiest!)

Go to: https://console.cloud.google.com/logs

Then use the query builder - no quote escaping needed!
