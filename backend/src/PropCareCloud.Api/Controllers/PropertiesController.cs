using Microsoft.AspNetCore.Mvc;
using PropCareCloud.Api.DTOs.Properties;
using PropCareCloud.Api.Services;

namespace PropCareCloud.Api.Controllers;

/// <summary>
/// Property and rental unit management endpoints. Authentication and authorization will be added later.
/// </summary>
[ApiController]
[Route("api/properties")]
public sealed class PropertiesController(IPropertyService propertyService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<PropertyResponse>>> GetProperties()
    {
        var properties = await propertyService.GetPropertiesAsync();

        return Ok(properties);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<PropertyResponse>> GetProperty(Guid id)
    {
        var property = await propertyService.GetPropertyByIdAsync(id);
        if (property is null)
        {
            return NotFound();
        }

        return Ok(property);
    }

    [HttpPost]
    public async Task<ActionResult<PropertyResponse>> CreateProperty(PropertyCreateRequest request)
    {
        var property = await propertyService.CreatePropertyAsync(request);

        return CreatedAtAction(nameof(GetProperty), new { id = property.Id }, property);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<PropertyResponse>> UpdateProperty(
        Guid id,
        PropertyUpdateRequest request)
    {
        var property = await propertyService.UpdatePropertyAsync(id, request);
        if (property is null)
        {
            return NotFound();
        }

        return Ok(property);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteProperty(Guid id)
    {
        var property = await propertyService.GetPropertyByIdAsync(id);
        if (property is null)
        {
            return NotFound();
        }

        var deleted = await propertyService.DeletePropertyAsync(id);
        if (!deleted)
        {
            return Conflict(new
            {
                message = "Property cannot be deleted while rental units exist."
            });
        }

        return NoContent();
    }

    [HttpGet("{propertyId:guid}/units")]
    public async Task<ActionResult<List<RentalUnitResponse>>> GetUnits(Guid propertyId)
    {
        var property = await propertyService.GetPropertyByIdAsync(propertyId);
        if (property is null)
        {
            return NotFound();
        }

        var units = await propertyService.GetUnitsByPropertyAsync(propertyId);

        return Ok(units);
    }

    [HttpGet("{propertyId:guid}/units/{unitId:guid}")]
    public async Task<ActionResult<RentalUnitResponse>> GetUnit(Guid propertyId, Guid unitId)
    {
        var unit = await propertyService.GetUnitByIdAsync(propertyId, unitId);
        if (unit is null)
        {
            return NotFound();
        }

        return Ok(unit);
    }

    [HttpPost("{propertyId:guid}/units")]
    public async Task<ActionResult<RentalUnitResponse>> CreateUnit(
        Guid propertyId,
        RentalUnitCreateRequest request)
    {
        var unit = await propertyService.CreateUnitAsync(propertyId, request);
        if (unit is null)
        {
            return NotFound();
        }

        return CreatedAtAction(
            nameof(GetUnit),
            new { propertyId, unitId = unit.Id },
            unit);
    }

    [HttpPut("{propertyId:guid}/units/{unitId:guid}")]
    public async Task<ActionResult<RentalUnitResponse>> UpdateUnit(
        Guid propertyId,
        Guid unitId,
        RentalUnitUpdateRequest request)
    {
        var unit = await propertyService.UpdateUnitAsync(propertyId, unitId, request);
        if (unit is null)
        {
            return NotFound();
        }

        return Ok(unit);
    }

    [HttpDelete("{propertyId:guid}/units/{unitId:guid}")]
    public async Task<IActionResult> DeleteUnit(Guid propertyId, Guid unitId)
    {
        var unit = await propertyService.GetUnitByIdAsync(propertyId, unitId);
        if (unit is null)
        {
            return NotFound();
        }

        var deleted = await propertyService.DeleteUnitAsync(propertyId, unitId);
        if (!deleted)
        {
            return Conflict(new
            {
                message = "Rental unit cannot be deleted while maintenance requests exist."
            });
        }

        return NoContent();
    }
}
