using MailKit.Net.Smtp;
using MimeKit;
using System.Text.RegularExpressions;

namespace AmbermoonServer.Services;

public partial class EmailService(TemplateService templateService)
{
	private const string SMTPServerName = "ASPNETCORE_SMTP_SERVER";
	private const string SMTPPortName = "ASPNETCORE_SMTP_PORT";
	private const string SMTPUserName = "ASPNETCORE_SMTP_USER";
	private const string SMTPPasswordName = "ASPNETCORE_SMTP_PASSWORD";
    private readonly string smtpServer = Environment.GetEnvironmentVariable(SMTPServerName) ?? throw new KeyNotFoundException($"Missing {SMTPServerName} environment variable");
	private readonly string smtpPort = Environment.GetEnvironmentVariable(SMTPPortName) ?? throw new KeyNotFoundException($"Missing {SMTPPortName} environment variable");
	private readonly string smtpUser = Environment.GetEnvironmentVariable(SMTPUserName) ?? throw new KeyNotFoundException($"Missing {SMTPUserName} environment variable");
	private readonly string smtpPassword = Environment.GetEnvironmentVariable(SMTPPasswordName) ?? throw new KeyNotFoundException($"Missing {SMTPPasswordName} environment variable");
	private readonly Dictionary<string, DateTime> sendMailTimes = [];
	private static readonly TimeSpan SentMailMemoryTime = TimeSpan.FromSeconds(10);

	public static bool IsEmailValid(string email) => EmailRegex().IsMatch(email);

	private void CleanupSentMailMemory()
    {
		var now = DateTime.UtcNow;

		foreach (var key in sendMailTimes.Keys.ToList())
		{
			if (now - sendMailTimes[key] > SentMailMemoryTime)
				sendMailTimes.Remove(key);
		}
    }

    public async Task SendEmailAsync<T>(string email, string subject, string templateKey, T model)
	{
		string key = $"{email}-{templateKey}";
		var now = DateTime.UtcNow;

		lock (sendMailTimes)
		{
			if (sendMailTimes.TryGetValue(key, out var lastSendTime) && now - lastSendTime < SentMailMemoryTime)
				return;

			CleanupSentMailMemory();

			sendMailTimes[key] = now;
		}

        var mail = new MimeMessage();
		mail.From.Add(new MailboxAddress("Ambermoon Registration", smtpUser));
		mail.To.Add(new MailboxAddress("", email));
		mail.Subject = subject;
        mail.Body = new TextPart("html")
		{
			Text = await templateService.RenderTemplateAsync(templateKey, model)
        };

		using var client = new SmtpClient();

		try
		{
			var port = int.Parse(smtpPort);

			// Connect to the SMTP server
			await client.ConnectAsync(smtpServer, port, MailKit.Security.SecureSocketOptions.StartTls);
			await client.AuthenticateAsync(smtpUser, smtpPassword);

			// Send the email
			await client.SendAsync(mail);
		}
		finally
		{
			// Disconnect and clean up
			await client.DisconnectAsync(true);
		}
	}

    [GeneratedRegex("^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\\.[a-zA-Z]{2,}$", RegexOptions.Compiled)]
    private static partial Regex EmailRegex();
}
