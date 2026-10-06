using MediatR;

using Microsoft.EntityFrameworkCore;

using MohamedTransit.Application.DTO;
using MohamedTransit.Application.Queries;
using MohamedTransit.Domain.Data;
// የእርስዎ IApplicationDbContext የሚገኝበትን ትክክለኛ namespace እዚህ ያስገቡ (ለምሳሌ MohamedTransit.Application.Common.Interfaces)

namespace MohamedTransit.Application.Handlers.DataEncoder
{
    public class GetAllServicesQueryHandler : IRequestHandler<GetAllServicesQuery, IEnumerable<ServiceDto>>
    {
        private readonly ApplicationDbContext _context;

        public GetAllServicesQueryHandler(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<ServiceDto>> Handle(GetAllServicesQuery request, CancellationToken cancellationToken)
        {
            var services = await _context.Services
                .Select(s => new ServiceDto
                {
                    Id = s.Id,
                    ServiceName = s.ServiceName,
                    Description = s.Description,
                    Fee = s.Fee,
                    Status = s.Status
                })
                .ToListAsync(cancellationToken);

            return services;
        }
    }
}
