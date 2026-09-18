namespace Application.DTOs;

/// <summary>
/// Solicitud para registrar un nuevo paciente por parte de un gestor (CA-1).
/// </summary>
public class PatientRegisterRequestDto
{
    /// <summary>Nombre completo del paciente.</summary>
    /// <example>Carlos Mendoza</example>
    public string FullName { get; set; } = string.Empty;

    /// <summary>Tipo de documento oficial (CC, CE, DNI, RUT, PASAPORTE).</summary>
    /// <example>CC</example>
    public string DocumentType { get; set; } = string.Empty;

    /// <summary>Número de documento de identidad único.</summary>
    /// <example>1029384756</example>
    public string DocumentNumber { get; set; } = string.Empty;

    /// <summary>Código ISO de país (CO, PE, EC).</summary>
    /// <example>CO</example>
    public string CountryCode { get; set; } = string.Empty;

    /// <summary>Número de teléfono en formato internacional E.164 (+57, +51, +593).</summary>
    /// <example>+573001234567</example>
    public string Phone { get; set; } = string.Empty;

    /// <summary>Correo electrónico único del paciente.</summary>
    /// <example>carlos.mendoza@example.com</example>
    public string Email { get; set; } = string.Empty;

    /// <summary>Ciudad de residencia del paciente.</summary>
    /// <example>Bogotá</example>
    public string City { get; set; } = string.Empty;

    /// <summary>Fecha de inicio de tratamiento farmacéutico.</summary>
    /// <example>2026-03-01T00:00:00Z</example>
    public DateTime TreatmentStart { get; set; }

    /// <summary>Frecuencia de seguimiento en días (ej. 15, 30, 60).</summary>
    /// <example>30</example>
    public int FollowUpDays { get; set; }

    /// <summary>Fecha en que se otorgó el consentimiento informado (opcional).</summary>
    public DateTime? ConsentDate { get; set; }

    /// <summary>Identificador UUID del usuario gestor que realiza el registro.</summary>
    /// <example>11111111-1111-1111-1111-111111111111</example>
    public Guid CreatedBy { get; set; }
}

/// <summary>
/// Solicitud de actualización de datos de paciente con auditoría GxP obligatoria (CA-3).
/// </summary>
public class PatientUpdateRequestDto
{
    /// <summary>Nuevo número telefónico en formato E.164.</summary>
    /// <example>+573009876543</example>
    public string Phone { get; set; } = string.Empty;

    /// <summary>Nueva ciudad de residencia.</summary>
    /// <example>Medellín</example>
    public string City { get; set; } = string.Empty;

    /// <summary>Nueva frecuencia de seguimiento en días.</summary>
    /// <example>15</example>
    public int FollowUpDays { get; set; }

    /// <summary>Nuevo estado del paciente (ACTIVE, INACTIVE, SUSPENDED).</summary>
    /// <example>ACTIVE</example>
    public string Status { get; set; } = string.Empty;

    /// <summary>UUID del usuario que realiza la modificación.</summary>
    public Guid ChangedBy { get; set; }

    /// <summary>Justificación clínica/administrativa obligatoria (mínimo 10 caracteres).</summary>
    /// <example>Actualización de número telefónico por cambio de residencia del paciente</example>
    public string Reason { get; set; } = string.Empty;
}

/// <summary>
/// Representación de los datos del paciente retornados por la API.
/// </summary>
public class PatientResponseDto
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? DocumentType { get; set; }
    public string? DocumentNumber { get; set; }
    public string CountryCode { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string Email { get; set; } = string.Empty;
    public string? City { get; set; }
    public DateTime? TreatmentStart { get; set; }
    public int? FollowUpDays { get; set; }
    public string Status { get; set; } = string.Empty;
    public string RegistrationSource { get; set; } = string.Empty;
    public DateTime? ConsentDate { get; set; }
    public Guid CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

/// <summary>
/// Respuesta paginada para el directorio central de pacientes.
/// </summary>
public class PatientListResponseDto
{
    public IEnumerable<PatientResponseDto> Items { get; set; } = Enumerable.Empty<PatientResponseDto>();
    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
}

/// <summary>
/// Solicitud para registrar una interacción/contacto con un paciente activo (CA-2).
/// </summary>
public class ContactCreateRequestDto
{
    /// <summary>Identificador del paciente (debe estar en estado ACTIVE).</summary>
    public Guid PatientId { get; set; }

    /// <summary>Fecha y hora de la interacción.</summary>
    public DateTime ContactDate { get; set; }

    /// <summary>Canal de comunicación (CALL, WHATSAPP, EMAIL, SMS, IN_PERSON).</summary>
    /// <example>CALL</example>
    public string Channel { get; set; } = string.Empty;

    /// <summary>Resultado de la interacción (ANSWERED, NO_ANSWER, WRONG_NUMBER, SCHEDULED_CALL, REJECTED).</summary>
    /// <example>ANSWERED</example>
    public string Result { get; set; } = string.Empty;

    /// <summary>Notas clínicas y observaciones de la interacción.</summary>
    /// <example>Paciente confirma adherencia al tratamiento farmacológico sin efectos adversos.</example>
    public string? Notes { get; set; }

    /// <summary>UUID del usuario gestor que registra la interacción.</summary>
    public Guid RegisteredBy { get; set; }
}

/// <summary>
/// Solicitud para corregir un contacto existente bajo política GxP (CA-3).
/// </summary>
public class ContactCorrectRequestDto
{
    /// <summary>Nueva fecha del contacto corregido.</summary>
    public DateTime NewContactDate { get; set; }

    /// <summary>Nuevo canal de contacto.</summary>
    /// <example>WHATSAPP</example>
    public string NewChannel { get; set; } = string.Empty;

    /// <summary>Nuevo resultado de contacto.</summary>
    /// <example>ANSWERED</example>
    public string NewResult { get; set; } = string.Empty;

    /// <summary>Nuevas notas asociadas.</summary>
    public string? NewNotes { get; set; }

    /// <summary>Justificación obligatoria de la corrección (mínimo 10 caracteres).</summary>
    /// <example>Corrección por tipificación errónea del canal de contacto reportada por gestor</example>
    public string Reason { get; set; } = string.Empty;

    /// <summary>UUID del usuario que efectúa la corrección.</summary>
    public Guid ChangedBy { get; set; }
}

/// <summary>
/// Respuesta con los datos de un contacto registrado o corregido.
/// </summary>
public class ContactResponseDto
{
    public Guid Id { get; set; }
    public Guid PatientId { get; set; }
    public DateTime ContactDate { get; set; }
    public string Channel { get; set; } = string.Empty;
    public string Result { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public bool IsActive { get; set; }
    public Guid RegisteredBy { get; set; }
    public string? RegisteredByName { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Solicitud para generar un enlace de autorregistro temporal (CA-1 Variante).
/// </summary>
public class RegistrationLinkCreateRequestDto
{
    /// <summary>UUID del gestor que genera el enlace.</summary>
    public Guid CreatedBy { get; set; }

    /// <summary>Días de vigencia del enlace (por defecto 7 días).</summary>
    /// <example>7</example>
    public int ExpiresInDays { get; set; } = 7;
}

/// <summary>
/// Enlace de autorregistro con token criptográfico seguro.
/// </summary>
public class RegistrationLinkResponseDto
{
    public string Token { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
}

/// <summary>
/// Paso 1 del autorregistro: Identificación inicial del paciente.
/// </summary>
public class SelfRegIdentifyRequestDto
{
    /// <summary>Nombre completo del paciente.</summary>
    /// <example>Laura Gómez</example>
    public string FullName { get; set; } = string.Empty;

    /// <summary>Correo electrónico único.</summary>
    /// <example>laura.gomez@example.com</example>
    public string Email { get; set; } = string.Empty;

    /// <summary>Código ISO de país (CO, PE, EC).</summary>
    /// <example>CO</example>
    public string CountryCode { get; set; } = string.Empty;
}

/// <summary>
/// Respuesta del paso 1 de autorregistro.
/// </summary>
public class SelfRegIdentifyResponseDto
{
    public Guid PatientId { get; set; }
    public string SessionToken { get; set; } = string.Empty;
    public string NextStep { get; set; } = string.Empty;
}

/// <summary>
/// Paso 2 del autorregistro: Completar datos obligatorios e invalidar token.
/// </summary>
public class SelfRegCompleteRequestDto
{
    /// <summary>Tipo de documento oficial.</summary>
    /// <example>CC</example>
    public string DocumentType { get; set; } = string.Empty;

    /// <summary>Número de documento de identidad único.</summary>
    /// <example>52987123</example>
    public string DocumentNumber { get; set; } = string.Empty;

    /// <summary>Teléfono móvil en formato E.164.</summary>
    /// <example>+573105558899</example>
    public string Phone { get; set; } = string.Empty;

    /// <summary>Ciudad de residencia.</summary>
    /// <example>Cali</example>
    public string City { get; set; } = string.Empty;

    /// <summary>Fecha de inicio del tratamiento.</summary>
    public DateTime TreatmentStart { get; set; }

    /// <summary>Frecuencia de seguimiento en días.</summary>
    /// <example>30</example>
    public int FollowUpDays { get; set; }

    /// <summary>Fecha de consentimiento informado.</summary>
    public DateTime? ConsentDate { get; set; }
}

/// <summary>
/// Registro de auditoría inmutable bajo estándar ALCOA+ / GxP (CA-3).
/// </summary>
public class AuditLogResponseDto
{
    public Guid Id { get; set; }
    public Guid EntityId { get; set; }
    public Guid ChangedBy { get; set; }
    public string? ChangedByName { get; set; }
    public DateTime ChangedAt { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string PreviousValue { get; set; } = string.Empty;
    public string NewValue { get; set; } = string.Empty;
}