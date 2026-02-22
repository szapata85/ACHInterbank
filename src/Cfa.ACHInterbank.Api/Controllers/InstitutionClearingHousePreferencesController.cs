using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Domain.Entities.Ach.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace Cfa.ACHInterbank.Api.Controllers;

[ApiController]
[Route("institution-clearing-house-preferences")]
[Authorize]
public class InstitutionClearingHousePreferencesController : ControllerBase
{
    private readonly IInstitutionClearingHousePreferenceService _service;

    public InstitutionClearingHousePreferencesController(IInstitutionClearingHousePreferenceService service)
    {
        _service = service;
    }
    /// <summary>
    /// Endpoint de la API ACH Interbank.
    /// </summary>

    [HttpGet]
    [Authorize(Policy = "CanManageAch")]
    public async Task<IActionResult> GetAll(CancellationToken ct = default)
        => Ok(await _service.GetAllAsync(ct));
    /// <summary>
    /// Endpoint de la API ACH Interbank.
    /// </summary>

    [HttpPost]
    [Authorize(Policy = "CanManageAch")]
    public async Task<IActionResult> Create([FromBody] InstitutionClearingHousePreferenceDto dto, CancellationToken ct = default)
        => Ok(await _service.CreateAsync(dto, ct));
    /// <summary>
    /// Endpoint de la API ACH Interbank.
    /// </summary>

    [HttpPut("{id}")]
    [Authorize(Policy = "CanManageAch")]
    public async Task<IActionResult> Update(int id, [FromBody] JsonElement body, CancellationToken ct = default)
    {
        var dto = new UpdateInstitutionClearingHousePreferenceDto();

        if (body.ValueKind == JsonValueKind.Object)
        {
            if (TryGetBoolean(body, "isDefault", out var isDefault))
            {
                dto.IsDefault = isDefault;
            }

            if (TryGetInt(body, "priority", out var priority))
            {
                dto.Priority = priority;
            }

            if (TryGetBoolean(body, "isActive", out var isActive))
            {
                dto.IsActive = isActive;
            }
        }

        return Ok(await _service.UpdateAsync(id, dto, ct));
    }
    /// <summary>
    /// Endpoint de la API ACH Interbank.
    /// </summary>

    [HttpDelete("{id}")]
    [Authorize(Policy = "CanManageAch")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct = default)
    {
        await _service.DeleteAsync(id, ct);
        return NoContent();
    }
    private static bool TryGetBoolean(JsonElement body, string propertyName, out bool value)
    {
        value = default;
        if (!TryGetPropertyIgnoreCase(body, propertyName, out var property))
        {
            return false;
        }

        if (property.ValueKind == JsonValueKind.True || property.ValueKind == JsonValueKind.False)
        {
            value = property.GetBoolean();
            return true;
        }

        if (property.ValueKind == JsonValueKind.String && bool.TryParse(property.GetString(), out var parsed))
        {
            value = parsed;
            return true;
        }

        return false;
    }

    private static bool TryGetInt(JsonElement body, string propertyName, out int value)
    {
        value = default;
        if (!TryGetPropertyIgnoreCase(body, propertyName, out var property))
        {
            return false;
        }

        if (property.ValueKind == JsonValueKind.Number && property.TryGetInt32(out value))
        {
            return true;
        }

        if (property.ValueKind == JsonValueKind.String && int.TryParse(property.GetString(), out var parsed))
        {
            value = parsed;
            return true;
        }

        return false;
    }

    private static bool TryGetPropertyIgnoreCase(JsonElement body, string propertyName, out JsonElement value)
    {
        if (body.TryGetProperty(propertyName, out value))
        {
            return true;
        }

        foreach (var property in body.EnumerateObject())
        {
            if (string.Equals(property.Name, propertyName, StringComparison.OrdinalIgnoreCase))
            {
                value = property.Value;
                return true;
            }
        }

        return false;
    }

}
