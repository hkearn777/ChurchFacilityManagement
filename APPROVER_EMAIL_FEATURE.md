# Approver Email Response Feature

## Overview
When an Approver responds to a maintenance request (Approve, Deny, or Defer), the system now:
1. Sends an email notification to the Manager
2. Logs the response in the Google Sheets Notes column (column P)

## Implementation Details

### Email Configuration
- **From Email**: `ChurchFacilityManagement@fbcmandeville.org` (service account)
- **To Email**: `facilities@fbcmandeville.org` (Manager's email from `Email:ManagerEmail` config)
- **App Password**: Configured in production via environment variable `Email__FromPassword`

### Configuration Settings Required

Add to your production environment (Cloud Run):
```json
"Email": {
  "SmtpHost": "smtp.gmail.com",
  "SmtpPort": "587",
  "FromEmail": "ChurchFacilityManagement@fbcmandeville.org",
  "FromPassword": "[set via environment variable]",
  "ReplyToEmail": "facilities@fbcmandeville.org",
  "ManagerEmail": "facilities@fbcmandeville.org",
  "ApproverEmail": "mike@fbcmandeville.org",
  "ApproverPageUrl": "https://church-facility-management-902794624514.us-central1.run.app/approver"
}
```

**New Setting Added:**
- `Email:ManagerEmail` - The Manager's email address to receive approver response notifications

### Changes Made

#### 1. appsettings.json / appsettings.Development.json
- Added new configuration setting: `Email:ManagerEmail` = `facilities@fbcmandeville.org`
- This clarifies the purpose vs. `Email:ReplyToEmail` which is used in the Manager→Approver email flow

#### 2. EmailService.cs
- Added `SendApproverResponseNotificationAsync()` method
  - Sends HTML email to Manager with color-coded status (Green=Approved, Red=Not Approved, Yellow=Deferred)
  - Includes request details: ID, Description, Requested By, Approver Action
  - Uses same SMTP settings as existing approval notification emails
  - Password comes from environment variable in production (same as Manager→Approver emails)

#### 3. Program.cs - Approver Endpoints
All three endpoints updated:
- `/approver/approve/{id}` - Sets status to "Approved"
- `/approver/reject/{id}` - Sets status to "Not Approved"  
- `/approver/defer/{id}` - Sets status to "Deferred" with notes

Each endpoint now:
1. Updates the request status
2. Appends log to Notes field: `Approver email response on <yyyy-MM-dd HH:mm:ss>`
3. Retrieves Manager email from `Email:ManagerEmail` configuration
4. Sends email notification to Manager
5. Logs the entire process for debugging

#### 4. GoogleSheetsService.cs
- Added `GetManagerEmailAsync()` method (available but not currently used)
  - Can read Manager email from Roles sheet (Role="Manager", column C) if needed in future
  - Currently using config-based email for simplicity and consistency

### Notes Field Format

**For Approve/Reject:**
```
<existing notes>
Approver email response on 2025-01-15 14:30:22
```

**For Defer:**
```
<existing notes>
Deferred 2025-01-15 - <defer reason from approver>
Approver email response on 2025-01-15 14:30:22
```

### Testing in Production
1. Deploy to Cloud Run (password already configured via environment variable)
2. Navigate to `/approver` page
3. Click Approve, Not Approve, or Defer on any request with "Need Approval" status
4. Verify:
   - ✅ Google Sheets Notes column (P) is updated with timestamp
   - ✅ Email is received at `facilities@fbcmandeville.org`
   - ✅ Email contains correct action and request details
   - ✅ Cloud Run logs show successful email send

### Local Testing Note
Local testing will show the Notes update working, but emails will NOT send without the app password configured. This is expected behavior. Test email functionality in production where the `Email__FromPassword` environment variable is already configured.

### Email Sample

**Subject:** `Approver Response: Request #198 - Approved`

**Body:** Formatted HTML email with:
- Color-coded header based on action (Green/Red/Yellow)
- Request details (ID, Description, Requested By)
- Clear indication of approver's action
- Professional church branding

### Deployment Checklist
- [x] Code changes committed
- [ ] Build successful
- [ ] Deploy to Cloud Run
- [ ] Verify `Email:ManagerEmail` is set in production config
- [ ] Test with real approval action in production
- [ ] Confirm email received and Notes logged
