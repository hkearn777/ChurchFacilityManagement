using System.Net;
using System.Net.Mail;

namespace ChurchFacilityManagement.Services
{
    public class EmailService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<bool> SendApprovalNotificationAsync(int requestId, string description, string requestedBy)
        {
            try
            {
                var smtpHost = _configuration["Email:SmtpHost"];
                var smtpPort = int.Parse(_configuration["Email:SmtpPort"] ?? "587");
                var fromEmail = _configuration["Email:FromEmail"];
                var fromPassword = _configuration["Email:FromPassword"];
                var replyToEmail = _configuration["Email:ReplyToEmail"];
                var toEmail = _configuration["Email:ApproverEmail"];
                var approverPageUrl = _configuration["Email:ApproverPageUrl"];

                if (string.IsNullOrEmpty(fromEmail) || string.IsNullOrEmpty(fromPassword) || string.IsNullOrEmpty(toEmail))
                {
                    _logger.LogWarning("Email configuration is incomplete. Skipping email notification.");
                    return false;
                }

                using var smtpClient = new SmtpClient(smtpHost)
                {
                    Port = smtpPort,
                    Credentials = new NetworkCredential(fromEmail, fromPassword),
                    EnableSsl = true
                };

                var mailMessage = new MailMessage
                {
                    From = new MailAddress(fromEmail, "Church Facilities Management"),
                    Subject = $"Approval Needed: Request #{requestId}",
                    Body = GenerateEmailBody(requestId, description, requestedBy, approverPageUrl),
                    IsBodyHtml = true
                };

                mailMessage.To.Add(toEmail);

                // Set Reply-To so responses go to Brian's monitored email
                if (!string.IsNullOrEmpty(replyToEmail))
                {
                    mailMessage.ReplyToList.Add(new MailAddress(replyToEmail, "Facilities Manager"));
                }

                await smtpClient.SendMailAsync(mailMessage);
                _logger.LogInformation($"Approval email sent for request #{requestId} to {toEmail}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to send approval email for request #{requestId}");
                return false;
            }
        }

        private string GenerateEmailBody(int requestId, string description, string requestedBy, string approverPageUrl)
        {
            return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
</head>
<body style='font-family: Arial, sans-serif; line-height: 1.6; color: #333; max-width: 600px; margin: 0 auto; padding: 20px;'>
    <div style='background: #4285f4; color: white; padding: 20px; border-radius: 8px 8px 0 0;'>
        <h1 style='margin: 0; font-size: 1.5em;'>Approval Required</h1>
    </div>

    <div style='background: #f8f9fa; padding: 20px; border: 1px solid #ddd; border-top: none; border-radius: 0 0 8px 8px;'>
        <p style='font-size: 1.1em; margin-top: 0;'>A maintenance request needs your approval.</p>

        <div style='background: white; padding: 15px; border-radius: 4px; margin: 15px 0;'>
            <p><strong>Request ID:</strong> #{requestId}</p>
            <p><strong>Description:</strong> {System.Web.HttpUtility.HtmlEncode(description)}</p>
            <p><strong>Requested By:</strong> {System.Web.HttpUtility.HtmlEncode(requestedBy)}</p>
        </div>

        <div style='text-align: center; margin: 25px 0;'>
            <a href='{approverPageUrl}' style='display: inline-block; padding: 12px 30px; background: #34a853; color: white; text-decoration: none; border-radius: 4px; font-weight: bold; font-size: 1.1em;'>
                Review and Approve
            </a>
        </div>

        <p style='font-size: 0.9em; color: #666; margin-top: 20px;'>
            Click the button above to view all pending approvals and take action.
        </p>
    </div>

    <div style='text-align: center; font-size: 0.8em; color: #999; margin-top: 20px;'>
        <p>Church Facility Management System</p>
    </div>
</body>
</html>";
        }

        public async Task<bool> SendImageUploadErrorNotificationAsync(int requestId, string description, string requestedBy, string errorMessage, string managerEmail)
        {
            try
            {
                var smtpHost = _configuration["Email:SmtpHost"];
                var smtpPort = int.Parse(_configuration["Email:SmtpPort"] ?? "587");
                var fromEmail = _configuration["Email:FromEmail"];
                var fromPassword = _configuration["Email:FromPassword"];
                var replyToEmail = _configuration["Email:ReplyToEmail"];

                // Check for password in environment variable (for Cloud Run)
                if (string.IsNullOrEmpty(fromPassword))
                {
                    fromPassword = Environment.GetEnvironmentVariable("Email__FromPassword");
                }

                if (string.IsNullOrEmpty(fromEmail) || string.IsNullOrEmpty(fromPassword) || string.IsNullOrEmpty(managerEmail))
                {
                    _logger.LogWarning("Email configuration is incomplete. Skipping error notification.");
                    return false;
                }

                using var smtpClient = new SmtpClient(smtpHost)
                {
                    Port = smtpPort,
                    Credentials = new NetworkCredential(fromEmail, fromPassword),
                    EnableSsl = true
                };

                var mailMessage = new MailMessage
                {
                    From = new MailAddress(fromEmail, "Church Facilities Management"),
                    Subject = $"⚠️ Image Upload Failed for Request #{requestId}",
                    Body = GenerateErrorEmailBody(requestId, description, requestedBy, errorMessage),
                    IsBodyHtml = true
                };

                mailMessage.To.Add(managerEmail);

                if (!string.IsNullOrEmpty(replyToEmail))
                {
                    mailMessage.ReplyToList.Add(new MailAddress(replyToEmail, "Facilities Manager"));
                }

                await smtpClient.SendMailAsync(mailMessage);
                _logger.LogInformation($"Error notification email sent for request #{requestId} to {managerEmail}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to send error notification email for request #{requestId}");
                return false;
            }
        }

        private string GenerateErrorEmailBody(int requestId, string description, string requestedBy, string errorMessage)
        {
            return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
</head>
<body style='font-family: Arial, sans-serif; line-height: 1.6; color: #333; max-width: 600px; margin: 0 auto; padding: 20px;'>
    <div style='background: #d93025; color: white; padding: 20px; border-radius: 8px 8px 0 0;'>
        <h1 style='margin: 0; font-size: 1.5em;'>⚠️ Image Upload Error</h1>
    </div>

    <div style='background: #f8f9fa; padding: 20px; border: 1px solid #ddd; border-top: none; border-radius: 0 0 8px 8px;'>
        <p style='font-size: 1.1em; margin-top: 0;'>An error occurred while uploading images for a maintenance request.</p>

        <div style='background: white; padding: 15px; border-radius: 4px; margin: 15px 0;'>
            <p><strong>Request ID:</strong> #{requestId}</p>
            <p><strong>Description:</strong> {System.Web.HttpUtility.HtmlEncode(description)}</p>
            <p><strong>Requested By:</strong> {System.Web.HttpUtility.HtmlEncode(requestedBy)}</p>
        </div>

        <div style='background: #fef7e0; border-left: 4px solid #fbbc04; padding: 15px; margin: 15px 0;'>
            <p style='margin: 0;'><strong>Error Details:</strong></p>
            <p style='margin: 5px 0 0 0; font-family: monospace; font-size: 0.9em; color: #d93025;'>{System.Web.HttpUtility.HtmlEncode(errorMessage)}</p>
        </div>

        <div style='background: #e6f4ea; border-left: 4px solid #34a853; padding: 15px; margin: 15px 0;'>
            <p style='margin: 0;'><strong>✅ Request Created Successfully</strong></p>
            <p style='margin: 5px 0 0 0; font-size: 0.9em;'>The maintenance request was saved to Google Sheets, but the images could not be uploaded to Dropbox.</p>
        </div>

        <p style='font-size: 0.9em; color: #666; margin-top: 20px;'>
            <strong>Action Required:</strong><br>
            • User has been notified of the upload failure<br>
            • Request data is safe in Google Sheets<br>
            • User can re-upload images by editing the request<br>
            • Check Dropbox configuration if issue persists
        </p>
    </div>

    <div style='text-align: center; font-size: 0.8em; color: #999; margin-top: 20px;'>
        <p>Church Facility Management System - Automated Error Notification</p>
    </div>
</body>
</html>";
        }
    }
}
