# 🏢 Church Facility Management

A web-based application for managing Church Facilities maintenance requests. Built with ASP.NET Core and integrated with Google Sheets for data storage.

## Features

- **CRUD Operations**: Add, View, Edit, and Delete maintenance requests
- **Filtering & Search**: Filter by status, priority, building, or search text
- **Roles Management**: Manager, Approver, Requestor, and Proxy roles
- **Tracking Reports**: View overdue, started, and not-started requests
- **Mobile Responsive**: Works on desktop, tablet, and mobile devices

## Spreadsheet Structure

### Spreadsheet Name: `FBC Maint Tasks`

The spreadsheet contains three tabs:

#### 1. **Tasks** (Active Requests)
| Column | Field | Type | Description |
|--------|-------|------|-------------|
| A | ID | Integer | Sequential identifier |
| B | Report Date | Date | When request was created |
| C | Description | Text | Multi-line details of issue |
| D | Requested By | Text | Name of requestor |
| E | Request Method | Text | Inspection, Verbal, Email, Proxy |
| F | Building | Text | Building location |
| G | Priority | Text | 1=High, 2=Normal, 3=Low |
| H | Status | Text | In Progress, Completed, AWP, AWM, etc. |
| I | Assigned | Text | Person assigned to work |
| J | Trade | Text | Electrical, Plumbing, General |
| K | Corrective Action | Text | Multi-line action details |
| L | Due Date | Date | Completion deadline |
| M | Start Date | Date | When work started |
| N | Completed | Date | When work finished |
| O | Attachments | URL | Google Drive link |

#### 2. **Completed Tasks**
Same structure as Tasks tab, for historical record.

#### 3. **Roles**
| Column | Field | Description |
|--------|-------|-------------|
| A | Role | Manager, Approver, Requestor, Proxy |
| B | Person | Name of person |
| C | Contact | Email or SMS contact |

#### 4. **Dropdowns**
The dropdown values for the Tasks tab are stored here for easy management:
| Column | Dropdown Values |
|--------|-------|
| A | Buildings |
| B | Priorities |
| C | Status |
| D | Request Method |
These values are read by the application to populate form dropdowns.
Rows 2 and below contain the actual options (e.g., Building A, Building B, etc.)

## Prerequisites

- **.NET 10 SDK** or later
- **Google Cloud Platform** account with Sheets API enabled
- **Google Sheets API credentials** (service account JSON key)

## Getting Started

### 1. Set Up Google Sheets API

1. Go to [Google Cloud Console](https://console.cloud.google.com)
2. Create a new project or select existing one
3. Enable the **Google Sheets API**
4. Create a **service account** and download JSON key file
5. Share your Google Sheets spreadsheet with the service account email (found in JSON file)

### 2. Configure the Application

Edit `appsettings.json` and update:
```json
{
  "GoogleSheets": {
    "CredentialsPath": "credentials.json",
    "SpreadsheetId": "YOUR_SPREADSHEET_ID_HERE",
    "ApplicationName": "Church Facility Management"
  }
}
```

**Finding your Spreadsheet ID:**
From your Google Sheets URL: `https://docs.google.com/spreadsheets/d/SPREADSHEET_ID_HERE/edit`

### 3. Add Credentials File

Place your downloaded `credentials.json` file in the project root directory.

**⚠️ Important**: Never commit `credentials.json` to source control (already in `.gitignore`).

### 4. Run the Application

```bash
dotnet restore
dotnet run
```

The app will be available at: `http://localhost:5118`

## Deployment to Google Cloud Run

### One-Time Setup

1. Install [Google Cloud SDK](https://cloud.google.com/sdk/docs/install)
2. Authenticate: `gcloud auth login`
3. Set your project: `gcloud config set project YOUR_PROJECT_ID`

### Deploy

Run the deployment script:

```powershell
.\Deploy-CFM.ps1
```

The script will:
1. Verify your `credentials.json` file exists
2. Prompt you to set up secrets in Google Cloud Secret Manager (first deployment only):
   - **Dropbox Access Token**
   - **Gmail App Password**
3. Deploy the application to Cloud Run
4. Display your live application URL

**On subsequent deployments**, the script will automatically retrieve secrets from Secret Manager.

### Updating Secrets

If you need to update a secret (e.g., new Dropbox token):

```bash
# Update Dropbox token
echo "YOUR_NEW_TOKEN" | gcloud secrets versions add cfm-dropbox-token --data-file=-

# Update email password
echo "YOUR_NEW_PASSWORD" | gcloud secrets versions add cfm-email-password --data-file=-
```

## Troubleshooting

### "expired_access_token" Error

If you see this error when uploading images to Dropbox:
1. Your Dropbox access token has expired
2. Generate a new token from the [Dropbox App Console](https://www.dropbox.com/developers/apps)
3. Update the secret: `echo "NEW_TOKEN" | gcloud secrets versions add cfm-dropbox-token --data-file=-`
4. Redeploy: `.\Deploy-CFM.ps1`

For local development, update the environment variable:
- Windows: Run `.\Setup-LocalEnvironment.ps1` as Administrator
- Or manually: `$env:DROPBOX_ACCESS_TOKEN = "your_new_token"`

### Images Not Uploading Locally

Ensure the `DROPBOX_ACCESS_TOKEN` environment variable is set:
```powershell
# Check if set
echo $env:DROPBOX_ACCESS_TOKEN

# Set it (current session only)
$env:DROPBOX_ACCESS_TOKEN = "your_token_here"

# Or run the setup script
.\Setup-LocalEnvironment.ps1
```

### Email Notifications Not Working

Ensure the `Email__FromPassword` environment variable is set (note the double underscore):
```powershell
# Check if set
echo $env:Email__FromPassword

# Set it (current session only)
$env:Email__FromPassword = "your_app_password"

# Or run the setup script
.\Setup-LocalEnvironment.ps1
```

## Usage

### Creating Requests
1. Click **"+ New Request"** on the home page
2. Fill in required fields (Description, Requested By, Method, Building, Priority, Status)
3. Optionally add: Assigned person, Trade, Due Date, Attachments
4. Click **"Create Request"**

### Viewing & Editing
1. Click **"View"** on any request in the list
2. Click **"Edit"** to modify request details
3. Update fields including Start Date and Completed Date
4. Click **"Update Request"**

### Filtering Requests
Use the filter bar to:
- Filter by **Status** (In Progress, Completed, AWP, AWM)
- Filter by **Priority** (High, Normal, Low)
- Filter by **Building**
- **Search** text in Description, Requested By, or Assigned fields

### Reports
Click **"📊 Reports"** to view:
- **Overdue Requests**: Past due date and not completed
- **Started (Not Completed)**: Work started but not finished
- **Not Started**: Not yet begun

### Deleting Requests
1. View a request
2. Click **"Delete"** button
3. Confirm deletion

## Project Structure

```
ChurchFacilityManagement/
├── Models/
│   ├── MaintenanceRequest.cs
│   └── Role.cs
├── Services/
│   └── GoogleSheetsService.cs
├── Properties/
│   └── launchSettings.json
├── Program.cs
├── appsettings.json
├── .gitignore
└── README.md
```

## Technologies Used

- **ASP.NET Core 10.0**: Minimal API framework
- **Google Sheets API v4**: Data storage and synchronization
- **HTML/CSS/JavaScript**: Frontend interface
- **C# 14.0**: Programming language

## API Routes

| Route | Method | Description |
|-------|--------|-------------|
| `/` | GET | List all requests with filters |
| `/request/new` | GET | New request form |
| `/request/create` | POST | Create new request |
| `/request/{id}` | GET | View request details |
| `/request/{id}/edit` | GET | Edit request form |
| `/request/{id}/update` | POST | Update request |
| `/request/{id}/delete` | GET | Delete request |
| `/reports` | GET | Tracking reports |
| `/privacy` | GET | Privacy policy (for Dropbox production) |
| `/dropbox/setup` | GET | Dropbox OAuth setup |
| `/dropbox/callback` | GET | Dropbox OAuth callback |

## Dropbox Integration

### Development Mode (Recommended)
For internal church use, stay in **Development Mode**:
1. Go to [Dropbox App Console](https://www.dropbox.com/developers/apps)
2. Select your app → **Settings** → **Development users**
3. Add email addresses of all users who need access
4. **Supports up to 500 users** - sufficient for most churches!

### Production Mode (Optional)
If you need unlimited users or public access:
1. Complete the Dropbox production approval process
2. Provide **Privacy Policy URL**: `https://your-domain.com/privacy`
3. The privacy policy is automatically served at `/privacy` endpoint
4. **Privacy Policy link is available in the footer of all pages**

## Future Enhancements

- [x] Email/SMS notifications
- [x] File upload support (Dropbox integration)
- [ ] Dashboard with charts/graphs
- [ ] Export to PDF
- [ ] Calendar view for due dates

## Contributing

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add amazing feature'`)
4. Push to branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

## License

This project is licensed under the MIT License.

## Support

For questions or issues, please open an issue on the GitHub repository:
https://github.com/hkearn777/ChurchFacilityManagement

## Acknowledgments

- Inspired by **ChurchSecurityScheduler** project
- Built for church maintenance coordination
- Uses Google Sheets for easy data management and sharing
