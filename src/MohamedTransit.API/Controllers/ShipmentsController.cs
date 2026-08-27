using Mapster;

using MediatR;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

// 1. Request DTO Namespaces
using MohamedTransit.API.DTO.Document.Request;
using MohamedTransit.API.DTO.Shipment.Request;
using MohamedTransit.API.Helpers;
using MohamedTransit.Application;

using MohamedTransit.Application.Commands;


// 2. Application Commands & Queries Namespaces
//using MohamedTransit.Application.Commands.Document;
using MohamedTransit.Application.Commands.Shipment;
using MohamedTransit.Application.Queries.Shipment;

// 3. Domain & Data Namespaces
using MohamedTransit.Domain.Common;
using MohamedTransit.Domain.Data;


namespace MohamedTransit.API.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class ShipmentsController : BaseController
{
    private readonly ApplicationDbContext _context;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public ShipmentsController(ApplicationDbContext context, IHttpContextAccessor httpContextAccessor)
    {
        _context = context;
        _httpContextAccessor = httpContextAccessor;
    }

    // ==========================================
    // 1. Create Shipment
    // ==========================================
    [HttpPost("Create")]
    public async Task<IActionResult> Create([FromBody] CreateShipmentRequest request, CancellationToken ct)
    {
        try
        {
            var command = request.Adapt<CreateShipmentCommand>();
            var result = await _mediator.Send(command, ct);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    // ==========================================
    // 2. Assign Shipment
    // ==========================================
    [HttpPut("Assign")]
    public async Task<IActionResult> Assign([FromBody] AssignShipmentRequest request, CancellationToken ct)
    {
        var command = request.Adapt<AssignShipmentCommand>();
        var result = await _mediator.Send(command, ct);
        return Ok(result);
    }

    // ==========================================
    // 3. Create Stage Transport
    // ==========================================
    [HttpPost("CreateStageTransport")]
    public async Task<IActionResult> CreateStageTransport([FromForm] CreateStageTransportRequest request, CancellationToken ct)
    {
        var command = request.Adapt<CreateStageTransportCommand>();
        var result = await _mediator.Send(command, ct);
        return Ok(result);
    }

    // ==========================================
    // 4. Update Shipment
    // ==========================================
    [HttpPut("Update")]
    public async Task<IActionResult> Update([FromBody] UpdateShipmentRequest request, CancellationToken ct)
    {
        var command = request.Adapt<UpdateShipmentCommand>();
        var result = await _mediator.Send(command, ct);
        return Ok(result);
    }

    // ==========================================
    // 5. Update Shipment Status
    // ==========================================
    [HttpPut("UpdateStatus/{id}")]
    public async Task<IActionResult> UpdateStatus(
        [FromRoute] long id,
        [FromBody] UpdateStatusRequest request,
        CancellationToken ct)
    {
        try
        {
            var command = new UpdateShipmentStatusCommand(
                id,
                request.NewStatus,
                request.UpdatedByHub,
                request.Remarks);

            var result = await _mediator.Send(command, ct);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    // ==========================================
    // 6. Update Service Stage
    // ==========================================
    [HttpPut("UpdateServiceStage")]
    public async Task<IActionResult> UpdateServiceStage([FromBody] UpdateServiceStageRequest request, CancellationToken ct)
    {
        var command = request.Adapt<UpdateServiceStageCommand>();
        var result = await _mediator.Send(command, ct);
        return Ok(result);
    }

    // ==========================================
    // 7. Update Stage Transport
    // ==========================================
    [HttpPut("UpdateStageTransport")]
    public async Task<IActionResult> UpdateStageTransport([FromForm] UpdateStageTransportRequest request, CancellationToken ct)
    {
        var command = request.Adapt<UpdateStageTransportCommand>();
        var result = await _mediator.Send(command, ct);
        return Ok(result);
    }

    // ==========================================
    // 8. Upload Document
    // ==========================================
    [HttpPost("UploadDocument")]
    public async Task<IActionResult> UploadDocument([FromForm] UploadDocumentRequest request, CancellationToken ct)
    {
        var command = request.Adapt<UploadDocumentCommand>();
        var result = await _mediator.Send(command, ct);
        return Ok(result);
    }

    // ==========================================
    // 9. Get All Shipments
    // ==========================================
    [HttpGet("GetAll")]
    public async Task<IActionResult> GetAll([FromQuery] RecordStatus recordStatus, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetAllShipmentsQuery(recordStatus), ct);
        return Ok(result);
    }

    // ==========================================
    // 10. Get Shipment By Id
    // ==========================================
    [HttpGet("GetById/{id}")]
    public async Task<IActionResult> GetById([FromRoute] long id, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetShipmentByIdQuery(id), ct);

        if (result is null)
            return NotFound(new { message = $"Shipment with ID {id} was not found." });

        return Ok(result);
    }

    // ==========================================
    // 11. Get Shipments By Importer
    // ==========================================
    [HttpGet("GetByImporter/{importerId}")]
    public async Task<IActionResult> GetByImporter([FromRoute] long importerId, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetShipmentsByImporterQuery(importerId), ct);
        return Ok(result);
    }

    // ==========================================
    // 12. Delete Shipment
    // ==========================================
    [HttpDelete("Delete/{id}")]
    public async Task<IActionResult> Delete([FromRoute] long id, CancellationToken ct)
    {
        var result = await _mediator.Send(new DeleteShipmentCommand(id), ct);

        if (!result)
            return NotFound(new { message = $"Shipment with ID {id} was not found." });

        return NoContent();
    }
}

public record UpdateStatusRequest(
    ShipmentStatus NewStatus,
    HubLocation UpdatedByHub,
    string Remarks
);
