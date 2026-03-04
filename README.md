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

## Future Enhancements

- [ ] Email/SMS notifications
- [ ] User authentication and role-based permissions
- [ ] File upload support (not just links)
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
