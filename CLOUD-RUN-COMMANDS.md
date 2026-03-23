# Cloud Run Commands Reference

## Redeploy Service (Recommended)
This creates a new revision and picks up updated secrets:

```powershell
gcloud run deploy church-facility-management --region=us-central1
```

## Update Environment Variables
If you need to add/update environment variables:

```powershell
# Add or update a variable
gcloud run services update church-facility-management `
  --region=us-central1 `
  --update-env-vars KEY=VALUE

# Remove a variable
gcloud run services update church-facility-management `
  --region=us-central1 `
  --remove-env-vars KEY
```

## View Service Details

```powershell
# Get service info
gcloud run services describe church-facility-management --region=us-central1

# List all revisions
gcloud run revisions list --service=church-facility-management --region=us-central1

# Get latest revision
gcloud run revisions describe $(gcloud run revisions list --service=church-facility-management --region=us-central1 --limit=1 --format="value(name)") --region=us-central1
```

## View Logs

```powershell
# Recent logs (all)
gcloud logging read 'resource.type="cloud_run_revision" AND resource.labels.service_name="church-facility-management"' --limit=50 --format="table(timestamp,severity,textPayload)"

# Dropbox-related logs
gcloud logging read --filter 'textPayload=~"Dropbox"' --limit 20 --format "table(timestamp,severity,textPayload)"

# Error logs only
gcloud logging read 'resource.type="cloud_run_revision" AND resource.labels.service_name="church-facility-management" AND severity>=ERROR' --limit=20
```

## Common Issues

### Issue: "No configuration change requested"
**Error:**
```
ERROR: (gcloud.run.services.update) No configuration change requested.
```

**Solution:** Use `gcloud run deploy` instead of `gcloud run services update` when you want to force a new revision:
```powershell
gcloud run deploy church-facility-management --region=us-central1
```

### Issue: Service not picking up new secret
**Solution:** Deploy creates a new revision which loads the latest secret versions:
```powershell
gcloud run deploy church-facility-management --region=us-central1
```

### Issue: Need to force immediate revision switch
**Solution:** Deploy automatically routes 100% traffic to the new revision:
```powershell
gcloud run deploy church-facility-management --region=us-central1
```

## Production URL
https://church-facility-management-902794624514.us-central1.run.app
