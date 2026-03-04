using Google.Apis.Auth.OAuth2;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;
using Google.Apis.Services;
using ChurchFacilityManagement.Models;

namespace ChurchFacilityManagement.Services
{
    public class GoogleSheetsService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<GoogleSheetsService> _logger;
        private SheetsService? _sheetsService;

        private const string TASKS_SHEET = "Tasks";
        private const string COMPLETED_SHEET = "Completed Tasks";
        private const string ROLES_SHEET = "Roles";
        private const string DROPDOWNS_SHEET = "Dropdowns";

        public GoogleSheetsService(IConfiguration configuration, ILogger<GoogleSheetsService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        private async Task<SheetsService> GetSheetsServiceAsync()
        {
            if (_sheetsService != null)
                return _sheetsService;

            string jsonString;
            
            var credentialsJson = Environment.GetEnvironmentVariable("GOOGLE_CREDENTIALS_JSON");
            
            if (!string.IsNullOrEmpty(credentialsJson))
            {
                jsonString = credentialsJson;
                _logger.LogInformation("Using credentials from environment variable");
            }
            else
            {
                var credentialsPath = _configuration["GoogleSheets:CredentialsPath"] ?? "credentials.json";
                jsonString = await File.ReadAllTextAsync(credentialsPath);
                _logger.LogInformation("Using credentials from file");
            }

#pragma warning disable CS0618
            var credential = GoogleCredential.FromJson(jsonString)
                .CreateScoped(SheetsService.Scope.Spreadsheets);
#pragma warning restore CS0618

            _sheetsService = new SheetsService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = _configuration["GoogleSheets:ApplicationName"] ?? "Church Facility Management",
            });

            return _sheetsService;
        }

        public async Task<List<MaintenanceRequest>> GetAllRequestsAsync()
        {
            var service = await GetSheetsServiceAsync();
            var spreadsheetId = _configuration["GoogleSheets:SpreadsheetId"];
            var range = $"{TASKS_SHEET}!A2:O";

            try
            {
                var request = service.Spreadsheets.Values.Get(spreadsheetId, range);
                var response = await request.ExecuteAsync();
                var values = response.Values;

                var requests = new List<MaintenanceRequest>();

                if (values != null && values.Count > 0)
                {
                    int rowNumber = 2;
                    foreach (var row in values)
                    {
                        requests.Add(ParseRowToRequest(row, rowNumber));
                        rowNumber++;
                    }
                }

                return requests;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reading maintenance requests");
                return new List<MaintenanceRequest>();
            }
        }

        public async Task<MaintenanceRequest?> GetRequestByIdAsync(int id)
        {
            var requests = await GetAllRequestsAsync();
            return requests.FirstOrDefault(r => r.Id == id);
        }

        public async Task<int> GetNextIdAsync()
        {
            var requests = await GetAllRequestsAsync();
            return requests.Any() ? requests.Max(r => r.Id) + 1 : 1;
        }

        public async Task<int> CreateRequestAsync(MaintenanceRequest request)
        {
            var service = await GetSheetsServiceAsync();
            var spreadsheetId = _configuration["GoogleSheets:SpreadsheetId"];

            try
            {
                request.Id = await GetNextIdAsync();
                request.ReportDate = DateTime.Now;

                var range = $"{TASKS_SHEET}!A:O";
                var valueRange = new ValueRange
                {
                    Values = new List<IList<object>>
                    {
                        new List<object>
                        {
                            request.Id,
                            request.ReportDate.ToString("yyyy-MM-dd"),
                            request.Description,
                            request.RequestedBy,
                            request.RequestMethod,
                            request.Building,
                            request.Priority,
                            request.Status,
                            request.Assigned,
                            request.Trade,
                            request.CorrectiveAction,
                            request.DueDate?.ToString("yyyy-MM-dd") ?? "",
                            request.StartDate?.ToString("yyyy-MM-dd") ?? "",
                            request.CompletedDate?.ToString("yyyy-MM-dd") ?? "",
                            request.Attachments
                        }
                    }
                };

                var appendRequest = service.Spreadsheets.Values.Append(valueRange, spreadsheetId, range);
                appendRequest.ValueInputOption = SpreadsheetsResource.ValuesResource.AppendRequest.ValueInputOptionEnum.RAW;
                await appendRequest.ExecuteAsync();

                _logger.LogInformation($"Created new request with ID: {request.Id}");
                return request.Id;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating maintenance request");
                return 0;
            }
        }

        public async Task<bool> UpdateRequestAsync(MaintenanceRequest request)
        {
            var service = await GetSheetsServiceAsync();
            var spreadsheetId = _configuration["GoogleSheets:SpreadsheetId"];

            try
            {
                var range = $"{TASKS_SHEET}!A{request.RowNumber}:O{request.RowNumber}";
                var valueRange = new ValueRange
                {
                    Values = new List<IList<object>>
                    {
                        new List<object>
                        {
                            request.Id,
                            request.ReportDate.ToString("yyyy-MM-dd"),
                            request.Description,
                            request.RequestedBy,
                            request.RequestMethod,
                            request.Building,
                            request.Priority,
                            request.Status,
                            request.Assigned,
                            request.Trade,
                            request.CorrectiveAction,
                            request.DueDate?.ToString("yyyy-MM-dd") ?? "",
                            request.StartDate?.ToString("yyyy-MM-dd") ?? "",
                            request.CompletedDate?.ToString("yyyy-MM-dd") ?? "",
                            request.Attachments
                        }
                    }
                };

                var updateRequest = service.Spreadsheets.Values.Update(valueRange, spreadsheetId, range);
                updateRequest.ValueInputOption = SpreadsheetsResource.ValuesResource.UpdateRequest.ValueInputOptionEnum.RAW;
                await updateRequest.ExecuteAsync();

                _logger.LogInformation($"Updated request ID: {request.Id}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating request ID: {request.Id}");
                return false;
            }
        }

        public async Task<bool> DeleteRequestAsync(int id)
        {
            var service = await GetSheetsServiceAsync();
            var spreadsheetId = _configuration["GoogleSheets:SpreadsheetId"];

            try
            {
                var request = await GetRequestByIdAsync(id);
                if (request == null)
                    return false;

                var deleteRequest = new BatchUpdateSpreadsheetRequest
                {
                    Requests = new List<Request>
                    {
                        new Request
                        {
                            DeleteDimension = new DeleteDimensionRequest
                            {
                                Range = new DimensionRange
                                {
                                    SheetId = await GetSheetIdAsync(TASKS_SHEET),
                                    Dimension = "ROWS",
                                    StartIndex = request.RowNumber - 1,
                                    EndIndex = request.RowNumber
                                }
                            }
                        }
                    }
                };

                await service.Spreadsheets.BatchUpdate(deleteRequest, spreadsheetId).ExecuteAsync();

                _logger.LogInformation($"Deleted request ID: {id}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting request ID: {id}");
                return false;
            }
        }

        public async Task<bool> MoveToCompletedAsync(int id)
        {
            var request = await GetRequestByIdAsync(id);
            if (request == null)
                return false;

            var service = await GetSheetsServiceAsync();
            var spreadsheetId = _configuration["GoogleSheets:SpreadsheetId"];

            try
            {
                var range = $"{COMPLETED_SHEET}!A:O";
                var valueRange = new ValueRange
                {
                    Values = new List<IList<object>>
                    {
                        new List<object>
                        {
                            request.Id,
                            request.ReportDate.ToString("yyyy-MM-dd"),
                            request.Description,
                            request.RequestedBy,
                            request.RequestMethod,
                            request.Building,
                            request.Priority,
                            request.Status,
                            request.Assigned,
                            request.Trade,
                            request.CorrectiveAction,
                            request.DueDate?.ToString("yyyy-MM-dd") ?? "",
                            request.StartDate?.ToString("yyyy-MM-dd") ?? "",
                            request.CompletedDate?.ToString("yyyy-MM-dd") ?? "",
                            request.Attachments
                        }
                    }
                };

                var appendRequest = service.Spreadsheets.Values.Append(valueRange, spreadsheetId, range);
                appendRequest.ValueInputOption = SpreadsheetsResource.ValuesResource.AppendRequest.ValueInputOptionEnum.RAW;
                await appendRequest.ExecuteAsync();

                await DeleteRequestAsync(id);

                _logger.LogInformation($"Moved request ID {id} to completed");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error moving request ID {id} to completed");
                return false;
            }
        }

        public async Task<List<Role>> GetRolesAsync()
        {
            var service = await GetSheetsServiceAsync();
            var spreadsheetId = _configuration["GoogleSheets:SpreadsheetId"];
            var range = $"{ROLES_SHEET}!A2:C";

            try
            {
                var request = service.Spreadsheets.Values.Get(spreadsheetId, range);
                var response = await request.ExecuteAsync();
                var values = response.Values;

                var roles = new List<Role>();

                if (values != null && values.Count > 0)
                {
                    foreach (var row in values)
                    {
                        roles.Add(new Role
                        {
                            RoleName = row.Count > 0 ? row[0].ToString() ?? "" : "",
                            PersonName = row.Count > 1 ? row[1].ToString() ?? "" : "",
                            Contact = row.Count > 2 ? row[2].ToString() ?? "" : ""
                        });
                    }
                }

                return roles;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reading roles");
                return new List<Role>();
            }
        }

        public async Task<DropdownValues> GetDropdownValuesAsync()
        {
            var service = await GetSheetsServiceAsync();
            var spreadsheetId = _configuration["GoogleSheets:SpreadsheetId"];
            var range = $"{DROPDOWNS_SHEET}!A2:C";

            var dropdowns = new DropdownValues();

            try
            {
                var request = service.Spreadsheets.Values.Get(spreadsheetId, range);
                var response = await request.ExecuteAsync();
                var values = response.Values;

                if (values != null && values.Count > 0)
                {
                    foreach (var row in values)
                    {
                        if (row.Count > 0 && !string.IsNullOrWhiteSpace(row[0].ToString()))
                            dropdowns.Buildings.Add(row[0].ToString()!.Trim());

                        if (row.Count > 1 && !string.IsNullOrWhiteSpace(row[1].ToString()))
                            dropdowns.Priorities.Add(row[1].ToString()!.Trim());

                        if (row.Count > 2 && !string.IsNullOrWhiteSpace(row[2].ToString()))
                            dropdowns.Statuses.Add(row[2].ToString()!.Trim());
                    }
                }

                _logger.LogInformation($"Loaded dropdowns: {dropdowns.Buildings.Count} buildings, {dropdowns.Priorities.Count} priorities, {dropdowns.Statuses.Count} statuses");
                return dropdowns;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reading dropdown values");
                return dropdowns;
            }
        }

        private async Task<int> GetSheetIdAsync(string sheetName)
        {
            var service = await GetSheetsServiceAsync();
            var spreadsheetId = _configuration["GoogleSheets:SpreadsheetId"];

            var spreadsheet = await service.Spreadsheets.Get(spreadsheetId).ExecuteAsync();
            var sheet = spreadsheet.Sheets.FirstOrDefault(s => s.Properties.Title == sheetName);

            return sheet?.Properties.SheetId ?? 0;
        }

        private MaintenanceRequest ParseRowToRequest(IList<object> row, int rowNumber)
        {
            return new MaintenanceRequest
            {
                Id = int.TryParse(row.Count > 0 ? row[0].ToString() : "0", out var id) ? id : 0,
                ReportDate = DateTime.TryParse(row.Count > 1 ? row[1].ToString() : "", out var reportDate) ? reportDate : DateTime.Now,
                Description = row.Count > 2 ? row[2].ToString() ?? "" : "",
                RequestedBy = row.Count > 3 ? row[3].ToString() ?? "" : "",
                RequestMethod = row.Count > 4 ? row[4].ToString() ?? "" : "",
                Building = row.Count > 5 ? row[5].ToString() ?? "" : "",
                Priority = row.Count > 6 ? row[6].ToString() ?? "" : "",
                Status = row.Count > 7 ? row[7].ToString() ?? "" : "",
                Assigned = row.Count > 8 ? row[8].ToString() ?? "" : "",
                Trade = row.Count > 9 ? row[9].ToString() ?? "" : "",
                CorrectiveAction = row.Count > 10 ? row[10].ToString() ?? "" : "",
                DueDate = DateTime.TryParse(row.Count > 11 ? row[11].ToString() : "", out var dueDate) ? dueDate : null,
                StartDate = DateTime.TryParse(row.Count > 12 ? row[12].ToString() : "", out var startDate) ? startDate : null,
                CompletedDate = DateTime.TryParse(row.Count > 13 ? row[13].ToString() : "", out var completedDate) ? completedDate : null,
                Attachments = row.Count > 14 ? row[14].ToString() ?? "" : "",
                RowNumber = rowNumber
            };
        }
    }
}
