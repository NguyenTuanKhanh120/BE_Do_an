using MailKit.Net.Smtp;
using MimeKit;

namespace UniKnowledge.Services;

public interface IEmailService
{
    Task SendOtpEmailAsync(string toEmail, string otpCode);
}

public class EmailService : IEmailService
{
    private const string GMAIL_ADDRESS = "nguyentuankhanhqnu09870@gmail.com";
    private const string GMAIL_APP_PASSWORD = "hmzpywljjahyzdvs";

    public async Task SendOtpEmailAsync(string toEmail, string otpCode)
    {
        Console.WriteLine("═══════════════════════════════════════");
        Console.WriteLine($"📧 Attempting to send OTP to: {toEmail}");
        Console.WriteLine($"🔐 OTP Code: {otpCode}");

        try
        {
            Console.WriteLine("🔵 Creating email message...");
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress("UniKnowledge", GMAIL_ADDRESS));
            message.To.Add(new MailboxAddress("", toEmail));
            message.Subject = "Password Reset - OTP Code";

            message.Body = new TextPart("html")
            {
                Text = $@"
                    <html><body style='font-family:Arial'>
                    <div style='max-width:600px;margin:auto;padding:20px'>
                      <h1 style='color:#667eea'>UniKnowledge - Password Reset</h1>
                      <p>Your OTP code is:</p>
                      <div style='background:#f0f0f0;padding:20px;text-align:center;font-size:32px;letter-spacing:5px;color:#667eea;font-weight:bold'>
                        {otpCode}
                      </div>
                      <p>This code expires in 5 minutes.</p>
                    </div>
                    </body></html>
                "
            };

            Console.WriteLine("🔵 Connecting to Gmail SMTP server...");
            using var client = new SmtpClient();
            await client.ConnectAsync("smtp.gmail.com", 587, false);

            Console.WriteLine("🔵 Authenticating with Gmail...");
            await client.AuthenticateAsync(GMAIL_ADDRESS, GMAIL_APP_PASSWORD);

            Console.WriteLine("🔵 Sending email...");
            await client.SendAsync(message);

            Console.WriteLine("🔵 Disconnecting...");
            await client.DisconnectAsync(true);

            Console.WriteLine("✅ Email sent successfully!");
            Console.WriteLine("═══════════════════════════════════════");
        }
        catch (Exception ex)
        {
            Console.WriteLine("❌ FAILED TO SEND EMAIL!");
            Console.WriteLine($"❌ Error Type: {ex.GetType().Name}");
            Console.WriteLine($"❌ Error Message: {ex.Message}");
            Console.WriteLine($"❌ Stack Trace: {ex.StackTrace}");
            Console.WriteLine("═══════════════════════════════════════");
            throw;
        }
    }
}