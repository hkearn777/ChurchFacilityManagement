using ChurchFacilityManagement.Services;
using ChurchFacilityManagement.Models;

namespace ChurchFacilityManagement
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddSingleton<GoogleSheetsService>();
            builder.Services.AddSingleton<DropboxOAuthService>();
            builder.Services.AddSingleton<DropboxService>();
            builder.Services.AddSingleton<EmailService>();
            builder.Services.AddSingleton<PdfReportService>();

            var app = builder.Build();

            // Dropbox OAuth endpoints
            app.MapGet("/dropbox/setup", async (DropboxOAuthService oauthService) =>
            {
                try
                {
                    var authUrl = oauthService.GetAuthorizationUrl();

                    var html = $@"
<!DOCTYPE html>
<html>
<head>
    <title>Dropbox Setup</title>
    <style>
        body {{ font-family: Arial, sans-serif; padding: 40px; background: #f5f5f5; }}
        .container {{ max-width: 600px; margin: 0 auto; background: white; padding: 30px; border-radius: 8px; box-shadow: 0 2px 4px rgba(0,0,0,0.1); }}
        h1 {{ color: #333; }}
        .btn {{ display: inline-block; padding: 12px 24px; background: #4285f4; color: white; text-decoration: none; border-radius: 4px; font-size: 1em; }}
        .btn:hover {{ background: #3367d6; }}
        p {{ line-height: 1.6; }}
    </style>
</head>
<body>
    <div class='container'>
        <h1>🔐 Dropbox OAuth Setup</h1>
        <p>Click the button below to authorize this application to access your Dropbox account.</p>
        <p>This is a one-time setup that will provide a refresh token for ongoing access.</p>
        <a href='{authUrl}' class='btn'>Authorize Dropbox</a>
    </div>
</body>
</html>";
                    return Results.Content(html, "text/html");
                }
                catch (Exception ex)
                {
                    return Results.Content($@"
<!DOCTYPE html>
<html>
<head><title>Error</title></head>
<body>
    <h1>Configuration Error</h1>
    <p>{ex.Message}</p>
    <p>Make sure DROPBOX_APP_KEY, DROPBOX_APP_SECRET, and DROPBOX_REDIRECT_URI are configured.</p>
</body>
</html>", "text/html");
                }
            });

            app.MapGet("/dropbox/callback", async (HttpContext context, DropboxOAuthService oauthService) =>
            {
                var code = context.Request.Query["code"].ToString();
                var error = context.Request.Query["error"].ToString();

                if (!string.IsNullOrEmpty(error))
                {
                    return Results.Content($@"
<!DOCTYPE html>
<html>
<head><title>Authorization Failed</title></head>
<body>
    <h1>❌ Authorization Failed</h1>
    <p>Error: {error}</p>
    <a href='/dropbox/setup'>Try Again</a>
</body>
</html>", "text/html");
                }

                try
                {
                    var tokens = await oauthService.ExchangeCodeForTokenAsync(code);

                    var html = $@"
<!DOCTYPE html>
<html>
<head>
    <title>Dropbox Setup Complete</title>
    <style>
        body {{ font-family: Arial, sans-serif; padding: 40px; background: #f5f5f5; }}
        .container {{ max-width: 700px; margin: 0 auto; background: white; padding: 30px; border-radius: 8px; box-shadow: 0 2px 4px rgba(0,0,0,0.1); }}
        h1 {{ color: #34a853; }}
        .success {{ background: #e6f4ea; padding: 15px; border-radius: 4px; margin: 20px 0; }}
        .token-box {{ background: #f8f9fa; padding: 15px; border-radius: 4px; font-family: monospace; word-break: break-all; margin: 15px 0; }}
        .btn {{ display: inline-block; padding: 10px 20px; background: #4285f4; color: white; text-decoration: none; border-radius: 4px; }}
        .warning {{ background: #fef7e0; padding: 15px; border-radius: 4px; margin: 20px 0; }}
    </style>
</head>
<body>
    <div class='container'>
        <h1>✅ Dropbox Setup Complete!</h1>
        <div class='success'>
            <p><strong>Your refresh token has been obtained and saved.</strong></p>
            <p>For local development, the token is saved in 'dropbox_tokens.json'.</p>
        </div>

        <div class='warning'>
            <p><strong>📋 For Cloud Deployment:</strong></p>
            <p>Save this refresh token to Google Cloud Secret Manager:</p>
            <div class='token-box'>{tokens.RefreshToken}</div>
            <p>Run this command:</p>
            <pre style='background: #333; color: #0f0; padding: 10px; border-radius: 4px; overflow-x: auto;'>echo ""{tokens.RefreshToken}"" | gcloud secrets create cfm-dropbox-refresh-token --data-file=-</pre>
        </div>

        <a href='/' class='btn'>Go to Home</a>
    </div>
</body>
</html>";
                    return Results.Content(html, "text/html");
                }
                catch (Exception ex)
                {
                    return Results.Content($@"
<!DOCTYPE html>
<html>
<head><title>Error</title></head>
<body>
    <h1>❌ Token Exchange Failed</h1>
    <p>{ex.Message}</p>
    <a href='/dropbox/setup'>Try Again</a>
</body>
</html>", "text/html");
                }
            });

            // Home page - List all requests
            app.MapGet("/", async (GoogleSheetsService sheetsService, HttpContext context) =>
            {
                var requests = await sheetsService.GetAllRequestsAsync();
                var dropdowns = await sheetsService.GetDropdownValuesAsync();

                // Filter out null values to ensure non-nullable string array
                var filterStatuses = context.Request.Query["status"].Where(s => !string.IsNullOrEmpty(s)).Select(s => s!).ToArray();
                var filterPriority = context.Request.Query["priority"].ToString();
                var filterBuilding = context.Request.Query["building"].ToString();
                var searchText = context.Request.Query["search"].ToString();
                var sortOrder = context.Request.Query["sort"].ToString();

                // Default to descending (newest first) if not specified
                if (string.IsNullOrEmpty(sortOrder))
                    sortOrder = "desc";

                // If no statuses selected, show all; otherwise filter by selected statuses
                if (filterStatuses.Length > 0)
                    requests = requests.Where(r => filterStatuses.Contains(r.Status, StringComparer.OrdinalIgnoreCase)).ToList();

                if (!string.IsNullOrEmpty(filterPriority))
                    requests = requests.Where(r => r.Priority.Equals(filterPriority, StringComparison.OrdinalIgnoreCase)).ToList();

                if (!string.IsNullOrEmpty(filterBuilding))
                    requests = requests.Where(r => r.Building.Equals(filterBuilding, StringComparison.OrdinalIgnoreCase)).ToList();

                if (!string.IsNullOrEmpty(searchText))
                    requests = requests.Where(r => 
                        r.Description.Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
                        r.RequestedBy.Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
                        r.Assigned.Contains(searchText, StringComparison.OrdinalIgnoreCase)).ToList();

                var html = @"
<!DOCTYPE html>
<html>
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>Church Facility Management</title>
    <style>
        body { font-family: Arial, sans-serif; padding: 20px; background: #f5f5f5; margin: 0; }
        .container { max-width: 1400px; margin: 0 auto; background: white; padding: 20px; border-radius: 8px; box-shadow: 0 2px 4px rgba(0,0,0,0.1); }
        h1 { color: #333; font-size: 1.8em; margin-top: 0; }
        .header-actions { display: flex; justify-content: space-between; align-items: center; margin-bottom: 20px; }
        .btn { display: inline-block; padding: 10px 20px; background: #4285f4; color: white; text-decoration: none; border-radius: 4px; border: none; font-size: 0.9em; cursor: pointer; }
        .btn:hover { background: #3367d6; }
        .btn-success { background: #34a853; }
        .btn-success:hover { background: #2d8e47; }
        .btn-danger { background: #d93025; }
        .btn-danger:hover { background: #b52a1f; }
        .btn-small { padding: 6px 12px; font-size: 0.8em; }
        .filters { background: #f8f9fa; padding: 15px; border-radius: 4px; margin-bottom: 20px; }
        .filter-group { display: flex; gap: 10px; flex-wrap: wrap; align-items: center; }
        .filter-group label { font-weight: bold; }
        .filter-group select, .filter-group input { padding: 8px; border: 1px solid #ddd; border-radius: 4px; }
        .checkbox-group { display: flex; gap: 15px; flex-wrap: wrap; align-items: center; background: white; padding: 10px; border: 1px solid #ddd; border-radius: 4px; }
        .checkbox-group label { font-weight: normal; display: flex; align-items: center; gap: 5px; cursor: pointer; }
        .checkbox-group input[type=""checkbox""] { cursor: pointer; width: 16px; height: 16px; }
        .table-wrapper { overflow-x: auto; }
        table { border-collapse: collapse; width: 100%; }
        th { background: #4285f4; color: white; padding: 12px 8px; text-align: left; font-size: 0.85em; }
        td { padding: 10px 8px; border: 1px solid #ddd; font-size: 0.85em; }
        tbody tr:hover { filter: brightness(0.95); cursor: pointer; }
        .priority-high { color: #d93025; font-weight: bold; }
        .priority-normal { color: #4285f4; }
        .priority-low { color: #34a853; }
        .status-badge { display: inline-block; padding: 4px 8px; border-radius: 4px; font-size: 0.75em; }
        .status-progress { background: #fef7e0; color: #5f4b00; }
        .status-completed { background: #e6f4ea; color: #1e7e34; }
        .status-awp { background: #fee; color: #c00; }
        .actions { white-space: nowrap; }
        @media (max-width: 768px) {
            body { padding: 10px; }
            .container { padding: 15px; }
            table { font-size: 0.75em; }
            .filter-group { flex-direction: column; align-items: stretch; }
        }
    </style>
</head>
<body>
    <div class='container'>
        <div class='header-actions'>
            <h1>🏢 Church Facility Management</h1>
            <div>
                <a href='/request/new' class='btn btn-success'>+ New Request</a>
                <a href='/reports' class='btn'>📊 Reports</a>
                <a href='/?sort=" + (sortOrder == "desc" ? "asc" : "desc") + (filterStatuses.Length > 0 ? string.Concat(filterStatuses.Select(s => "&status=" + Uri.EscapeDataString(s ?? ""))) : "") + (string.IsNullOrEmpty(filterPriority) ? "" : "&priority=" + filterPriority) + (string.IsNullOrEmpty(filterBuilding) ? "" : "&building=" + filterBuilding) + (string.IsNullOrEmpty(searchText) ? "" : "&search=" + searchText) + @"' class='btn' title='Toggle sort order'>
                    " + (sortOrder == "desc" ? "📅 Newest First ▼" : "📅 Oldest First ▲") + @"
                </a>
            </div>
        </div>

        <div class='filters'>
            <form method='get'>
                <div class='filter-group'>
                    <label>Status:</label>
                    <div class='checkbox-group'>
                        " + GenerateStatusCheckboxes(dropdowns.Statuses, filterStatuses) + @"
                    </div>

                    <label>Priority:</label>
                    <select name='priority' onchange='this.form.submit()'>
                        <option value=''>All</option>
                        " + GenerateDropdownOptionsWithSelected(dropdowns.Priorities, filterPriority) + @"
                    </select>

                    <label>Building:</label>
                    <select name='building' onchange='this.form.submit()'>
                        <option value=''>All</option>
                        " + GenerateDropdownOptionsWithSelected(dropdowns.Buildings, filterBuilding) + @"
                    </select>

                    <label>Search:</label>
                    <input type='text' name='search' value='" + searchText + @"' placeholder='Search...'>
                    <button type='submit' class='btn btn-small'>Filter</button>
                    <a href='/' class='btn btn-small'>Clear</a>
                </div>
            </form>
        </div>

        <div class='table-wrapper'>
            <table>
                <tr>
                    <th>ID</th>
                    <th>Report Date</th>
                    <th>Description</th>
                    <th>Requested By</th>
                    <th>Building</th>
                    <th>Priority</th>
                    <th>Status</th>
                    <th>Assigned</th>
                    <th>Due Date</th>
                    <th>Actions</th>
                </tr>";

                if (requests.Count == 0)
                {
                    html += "<tr><td colspan='10' style='text-align:center;padding:20px;'>No requests found. <a href='/request/new'>Create your first request</a></td></tr>";
                }
                else
                {
                    var sortedRequests = sortOrder == "asc" 
                        ? requests.OrderBy(r => r.ReportDate) 
                        : requests.OrderByDescending(r => r.ReportDate);

                    foreach (var req in sortedRequests)
                    {
                        var priorityClass = req.Priority switch
                        {
                            "1" => "priority-high",
                            "2" => "priority-normal",
                            "3" => "priority-low",
                            _ => ""
                        };

                        var statusClass = req.Status switch
                        {
                            "In Progress" => "status-progress",
                            "Completed" => "status-completed",
                            "AWP" => "status-awp",
                            _ => ""
                        };

                        var priorityText = req.Priority switch
                        {
                            "1" => "High",
                            "2" => "Normal",
                            "3" => "Low",
                            _ => req.Priority
                        };

                        var rowStyle = !string.IsNullOrEmpty(req.StatusColor) 
                            ? $"style='background-color: {req.StatusColor}; opacity: 0.85;'" 
                            : "";

                        html += $@"
                <tr {rowStyle}>
                    <td>{req.Id}</td>
                    <td>{req.ReportDate:yyyy-MM-dd}</td>
                    <td>{TruncateText(req.Description, 50)}</td>
                    <td>{req.RequestedBy}</td>
                    <td>{req.Building}</td>
                    <td class='{priorityClass}'>{priorityText}</td>
                    <td><span class='status-badge {statusClass}'>{req.Status}</span></td>
                    <td>{req.Assigned}</td>
                    <td>{(req.DueDate.HasValue ? req.DueDate.Value.ToString("yyyy-MM-dd") : "")}</td>
                    <td class='actions'>
                        <a href='/request/{req.Id}' class='btn btn-small'>View</a>
                        <a href='/request/{req.Id}/edit' class='btn btn-small'>Edit</a>
                    </td>
                </tr>";
                    }
                }

                html += @"
            </table>
        </div>
        <div style='text-align: center; margin-top: 30px; padding-top: 20px; border-top: 1px solid #ddd; color: #7f8c8d; font-size: 0.9em;'>
            <p>© 2025 Church Facility Management | <a href='/privacy' style='color: #4285f4; text-decoration: none;'>Privacy Policy</a></p>
        </div>
    </div>
</body>
</html>";

                return Results.Text(html, "text/html");
            });

            // Requestor route - Simplified form for regular users
            app.MapGet("/requestor/new", async (GoogleSheetsService sheetsService) =>
            {
                var dropdowns = await sheetsService.GetDropdownValuesAsync();
                var html = GenerateRequestorForm(dropdowns);
                return Results.Text(html, "text/html");
            });

            // Requestor form submission
            app.MapPost("/requestor/create", async (HttpContext context, GoogleSheetsService sheetsService, DropboxService dropboxService, EmailService emailService, ILogger<Program> logger) =>
            {
                var form = context.Request.Form;

                var request = new MaintenanceRequest
                {
                    Description = form["description"].ToString(),
                    RequestedBy = form["requestedBy"].ToString(),
                    RequestMethod = "Web App",
                    Building = form["building"].ToString(),
                    Priority = "",
                    Status = "Submitted",
                    Assigned = "",
                    Trade = "",
                    CorrectiveAction = "",
                    DueDate = null,
                    Attachments = ""
                };

                var newId = await sheetsService.CreateRequestAsync(request);
                logger.LogInformation($"Created request {newId} via requestor form");
                var imageUploadError = "";
                var imageUploadSuccess = false;

                var files = form.Files.GetFiles("images");
                logger.LogInformation($"Requestor upload: Received {files.Count} file(s) for request {newId}");
                if (files.Count > 0)
                {
                    var validFiles = files.Take(3).Where(f => f.Length <= 5 * 1024 * 1024).ToList();

                    if (validFiles.Count > 0)
                    {
                        logger.LogInformation($"Requestor upload: Attempting to upload {validFiles.Count} valid file(s) to Dropbox for request {newId}");
                        try
                        {
                            var links = await dropboxService.UploadMultipleImagesAsync(newId, validFiles);

                            if (links.Count > 0)
                            {
                                // Fetch the request to get the RowNumber
                                var createdRequest = await sheetsService.GetRequestByIdAsync(newId);
                                if (createdRequest != null)
                                {
                                                        createdRequest.Attachments = string.Join(", ", links);
                                                        await sheetsService.UpdateRequestAsync(createdRequest);
                                                        imageUploadSuccess = true;
                                                    }
                                                }
                                                else
                                                {
                                                    logger.LogWarning($"Requestor upload: No images uploaded successfully for request {newId}");
                                                    imageUploadError = "No images were uploaded successfully. Please check file sizes and formats.";
                                                }
                                            }
                                            catch (Exception ex)
                                            {
                                                logger.LogError(ex, $"Requestor upload: Image upload failed for request {newId}");
                                                imageUploadError = $"Image upload failed: {ex.Message}";

                                                // Store error in Attachments column
                                                var createdRequest = await sheetsService.GetRequestByIdAsync(newId);
                                                if (createdRequest != null)
                                                {
                                                    createdRequest.Attachments = $"IMAGE UPLOAD FAILED: {ex.Message}";
                                                    await sheetsService.UpdateRequestAsync(createdRequest);
                                                }

                                                // Send error notification to Manager
                                                var roles = await sheetsService.GetRolesAsync();
                                                var manager = roles.FirstOrDefault(r => r.RoleName.Equals("Manager", StringComparison.OrdinalIgnoreCase));
                                                if (manager != null && !string.IsNullOrEmpty(manager.Contact))
                                                {
                                                    await emailService.SendImageUploadErrorNotificationAsync(newId, request.Description, request.RequestedBy, ex.Message, manager.Contact);
                                                }
                                            }
                                        }
                                    }

                                    var statusMessage = imageUploadSuccess 
                                        ? "Request submitted successfully with images!" 
                                        : !string.IsNullOrEmpty(imageUploadError) 
                                            ? $"Request created, but image upload failed" 
                                            : "Request submitted successfully!";

                                    var statusColor = imageUploadSuccess ? "#34a853" : !string.IsNullOrEmpty(imageUploadError) ? "#fbbc04" : "#34a853";
                                    var statusIcon = imageUploadSuccess ? "✅" : !string.IsNullOrEmpty(imageUploadError) ? "⚠️" : "✅";

                return Results.Text($@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>Request Submitted</title>
    <style>
        body {{ font-family: Arial, sans-serif; padding: 20px; background: #f5f5f5; text-align: center; }}
        .container {{ max-width: 600px; margin: 50px auto; background: white; padding: 40px; border-radius: 8px; box-shadow: 0 2px 4px rgba(0,0,0,0.1); }}
        h1 {{ color: {statusColor}; }}
        .error-box {{ background: #fef7e0; border-left: 4px solid #fbbc04; padding: 15px; margin: 20px 0; text-align: left; border-radius: 4px; }}
        .error-box p {{ margin: 5px 0; color: #5f4b00; }}
        .btn {{ display: inline-block; margin-top: 20px; padding: 10px 20px; background: #4285f4; color: white; text-decoration: none; border-radius: 4px; }}
        .btn:hover {{ background: #3367d6; }}
    </style>
</head>
<body>
    <div class='container'>
        <h1>{statusIcon} {statusMessage}</h1>
        <p>Your maintenance request has been submitted and will be reviewed by our team.</p>
        <p>Request ID: <strong>{newId}</strong></p>
        {(!string.IsNullOrEmpty(imageUploadError) ? $@"
        <div class='error-box'>
            <p><strong>⚠️ Image Upload Issue:</strong></p>
            <p>{imageUploadError}</p>
            <p><strong>Don't worry - your request details have been saved.</strong> You can contact the facilities manager to add images later.</p>
            <p style='margin-top: 10px; font-size: 0.9em; color: #666;'>The facility manager has been notified of this issue.</p>
        </div>" : "")}
        <a href='/requestor/new' class='btn'>Submit Another Request</a>
    </div>
</body>
</html>", "text/html");
            });

            // Proxy route - Similar to Requestor but with Request Method dropdown
            app.MapGet("/proxy/new", async (GoogleSheetsService sheetsService) =>
            {
                var dropdowns = await sheetsService.GetDropdownValuesAsync();
                var html = GenerateProxyForm(dropdowns);
                return Results.Text(html, "text/html");
            });

            // Proxy form submission
            app.MapPost("/proxy/create", async (HttpContext context, GoogleSheetsService sheetsService, DropboxService dropboxService, EmailService emailService, ILogger<Program> logger) =>
            {
                var form = context.Request.Form;

                var request = new MaintenanceRequest
                {
                    Description = form["description"].ToString(),
                    RequestedBy = form["requestedBy"].ToString(),
                    RequestMethod = form["requestMethod"].ToString(),
                    Building = form["building"].ToString(),
                    Priority = "",
                    Status = "Submitted",
                    Assigned = "",
                    Trade = "",
                    CorrectiveAction = "",
                    DueDate = null,
                    Attachments = ""
                };

                var newId = await sheetsService.CreateRequestAsync(request);
                logger.LogInformation($"Created request {newId} via proxy form");
                var imageUploadError = "";
                var imageUploadSuccess = false;

                var files = form.Files.GetFiles("images");
                logger.LogInformation($"Proxy upload: Received {files.Count} file(s) for request {newId}");
                if (files.Count > 0)
                {
                    var validFiles = files.Take(3).Where(f => f.Length <= 5 * 1024 * 1024).ToList();

                    if (validFiles.Count > 0)
                    {
                        logger.LogInformation($"Proxy upload: Attempting to upload {validFiles.Count} valid file(s) to Dropbox for request {newId}");
                        try
                        {
                            var links = await dropboxService.UploadMultipleImagesAsync(newId, validFiles);

                            if (links.Count > 0)
                            {
                                // Fetch the request to get the RowNumber
                                var createdRequest = await sheetsService.GetRequestByIdAsync(newId);
                                if (createdRequest != null)
                                {
                                    createdRequest.Attachments = string.Join(", ", links);
                                    await sheetsService.UpdateRequestAsync(createdRequest);
                                    imageUploadSuccess = true;
                                }
                            }
                            else
                            {
                                logger.LogWarning($"Proxy upload: No images uploaded successfully for request {newId}");
                                imageUploadError = "No images were uploaded successfully. Please check file sizes and formats.";
                            }
                        }
                        catch (Exception ex)
                        {
                            logger.LogError(ex, $"Proxy upload: Image upload failed for request {newId}");
                            imageUploadError = $"Image upload failed: {ex.Message}";

                            // Store error in Attachments column
                            var createdRequest = await sheetsService.GetRequestByIdAsync(newId);
                            if (createdRequest != null)
                            {
                                createdRequest.Attachments = $"IMAGE UPLOAD FAILED: {ex.Message}";
                                await sheetsService.UpdateRequestAsync(createdRequest);
                            }

                            // Send error notification to Manager
                            var roles = await sheetsService.GetRolesAsync();
                            var manager = roles.FirstOrDefault(r => r.RoleName.Equals("Manager", StringComparison.OrdinalIgnoreCase));
                            if (manager != null && !string.IsNullOrEmpty(manager.Contact))
                            {
                                await emailService.SendImageUploadErrorNotificationAsync(newId, request.Description, request.RequestedBy, ex.Message, manager.Contact);
                            }
                        }
                    }
                }

                var statusMessage = imageUploadSuccess 
                    ? "Request submitted successfully with images!" 
                    : !string.IsNullOrEmpty(imageUploadError) 
                        ? $"Request created, but image upload failed: {imageUploadError}" 
                        : "Request submitted successfully!";

                var statusColor = imageUploadSuccess ? "#34a853" : !string.IsNullOrEmpty(imageUploadError) ? "#fbbc04" : "#34a853";
                var statusIcon = imageUploadSuccess ? "✅" : !string.IsNullOrEmpty(imageUploadError) ? "⚠️" : "✅";

                return Results.Text($@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>Request Submitted</title>
    <style>
        body {{ font-family: Arial, sans-serif; padding: 20px; background: #f5f5f5; text-align: center; }}
        .container {{ max-width: 600px; margin: 50px auto; background: white; padding: 40px; border-radius: 8px; box-shadow: 0 2px 4px rgba(0,0,0,0.1); }}
        h1 {{ color: {statusColor}; }}
        .error-box {{ background: #fef7e0; border-left: 4px solid #fbbc04; padding: 15px; margin: 20px 0; text-align: left; border-radius: 4px; }}
        .error-box p {{ margin: 5px 0; color: #5f4b00; }}
        .btn {{ display: inline-block; margin-top: 20px; padding: 10px 20px; background: #4285f4; color: white; text-decoration: none; border-radius: 4px; }}
        .btn:hover {{ background: #3367d6; }}
    </style>
</head>
<body>
    <div class='container'>
        <h1>{statusIcon} {statusMessage}</h1>
        <p>Request ID: <strong>{newId}</strong></p>
        {(!string.IsNullOrEmpty(imageUploadError) ? $@"
        <div class='error-box'>
            <p><strong>⚠️ Image Upload Issue:</strong></p>
            <p>{imageUploadError}</p>
            <p><strong>Your request data has been saved.</strong> You can edit the request later to add images.</p>
            <p style='margin-top: 10px; font-size: 0.9em; color: #666;'>The facility manager has been notified of this issue.</p>
        </div>" : "")}
        <a href='/proxy/new' class='btn'>Submit Another Request</a>
    </div>
</body>
</html>", "text/html");
            });

            // New request form
            app.MapGet("/request/new", async (GoogleSheetsService sheetsService) =>
            {
                var dropdowns = await sheetsService.GetDropdownValuesAsync();
                var html = GenerateRequestForm(null, "Create New Request", "/request/create", dropdowns);
                return Results.Text(html, "text/html");
            });

            // Create request
            app.MapPost("/request/create", async (HttpContext context, GoogleSheetsService sheetsService, DropboxService dropboxService, EmailService emailService, ILogger<Program> logger) =>
            {
                var form = context.Request.Form;

                var request = new MaintenanceRequest
                {
                    Description = form["description"].ToString(),
                    RequestedBy = form["requestedBy"].ToString(),
                    RequestMethod = form["requestMethod"].ToString(),
                    Building = form["building"].ToString(),
                    Priority = form["priority"].ToString(),
                    Status = form["status"].ToString(),
                    Notes = form["notes"].ToString(),
                    Assigned = form["assigned"].ToString(),
                    Trade = form["trade"].ToString(),
                    CorrectiveAction = form["correctiveAction"].ToString(),
                    DueDate = DateTime.TryParse(form["dueDate"].ToString(), out var dueDate) ? dueDate : null,
                    Attachments = ""
                };

                var newId = await sheetsService.CreateRequestAsync(request);
                logger.LogInformation($"Created request {newId} via admin form");
                var imageUploadError = "";

                var files = form.Files.GetFiles("images");
                logger.LogInformation($"Admin upload: Received {files.Count} file(s) for request {newId}");
                if (files.Count > 0)
                {
                    var validFiles = files.Take(3).Where(f => f.Length <= 5 * 1024 * 1024).ToList();

                    if (validFiles.Count > 0)
                    {
                        logger.LogInformation($"Admin upload: Attempting to upload {validFiles.Count} valid file(s) to Dropbox for request {newId}");
                        try
                        {
                            var links = await dropboxService.UploadMultipleImagesAsync(newId, validFiles);

                            if (links.Count > 0)
                            {
                                // Fetch the request to get the RowNumber
                                var createdRequest = await sheetsService.GetRequestByIdAsync(newId);
                                if (createdRequest != null)
                                {
                                    createdRequest.Attachments = string.Join(", ", links);
                                    await sheetsService.UpdateRequestAsync(createdRequest);
                                }
                            }
                            else
                            {
                                imageUploadError = "No images were uploaded successfully.";
                            }
                        }
                        catch (Exception ex)
                        {
                            logger.LogError(ex, $"Admin upload: Image upload error for request {newId}");
                            imageUploadError = $"Image upload error: {ex.Message}";

                            // Store error in Attachments column
                            var createdRequest = await sheetsService.GetRequestByIdAsync(newId);
                            if (createdRequest != null)
                            {
                                createdRequest.Attachments = $"IMAGE UPLOAD FAILED: {ex.Message}";
                                await sheetsService.UpdateRequestAsync(createdRequest);
                            }

                            // Send error notification to Manager
                            var roles = await sheetsService.GetRolesAsync();
                            var manager = roles.FirstOrDefault(r => r.RoleName.Equals("Manager", StringComparison.OrdinalIgnoreCase));
                            if (manager != null && !string.IsNullOrEmpty(manager.Contact))
                            {
                                await emailService.SendImageUploadErrorNotificationAsync(newId, request.Description, request.RequestedBy, ex.Message, manager.Contact);
                            }
                        }
                    }
                }

                // Redirect to home with error message if needed
                if (!string.IsNullOrEmpty(imageUploadError))
                {
                    return Results.Text($@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='UTF-8'>
    <meta http-equiv='refresh' content='5;url=/'>
    <title>Request Created</title>
    <style>
        body {{ font-family: Arial, sans-serif; padding: 20px; background: #f5f5f5; text-align: center; }}
        .container {{ max-width: 600px; margin: 50px auto; background: white; padding: 40px; border-radius: 8px; box-shadow: 0 2px 4px rgba(0,0,0,0.1); }}
        h1 {{ color: #fbbc04; }}
        .error-box {{ background: #fef7e0; border-left: 4px solid #fbbc04; padding: 15px; margin: 20px 0; text-align: left; border-radius: 4px; }}
        .error-box p {{ margin: 5px 0; color: #5f4b00; }}
    </style>
</head>
<body>
    <div class='container'>
        <h1>⚠️ Request Created with Warning</h1>
        <p>Request ID: <strong>{newId}</strong></p>
        <div class='error-box'>
            <p><strong>Image Upload Failed:</strong></p>
            <p>{imageUploadError}</p>
            <p style='margin-top: 10px;'><strong>Request data saved successfully.</strong> You can edit the request to add images later.</p>
        </div>
        <p>Redirecting to home page in 5 seconds...</p>
    </div>
</body>
</html>", "text/html");
                }

                return Results.Redirect("/");
            });

            // Approver Dashboard - Shows only requests needing approval
            app.MapGet("/approver", async (GoogleSheetsService sheetsService) =>
            {
                var allRequests = await sheetsService.GetAllRequestsAsync();
                var requestsNeedingApproval = allRequests.Where(r => r.Status.Equals("Need Approval", StringComparison.OrdinalIgnoreCase)).ToList();

                var html = @"
<!DOCTYPE html>
<html>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>Approver Dashboard</title>
    <style>
        body { font-family: Arial, sans-serif; padding: 20px; background: #f5f5f5; margin: 0; }
        .container { max-width: 1400px; margin: 0 auto; background: white; padding: 20px; border-radius: 8px; box-shadow: 0 2px 4px rgba(0,0,0,0.1); }
        h1 { color: #333; font-size: 1.8em; margin-top: 0; }
        .count-badge { display: inline-block; background: #fbbc04; color: white; padding: 5px 15px; border-radius: 20px; font-size: 0.9em; margin-left: 10px; }
        .table-wrapper { overflow-x: auto; margin-top: 20px; }
        table { border-collapse: collapse; width: 100%; }
        th { background: #4285f4; color: white; padding: 12px 8px; text-align: left; font-size: 0.85em; }
        td { padding: 10px 8px; border: 1px solid #ddd; font-size: 0.85em; }
        tr:hover { background: #f8f9fa; }
        .btn { display: inline-block; padding: 8px 16px; color: white; text-decoration: none; border-radius: 4px; border: none; font-size: 0.85em; cursor: pointer; margin-right: 5px; }
        .btn-approve { background: #34a853; }
        .btn-approve:hover { background: #2d8e47; }
        .btn-reject { background: #d93025; }
        .btn-reject:hover { background: #b52a1f; }
        .btn-defer { background: #fbbc04; }
        .btn-defer:hover { background: #f9ab00; }
        .btn-view { background: #4285f4; }
        .btn-view:hover { background: #3367d6; }
        .empty-state { text-align: center; padding: 40px; color: #666; }
        .empty-state h2 { color: #34a853; }
        .modal { display: none; position: fixed; z-index: 1000; left: 0; top: 0; width: 100%; height: 100%; overflow: auto; background-color: rgba(0,0,0,0.4); }
        .modal-content { background-color: #fefefe; margin: 10% auto; padding: 30px; border: 1px solid #888; border-radius: 8px; width: 90%; max-width: 500px; box-shadow: 0 4px 6px rgba(0,0,0,0.1); }
        .modal-header { font-size: 1.3em; font-weight: bold; margin-bottom: 20px; color: #333; }
        .modal-close { color: #aaa; float: right; font-size: 28px; font-weight: bold; line-height: 20px; cursor: pointer; }
        .modal-close:hover, .modal-close:focus { color: #000; }
        .modal-textarea { width: 100%; padding: 10px; margin: 10px 0 20px 0; border: 1px solid #ddd; border-radius: 4px; font-family: Arial, sans-serif; font-size: 0.9em; min-height: 100px; resize: vertical; }
        .modal-buttons { text-align: right; margin-top: 20px; }
        .modal-btn { padding: 10px 20px; margin-left: 10px; border: none; border-radius: 4px; cursor: pointer; font-size: 0.9em; }
        .modal-btn-submit { background: #fbbc04; color: white; }
        .modal-btn-submit:hover { background: #f9ab00; }
        .modal-btn-cancel { background: #666; color: white; }
        .modal-btn-cancel:hover { background: #555; }
        @media (max-width: 768px) {
            body { padding: 10px; }
            .container { padding: 15px; }
            table { font-size: 0.75em; }
            .modal-content { width: 95%; margin: 20% auto; padding: 20px; }
        }
    </style>
    <script>
        function openDeferModal(requestId) {
            document.getElementById('deferModal').style.display = 'block';
            document.getElementById('deferRequestId').value = requestId;
            document.getElementById('deferNotes').value = '';
            document.getElementById('deferNotes').focus();
        }

        function closeDeferModal() {
            document.getElementById('deferModal').style.display = 'none';
        }

        function submitDefer() {
            var requestId = document.getElementById('deferRequestId').value;
            var notes = document.getElementById('deferNotes').value.trim();

            if (notes === '') {
                alert('Please enter notes for the deferral.');
                return;
            }

            window.location.href = '/approver/defer/' + requestId + '?notes=' + encodeURIComponent(notes);
        }

        window.onclick = function(event) {
            var modal = document.getElementById('deferModal');
            if (event.target == modal) {
                closeDeferModal();
            }
        }
    </script>
</head>
<body>
    <div class='container'>
        <h1>🔍 Approver Dashboard<span class='count-badge'>" + requestsNeedingApproval.Count + @" Pending</span></h1>

        <!-- Defer Modal -->
        <div id='deferModal' class='modal'>
            <div class='modal-content'>
                <span class='modal-close' onclick='closeDeferModal()'>&times;</span>
                <div class='modal-header'>📝 Defer Request</div>
                <p style='color: #666; margin-bottom: 15px;'>Please provide notes for deferring this request:</p>
                <textarea id='deferNotes' class='modal-textarea' placeholder='Enter deferral notes here...'></textarea>
                <input type='hidden' id='deferRequestId' value='' />
                <div class='modal-buttons'>
                    <button class='modal-btn modal-btn-cancel' onclick='closeDeferModal()'>Cancel</button>
                    <button class='modal-btn modal-btn-submit' onclick='submitDefer()'>Submit Deferral</button>
                </div>
            </div>
        </div>";

                if (requestsNeedingApproval.Count == 0)
                {
                    html += @"
        <div class='empty-state'>
            <h2>✅ All Caught Up!</h2>
            <p>There are no requests pending approval at this time.</p>
        </div>";
                }
                else
                {
                    html += @"
        <div class='table-wrapper'>
            <table>
                <tr>
                    <th>ID</th>
                    <th>Report Date</th>
                    <th>Description</th>
                    <th>Requested By</th>
                    <th>Building</th>
                    <th>Priority</th>
                    <th>Actions</th>
                </tr>";

                    foreach (var req in requestsNeedingApproval.OrderBy(r => r.ReportDate))
                    {
                        var priorityText = req.Priority switch
                        {
                            "1" => "High",
                            "2" => "Normal",
                            "3" => "Low",
                            _ => req.Priority
                        };

                        html += $@"
                <tr>
                    <td>{req.Id}</td>
                    <td>{req.ReportDate:yyyy-MM-dd}</td>
                    <td>{TruncateText(req.Description, 50)}</td>
                    <td>{req.RequestedBy}</td>
                    <td>{req.Building}</td>
                    <td>{priorityText}</td>
                    <td>
                        <a href='/request/{req.Id}' class='btn btn-view'>View</a>
                        <a href='/approver/approve/{req.Id}' class='btn btn-approve' onclick='return confirm(""Approve this request?"")'>✅ Approve</a>
                        <a href='/approver/reject/{req.Id}' class='btn btn-reject' onclick='return confirm(""Mark as Not Approved?"")'>❌ Not Approve</a>
                        <button class='btn btn-defer' onclick='openDeferModal({req.Id})'>⏸️ Defer</button>
                    </td>
                </tr>";
                    }

                    html += @"
            </table>
        </div>";
                }

                html += @"
    </div>
</body>
</html>";

                return Results.Text(html, "text/html");
            });

            // Approve a request
            app.MapGet("/approver/approve/{id}", async (int id, GoogleSheetsService sheetsService) =>
            {
                var request = await sheetsService.GetRequestByIdAsync(id);
                if (request != null)
                {
                    request.Status = "Approved";
                    await sheetsService.UpdateRequestAsync(request);
                }
                return Results.Redirect("/approver");
            });

            // Reject a request
            app.MapGet("/approver/reject/{id}", async (int id, GoogleSheetsService sheetsService) =>
            {
                var request = await sheetsService.GetRequestByIdAsync(id);
                if (request != null)
                {
                    request.Status = "Not Approved";
                    await sheetsService.UpdateRequestAsync(request);
                }
                return Results.Redirect("/approver");
            });

            // Defer a request with notes
            app.MapGet("/approver/defer/{id}", async (int id, string notes, GoogleSheetsService sheetsService) =>
            {
                var request = await sheetsService.GetRequestByIdAsync(id);
                if (request != null)
                {
                    request.Status = "Deferred";
                    var deferredDate = DateTime.Now.ToString("yyyy-MM-dd");
                    request.Notes = $"Deferred {deferredDate} - {notes}";
                    await sheetsService.UpdateRequestAsync(request);
                }
                return Results.Redirect("/approver");
            });

            // View request details
            app.MapGet("/request/{id}", async (int id, GoogleSheetsService sheetsService) =>
            {
                var request = await sheetsService.GetRequestByIdAsync(id);
                
                if (request == null)
                {
                    return Results.Text("<h1>Request not found</h1><a href='/'>← Back</a>", "text/html");
                }

                var html = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>Request #{request.Id}</title>
    <style>
        body {{ font-family: Arial, sans-serif; padding: 20px; background: #f5f5f5; }}
        .container {{ max-width: 900px; margin: 0 auto; background: white; padding: 20px; border-radius: 8px; box-shadow: 0 2px 4px rgba(0,0,0,0.1); }}
        h1 {{ color: #333; }}
        .back-link {{ display: inline-block; margin-bottom: 15px; color: #4285f4; text-decoration: none; }}
        .back-link:hover {{ text-decoration: underline; }}
        .field {{ margin-bottom: 15px; }}
        .field-label {{ font-weight: bold; color: #666; margin-bottom: 5px; }}
        .field-value {{ padding: 10px; background: #f8f9fa; border-radius: 4px; }}
        .btn {{ display: inline-block; padding: 10px 20px; background: #4285f4; color: white; text-decoration: none; border-radius: 4px; margin-right: 10px; }}
        .btn:hover {{ background: #3367d6; }}
        .btn-danger {{ background: #d93025; }}
        .btn-danger:hover {{ background: #b52a1f; }}
        .actions {{ margin-top: 20px; }}
    </style>
</head>
<body>
    <div class='container'>
        <a href='/' class='back-link'>← Back to List</a>
        <h1>Request #{request.Id}</h1>
        
        <div class='field'>
            <div class='field-label'>Report Date</div>
            <div class='field-value'>{request.ReportDate:yyyy-MM-dd}</div>
        </div>
        
        <div class='field'>
            <div class='field-label'>Description</div>
            <div class='field-value'>{request.Description}</div>
        </div>
        
        <div class='field'>
            <div class='field-label'>Requested By</div>
            <div class='field-value'>{request.RequestedBy}</div>
        </div>
        
        <div class='field'>
            <div class='field-label'>Request Method</div>
            <div class='field-value'>{request.RequestMethod}</div>
        </div>
        
        <div class='field'>
            <div class='field-label'>Building</div>
            <div class='field-value'>{request.Building}</div>
        </div>
        
        <div class='field'>
            <div class='field-label'>Priority</div>
            <div class='field-value'>{request.Priority}</div>
        </div>
        
        <div class='field'>
            <div class='field-label'>Status</div>
            <div class='field-value'>{request.Status}</div>
        </div>

        <div class='field'>
            <div class='field-label'>Notes</div>
            <div class='field-value'>{(string.IsNullOrEmpty(request.Notes) ? "None" : request.Notes)}</div>
        </div>

        <div class='field'>
            <div class='field-label'>Assigned To</div>
            <div class='field-value'>{request.Assigned}</div>
        </div>
        
        <div class='field'>
            <div class='field-label'>Trade</div>
            <div class='field-value'>{request.Trade}</div>
        </div>
        
        <div class='field'>
            <div class='field-label'>Corrective Action</div>
            <div class='field-value'>{request.CorrectiveAction}</div>
        </div>
        
        <div class='field'>
            <div class='field-label'>Due Date</div>
            <div class='field-value'>{(request.DueDate.HasValue ? request.DueDate.Value.ToString("yyyy-MM-dd") : "Not set")}</div>
        </div>
        
        <div class='field'>
            <div class='field-label'>Start Date</div>
            <div class='field-value'>{(request.StartDate.HasValue ? request.StartDate.Value.ToString("yyyy-MM-dd") : "Not started")}</div>
        </div>
        
        <div class='field'>
            <div class='field-label'>Completed Date</div>
            <div class='field-value'>{(request.CompletedDate.HasValue ? request.CompletedDate.Value.ToString("yyyy-MM-dd") : "Not completed")}</div>
        </div>
        
        <div class='field'>
            <div class='field-label'>Attachments</div>
            <div class='field-value'>{GenerateAttachmentsHtml(request.Attachments)}</div>
        </div>
        
        <div class='actions'>
            <a href='/request/{request.Id}/edit' class='btn'>Edit</a>
            <a href='/request/{request.Id}/delete' class='btn btn-danger' onclick='return confirm(""Are you sure you want to delete this request?"")'>Delete</a>
        </div>
        <p></p>
        <a href='/' class='back-link'>← Back to List</a>
        <div style='text-align: center; margin-top: 30px; padding-top: 20px; border-top: 1px solid #ddd; color: #7f8c8d; font-size: 0.9em;'>
            <p>© 2025 Church Facility Management | <a href='/privacy' style='color: #4285f4; text-decoration: none;'>Privacy Policy</a></p>
        </div>
    </div>
</body>
</html>";

                return Results.Text(html, "text/html");
            });

            // Edit request form
            app.MapGet("/request/{id}/edit", async (int id, GoogleSheetsService sheetsService) =>
            {
                var request = await sheetsService.GetRequestByIdAsync(id);

                if (request == null)
                {
                    return Results.Text("<h1>Request not found</h1><a href='/'>← Back</a>", "text/html");
                }

                var dropdowns = await sheetsService.GetDropdownValuesAsync();
                var html = GenerateRequestForm(request, $"Edit Request #{id}", $"/request/{id}/update", dropdowns);
                return Results.Text(html, "text/html");
            });

            // Update request
            app.MapPost("/request/{id}/update", async (int id, HttpContext context, GoogleSheetsService sheetsService, DropboxService dropboxService, EmailService emailService, ILogger<Program> logger) =>
            {
                var request = await sheetsService.GetRequestByIdAsync(id);
                if (request == null)
                    return Results.Redirect("/");

                var form = context.Request.Form;

                var oldStatus = request.Status;
                var newStatus = form["status"].ToString();

                request.Description = form["description"].ToString();
                request.RequestedBy = form["requestedBy"].ToString();
                request.RequestMethod = form["requestMethod"].ToString();
                request.Building = form["building"].ToString();
                request.Priority = form["priority"].ToString();
                request.Status = newStatus;
                request.Notes = form["notes"].ToString();
                request.Assigned = form["assigned"].ToString();
                request.Trade = form["trade"].ToString();
                request.CorrectiveAction = form["correctiveAction"].ToString();
                request.DueDate = DateTime.TryParse(form["dueDate"].ToString(), out var dueDate) ? dueDate : null;
                request.StartDate = DateTime.TryParse(form["startDate"].ToString(), out var startDate) ? startDate : null;
                request.CompletedDate = DateTime.TryParse(form["completedDate"].ToString(), out var completedDate) ? completedDate : null;

                var files = form.Files.GetFiles("images");
                logger.LogInformation($"Update request {id}: Received {files.Count} file(s)");

                if (files.Count > 0)
                {
                    var validFiles = files.Take(3).Where(f => f.Length <= 5 * 1024 * 1024).ToList();

                    if (validFiles.Count > 0)
                    {
                        logger.LogInformation($"Update request {id}: Attempting to upload {validFiles.Count} valid file(s) to Dropbox");
                        var links = await dropboxService.UploadMultipleImagesAsync(id, validFiles);

                        if (links.Count > 0)
                        {
                            var existingLinks = string.IsNullOrEmpty(request.Attachments) 
                                ? new List<string>() 
                                : request.Attachments.Split(',').Select(l => l.Trim()).ToList();

                            existingLinks.AddRange(links);
                            request.Attachments = string.Join(", ", existingLinks);
                        }
                    }
                }

                await sheetsService.UpdateRequestAsync(request);

                if (oldStatus != "Need Approval" && newStatus == "Need Approval")
                {
                    await emailService.SendApprovalNotificationAsync(request.Id, request.Description, request.RequestedBy);
                }

                return Results.Redirect($"/request/{id}");
            });

            // Delete request
            app.MapGet("/request/{id}/delete", async (int id, GoogleSheetsService sheetsService) =>
            {
                await sheetsService.DeleteRequestAsync(id);
                return Results.Redirect("/");
            });

            // Reports page
            app.MapGet("/reports", async (GoogleSheetsService sheetsService) =>
            {
                var requests = await sheetsService.GetAllRequestsAsync();
                
                var overdue = requests.Where(r => r.DueDate.HasValue && r.DueDate.Value < DateTime.Now && !r.CompletedDate.HasValue).ToList();
                var started = requests.Where(r => r.StartDate.HasValue && !r.CompletedDate.HasValue).ToList();
                var notStarted = requests.Where(r => !r.StartDate.HasValue && !r.CompletedDate.HasValue).ToList();

                var html = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>Reports - Church Facility Management</title>
    <style>
        body {{ font-family: Arial, sans-serif; padding: 20px; background: #f5f5f5; }}
        .container {{ max-width: 1200px; margin: 0 auto; background: white; padding: 20px; border-radius: 8px; box-shadow: 0 2px 4px rgba(0,0,0,0.1); }}
        h1 {{ color: #333; }}
        .back-link {{ display: inline-block; margin-bottom: 15px; color: #4285f4; text-decoration: none; }}
        .back-link:hover {{ text-decoration: underline; }}
        .btn {{ display: inline-block; padding: 10px 20px; background: #4285f4; color: white; text-decoration: none; border-radius: 4px; border: none; font-size: 0.9em; cursor: pointer; }}
        .btn:hover {{ background: #3367d6; }}
        .report-section {{ margin-bottom: 30px; }}
        .report-section h2 {{ color: #666; border-bottom: 2px solid #4285f4; padding-bottom: 10px; }}
        .count {{ font-size: 2em; font-weight: bold; color: #4285f4; }}
        .report-grid {{ display: grid; grid-template-columns: repeat(auto-fit, minmax(300px, 1fr)); gap: 20px; margin-top: 20px; }}
        .report-card {{ background: #f8f9fa; padding: 15px; border-radius: 8px; border-left: 4px solid #4285f4; }}
        .report-card h3 {{ margin-top: 0; color: #333; }}
        .report-card.overdue {{ border-left-color: #d93025; }}
        .report-card.started {{ border-left-color: #fbbc04; }}
        .report-card.not-started {{ border-left-color: #34a853; }}
        .report-list {{ margin-top: 15px; }}
        .report-item {{ display: flex; align-items: center; padding: 15px; margin-bottom: 10px; background: #f8f9fa; border-radius: 4px; border-left: 3px solid #4285f4; }}
        .report-item:hover {{ background: #e8f0fe; }}
        .report-item-content {{ flex: 1; }}
        .report-item h3 {{ margin: 0 0 5px 0; color: #333; font-size: 1.1em; }}
        .report-item p {{ margin: 0; color: #666; font-size: 0.9em; }}
        table {{ width: 100%; border-collapse: collapse; margin-top: 10px; }}
        th {{ background: #4285f4; color: white; padding: 10px; text-align: left; }}
        td {{ padding: 8px; border: 1px solid #ddd; }}
        tr:hover {{ background: #f8f9fa; }}
    </style>
</head>
<body>
    <div class='container'>
        <a href='/' class='back-link'>← Back to Requests</a>
        <h1>📊 Reports</h1>

        <div class='report-section'>
            <h2>📄 Workday PDF Reports</h2>
            <p style='color: #666; margin-bottom: 15px;'>Generate printable reports for Workday events. Click on any report to configure and generate.</p>

            <div class='report-list'>
                <div class='report-item'>
                    <div class='report-item-content'>
                        <h3>📋 Report 1: By Status</h3>
                        <p>All requests grouped by status values (oldest to newest)</p>
                    </div>
                    <a href='/reports/configure/1' class='btn'>Configure & Generate</a>
                </div>

                <div class='report-item'>
                    <div class='report-item-content'>
                        <h3>📋 Report 2: By Status (Filtered)</h3>
                        <p>Select specific status values to include in the report</p>
                    </div>
                    <a href='/reports/configure/2' class='btn'>Configure & Generate</a>
                </div>

                <div class='report-item'>
                    <div class='report-item-content'>
                        <h3>📋 Report 3: Completed Report</h3>
                        <p>Completed tasks within a specific date range</p>
                    </div>
                    <a href='/reports/configure/3' class='btn'>Configure & Generate</a>
                </div>
            </div>
        </div>

        <div class='report-grid'>
            <div class='report-card overdue'>
                <h3>Overdue Requests</h3>
                <div class='count'>{overdue.Count}</div>
            </div>
            <div class='report-card started'>
                <h3>Started (Not Completed)</h3>
                <div class='count'>{started.Count}</div>
            </div>
            <div class='report-card not-started'>
                <h3>Not Started</h3>
                <div class='count'>{notStarted.Count}</div>
            </div>
        </div>
        
        <div class='report-section'>
            <h2>Overdue Requests</h2>
            {GenerateReportTable(overdue)}
        </div>
        
        <div class='report-section'>
            <h2>Started but Not Completed</h2>
            {GenerateReportTable(started)}
        </div>
        
        <div class='report-section'>
            <h2>Not Started</h2>
            {GenerateReportTable(notStarted)}
        </div>
        <div style='text-align: center; margin-top: 30px; padding-top: 20px; border-top: 1px solid #ddd; color: #7f8c8d; font-size: 0.9em;'>
            <p>© 2025 Church Facility Management | <a href='/privacy' style='color: #4285f4; text-decoration: none;'>Privacy Policy</a></p>
        </div>
    </div>
</body>
</html>";

                return Results.Text(html, "text/html");
            });

            // Privacy Policy endpoint (for Dropbox production approval)
            app.MapGet("/privacy", async () =>
            {
                var privacyPolicyPath = Path.Combine(Directory.GetCurrentDirectory(), "privacy-policy.html");

                if (File.Exists(privacyPolicyPath))
                {
                    var html = await File.ReadAllTextAsync(privacyPolicyPath);
                    return Results.Content(html, "text/html");
                }

                return Results.Content(@"
<!DOCTYPE html>
<html>
<head><title>Privacy Policy</title></head>
<body>
    <h1>Privacy Policy</h1>
    <p>Privacy policy file not found. Please contact the administrator.</p>
</body>
</html>", "text/html");
            });

            // Report 1 Configuration Page - By Status (All)
            app.MapGet("/reports/configure/1", async (GoogleSheetsService sheetsService) =>
            {
                var html = @"
<!DOCTYPE html>
<html>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>Configure Report 1</title>
    <style>
        body { font-family: Arial, sans-serif; padding: 20px; background: #f5f5f5; }
        .container { max-width: 700px; margin: 0 auto; background: white; padding: 30px; border-radius: 8px; box-shadow: 0 2px 4px rgba(0,0,0,0.1); }
        h1 { color: #333; margin-top: 0; }
        .back-link { display: inline-block; margin-bottom: 15px; color: #4285f4; text-decoration: none; }
        .back-link:hover { text-decoration: underline; }
        .info-box { background: #e3f2fd; padding: 20px; border-radius: 4px; margin: 20px 0; border-left: 4px solid #4285f4; }
        .info-box h3 { margin-top: 0; color: #1565c0; }
        .info-box ul { margin: 10px 0; padding-left: 20px; }
        .info-box li { margin: 5px 0; color: #333; }
        .btn { display: inline-block; padding: 12px 30px; background: #4285f4; color: white; text-decoration: none; border: none; border-radius: 4px; cursor: pointer; font-size: 1em; font-weight: bold; }
        .btn:hover { background: #3367d6; }
        .btn-secondary { background: #666; margin-left: 10px; }
        .btn-secondary:hover { background: #555; }
    </style>
</head>
<body>
    <div class='container'>
        <a href='/reports' class='back-link'>← Back to Reports</a>
        <h1>📋 Report 1: By Status</h1>

        <div class='info-box'>
            <h3>Report Configuration</h3>
            <p><strong>This report includes all maintenance requests grouped by their status values.</strong></p>
            <ul>
                <li><strong>Grouping:</strong> By Status</li>
                <li><strong>Columns:</strong> Status, Building, Priority, Assigned To, Description</li>
                <li><strong>Sort Order:</strong> Oldest to Newest (by Report Date)</li>
                <li><strong>Format:</strong> PDF (Portrait, 8.5×11 inches)</li>
            </ul>
            <p style='margin-top: 15px; color: #666;'>No configuration options are needed for this report.</p>
        </div>

        <form method='post' action='/reports/generate/1'>
            <button type='submit' class='btn'>📄 Generate PDF Report</button>
            <a href='/reports' class='btn btn-secondary'>Cancel</a>
        </form>
    </div>
</body>
</html>";

                return Results.Text(html, "text/html");
            });

            // Report 1 Generation - POST
            app.MapPost("/reports/generate/1", async (GoogleSheetsService sheetsService, PdfReportService pdfService) =>
            {
                var allRequests = await sheetsService.GetAllRequestsAsync();
                var pdfBytes = pdfService.GenerateReportByStatus(allRequests);

                return Results.File(pdfBytes, "application/pdf", $"Workday_Report_By_Status_{DateTime.Now:yyyyMMdd}.pdf");
            });

            // Report 2 Configuration Page - By Status (Filtered)
            app.MapGet("/reports/configure/2", async (GoogleSheetsService sheetsService) =>
            {
                var dropdowns = await sheetsService.GetDropdownValuesAsync();
                var statusCheckboxes = "";

                foreach (var status in dropdowns.Statuses)
                {
                    statusCheckboxes += $@"
                        <label class='checkbox-label'>
                            <input type='checkbox' name='statuses' value='{status}' checked>
                            <span>{status}</span>
                        </label>";
                }

                var html = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>Configure Report 2</title>
    <style>
        body {{ font-family: Arial, sans-serif; padding: 20px; background: #f5f5f5; }}
        .container {{ max-width: 700px; margin: 0 auto; background: white; padding: 30px; border-radius: 8px; box-shadow: 0 2px 4px rgba(0,0,0,0.1); }}
        h1 {{ color: #333; margin-top: 0; }}
        .back-link {{ display: inline-block; margin-bottom: 15px; color: #4285f4; text-decoration: none; }}
        .back-link:hover {{ text-decoration: underline; }}
        .info-box {{ background: #e3f2fd; padding: 20px; border-radius: 4px; margin: 20px 0; border-left: 4px solid #4285f4; }}
        .info-box h3 {{ margin-top: 0; color: #1565c0; }}
        .info-box ul {{ margin: 10px 0; padding-left: 20px; }}
        .info-box li {{ margin: 5px 0; color: #333; }}
        .form-section {{ margin: 25px 0; }}
        .form-section label.section-label {{ display: block; font-weight: bold; font-size: 1.1em; margin-bottom: 15px; color: #333; }}
        .checkbox-group {{ display: grid; grid-template-columns: repeat(auto-fill, minmax(200px, 1fr)); gap: 12px; }}
        .checkbox-label {{ display: flex; align-items: center; padding: 10px; border: 1px solid #ddd; border-radius: 4px; cursor: pointer; transition: background 0.2s; }}
        .checkbox-label:hover {{ background: #f8f9fa; }}
        .checkbox-label input[type='checkbox'] {{ margin-right: 10px; width: 18px; height: 18px; cursor: pointer; }}
        .checkbox-label span {{ flex: 1; }}
        .action-links {{ margin-top: 10px; font-size: 0.9em; }}
        .action-links a {{ color: #4285f4; text-decoration: none; margin-right: 15px; cursor: pointer; }}
        .action-links a:hover {{ text-decoration: underline; }}
        .btn {{ display: inline-block; padding: 12px 30px; background: #4285f4; color: white; text-decoration: none; border: none; border-radius: 4px; cursor: pointer; font-size: 1em; font-weight: bold; }}
        .btn:hover {{ background: #3367d6; }}
        .btn:disabled {{ background: #ccc; cursor: not-allowed; }}
        .btn-secondary {{ background: #666; margin-left: 10px; }}
        .btn-secondary:hover {{ background: #555; }}
        .warning {{ background: #fff3cd; padding: 15px; border-radius: 4px; margin: 15px 0; color: #856404; border-left: 4px solid #ffc107; display: none; }}
    </style>
    <script>
        function selectAll() {{
            document.querySelectorAll('input[name=""statuses""]').forEach(cb => cb.checked = true);
            updateButtonState();
        }}

        function selectNone() {{
            document.querySelectorAll('input[name=""statuses""]').forEach(cb => cb.checked = false);
            updateButtonState();
        }}

        function updateButtonState() {{
            const checkedCount = document.querySelectorAll('input[name=""statuses""]:checked').length;
            const generateBtn = document.getElementById('generateBtn');
            const warning = document.getElementById('warning');

            if (checkedCount === 0) {{
                generateBtn.disabled = true;
                warning.style.display = 'block';
            }} else {{
                generateBtn.disabled = false;
                warning.style.display = 'none';
            }}
        }}

        document.addEventListener('DOMContentLoaded', function() {{
            document.querySelectorAll('input[name=""statuses""]').forEach(cb => {{
                cb.addEventListener('change', updateButtonState);
            }});
            updateButtonState();
        }});
    </script>
</head>
<body>
    <div class='container'>
        <a href='/reports' class='back-link'>← Back to Reports</a>
        <h1>📋 Report 2: By Status (Filtered)</h1>

        <div class='info-box'>
            <h3>Report Configuration</h3>
            <p><strong>This report includes requests grouped by selected status values.</strong></p>
            <ul>
                <li><strong>Grouping:</strong> By Status (filtered)</li>
                <li><strong>Columns:</strong> Status, Building, Priority, Assigned To, Description</li>
                <li><strong>Sort Order:</strong> Oldest to Newest (by Report Date)</li>
                <li><strong>Format:</strong> PDF (Portrait, 8.5×11 inches)</li>
            </ul>
        </div>

        <form method='post' action='/reports/generate/2'>
            <div class='form-section'>
                <label class='section-label'>Select Status Values to Include:</label>
                <div class='action-links'>
                    <a onclick='selectAll()'>Select All</a>
                    <a onclick='selectNone()'>Select None</a>
                </div>
                <div class='checkbox-group'>
                    {statusCheckboxes}
                </div>
            </div>

            <div id='warning' class='warning'>
                <strong>⚠️ Warning:</strong> Please select at least one status value to generate the report.
            </div>

            <div style='margin-top: 25px;'>
                <button type='submit' id='generateBtn' class='btn'>📄 Generate PDF Report</button>
                <a href='/reports' class='btn btn-secondary'>Cancel</a>
            </div>
        </form>
    </div>
</body>
</html>";

                return Results.Text(html, "text/html");
            });

            // Report 2 Generation - POST
            app.MapPost("/reports/generate/2", async (HttpContext context, GoogleSheetsService sheetsService, PdfReportService pdfService) =>
            {
                var form = context.Request.Form;
                var selectedStatuses = form["statuses"].Where(s => !string.IsNullOrEmpty(s)).Select(s => s!).ToList();

                if (selectedStatuses.Count == 0)
                {
                    return Results.Redirect("/reports/configure/2");
                }

                var allRequests = await sheetsService.GetAllRequestsAsync();
                var pdfBytes = pdfService.GenerateReportByStatusFiltered(allRequests, selectedStatuses);

                return Results.File(pdfBytes, "application/pdf", $"Workday_Report_By_Status_Filtered_{DateTime.Now:yyyyMMdd}.pdf");
            });

            // Report 3 Configuration Page - Completed Report
            app.MapGet("/reports/configure/3", async (GoogleSheetsService sheetsService) =>
            {
                var html = @"
<!DOCTYPE html>
<html>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>Configure Report 3</title>
    <style>
        body { font-family: Arial, sans-serif; padding: 20px; background: #f5f5f5; }
        .container { max-width: 700px; margin: 0 auto; background: white; padding: 30px; border-radius: 8px; box-shadow: 0 2px 4px rgba(0,0,0,0.1); }
        h1 { color: #333; margin-top: 0; }
        .back-link { display: inline-block; margin-bottom: 15px; color: #4285f4; text-decoration: none; }
        .back-link:hover { text-decoration: underline; }
        .info-box { background: #e3f2fd; padding: 20px; border-radius: 4px; margin: 20px 0; border-left: 4px solid #4285f4; }
        .info-box h3 { margin-top: 0; color: #1565c0; }
        .info-box ul { margin: 10px 0; padding-left: 20px; }
        .info-box li { margin: 5px 0; color: #333; }
        .form-section { margin: 25px 0; }
        .form-section label { display: block; font-weight: bold; margin-bottom: 8px; color: #333; }
        .form-section input[type='date'] { width: 100%; padding: 10px; border: 1px solid #ddd; border-radius: 4px; font-size: 1em; box-sizing: border-box; }
        .date-inputs { display: grid; grid-template-columns: 1fr 1fr; gap: 20px; }
        .btn { display: inline-block; padding: 12px 30px; background: #4285f4; color: white; text-decoration: none; border: none; border-radius: 4px; cursor: pointer; font-size: 1em; font-weight: bold; }
        .btn:hover { background: #3367d6; }
        .btn:disabled { background: #ccc; cursor: not-allowed; }
        .btn-secondary { background: #666; margin-left: 10px; }
        .btn-secondary:hover { background: #555; }
        .warning { background: #fff3cd; padding: 15px; border-radius: 4px; margin: 15px 0; color: #856404; border-left: 4px solid #ffc107; display: none; }
    </style>
    <script>
        function validateDates() {
            const startDate = document.getElementById('startDate').value;
            const endDate = document.getElementById('endDate').value;
            const generateBtn = document.getElementById('generateBtn');
            const warning = document.getElementById('warning');

            if (!startDate || !endDate) {
                generateBtn.disabled = true;
                warning.style.display = 'block';
                warning.innerHTML = '<strong>⚠️ Warning:</strong> Please select both start and end dates.';
            } else if (new Date(startDate) > new Date(endDate)) {
                generateBtn.disabled = true;
                warning.style.display = 'block';
                warning.innerHTML = '<strong>⚠️ Warning:</strong> Start date must be before or equal to end date.';
            } else {
                generateBtn.disabled = false;
                warning.style.display = 'none';
            }
        }

        document.addEventListener('DOMContentLoaded', function() {
            document.getElementById('startDate').addEventListener('change', validateDates);
            document.getElementById('endDate').addEventListener('change', validateDates);
            validateDates();
        });
    </script>
</head>
<body>
    <div class='container'>
        <a href='/reports' class='back-link'>← Back to Reports</a>
        <h1>📋 Report 3: Completed Report</h1>

        <div class='info-box'>
            <h3>Report Configuration</h3>
            <p><strong>This report includes all completed maintenance requests within a specific date range.</strong></p>
            <ul>
                <li><strong>Criteria:</strong> Status = 'Completed' and Date Completed within range</li>
                <li><strong>Columns:</strong> Status, Building, Priority, Assigned To, Description, Corrective Action</li>
                <li><strong>Sort Order:</strong> Date Completed (Oldest to Newest)</li>
                <li><strong>Format:</strong> PDF (Portrait, 8.5×11 inches)</li>
            </ul>
        </div>

        <form method='post' action='/reports/generate/3'>
            <div class='form-section'>
                <label>Select Date Range:</label>
                <div class='date-inputs'>
                    <div>
                        <label for='startDate'>Start Date</label>
                        <input type='date' id='startDate' name='startDate' required>
                    </div>
                    <div>
                        <label for='endDate'>End Date</label>
                        <input type='date' id='endDate' name='endDate' required>
                    </div>
                </div>
            </div>

            <div id='warning' class='warning'>
                <strong>⚠️ Warning:</strong> Please select both start and end dates.
            </div>

            <div style='margin-top: 25px;'>
                <button type='submit' id='generateBtn' class='btn'>📄 Generate PDF Report</button>
                <a href='/reports' class='btn btn-secondary'>Cancel</a>
            </div>
        </form>
    </div>
</body>
</html>";

                return Results.Text(html, "text/html");
            });

            // Report 3 Generation - POST
            app.MapPost("/reports/generate/3", async (HttpContext context, GoogleSheetsService sheetsService, PdfReportService pdfService) =>
            {
                var form = context.Request.Form;
                var startDateStr = form["startDate"].ToString();
                var endDateStr = form["endDate"].ToString();

                if (string.IsNullOrEmpty(startDateStr) || string.IsNullOrEmpty(endDateStr))
                {
                    return Results.Redirect("/reports/configure/3");
                }

                if (!DateTime.TryParse(startDateStr, out var startDate) || !DateTime.TryParse(endDateStr, out var endDate))
                {
                    return Results.Redirect("/reports/configure/3");
                }

                if (startDate > endDate)
                {
                    return Results.Redirect("/reports/configure/3");
                }

                var allRequests = await sheetsService.GetAllRequestsAsync();
                var pdfBytes = pdfService.GenerateCompletedReport(allRequests, startDate, endDate);

                return Results.File(pdfBytes, "application/pdf", $"Workday_Completed_Report_{startDate:yyyyMMdd}_to_{endDate:yyyyMMdd}.pdf");
            });


            app.Run();
        }

        private static string GenerateRequestForm(MaintenanceRequest? request, string title, string action, DropdownValues dropdowns)
        {
            var isEdit = request != null;
            var req = request ?? new MaintenanceRequest();

            return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>{title}</title>
    <style>
        body {{ font-family: Arial, sans-serif; padding: 20px; background: #f5f5f5; }}
        .container {{ max-width: 800px; margin: 0 auto; background: white; padding: 20px; border-radius: 8px; box-shadow: 0 2px 4px rgba(0,0,0,0.1); }}
        h1 {{ color: #333; }}
        .back-link {{ display: inline-block; margin-bottom: 15px; color: #4285f4; text-decoration: none; }}
        .back-link:hover {{ text-decoration: underline; }}
        .form-group {{ margin-bottom: 15px; }}
        .form-group label {{ display: block; font-weight: bold; margin-bottom: 5px; color: #666; }}
        .form-group input, .form-group select, .form-group textarea {{ width: 100%; padding: 10px; border: 1px solid #ddd; border-radius: 4px; box-sizing: border-box; }}
        .form-group textarea {{ min-height: 100px; resize: vertical; }}
        .btn {{ padding: 10px 20px; background: #4285f4; color: white; border: none; border-radius: 4px; cursor: pointer; font-size: 1em; }}
        .btn:hover {{ background: #3367d6; }}
        .btn-secondary {{ background: #666; margin-left: 10px; }}
        .btn-secondary:hover {{ background: #555; }}
    </style>
</head>
<body>
    <div class='container'>
        <a href='/' class='back-link'>← Back to List</a>
        <h1>{title}</h1>

        <form method='post' action='{action}' enctype='multipart/form-data'>
            <div class='form-group'>
                <label>Description *</label>
                <textarea name='description' required>{req.Description}</textarea>
            </div>
            
            <div class='form-group'>
                <label>Requested By *</label>
                <input type='text' name='requestedBy' value='{req.RequestedBy}' required>
            </div>
            
            <div class='form-group'>
                <label>Request Method *</label>
                <select name='requestMethod' required>
                    <option value=''>Select...</option>
                    {GenerateDropdownOptions(dropdowns.RequestMethods, req.RequestMethod)}
                </select>
            </div>
            
            <div class='form-group'>
                <label>Building *</label>
                <select name='building' required>
                    <option value=''>Select...</option>
                    {GenerateDropdownOptions(dropdowns.Buildings, req.Building)}
                </select>
            </div>

            <div class='form-group'>
                <label>Priority *</label>
                <select name='priority' required>
                    <option value=''>Select...</option>
                    {GenerateDropdownOptions(dropdowns.Priorities, req.Priority)}
                </select>
            </div>

            <div class='form-group'>
                <label>Status *</label>
                <select name='status' required>
                    <option value=''>Select...</option>
                    {GenerateDropdownOptions(dropdowns.Statuses, req.Status)}
                </select>
            </div>

            <div class='form-group'>
                <label>Notes</label>
                <textarea name='notes'>{req.Notes}</textarea>
            </div>

            <div class='form-group'>
                <label>Assigned To</label>
                <input type='text' name='assigned' value='{req.Assigned}'>
            </div>
            
            <div class='form-group'>
                <label>Trade</label>
                <select name='trade'>
                    <option value=''>Select...</option>
                    <option value='Electrical'{(req.Trade == "Electrical" ? " selected" : "")}>Electrical</option>
                    <option value='Plumbing'{(req.Trade == "Plumbing" ? " selected" : "")}>Plumbing</option>
                    <option value='General'{(req.Trade == "General" ? " selected" : "")}>General</option>
                    <option value='HVAC'{(req.Trade == "HVAC" ? " selected" : "")}>HVAC</option>
                    <option value='Carpentry'{(req.Trade == "Carpentry" ? " selected" : "")}>Carpentry</option>
                </select>
            </div>
            
            <div class='form-group'>
                <label>Corrective Action</label>
                <textarea name='correctiveAction'>{req.CorrectiveAction}</textarea>
            </div>
            
            <div class='form-group'>
                <label>Due Date</label>
                <input type='date' name='dueDate' value='{(req.DueDate.HasValue ? req.DueDate.Value.ToString("yyyy-MM-dd") : "")}'>
            </div>
            
            {(isEdit ? $@"
            <div class='form-group'>
                <label>Start Date</label>
                <input type='date' name='startDate' value='{(req.StartDate.HasValue ? req.StartDate.Value.ToString("yyyy-MM-dd") : "")}'>
            </div>

            <div class='form-group'>
                <label>Completed Date</label>
                <input type='date' name='completedDate' value='{(req.CompletedDate.HasValue ? req.CompletedDate.Value.ToString("yyyy-MM-dd") : "")}'>
            </div>" : "")}

            <div class='form-group'>
                <label>Attachments (Upload Images)</label>
                <input type='file' name='images' accept='image/*' multiple>
                <small style='color: #666; display: block; margin-top: 5px;'>Upload up to 3 images (5MB each max). On mobile, choose camera or gallery.</small>
                {(isEdit && !string.IsNullOrEmpty(req.Attachments) ? $"<div style='margin-top: 10px;'><strong>Current:</strong> <a href='{req.Attachments}' target='_blank'>View Attachments</a></div>" : "")}
            </div>

            <div>
                <button type='submit' class='btn'>{(isEdit ? "Update" : "Create")} Request</button>
                <a href='/' class='btn btn-secondary'>Cancel</a>
            </div>
        </form>
        <p></p><a href='/' class='back-link'>← Back to List</a>
    </div>
</body>
</html>";
        }

        private static string GenerateReportTable(List<MaintenanceRequest> requests)
        {
            if (requests.Count == 0)
                return "<p>No requests found.</p>";

            var html = @"
            <table>
                <tr>
                    <th>ID</th>
                    <th>Description</th>
                    <th>Status</th>
                    <th>Priority</th>
                    <th>Assigned</th>
                    <th>Due Date</th>
                    <th>Actions</th>
                </tr>";

            foreach (var req in requests)
            {
                html += $@"
                <tr>
                    <td>{req.Id}</td>
                    <td>{TruncateText(req.Description, 40)}</td>
                    <td>{req.Status}</td>
                    <td>{req.Priority}</td>
                    <td>{req.Assigned}</td>
                    <td>{(req.DueDate.HasValue ? req.DueDate.Value.ToString("yyyy-MM-dd") : "")}</td>
                    <td><a href='/request/{req.Id}'>View</a></td>
                </tr>";
            }

            html += "</table>";
            return html;
        }

        private static string TruncateText(string text, int maxLength)
        {
            if (string.IsNullOrEmpty(text) || text.Length <= maxLength)
                return text;

            return text.Substring(0, maxLength) + "...";
        }

        private static string GenerateAttachmentsHtml(string attachments)
        {
            if (string.IsNullOrEmpty(attachments))
                return "None";

            var links = attachments.Split(',').Select(l => l.Trim()).Where(l => !string.IsNullOrEmpty(l)).ToList();

            if (links.Count == 0)
                return "None";

            var html = "<div>";
            for (int i = 0; i < links.Count; i++)
            {
                html += $"<div style='margin-bottom: 8px;'><a href='{links[i]}' target='_blank' style='color: #4285f4;'>📷 Image {i + 1}</a></div>";
            }
            html += "</div>";

            return html;
        }

        private static string GenerateDropdownOptions(List<string> options, string selectedValue)
        {
            var html = "";
            foreach (var option in options)
            {
                var selected = option == selectedValue ? " selected" : "";
                html += $"<option value='{option}'{selected}>{option}</option>";
            }
            return html;
        }

        private static string GenerateDropdownOptionsWithSelected(List<string> options, string selectedValue)
        {
            var html = "";
            foreach (var option in options)
            {
                var selected = option == selectedValue ? " selected" : "";
                html += $"<option value='{option}'{selected}>{option}</option>";
            }
            return html;
        }

        private static string GenerateStatusCheckboxes(List<string> statuses, string[] selectedStatuses)
        {
            var html = "";
            foreach (var status in statuses)
            {
                // If no filters selected (empty array), check all boxes by default
                // Otherwise, only check the selected ones
                var isChecked = selectedStatuses.Length == 0 || selectedStatuses.Contains(status, StringComparer.OrdinalIgnoreCase);
                var checkedAttr = isChecked ? " checked" : "";

                html += $@"
                        <label>
                            <input type='checkbox' name='status' value='{status}'{checkedAttr} onchange='this.form.submit()'>
                            {status}
                        </label>";
            }
            return html;
        }

        private static string GenerateRequestorForm(DropdownValues dropdowns)
        {
            return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>Submit Maintenance Request</title>
    <style>
        body {{ font-family: Arial, sans-serif; padding: 20px; background: #f5f5f5; }}
        .container {{ max-width: 700px; margin: 0 auto; background: white; padding: 30px; border-radius: 8px; box-shadow: 0 2px 4px rgba(0,0,0,0.1); }}
        h1 {{ color: #333; margin-top: 0; }}
        .info {{ background: #e3f2fd; padding: 15px; border-radius: 4px; margin-bottom: 20px; color: #1565c0; }}
        .form-group {{ margin-bottom: 20px; }}
        .form-group label {{ display: block; font-weight: bold; margin-bottom: 5px; color: #666; }}
        .form-group input, .form-group select, .form-group textarea {{ width: 100%; padding: 12px; border: 1px solid #ddd; border-radius: 4px; box-sizing: border-box; font-size: 1em; }}
        .form-group textarea {{ min-height: 120px; resize: vertical; }}
        .btn {{ padding: 12px 30px; background: #34a853; color: white; border: none; border-radius: 4px; cursor: pointer; font-size: 1em; font-weight: bold; }}
        .btn:hover {{ background: #2d8e47; }}
        small {{ color: #666; display: block; margin-top: 5px; }}
    </style>
</head>
<body>
    <div class='container'>
        <h1>🛠️ Submit Maintenance Request</h1>
        <div class='info'>
            <strong>Instructions:</strong> Please describe the maintenance issue and we'll review it promptly.
        </div>

        <form method='post' action='/requestor/create' enctype='multipart/form-data'>
            <div class='form-group'>
                <label>What needs to be fixed? *</label>
                <textarea name='description' required placeholder='Describe the maintenance issue in detail...'></textarea>
            </div>

            <div class='form-group'>
                <label>Your Name *</label>
                <input type='text' name='requestedBy' required placeholder='Enter your name'>
            </div>

            <div class='form-group'>
                <label>Building Location *</label>
                <select name='building' required>
                    <option value=''>Select a building...</option>
                    {GenerateDropdownOptions(dropdowns.Buildings, "")}
                </select>
            </div>

            <div class='form-group'>
                <label>Attach Photos (Optional)</label>
                <input type='file' name='images' accept='image/*' multiple>
                <small>You can upload up to 3 photos (5MB each max)</small>
            </div>

            <div>
                <button type='submit' class='btn'>Submit Request</button>
            </div>
        </form>
    </div>
</body>
</html>";
        }

        private static string GenerateProxyForm(DropdownValues dropdowns)
        {
            return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>Submit Request (Proxy)</title>
    <style>
        body {{ font-family: Arial, sans-serif; padding: 20px; background: #f5f5f5; }}
        .container {{ max-width: 700px; margin: 0 auto; background: white; padding: 30px; border-radius: 8px; box-shadow: 0 2px 4px rgba(0,0,0,0.1); }}
        h1 {{ color: #333; margin-top: 0; }}
        .info {{ background: #fff3cd; padding: 15px; border-radius: 4px; margin-bottom: 20px; color: #856404; }}
        .form-group {{ margin-bottom: 20px; }}
        .form-group label {{ display: block; font-weight: bold; margin-bottom: 5px; color: #666; }}
        .form-group input, .form-group select, .form-group textarea {{ width: 100%; padding: 12px; border: 1px solid #ddd; border-radius: 4px; box-sizing: border-box; font-size: 1em; }}
        .form-group textarea {{ min-height: 120px; resize: vertical; }}
        .btn {{ padding: 12px 30px; background: #4285f4; color: white; border: none; border-radius: 4px; cursor: pointer; font-size: 1em; font-weight: bold; }}
        .btn:hover {{ background: #3367d6; }}
        small {{ color: #666; display: block; margin-top: 5px; }}
    </style>
</head>
<body>
    <div class='container'>
        <h1>📝 Submit Request (Proxy)</h1>
        <div class='info'>
            <strong>Proxy Mode:</strong> You are entering a request on behalf of someone else.
        </div>

        <form method='post' action='/proxy/create' enctype='multipart/form-data'>
            <div class='form-group'>
                <label>Maintenance Issue Description *</label>
                <textarea name='description' required placeholder='Describe the maintenance issue...'></textarea>
            </div>

            <div class='form-group'>
                <label>Requested By (Person's Name) *</label>
                <input type='text' name='requestedBy' required placeholder='Enter the requestor name'>
            </div>

            <div class='form-group'>
                <label>Request Method *</label>
                <select name='requestMethod' required>
                    <option value=''>How was this request received?</option>
                    {GenerateDropdownOptions(dropdowns.RequestMethods, "")}
                </select>
            </div>

            <div class='form-group'>
                <label>Building Location *</label>
                <select name='building' required>
                    <option value=''>Select a building...</option>
                    {GenerateDropdownOptions(dropdowns.Buildings, "")}
                </select>
            </div>

            <div class='form-group'>
                <label>Attach Photos (Optional)</label>
                <input type='file' name='images' accept='image/*' multiple>
                <small>You can upload up to 3 photos (5MB each max)</small>
            </div>

            <div>
                <button type='submit' class='btn'>Submit Request</button>
            </div>
        </form>
    </div>
</body>
</html>";
        }
    }
}
