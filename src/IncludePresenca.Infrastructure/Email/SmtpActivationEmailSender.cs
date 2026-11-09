using System.Net;
using IncludePresenca.Application.Interfaces.Email;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using MimeKit;

namespace IncludePresenca.Infrastructure.Email;

public class SmtpActivationEmailSender : IActivationEmailSender
{
    private readonly IConfiguration _configuration;

    public SmtpActivationEmailSender(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task SendActivationEmailAsync(
        string email,
        string name,
        string activationToken)
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

        string activationUrl =
            $"{frontendUrl.TrimEnd('/')}/activate?token={Uri.EscapeDataString(activationToken)}";

        MimeMessage message = new MimeMessage();
        message.From.Add(new MailboxAddress("IncludePresença", senderEmail));
        message.To.Add(MailboxAddress.Parse(email));
        message.Subject = "Ative sua conta no IncludePresença";

        message.Body = new BodyBuilder
        {
            HtmlBody = $"""
                <h2>Olá, {WebUtility.HtmlEncode(name)}!</h2>
                <p>Você foi cadastrado no IncludePresença.</p>
                <p>Clique no link abaixo para definir sua senha:</p>
                <p><a href="{WebUtility.HtmlEncode(activationUrl)}">Ativar minha conta</a></p>
                <p>Este link expira em 24 horas.</p>
                <p>Se você não esperava este convite, ignore esta mensagem.</p>
                """
        }.ToMessageBody();

        using SmtpClient client = new SmtpClient();

        await client.ConnectAsync(host, port, SecureSocketOptions.SslOnConnect);
        await client.AuthenticateAsync(senderEmail, password);
        await client.SendAsync(message);
        await client.DisconnectAsync(true);
    }
}