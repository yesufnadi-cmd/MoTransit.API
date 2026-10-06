using MediatR;

namespace MohamedTransit.Application.Commands.DataEncoder; // (ወይም በፕሮጀክትዎ ውስጥ ያለው ትክክለኛው namespace)

public record CreateServiceCommand : IRequest<bool> // (ወይም ErrorOr<bool>)
{
    public string Reference { get; set; } = string.Empty;
    public string ContactPerson { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;

    // በፕሮጀክትዎ ትዕዛዝ መሠረት የሚያስፈልጉ ከሆነ (ለምሳሌ ImporterId) እዚህ ላይ ማካተት ይቻላል
    public long ImporterId { get; set; }
}
