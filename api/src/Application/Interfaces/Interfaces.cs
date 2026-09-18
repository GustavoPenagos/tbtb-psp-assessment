using System.Data;
using Application.DTOs;
using Domain.Entities;

namespace Application.Interfaces;

public interface ISqlConnectionFactory
{
    IDbConnection CreateConnection();
}

public interface IPatientRepository
{
    Task<Guid> RegisterAsync(Patient patient, CancellationToken cancellationToken = default);
    Task<Patient?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<(IEnumerable<Patient> Items, int TotalCount)> GetPagedAsync(string? countryCode, string? status, int pageNumber, int pageSize, CancellationToken cancellationToken = default);
    Task UpdateWithAuditAsync(Guid patientId, string phone, string city, int followUpDays, string status, Guid changedBy, string reason, CancellationToken cancellationToken = default);
    Task<IEnumerable<PatientAuditLog>> GetAuditHistoryAsync(Guid patientId, CancellationToken cancellationToken = default);
}

public interface IContactRepository
{
    Task<Guid> CreateAsync(Contact contact, CancellationToken cancellationToken = default);
    Task<IEnumerable<Contact>> GetByPatientIdAsync(Guid patientId, CancellationToken cancellationToken = default);
    Task<Guid> CorrectAsync(Guid originalContactId, Guid changedBy, DateTime newDate, string newChannel, string newResult, string? newNotes, string reason, CancellationToken cancellationToken = default);
}

public interface IRegistrationLinkRepository
{
    Task<(string Token, DateTime ExpiresAt)> CreateAsync(Guid createdBy, int expiresInDays, CancellationToken cancellationToken = default);
    Task<RegistrationLink?> ValidateAsync(string token, CancellationToken cancellationToken = default);
    Task<(Guid PatientId, string SessionToken)> IdentifyPatientAsync(string token, string fullName, string email, string countryCode, CancellationToken cancellationToken = default);
    Task<Guid> CompletePatientAsync(string token, string docType, string docNumber, string phone, string city, DateTime treatmentStart, int followUpDays, DateTime? consentDate, CancellationToken cancellationToken = default);
}

public interface IPatientService
{
    Task<PatientResponseDto> RegisterPatientAsync(PatientRegisterRequestDto request, CancellationToken cancellationToken = default);
    Task<PatientResponseDto> GetPatientByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<PatientListResponseDto> GetPatientsPagedAsync(string? countryCode, string? status, int pageNumber, int pageSize, CancellationToken cancellationToken = default);
    Task UpdatePatientWithAuditAsync(Guid id, PatientUpdateRequestDto request, CancellationToken cancellationToken = default);
    Task<IEnumerable<AuditLogResponseDto>> GetPatientAuditHistoryAsync(Guid id, CancellationToken cancellationToken = default);
}

public interface IContactService
{
    Task<ContactResponseDto> CreateContactAsync(ContactCreateRequestDto request, CancellationToken cancellationToken = default);
    Task<IEnumerable<ContactResponseDto>> GetContactsByPatientIdAsync(Guid patientId, CancellationToken cancellationToken = default);
    Task<ContactResponseDto> CorrectContactAsync(Guid originalContactId, ContactCorrectRequestDto request, CancellationToken cancellationToken = default);
}

public interface IRegistrationService
{
    Task<RegistrationLinkResponseDto> CreateLinkAsync(RegistrationLinkCreateRequestDto request, CancellationToken cancellationToken = default);
    Task<RegistrationLinkResponseDto> ValidateLinkAsync(string token, CancellationToken cancellationToken = default);
    Task<SelfRegIdentifyResponseDto> IdentifyPatientAsync(string token, SelfRegIdentifyRequestDto request, CancellationToken cancellationToken = default);
    Task<PatientResponseDto> CompletePatientAsync(string token, SelfRegCompleteRequestDto request, CancellationToken cancellationToken = default);
}