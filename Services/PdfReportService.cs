using ChurchFacilityManagement.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace ChurchFacilityManagement.Services
{
    public class PdfReportService
    {
        public byte[] GenerateReportByStatus(List<MaintenanceRequest> requests)
        {
            QuestPDF.Settings.License = LicenseType.Community;

            // Group all requests by status, ordered by report date (age)
            var groupedByStatus = requests
                .OrderBy(r => r.ReportDate)
                .GroupBy(r => r.Status)
                .OrderBy(g => g.Key);

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.Letter);
                    page.Margin(0.5f, Unit.Inch);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(10).FontFamily("Arial"));

                    page.Header()
                        .Height(0.75f, Unit.Inch)
                        .AlignCenter()
                        .Column(column =>
                        {
                            column.Item().Text("Church Facility Management")
                                .FontSize(16)
                                .Bold()
                                .FontColor(Colors.Blue.Darken2);

                            column.Item().Text("Workday Report - Requests Grouped by Status")
                                .FontSize(12)
                                .SemiBold();

                            column.Item().Text($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm}")
                                .FontSize(9)
                                .FontColor(Colors.Grey.Darken1);
                        });

                    page.Content()
                        .Column(column =>
                        {
                            foreach (var statusGroup in groupedByStatus)
                            {
                                column.Item().PaddingTop(0.2f, Unit.Inch).Column(statusColumn =>
                                {
                                    statusColumn.Item().Text(statusGroup.Key)
                                        .FontSize(12)
                                        .Bold()
                                        .FontColor(Colors.Blue.Darken1);

                                    statusColumn.Item().PaddingTop(5).Table(table =>
                                    {
                                        table.ColumnsDefinition(columns =>
                                        {
                                            columns.RelativeColumn(2); // Status
                                            columns.RelativeColumn(2); // Building
                                            columns.RelativeColumn(1.5f); // Priority
                                            columns.RelativeColumn(2); // Assigned To
                                            columns.RelativeColumn(5); // Description
                                        });

                                        // Header
                                        table.Header(header =>
                                        {
                                            header.Cell().Background(Colors.Blue.Lighten2).Padding(5).Text("Status").Bold();
                                            header.Cell().Background(Colors.Blue.Lighten2).Padding(5).Text("Building").Bold();
                                            header.Cell().Background(Colors.Blue.Lighten2).Padding(5).Text("Priority").Bold();
                                            header.Cell().Background(Colors.Blue.Lighten2).Padding(5).Text("Assigned To").Bold();
                                            header.Cell().Background(Colors.Blue.Lighten2).Padding(5).Text("Description").Bold();
                                        });

                                        // Data rows
                                        foreach (var request in statusGroup)
                                        {
                                            var priorityText = request.Priority switch
                                            {
                                                "1" => "High",
                                                "2" => "Normal",
                                                "3" => "Low",
                                                _ => request.Priority
                                            };

                                            table.Cell().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(request.Status);
                                            table.Cell().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(request.Building);
                                            table.Cell().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(priorityText);
                                            table.Cell().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(request.Assigned);
                                            table.Cell().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(request.Description);
                                        }
                                    });
                                });
                            }

                            if (!groupedByStatus.Any())
                            {
                                column.Item().PaddingTop(50).AlignCenter().Text("No requests found.")
                                    .FontSize(12)
                                    .FontColor(Colors.Grey.Darken1);
                            }
                        });

                    page.Footer()
                        .Height(0.5f, Unit.Inch)
                        .AlignCenter()
                        .Text(text =>
                        {
                            text.Span("Page ");
                            text.CurrentPageNumber();
                            text.Span(" of ");
                            text.TotalPages();
                        });
                });
            });

            return document.GeneratePdf();
        }

        public byte[] GenerateReportByStatusFiltered(List<MaintenanceRequest> requests, List<string> selectedStatuses)
        {
            QuestPDF.Settings.License = LicenseType.Community;

            // Filter by selected statuses and group, ordered by report date (age)
            var groupedByStatus = requests
                .Where(r => selectedStatuses.Contains(r.Status, StringComparer.OrdinalIgnoreCase))
                .OrderBy(r => r.ReportDate)
                .GroupBy(r => r.Status)
                .OrderBy(g => g.Key);

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.Letter);
                    page.Margin(0.5f, Unit.Inch);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(10).FontFamily("Arial"));

                    page.Header()
                        .Height(0.75f, Unit.Inch)
                        .AlignCenter()
                        .Column(column =>
                        {
                            column.Item().Text("Church Facility Management")
                                .FontSize(16)
                                .Bold()
                                .FontColor(Colors.Blue.Darken2);

                            column.Item().Text("Workday Report - By Status (Filtered)")
                                .FontSize(12)
                                .SemiBold();

                            column.Item().Text($"Statuses: {string.Join(", ", selectedStatuses)}")
                                .FontSize(9)
                                .FontColor(Colors.Grey.Darken2);

                            column.Item().Text($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm}")
                                .FontSize(9)
                                .FontColor(Colors.Grey.Darken1);
                        });

                    page.Content()
                        .Column(column =>
                        {
                            foreach (var statusGroup in groupedByStatus)
                            {
                                column.Item().PaddingTop(0.2f, Unit.Inch).Column(statusColumn =>
                                {
                                    statusColumn.Item().Text(statusGroup.Key)
                                        .FontSize(12)
                                        .Bold()
                                        .FontColor(Colors.Blue.Darken1);

                                    statusColumn.Item().PaddingTop(5).Table(table =>
                                    {
                                        table.ColumnsDefinition(columns =>
                                        {
                                            columns.RelativeColumn(2); // Status
                                            columns.RelativeColumn(2); // Building
                                            columns.RelativeColumn(1.5f); // Priority
                                            columns.RelativeColumn(2); // Assigned To
                                            columns.RelativeColumn(5); // Description
                                        });

                                        // Header
                                        table.Header(header =>
                                        {
                                            header.Cell().Background(Colors.Blue.Lighten2).Padding(5).Text("Status").Bold();
                                            header.Cell().Background(Colors.Blue.Lighten2).Padding(5).Text("Building").Bold();
                                            header.Cell().Background(Colors.Blue.Lighten2).Padding(5).Text("Priority").Bold();
                                            header.Cell().Background(Colors.Blue.Lighten2).Padding(5).Text("Assigned To").Bold();
                                            header.Cell().Background(Colors.Blue.Lighten2).Padding(5).Text("Description").Bold();
                                        });

                                        // Data rows
                                        foreach (var request in statusGroup)
                                        {
                                            var priorityText = request.Priority switch
                                            {
                                                "1" => "High",
                                                "2" => "Normal",
                                                "3" => "Low",
                                                _ => request.Priority
                                            };

                                            table.Cell().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(request.Status);
                                            table.Cell().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(request.Building);
                                            table.Cell().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(priorityText);
                                            table.Cell().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(request.Assigned);
                                            table.Cell().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(request.Description);
                                        }
                                    });
                                });
                            }

                            if (!groupedByStatus.Any())
                            {
                                column.Item().PaddingTop(50).AlignCenter().Text("No requests found with selected statuses.")
                                    .FontSize(12)
                                    .FontColor(Colors.Grey.Darken1);
                            }
                        });

                    page.Footer()
                        .Height(0.5f, Unit.Inch)
                        .AlignCenter()
                        .Text(text =>
                        {
                            text.Span("Page ");
                            text.CurrentPageNumber();
                            text.Span(" of ");
                            text.TotalPages();
                        });
                });
            });

            return document.GeneratePdf();
        }
    }
}
