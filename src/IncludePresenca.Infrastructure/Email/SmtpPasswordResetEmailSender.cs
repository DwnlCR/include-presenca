
using System.Net;
using IncludePresenca.Application.Interfaces.Email;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using MimeKit;

namespace IncludePresenca.Infrastructure.Email;

public class SmtpPasswordResetEmailSender : IPasswordResetEmailSender
{
    private readonly IConfiguration _configuration;

    public SmtpPasswordResetEmailSender(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task SendPasswordResetEmailAsync(
        string email,
        string name,
        string resetToken)
    {
        string host = _configuration["Smtp:Host"]
            ?? throw new InvalidOperationException("SMTP host not configured.");

        int port = int.Parse(_configuration["Smtp:Port"]
            ?? throw new InvalidOperationException("SMTP port not configured."));

        string senderEmail = _configuration["Smtp:Email"]
            ?? throw new InvalidOperationException("SMTP email not configured.");

        string password = _configuration["Smtp:Password"]
            ?? throw new InvalidOperationException("SMTP password not configured.");

        string frontendUrl = _configuration["Frontend:BaseUrl"]
            ?? throw new InvalidOperationException("Frontend URL not configured.");

        string resetUrl =
            $"{frontendUrl.TrimEnd('/')}/reset-password?token={Uri.EscapeDataString(resetToken)}";

        MimeMessage message = new MimeMessage();

        message.From.Add(new MailboxAddress("IncludePresença", senderEmail));
        message.To.Add(MailboxAddress.Parse(email));
        message.Subject = "Redefinição de senha - IncludePresença";

        message.Body = new BodyBuilder
        {
            HtmlBody = $"""
                <h2>Olá, {WebUtility.HtmlEncode(name)}!</h2>
                <p>Recebemos uma solicitação para redefinir sua senha</p>
                <p>Clique no link abaixo para cadastrar uma nova senha</p>
                <p><a href="{WebUtility.HtmlEncode(resetUrl)}">Redefinir minha senha</a></p>
                <p>Este link expira em 30 minutos</p>
                """
        }.ToMessageBody();

        using SmtpClient client = new SmtpClient();

        await client.ConnectAsync(host, port, SecureSocketOptions.SslOnConnect);
        await client.AuthenticateAsync(senderEmail, password);
        await client.SendAsync(message);
        await client.DisconnectAsync(true);
    }
}
