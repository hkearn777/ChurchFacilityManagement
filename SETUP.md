# Church Facility Management - Quick Setup Guide

## ✅ What's Been Created

Your new **ChurchFacilityManagement** solution has been created at:
`C:\Users\906074897\Documents\Visual Studio 2022\Projects\VS Projects\ChurchFacilityManagement\`

### Project Structure:
```
ChurchFacilityManagement/
├── Models/
│   ├── MaintenanceRequest.cs    ✅ 15 properties matching your spreadsheet
│   └── Role.cs                  ✅ Role management model
├── Services/
│   └── GoogleSheetsService.cs   ✅ Full CRUD operations for requests
├── Properties/
│   └── launchSettings.json      ✅ Development settings
├── Program.cs                   ✅ All routes and UI pages
├── appsettings.json            ✅ Configuration (needs your Spreadsheet ID)
├── .gitignore                  ✅ Protects sensitive files
├── README.md                   ✅ Complete documentation
└── ChurchFacilityManagement.csproj ✅ Project file

✅ Build Status: SUCCESS
```

## 🚀 Next Steps

### 1. Update Configuration

Edit `appsettings.json` and replace `YOUR_SPREADSHEET_ID_HERE` with your actual Google Sheets ID:

```json
{
  "GoogleSheets": {
    "CredentialsPath": "credentials.json",
    "SpreadsheetId": "YOUR_ACTUAL_SPREADSHEET_ID",
    "ApplicationName": "Church Facility Management"
  }
}
```

**Find your Spreadsheet ID in the URL:**
```
https://docs.google.com/spreadsheets/d/[THIS_IS_YOUR_ID]/edit
```

### 2. Add Google Credentials

Copy your `credentials.json` file from the ChurchSecurityScheduler project:
```
FROM: ChurchSecurityScheduler\credentials.json
TO:   ChurchFacilityManagement\credentials.json
```

OR create a new service account if using a different Google Sheets document.

### 3. Prepare Your Google Sheet

Your spreadsheet should have 3 tabs with these names:

#### Tab 1: "Tasks"
Header row with columns A-O:
```
ID | Report Date | Description | Requested By | Request Method | Building | Priority | Status | Assigned | Trade | Corrective Action | Due Date | Start Date | Completed | Attachments
```

#### Tab 2: "Completed Tasks"
Same structure as Tasks (for archival)

#### Tab 3: "Roles"
Header row with columns A-C:
```
Role | Person | Contact
```

### 4. Share Sheet with Service Account

1. Open your Google Sheet
2. Click **Share** button
3. Add the service account email (from credentials.json)
4. Grant **Editor** permissions

### 5. Run the Application

```bash
cd "C:\Users\906074897\Documents\Visual Studio 2022\Projects\VS Projects\ChurchFacilityManagement"
dotnet run
```

Then open: http://localhost:5118

## 📋 Features Implemented

✅ **Home Page**: List all requests with filtering and search
✅ **Create Request**: Form to add new maintenance requests
✅ **View Request**: Detailed view of single request
✅ **Edit Request**: Update existing request
✅ **Delete Request**: Remove requests from sheet
✅ **Reports Page**: Overdue, Started, Not Started tracking
✅ **Filters**: Status, Priority, Building, Search text
✅ **Mobile Responsive**: Works on all devices

## 🎨 Key Differences from Security Scheduler

| Feature | Security Scheduler | Facility Management |
|---------|-------------------|---------------------|
| Data Model | Date-based sheets | Row-based requests |
| Operations | View/Edit positions | Full CRUD operations |
| Filtering | None | Status, Priority, Building, Search |
| Reports | None | Overdue, Started, Not Started |
| Fields | 5 columns | 15 columns |
| Roles | None | Role management tab |

## 🔧 Google Sheets Integration

The app uses **row-based operations**:
- **GetAllRequestsAsync()**: Reads all rows from Tasks sheet
- **CreateRequestAsync()**: Appends new row to Tasks sheet
- **UpdateRequestAsync()**: Updates specific row
- **DeleteRequestAsync()**: Deletes row by ID
- **MoveToCompletedAsync()**: Copies to Completed tab, removes from Tasks

Each request tracks its `RowNumber` for precise updates.

## 🐛 Troubleshooting

### "Spreadsheet not found"
- Check SpreadsheetId in appsettings.json
- Ensure sheet is shared with service account email

### "Credentials not found"
- Place credentials.json in project root
- Check CredentialsPath in appsettings.json

### "Sheet not found: Tasks"
- Create tabs named exactly: "Tasks", "Completed Tasks", "Roles"
- Tab names are case-sensitive

### Build errors
- Ensure .NET 10 SDK is installed
- Run: `dotnet restore` then `dotnet build`

## 📞 Support

Questions? Check the README.md for detailed documentation or open an issue at:
https://github.com/hkearn777/ChurchFacilityManagement

---

**Ready to start managing your church facilities! 🏢**
