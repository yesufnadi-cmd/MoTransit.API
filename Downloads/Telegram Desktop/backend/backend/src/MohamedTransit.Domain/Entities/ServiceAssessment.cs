using MohamedTransit.Domain.Common;

namespace MohamedTransit.Domain.Entities;

public class ServiceAssessment : BaseEntity
{
    public long ServiceId { get; private set; }
    public Shipment? Service { get; private set; }
    public long AssessorId { get; private set; }
    public User? Assessor { get; private set; }
    public string Decision { get; private set; } = string.Empty;
    public decimal TaxAmount { get; private set; }
    public decimal DutyAmount { get; private set; }
    public string? Notes { get; private set; }

    private ServiceAssessment() { }

    public static ServiceAssessment Create(
        long serviceId,
        long assessorId,
        string decision,
        decimal taxAmount,
        decimal dutyAmount,
        string? notes)
    {
        return new ServiceAssessment
        {
            ServiceId = serviceId,
            AssessorId = assessorId,
            Decision = decision,
            TaxAmount = taxAmount,
            DutyAmount = dutyAmount,
            Notes = notes,
            RecordStatus = RecordStatus.Active
        };
    }
}
