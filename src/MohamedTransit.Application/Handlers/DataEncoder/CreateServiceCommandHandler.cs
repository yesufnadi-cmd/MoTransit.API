using MediatR;

using MohamedTransit.Application.Commands.DataEncoder; // (የኮማንድዎ ትክክለኛ namespace)
using MohamedTransit.Domain.Data;
using MohamedTransit.Domain.Entities;

namespace MohamedTransit.Application.CommandHandlers
{
    public class CreateServiceCommandHandler : IRequestHandler<CreateServiceCommand, bool>
    {
        private readonly ApplicationDbContext _context;

        public CreateServiceCommandHandler(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<bool> Handle(CreateServiceCommand request, CancellationToken cancellationToken)
        {
            // 1. መረጃውን ወደ  entidade (Entity) መቀየር እና ዳታቤዝ ውስጥ ማስቀመጥ
            // (ማስታወሻ፡ እንደ ፕሮጀክትዎ ሎጂክ የ Service/Shipment ሞዴልን እዚህ ጋር ማስገባት ይችላሉ)

            // ለምሳሌ ያህል (Service entity ካለዎት):
            /*
            var service = Service.Create(
                request.Reference,
                request.ContactPerson,
                request.Email,
                request.Phone,
                request.Notes
            );

            _context.Services.Add(service);
            await _context.SaveChangesAsync(cancellationToken);
            */

            return true;
        }
    }
}
