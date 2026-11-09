
namespace IncludePresenca.Application.Interfaces.Email;

public interface IPasswordResetEmailSender
{
    Task SendPasswordResetEmailAsync(
        string email,
        string name,
        string resetToken
    );
}
