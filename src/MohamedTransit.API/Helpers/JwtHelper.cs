using System.IdentityModel.Tokens.Jwt;

using MohamedTransit.Domain.Data;
namespace MohamedTransit.API.Helpers;
public static class JwtHelper
{
    public static long? GetCurrentUserId(IHttpContextAccessor httpContextAccessor, ApplicationDbContext context)
    {
        var authorizationHeader = httpContextAccessor.HttpContext?.Request.Headers["Authorization"].ToString();
        if (string.IsNullOrEmpty(authorizationHeader) || !authorizationHeader.StartsWith("Bearer "))
            return null;

        var token = authorizationHeader.Replace("Bearer ", "");

        try
        {
            var handler = new JwtSecurityTokenHandler();
            var jsonToken = handler.ReadJwtToken(token);

            // Look for the "id" claim in the JWT token
            var idClaim = jsonToken.Claims.FirstOrDefault(x => x.Type == "id");
            if (idClaim != null && long.TryParse(idClaim.Value, out var userId))
                return userId;

            // If no "id" claim, try to get user ID from username
            var userNameClaim = jsonToken.Claims.FirstOrDefault(x => x.Type == "userName");
            if (userNameClaim != null)
            {
                var user = context.Users.FirstOrDefault(u => u.Username == userNameClaim.Value);
                if (user != null)
                    return user.Id;
            }

            return null;
        }
        catch
        {
            return null;
        }
    }

    public static string? GetCurrentUsername(IHttpContextAccessor httpContextAccessor)
    {
        var authorizationHeader = httpContextAccessor.HttpContext?.Request.Headers["Authorization"].ToString();
        if (string.IsNullOrEmpty(authorizationHeader) || !authorizationHeader.StartsWith("Bearer "))
            return null;

        var token = authorizationHeader.Replace("Bearer ", "");

        try
        {
            var handler = new JwtSecurityTokenHandler();
            var jsonToken = handler.ReadJwtToken(token);

            return jsonToken.Claims.FirstOrDefault(x => x.Type == "userName")?.Value;
        }
        catch
        {
            return null;
        }
    }
}








