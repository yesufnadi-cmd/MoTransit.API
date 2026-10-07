namespace MohamedTransit.Application.DTO
{
    public class UserProfileResponseDto
    {
        public string UserId { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty; 
        public string Branch { get; set; } = string.Empty;
        public string? ProfilePictureUrl { get; set; }
    }
}
