using Domain.Enums;

namespace Domain.Entities;

public class User
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class Patient
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
    public string Status { get; set; } = "PENDING";
    public string RegistrationSource { get; set; } = "GESTOR";
    public DateTime? ConsentDate { get; set; }
    public Guid CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class Contact
{
    public Guid Id { get; set; }
    public Guid PatientId { get; set; }
    public DateTime ContactDate { get; set; }
    public string Channel { get; set; } = string.Empty;
    public string Result { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public bool IsActive { get; set; } = true;
    public Guid RegisteredBy { get; set; }
    public string? RegisteredByName { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class RegistrationLink
{
    public Guid Id { get; set; }
    public string Token { get; set; } = string.Empty;
    public Guid CreatedBy { get; set; }
    public Guid? PatientId { get; set; }
    public string Status { get; set; } = "PENDING";
    public DateTime ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UsedAt { get; set; }
}

public class ContactAuditLog
{
    public Guid Id { get; set; }
    public Guid ContactId { get; set; }
    public Guid ChangedBy { get; set; }
    public string? ChangedByName { get; set; }
    public DateTime ChangedAt { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string PreviousValue { get; set; } = string.Empty;
    public string NewValue { get; set; } = string.Empty;
}

public class PatientAuditLog
{
    public Guid Id { get; set; }
    public Guid PatientId { get; set; }
    public Guid ChangedBy { get; set; }
    public string? ChangedByName { get; set; }
    public DateTime ChangedAt { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string PreviousValue { get; set; } = string.Empty;
    public string NewValue { get; set; } = string.Empty;
}