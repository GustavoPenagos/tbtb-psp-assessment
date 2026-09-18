using Application.DTOs;
using Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers;

[ApiController]
[Route("api/registration-links")]
[Produces("application/json")]
public class RegistrationLinksController : ControllerBase
{
    private readonly IRegistrationService _registrationService;

    public RegistrationLinksController(IRegistrationService registrationService)
    {
        _registrationService = registrationService;
    }

    /// <summary>
    /// CA-1 Variante: Gestor genera enlace con token criptográfico temporal.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(RegistrationLinkResponseDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> Create([FromBody] RegistrationLinkCreateRequestDto request, CancellationToken cancellationToken)
    {
        var result = await _registrationService.CreateLinkAsync(request, cancellationToken);
        return CreatedAtAction(nameof(Validate), new { token = result.Token }, result);
    }

    /// <summary>
    /// CA-1 Variante: Validar vigencia y estado del token de autorregistro.
    /// </summary>
    [HttpGet("{token}/validate")]
    [ProducesResponseType(typeof(RegistrationLinkResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Validate(string token, CancellationToken cancellationToken)
    {
        var result = await _registrationService.ValidateLinkAsync(token, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// CA-1 Variante Paso 1: Identificación y filtro previo (crea registro en PENDING).
    /// </summary>
    [HttpPost("{token}/identify")]
    [ProducesResponseType(typeof(SelfRegIdentifyResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Identify(string token, [FromBody] SelfRegIdentifyRequestDto request, CancellationToken cancellationToken)
    {
        var result = await _registrationService.IdentifyPatientAsync(token, request, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// CA-1 Variante Paso 2: Datos obligatorios finales (activa a ACTIVE e invalida token).
    /// </summary>
    [HttpPost("{token}/complete")]
    [ProducesResponseType(typeof(PatientResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Complete(string token, [FromBody] SelfRegCompleteRequestDto request, CancellationToken cancellationToken)
    {
        var result = await _registrationService.CompletePatientAsync(token, request, cancellationToken);
        return Ok(result);
    }
}
