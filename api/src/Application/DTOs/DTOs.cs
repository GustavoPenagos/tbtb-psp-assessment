namespace Application.DTOs;

public class PatientRegisterRequestDto
{
    public string FullName { get; set; } = string.Empty;
    public string DocumentType { get; set; } = string.Empty;
    public string DocumentNumber { get; set; } = string.Empty;
    public string CountryCode { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public DateTime TreatmentStart { get; set; }
    public int FollowUpDays { get; set; }
    public DateTime? ConsentDate { get; set; }
    public Guid CreatedBy { get; set; }
}

public class PatientUpdateRequestDto
{
    public string Phone { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public int FollowUpDays { get; set; }
    public string Status { get; set; } = string.Empty;
    public Guid ChangedBy { get; set; }
    public string Reason { get; set; } = string.Empty;
}

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

public class PatientListResponseDto
{
    public IEnumerable<PatientResponseDto> Items { get; set; } = Enumerable.Empty<PatientResponseDto>();
    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
}

public class ContactCreateRequestDto
{
    public Guid PatientId { get; set; }
    public DateTime ContactDate { get; set; }
    public string Channel { get; set; } = string.Empty;
    public string Result { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public Guid RegisteredBy { get; set; }
}

public class ContactCorrectRequestDto
{
    public DateTime NewContactDate { get; set; }
    public string NewChannel { get; set; } = string.Empty;
    public string NewResult { get; set; } = string.Empty;
    public string? NewNotes { get; set; }
    public string Reason { get; set; } = string.Empty;
    public Guid ChangedBy { get; set; }
}

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

public class RegistrationLinkCreateRequestDto
{
    public Guid CreatedBy { get; set; }
    public int ExpiresInDays { get; set; } = 7;
}

public class RegistrationLinkResponseDto
{
    public string Token { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
}

public class SelfRegIdentifyRequestDto
{
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string CountryCode { get; set; } = string.Empty;
}

public class SelfRegIdentifyResponseDto
{
    public Guid PatientId { get; set; }
    public string SessionToken { get; set; } = string.Empty;
    public string NextStep { get; set; } = string.Empty;
}

public class SelfRegCompleteRequestDto
{
    public string DocumentType { get; set; } = string.Empty;
    public string DocumentNumber { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public DateTime TreatmentStart { get; set; }
    public int FollowUpDays { get; set; }
    public DateTime? ConsentDate { get; set; }
}

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