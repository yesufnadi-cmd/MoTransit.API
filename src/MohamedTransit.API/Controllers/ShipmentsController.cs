using MediatR;

using Microsoft.AspNetCore.Mvc;

using MohamedTransit.Application.Commands.Shipment;
using MohamedTransit.Application.DTO;
using MohamedTransit.Application.Queries.Shipment;
using MohamedTransit.Domain.Common;

namespace MohamedTransit.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ShipmentsController : ControllerBase
{
    private readonly ISender _mediator;

    public ShipmentsController(ISender mediator)
    {
        _mediator = mediator;
    }

    // ==========================================
    // 1. Create Shipment
    // ==========================================
    [HttpPost("Create")]
    public async Task<IActionResult> Create(
        [FromBody] CreateShipmentCommand command,
        CancellationToken ct)
    {
        try
        {
            var result = await _mediator.Send(command, ct);

            return CreatedAtAction(
                nameof(GetShipmentById),
                new { id = result.Id },
                result);
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
    // 2. Get All Shipments
    // ==========================================
    [HttpGet("GetAll")]
    public async Task<IActionResult> GetAll(
        [FromQuery] RecordStatus recordStatus,
        CancellationToken ct)
    {
        var result = await _mediator.Send(
            new GetAllShipmentsQuery(recordStatus),
            ct);

        return Ok(result);
    }

    // ==========================================
    // 3. Get Shipment By Id
    // ==========================================
    [HttpGet("GetShipmentById/{id}")]
    public async Task<IActionResult> GetShipmentById(
        [FromRoute] long id,
        CancellationToken ct)
    {
        var result = await _mediator.Send(
            new GetShipmentByIdQuery(id),
            ct);

        if (result is null)
            return NotFound(new { message = $"Shipment with ID {id} was not found." });

        return Ok(result);
    }

    // ==========================================
    // 4. Get Shipments By Importer
    // ==========================================
    [HttpGet("GetByImporter/{importerId}")]
    public async Task<IActionResult> GetByImporter(
        long importerId,
        CancellationToken ct)
    {
        var result = await _mediator.Send(
            new GetShipmentsByImporterQuery(importerId),
            ct);

        return Ok(result);
    }

    // ==========================================
    // 5. Update Shipment Status
    // ==========================================
    [HttpPatch("UpdateStatus/{id}")]
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
    // 6. Delete Shipment
    // ==========================================
    [HttpDelete("Delete/{id}")]
    public async Task<IActionResult> Delete(
        [FromRoute] long id,
        CancellationToken ct)
    {
        var result = await _mediator.Send(
            new DeleteShipmentCommand(id),
            ct);

        if (!result)
            return NotFound(new { message = $"Shipment with ID {id} was not found." });

        return NoContent();
    }
}

// ==========================================
// Update Status Request
// ==========================================
public record UpdateStatusRequest(
    ShipmentStatus NewStatus,
    HubLocation UpdatedByHub,
    string Remarks
);
