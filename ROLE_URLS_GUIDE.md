# Church Facility Management - Role URL Reference Guide

## URL Distribution Guide

This guide shows which URL to provide to each role in the maintenance request system.

---

## 🛠️ **Requestor** (General Church Members)

**URL:** `https://your-app-url.com/requestor/new`

**Who:** Anyone in the church who notices a maintenance issue

**What they can do:**
- Submit maintenance requests
- Enter description of the issue
- Provide their name
- Select building location
- Upload photos (optional, up to 3 images)

**What gets auto-filled:**
- Request ID (next available number)
- Report Date (today's date)
- Request Method (always "Inspection")
- Status (always "Submitted")

---

## 📝 **Proxy** (Office/Secretary)

**URL:** `https://your-app-url.com/proxy/new`

**Who:** Office staff or secretary who enters requests on behalf of others

**What they can do:**
- Submit requests received via email, phone, or paper
- Enter description of the issue
- Enter the requestor's name (person who made the request)
- Select how the request was received (Email, Verbal, Proxy, Inspection)
- Select building location
- Upload photos (optional, up to 3 images)

**What gets auto-filled:**
- Request ID (next available number)
- Report Date (today's date)
- Status (always "Submitted")

---

## ✅ **Approver**

**URL:** `https://your-app-url.com/approver`

**Who:** The person who approves maintenance requests

**What they can do:**
- View all requests with Status = "Need Approval"
- See request details (ID, Date, Description, Requestor, Building, Priority)
- Approve requests (sets Status to "Approved")
- Reject requests (sets Status to "Not Approved")

**Important:**
- Only sees requests that the Manager has marked as "Need Approval"
- Simple dashboard with approve/reject buttons

---

## 👔 **Manager**

**URL:** `https://your-app-url.com/` (main page)

**Who:** The facility manager who oversees all maintenance work

**What they can do:**
- View all maintenance requests
- Create new requests with full details
- Edit any request
- Delete requests
- Assign requests to workers
- Set priorities
- Add corrective actions
- Set due dates
- Change status to "Need Approval" to send to Approver
- View reports and analytics
- Filter and search requests

**Full Access:** Manager has complete control over all aspects of the system

---

## Workflow Example

1. **Requestor** submits a request via `/requestor/new`
   - Status: "Submitted"

2. **Manager** reviews request at `/`
   - Adds priority, assigns worker, sets due date
   - Changes Status to "Need Approval"

3. **Approver** reviews at `/approver`
   - Clicks "✅ Approve" or "❌ Not Approve"
   - Status changes to "Approved" or "Not Approved"

4. **Manager** schedules work or follows up
   - If Approved: schedules the work
   - If Not Approved: contacts requestor with explanation

---

## Quick Setup Instructions

### For Deployment:

Replace `your-app-url.com` with your actual Cloud Run URL:

**Example:**
- Requestor: `https://church-facility-management-902794624514.us-central1.run.app/requestor/new`
- Proxy: `https://church-facility-management-902794624514.us-central1.run.app/proxy/new`
- Approver: `https://church-facility-management-902794624514.us-central1.run.app/approver`
- Manager: `https://church-facility-management-902794624514.us-central1.run.app/`

### Creating Shortcuts:

You can create browser bookmarks or desktop shortcuts for each role to make access easier.

---

## Status Values Reference

- **Submitted** - New request from Requestor/Proxy
- **Need Approval** - Manager marked for approval
- **Approved** - Approver approved the request
- **Not Approved** - Approver rejected the request
- **In Progress** - Work has started
- **Completed** - Work is finished
- **AWP** - Awaiting Parts
- **AWM** - Awaiting Maintenance

*(All status values are managed in the Dropdowns sheet)*

---

**Last Updated:** March 2026
