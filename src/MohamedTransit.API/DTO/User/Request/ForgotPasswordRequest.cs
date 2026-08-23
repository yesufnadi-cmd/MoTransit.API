namespace MohamedTransit.API.DTO.User.Request;

public class ForgotPasswordRequest
{
    public string Token { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

