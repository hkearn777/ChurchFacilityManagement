# 🛡️ Image Upload Error Handling

## Overview

The application now handles image upload errors gracefully, ensuring users are informed and managers are notified when issues occur.

## What Happens When Image Upload Fails?

### 1. ✅ Request Data is Saved
- The maintenance request is **always created** in Google Sheets
- User's work is **never lost**, even if images fail to upload
- Request ID is generated and displayed to the user

### 2. 👤 User Sees Clear Error Message
Users get a clear, friendly error message showing:
- ⚠️ Warning icon and yellow status
- Specific error details
- Confirmation that request data was saved
- Option to submit another request

**Example user message:**
```
⚠️ Request created, but image upload failed: expired_access_token

Don't worry - your request details have been saved.
You can contact the facilities manager to add images later.

The facility manager has been notified of this issue.
```

### 3. 📧 Manager Gets Email Notification
An automated email is sent to the Manager (from Roles tab) with:
- Request ID and details
- Exact error message
- Confirmation that request data was saved
- Instructions for follow-up

**Email includes:**
- Red header with ⚠️ warning
- Full error details
- Action items for troubleshooting

### 4. 📊 Error Logged in Google Sheets
The Attachments column shows:
```
IMAGE UPLOAD FAILED: expired_access_token/
```

This allows you to:
- See which requests had upload issues
- Track error patterns
- Follow up with users

### 5. 🔍 Detailed Logging
The application logs (console/Output window) show:
- Detailed error stack trace
- Upload attempt details
- Email notification status

## Benefits

| Benefit | Description |
|---------|-------------|
| **No Data Loss** | Request is always saved, regardless of image upload status |
| **User Transparency** | Users know exactly what happened |
| **Manager Awareness** | You're automatically notified of issues |
| **Easy Tracking** | Errors visible in Google Sheets |
| **Better Debugging** | Detailed logs for troubleshooting |

## Error Handling Flow

```
User submits request with images
         ↓
Create request in Google Sheets ✅
         ↓
Try to upload images
         ↓
    ┌────┴────┐
    ↓         ↓
SUCCESS    FAILURE
    ↓         ↓
Store      Store error in
links      Attachments column
    ↓         ↓
Show       Send email to Manager
success        ↓
message    Show error to user
```

## Examples of Errors Handled

### 1. Dropbox Token Expired
```
Error: expired_access_token/
Action: User notified, manager emailed, OAuth refresh needed
```

### 2. Network Issue
```
Error: Unable to connect to Dropbox
Action: User notified, manager emailed, retry later
```

### 3. File Too Large
```
Error: No images were uploaded successfully
Action: User sees error, manager notified
```

### 4. Permission Denied
```
Error: Access denied to Dropbox folder
Action: User notified, manager emailed, check permissions
```

## Manager Email Example

**Subject:** ⚠️ Image Upload Failed for Request #105

**Body:**
```
⚠️ Image Upload Error

An error occurred while uploading images for a maintenance request.

Request ID: #105
Description: Broken window in sanctuary
Requested By: John Smith

Error Details:
expired_access_token/

✅ Request Created Successfully
The maintenance request was saved to Google Sheets, but the 
images could not be uploaded to Dropbox.

Action Required:
• User has been notified of the upload failure
• Request data is safe in Google Sheets
• User can re-upload images by editing the request
• Check Dropbox configuration if issue persists
```

## Testing the Error Handling

### Test Scenario 1: Expired Token
1. Stop your Dropbox OAuth service temporarily
2. Create a request with an image
3. Observe: Error message shown to user, email sent to manager

### Test Scenario 2: Network Issue
1. Disable internet connection temporarily
2. Create a request with an image
3. Observe: User sees error, request data saved

### Test Scenario 3: Success Case
1. Ensure Dropbox OAuth is working
2. Create a request with an image
3. Observe: Success message, images uploaded

## Configuration

### Manager Email
The manager's email is retrieved from the **Roles** tab in Google Sheets:

| Role | Person | Contact |
|------|--------|---------|
| Manager | Your Name | manager@church.org |

Make sure the Manager role has a valid email in the Contact column.

### Email Settings
Check `appsettings.json` for email configuration:
```json
"Email": {
  "SmtpHost": "smtp.gmail.com",
  "SmtpPort": "587",
  "FromEmail": "ChurchFacilityManagement@fbcmandeville.org",
  "FromPassword": "",
  "ReplyToEmail": "facilities@fbcmandeville.org"
}
```

## Troubleshooting

### Manager Not Receiving Emails
1. Check Roles tab has "Manager" entry with valid email
2. Verify email configuration in `appsettings.json`
3. Check spam/junk folder
4. Look for email errors in application logs

### Errors Not Showing in Google Sheets
1. Verify UpdateRequestAsync is working
2. Check Google Sheets API permissions
3. Look for errors in application logs

### Users Not Seeing Error Messages
1. Check browser console for JavaScript errors
2. Verify HTML is rendering correctly
3. Test with different browsers

## Future Enhancements

Potential improvements:
- [ ] Retry logic for transient errors
- [ ] Queue failed uploads for later retry
- [ ] Allow users to re-upload images via edit form
- [ ] Error dashboard showing all failed uploads
- [ ] Automatic OAuth token refresh before expiration

## Related Documentation

- **OAuth-Implementation-Guide.md** - OAuth setup
- **QUICK-FIX-DROPBOX.md** - Dropbox configuration
- **README.md** - General documentation

---

**This error handling ensures a great user experience even when things go wrong!** ✅
