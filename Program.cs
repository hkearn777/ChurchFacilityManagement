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
            builder.Services.AddSingleton<GoogleDriveService>();

            var app = builder.Build();

            // Home page - List all requests
            app.MapGet("/", async (GoogleSheetsService sheetsService, HttpContext context) =>
            {
                var requests = await sheetsService.GetAllRequestsAsync();
                var dropdowns = await sheetsService.GetDropdownValuesAsync();

                var filterStatus = context.Request.Query["status"].ToString();
                var filterPriority = context.Request.Query["priority"].ToString();
                var filterBuilding = context.Request.Query["building"].ToString();
                var searchText = context.Request.Query["search"].ToString();

                if (!string.IsNullOrEmpty(filterStatus))
                    requests = requests.Where(r => r.Status.Equals(filterStatus, StringComparison.OrdinalIgnoreCase)).ToList();
                
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
        .table-wrapper { overflow-x: auto; }
        table { border-collapse: collapse; width: 100%; }
        th { background: #4285f4; color: white; padding: 12px 8px; text-align: left; font-size: 0.85em; }
        td { padding: 10px 8px; border: 1px solid #ddd; font-size: 0.85em; }
        tr:hover { background: #f8f9fa; }
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
            </div>
        </div>

        <div class='filters'>
            <form method='get'>
                <div class='filter-group'>
                    <label>Status:</label>
                    <select name='status' onchange='this.form.submit()'>
                        <option value=''>All</option>
                        " + GenerateDropdownOptionsWithSelected(dropdowns.Statuses, filterStatus) + @"
                    </select>

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
                    foreach (var req in requests.OrderByDescending(r => r.ReportDate))
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

                        html += $@"
                <tr>
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
    </div>
</body>
</html>";

                return Results.Text(html, "text/html");
            });

            // New request form
            app.MapGet("/request/new", async (GoogleSheetsService sheetsService) =>
            {
                var dropdowns = await sheetsService.GetDropdownValuesAsync();
                var html = GenerateRequestForm(null, "Create New Request", "/request/create", dropdowns);
                return Results.Text(html, "text/html");
            });

            // Create request
            app.MapPost("/request/create", async (HttpContext context, GoogleSheetsService sheetsService, GoogleDriveService driveService) =>
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
                    Assigned = form["assigned"].ToString(),
                    Trade = form["trade"].ToString(),
                    CorrectiveAction = form["correctiveAction"].ToString(),
                    DueDate = DateTime.TryParse(form["dueDate"].ToString(), out var dueDate) ? dueDate : null,
                    Attachments = ""
                };

                var newId = await sheetsService.CreateRequestAsync(request);

                var files = form.Files.GetFiles("images");
                if (files.Count > 0)
                {
                    var validFiles = files.Take(3).Where(f => f.Length <= 5 * 1024 * 1024).ToList();

                    if (validFiles.Count > 0)
                    {
                        var links = await driveService.UploadMultipleImagesAsync(newId, validFiles);

                        if (links.Count > 0)
                        {
                            request.Id = newId;
                            request.Attachments = string.Join(", ", links);
                            await sheetsService.UpdateRequestAsync(request);
                        }
                    }
                }

                return Results.Redirect("/");
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
            app.MapPost("/request/{id}/update", async (int id, HttpContext context, GoogleSheetsService sheetsService, GoogleDriveService driveService) =>
            {
                var request = await sheetsService.GetRequestByIdAsync(id);
                if (request == null)
                    return Results.Redirect("/");

                var form = context.Request.Form;

                request.Description = form["description"].ToString();
                request.RequestedBy = form["requestedBy"].ToString();
                request.RequestMethod = form["requestMethod"].ToString();
                request.Building = form["building"].ToString();
                request.Priority = form["priority"].ToString();
                request.Status = form["status"].ToString();
                request.Assigned = form["assigned"].ToString();
                request.Trade = form["trade"].ToString();
                request.CorrectiveAction = form["correctiveAction"].ToString();
                request.DueDate = DateTime.TryParse(form["dueDate"].ToString(), out var dueDate) ? dueDate : null;
                request.StartDate = DateTime.TryParse(form["startDate"].ToString(), out var startDate) ? startDate : null;
                request.CompletedDate = DateTime.TryParse(form["completedDate"].ToString(), out var completedDate) ? completedDate : null;

                var files = form.Files.GetFiles("images");
                if (files.Count > 0)
                {
                    var validFiles = files.Take(3).Where(f => f.Length <= 5 * 1024 * 1024).ToList();

                    if (validFiles.Count > 0)
                    {
                        var links = await driveService.UploadMultipleImagesAsync(id, validFiles);

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
        .report-section {{ margin-bottom: 30px; }}
        .report-section h2 {{ color: #666; border-bottom: 2px solid #4285f4; padding-bottom: 10px; }}
        .count {{ font-size: 2em; font-weight: bold; color: #4285f4; }}
        .report-grid {{ display: grid; grid-template-columns: repeat(auto-fit, minmax(300px, 1fr)); gap: 20px; margin-top: 20px; }}
        .report-card {{ background: #f8f9fa; padding: 15px; border-radius: 8px; border-left: 4px solid #4285f4; }}
        .report-card h3 {{ margin-top: 0; color: #333; }}
        .report-card.overdue {{ border-left-color: #d93025; }}
        .report-card.started {{ border-left-color: #fbbc04; }}
        .report-card.not-started {{ border-left-color: #34a853; }}
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
    </div>
</body>
</html>";

                return Results.Text(html, "text/html");
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
                    <option value='Inspection'{(req.RequestMethod == "Inspection" ? " selected" : "")}>Inspection</option>
                    <option value='Verbal'{(req.RequestMethod == "Verbal" ? " selected" : "")}>Verbal</option>
                    <option value='Email'{(req.RequestMethod == "Email" ? " selected" : "")}>Email</option>
                    <option value='Proxy'{(req.RequestMethod == "Proxy" ? " selected" : "")}>Proxy</option>
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
                <input type='file' name='images' accept='image/*' capture='environment' multiple>
                <small style='color: #666; display: block; margin-top: 5px;'>Upload up to 3 images (5MB each max). On mobile, use camera to capture photos.</small>
                {(isEdit && !string.IsNullOrEmpty(req.Attachments) ? $"<div style='margin-top: 10px;'><strong>Current:</strong> <a href='{req.Attachments}' target='_blank'>View Attachments</a></div>" : "")}
            </div>
            
            <div>
                <button type='submit' class='btn'>{(isEdit ? "Update" : "Create")} Request</button>
                <a href='/' class='btn btn-secondary'>Cancel</a>
            </div>
        </form>
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
    }
}
