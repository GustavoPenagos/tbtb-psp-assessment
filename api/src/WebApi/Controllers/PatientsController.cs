using Application.DTOs;
using Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class PatientsController : ControllerBase
{
    private readonly IPatientService _patientService;

    public PatientsController(IPatientService patientService)
    {
        _patientService = patientService;
    }

    /// <summary>
    /// CA-1: Registro de paciente por gestor (multi-país CO/PE/EC).
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(PatientResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register([FromBody] PatientRegisterRequestDto request, CancellationToken cancellationToken)
    {
        var result = await _patientService.RegisterPatientAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    /// <summary>
    /// Directorio de pacientes con filtros por país, estado y paginación.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PatientListResponseDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPaged(
        [FromQuery] string? countryCode,
        [FromQuery] string? status,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await _patientService.GetPatientsPagedAsync(countryCode, status, pageNumber, pageSize, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Obtiene el detalle de un paciente por ID.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(PatientResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _patientService.GetPatientByIdAsync(id, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// CA-3 / GxP: Actualización auditada de paciente con motivo obligatorio >= 10 caracteres.
    /// </summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateWithAudit(Guid id, [FromBody] PatientUpdateRequestDto request, CancellationToken cancellationToken)
    {
        await _patientService.UpdatePatientWithAuditAsync(id, request, cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// CA-3 / GxP: Historial inmutable de auditoría del paciente.
    /// </summary>
    [HttpGet("{id:guid}/audit")]
    [ProducesResponseType(typeof(IEnumerable<AuditLogResponseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAuditHistory(Guid id, CancellationToken cancellationToken)
    {
        var history = await _patientService.GetPatientAuditHistoryAsync(id, cancellationToken);
        return Ok(history);
    }
}
