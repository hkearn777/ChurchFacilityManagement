# Quick Reference: gcloud Logging Commands

## View All Recent Logs
```bash
gcloud logging read "resource.type=cloud_run_revision AND resource.labels.service_name=cfm-api" --limit=50
```

## Filter for Dropbox-Related Logs
```bash
gcloud logging read 'resource.type=cloud_run_revision AND textPayload=~"Dropbox"' --limit=30
```

## Filter for Upload Errors
```bash
gcloud logging read 'resource.type=cloud_run_revision AND textPayload=~"upload"' --limit=30
```

## Stream Live Logs (Real-Time)
```bash
gcloud logging tail "resource.type=cloud_run_revision AND resource.labels.service_name=cfm-api"
```

## Filter by Severity (Errors Only)
```bash
gcloud logging read "resource.type=cloud_run_revision AND severity>=ERROR" --limit=30
```

## Filter by Time Range (Last Hour)
```bash
gcloud logging read "resource.type=cloud_run_revision AND timestamp>=\"$(date -u -d '1 hour ago' '+%Y-%m-%dT%H:%M:%SZ')\"" --limit=50
```

## Better Formatted Output
```bash
gcloud logging read "resource.type=cloud_run_revision" --limit=20 --format="table(timestamp,severity,textPayload)"
```

## Save Logs to File
```bash
gcloud logging read "resource.type=cloud_run_revision" --limit=100 > production-logs.txt
```

---

## Important Syntax Notes

### ⚠️ Regular Expression Syntax
- Use **double quotes** inside single-quoted filters: `'textPayload=~"pattern"'`
- Or escape double quotes in double-quoted filters: `"textPayload=~\"pattern\""`

### ✅ Correct
```bash
gcloud logging read 'resource.type=cloud_run_revision AND textPayload=~"Dropbox"' --limit=20
```

### ❌ Incorrect (will error)
```bash
gcloud logging read "resource.type=cloud_run_revision AND textPayload=~'Dropbox'" --limit=20
```

---

## PowerShell-Specific Commands

When running from PowerShell, use **backtick (`)** to escape inner double quotes:

```powershell
# View Dropbox-related logs
gcloud logging read "resource.type=cloud_run_revision AND textPayload=~`"Dropbox`"" --limit=30

# View upload errors  
gcloud logging read "resource.type=cloud_run_revision AND textPayload=~`"upload`"" --limit=30

# View all errors
gcloud logging read "resource.type=cloud_run_revision AND severity>=ERROR" --limit=30
```

### Alternative: Use --filter parameter
```powershell
gcloud logging read --filter='textPayload=~"Dropbox"' --limit=30
```

---

## Common Filters

### By Request ID
```bash
gcloud logging read 'resource.type=cloud_run_revision AND textPayload=~"request 123"' --limit=10
```

### By Specific Error Message
```bash
gcloud logging read 'resource.type=cloud_run_revision AND textPayload=~"Image upload failed"' --limit=20
```

### Combine Multiple Conditions
```bash
gcloud logging read 'resource.type=cloud_run_revision AND severity>=ERROR AND textPayload=~"Dropbox"' --limit=20
```

---

## Easiest Option: Use the Scripts!

Instead of remembering these commands, just run:

**PowerShell:**
```powershell
.\view-production-logs.ps1
```

**Windows Batch:**
```cmd
view-production-logs.bat
```

**Linux/Mac:**
```bash
./view-production-logs.sh
```

These scripts now have the **correct syntax** and provide an easy menu interface! 🎯
