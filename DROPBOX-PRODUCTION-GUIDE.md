# 🔐 Dropbox Production vs Development Mode

## Summary: You Probably Don't Need Production!

For most church applications, **Development Mode** is sufficient and much easier.

---

## Option 1: Development Mode ⭐ RECOMMENDED

### Pros:
✅ **Up to 500 users** (plenty for a church)  
✅ **No approval process** - instant access  
✅ **No privacy policy required**  
✅ **Full functionality**  
✅ **Easier to manage**  

### Cons:
❌ Must manually add each user's email

### How to Use Development Mode:

1. **Go to Dropbox App Console:**
   - Visit: https://www.dropbox.com/developers/apps
   - Sign in and select your app

2. **Enable Your App** (if disabled):
   - Check if there's a "Status: Disabled" warning
   - Click **"Enable"** or **"Activate"** button

3. **Add Development Users:**
   - Go to **Settings** tab
   - Scroll to **"Development users"** section
   - Click **"Enable additional users"**
   - Add email addresses of:
     - Maintenance manager
     - Staff who submit requests
     - Approvers
     - Anyone who needs to upload photos
   - Each user can now authorize the app!

4. **Verify Token Settings:**
   - In **Settings** tab, find **"OAuth 2"** section
   - **Access token expiration** must be: **"Short-lived tokens with refresh tokens"**
   - If not, change it and re-run OAuth setup

5. **Set Permissions:**
   - Go to **Permissions** tab
   - Enable these scopes:
     - ✅ `files.content.write`
     - ✅ `files.content.read`
     - ✅ `sharing.write`
   - Click **"Submit"** at bottom of page

6. **Complete OAuth Setup:**
   - Run your app locally
   - Go to: `https://localhost:7139/dropbox/setup`
   - Click **"Authorize Dropbox"**
   - Complete the authorization

**Done!** Your app will now work for all development users.

---

## Option 2: Production Mode (If You Really Need It)

### When You Need Production:
- More than 500 users
- Public-facing application
- Don't want to manually manage user list

### Requirements:

#### 1. **Privacy Policy** ✅ (Already Created!)
Your app now serves a privacy policy at:
- **Local:** `https://localhost:7139/privacy`
- **Production:** `https://your-deployed-url/privacy`

To customize:
1. Edit the `privacy-policy.html` file
2. Update contact information (email, phone)
3. Update organization name

#### 2. **Terms of Service** (Optional but Recommended)
Create a terms of service document similar to privacy policy.

#### 3. **Submit for Approval**

1. **Go to Dropbox App Console:**
   - https://www.dropbox.com/developers/apps
   - Select your app

2. **Click "Apply for Production"**

3. **Fill Out Application:**
   - **App Name:** Church Facility Management
   - **App Description:** 
     ```
     Internal maintenance request management system for church facilities. 
     Allows staff to submit maintenance requests with photos, track work 
     progress, and manage facility upkeep.
     ```
   - **Privacy Policy URL:** `https://your-deployed-url/privacy`
   - **How users interact with your app:**
     ```
     Users upload photos of facility issues as part of maintenance requests. 
     Photos are stored in Dropbox and shared links are saved to Google Sheets 
     for tracking purposes.
     ```
   - **Why your app needs Dropbox:**
     ```
     To store maintenance issue photos and documentation. Users upload images 
     when submitting facility maintenance requests.
     ```

4. **Wait for Approval:**
   - Usually takes 1-2 weeks
   - Dropbox may ask clarifying questions
   - They'll review your privacy policy

---

## Recommended Approach

### For Your Church App:

1. ✅ **Start with Development Mode**
2. ✅ Add 5-10 key staff members as development users
3. ✅ Test thoroughly
4. ❓ Only apply for production if you hit the 500 user limit (unlikely!)

---

## Current Status Checklist

Use this to verify your setup:

### Dropbox App Setup:
- [ ] App is **Enabled** (not disabled)
- [ ] **Access token expiration** = "Short-lived with refresh tokens"
- [ ] **Permissions** enabled: `files.content.write`, `files.content.read`, `sharing.write`
- [ ] **Redirect URI** added: `https://localhost:7139/dropbox/callback` (and production URL if deployed)

### Development Mode (Recommended):
- [ ] Development users added (Settings → Development users)
- [ ] OAuth setup completed (`/dropbox/setup`)
- [ ] Test photo upload works

### Production Mode (Only if needed):
- [ ] Privacy policy deployed at `/privacy`
- [ ] Production approval submitted
- [ ] Waiting for Dropbox approval

---

## Troubleshooting

### "This app is currently disabled"
**Solution:** Enable the app in Dropbox App Console (Settings tab)

### "Error: invalid_grant" or 400 Bad Request
**Solutions:**
1. Check token settings: must be "Short-lived with refresh tokens"
2. Verify App Key and App Secret are correct
3. Re-run OAuth setup: `https://localhost:7139/dropbox/setup`

### "User not authorized"
**Solution:** Add user's email to Development users list (if in dev mode)

### Photos won't upload
**Solutions:**
1. Check logs for specific error
2. Verify OAuth tokens are valid
3. Check Dropbox permissions are enabled
4. Re-authorize: `https://localhost:7139/dropbox/setup`

---

## Privacy Policy Customization

Before using in production, update `privacy-policy.html`:

```html
<!-- Update these sections -->
<strong>Email:</strong> facilities@yourchurch.org  <!-- Change this -->
<strong>Phone:</strong> (XXX) XXX-XXXX             <!-- Change this -->
© 2025 Church Facility Management                   <!-- Update year/name -->
```

---

## Questions?

**Should I use Development or Production?**
→ Development mode for 99% of church applications

**How many users can I have in Development?**
→ Up to 500 users

**Can I switch from Development to Production later?**
→ Yes! Start with Development and upgrade if needed

**Is the privacy policy required for Development mode?**
→ No, only for Production

**Do my users need Dropbox accounts?**
→ Yes, they need a Dropbox account to authorize (free accounts work)
