using MohamedTransit.Domain.Common;

namespace MohamedTransit.Application.Helper;

public class Error
{
    public ErrorCode Code { get; set; }

    public string Message { get; set; } = string.Empty;
}
