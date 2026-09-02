using MohamedTransit.Domain.Common;
namespace MohamedTransit.API.DTO.MOT.Request;
public class CreateCustomerServiceRequest
{
    public string ItemDescription { get; set; } = string.Empty;
    public string RouteCategory { get; set; } = string.Empty;
    public decimal DeclaredValue { get; set; }
    public string TaxCategory { get; set; } = string.Empty;
    public string CountryOfOrigin { get; set; } = string.Empty;
    public ServiceType ServiceType { get; set; }
    public RiskLevel RiskLevel { get; set; } = RiskLevel.Blue;
    public string Priority { get; set; } = "Medium";
    public string SpecialInstructions { get; set; } = string.Empty;
}


