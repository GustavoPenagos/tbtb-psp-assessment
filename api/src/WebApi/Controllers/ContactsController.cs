using Application.DTOs;
using Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
[Tags("2. Contactos e Interacciones (CA-2 / CA-3)")]
public class ContactsController(IContactService contactService) : ControllerBase
{
    private readonly IContactService _contactService = contactService;

    /// <summary>
    /// CA-2: Registrar interacción con paciente en estado ACTIVE.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ContactResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Create([FromBody] ContactCreateRequestDto request, CancellationToken cancellationToken)
    {
        var result = await _contactService.CreateContactAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetByPatient), new { patientId = result.PatientId }, result);
    }

    /// <summary>
    /// CA-2: Obtener historial de contactos activos de un paciente.
    /// </summary>
    [HttpGet("patient/{patientId:guid}")]
    [ProducesResponseType(typeof(IEnumerable<ContactResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByPatient(Guid patientId, CancellationToken cancellationToken)
    {
        var contacts = await _contactService.GetContactsByPatientIdAsync(patientId, cancellationToken);
        return Ok(contacts);
    }

    /// <summary>
    /// CA-3: Corrección auditada de contacto (soft-update + log inmutable con motivo >= 10 caracteres).
    /// </summary>
    [HttpPut("{id:guid}/correct")]
    [ProducesResponseType(typeof(ContactResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Correct(Guid id, [FromBody] ContactCorrectRequestDto request, CancellationToken cancellationToken)
    {
        var result = await _contactService.CorrectContactAsync(id, request, cancellationToken);
        return Ok(result);
    }
}
