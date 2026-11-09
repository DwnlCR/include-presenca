namespace IncludePresenca.Application.Interfaces.Email;

public interface IActivationEmailSender
{
    Task SendActivationEmailAsync(string email, string name, string activationToken);
}