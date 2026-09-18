using AutoMapper;
using FluentValidation;
using Application.DTOs;
using Application.Interfaces;
using Domain.Entities;
using Domain.Exceptions;

namespace Application.Services;

public class PatientService : IPatientService
{
    private readonly IPatientRepository _patientRepository;
    private readonly IMapper _mapper;
    private readonly IValidator<PatientRegisterRequestDto> _registerValidator;
    private readonly IValidator<PatientUpdateRequestDto> _updateValidator;

    public PatientService(
        IPatientRepository patientRepository,
        IMapper mapper,
        IValidator<PatientRegisterRequestDto> registerValidator,
        IValidator<PatientUpdateRequestDto> updateValidator)
    {
        _patientRepository = patientRepository;
        _mapper = mapper;
        _registerValidator = registerValidator;
        _updateValidator = updateValidator;
    }

    public async Task<PatientResponseDto> RegisterPatientAsync(PatientRegisterRequestDto request, CancellationToken cancellationToken = default)
    {
        var validationResult = await _registerValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());
            throw new Domain.Exceptions.ValidationException(errors);
        }

        var patientEntity = _mapper.Map<Patient>(request);
        var patientId = await _patientRepository.RegisterAsync(patientEntity, cancellationToken);

        var created = await _patientRepository.GetByIdAsync(patientId, cancellationToken);
        if (created == null)
            throw new NotFoundException($"Patient with ID {patientId} was created but could not be retrieved.");

        return _mapper.Map<PatientResponseDto>(created);
    }

    public async Task<PatientResponseDto> GetPatientByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var patient = await _patientRepository.GetByIdAsync(id, cancellationToken);
        if (patient == null)
            throw new NotFoundException($"Patient with ID {id} was not found.");

        return _mapper.Map<PatientResponseDto>(patient);
    }

    public async Task<PatientListResponseDto> GetPatientsPagedAsync(string? countryCode, string? status, int pageNumber, int pageSize, CancellationToken cancellationToken = default)
    {
        var (items, totalCount) = await _patientRepository.GetPagedAsync(countryCode, status, pageNumber, pageSize, cancellationToken);

        return new PatientListResponseDto
        {
            Items = _mapper.Map<IEnumerable<PatientResponseDto>>(items),
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        };
    }

    public async Task UpdatePatientWithAuditAsync(Guid id, PatientUpdateRequestDto request, CancellationToken cancellationToken = default)
    {
        var validationResult = await _updateValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());
            throw new Domain.Exceptions.ValidationException(errors);
        }

        var existing = await _patientRepository.GetByIdAsync(id, cancellationToken);
        if (existing == null)
            throw new NotFoundException($"Patient with ID {id} was not found.");

        await _patientRepository.UpdateWithAuditAsync(id, request.Phone, request.City, request.FollowUpDays, request.Status, request.ChangedBy, request.Reason, cancellationToken);
    }

    public async Task<IEnumerable<AuditLogResponseDto>> GetPatientAuditHistoryAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var logs = await _patientRepository.GetAuditHistoryAsync(id, cancellationToken);
        return _mapper.Map<IEnumerable<AuditLogResponseDto>>(logs);
    }
}

public class ContactService : IContactService
{
    private readonly IContactRepository _contactRepository;
    private readonly IPatientRepository _patientRepository;
    private readonly IMapper _mapper;
    private readonly IValidator<ContactCreateRequestDto> _createValidator;
    private readonly IValidator<ContactCorrectRequestDto> _correctValidator;

    public ContactService(
        IContactRepository contactRepository,
        IPatientRepository patientRepository,
        IMapper mapper,
        IValidator<ContactCreateRequestDto> createValidator,
        IValidator<ContactCorrectRequestDto> correctValidator)
    {
        _contactRepository = contactRepository;
        _patientRepository = patientRepository;
        _mapper = mapper;
        _createValidator = createValidator;
        _correctValidator = correctValidator;
    }

    public async Task<ContactResponseDto> CreateContactAsync(ContactCreateRequestDto request, CancellationToken cancellationToken = default)
    {
        var validationResult = await _createValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());
            throw new Domain.Exceptions.ValidationException(errors);
        }

        var patient = await _patientRepository.GetByIdAsync(request.PatientId, cancellationToken);
        if (patient == null)
            throw new NotFoundException($"Patient with ID {request.PatientId} was not found.");

        if (patient.Status != "ACTIVE")
            throw new UnprocessableEntityException($"Cannot register contact: Patient status is {patient.Status}. Registration must be completed and active first.");

        var contact = _mapper.Map<Contact>(request);
        var contactId = await _contactRepository.CreateAsync(contact, cancellationToken);

        contact.Id = contactId;
        contact.IsActive = true;
        contact.CreatedAt = DateTime.UtcNow;

        return _mapper.Map<ContactResponseDto>(contact);
    }

    public async Task<IEnumerable<ContactResponseDto>> GetContactsByPatientIdAsync(Guid patientId, CancellationToken cancellationToken = default)
    {
        var patient = await _patientRepository.GetByIdAsync(patientId, cancellationToken);
        if (patient == null)
            throw new NotFoundException($"Patient with ID {patientId} was not found.");

        var contacts = await _contactRepository.GetByPatientIdAsync(patientId, cancellationToken);
        return _mapper.Map<IEnumerable<ContactResponseDto>>(contacts);
    }

    public async Task<ContactResponseDto> CorrectContactAsync(Guid originalContactId, ContactCorrectRequestDto request, CancellationToken cancellationToken = default)
    {
        var validationResult = await _correctValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());
            throw new Domain.Exceptions.ValidationException(errors);
        }

        var newContactId = await _contactRepository.CorrectAsync(
            originalContactId, request.ChangedBy, request.NewContactDate,
            request.NewChannel, request.NewResult, request.NewNotes, request.Reason, cancellationToken);

        return new ContactResponseDto
        {
            Id = newContactId,
            ContactDate = request.NewContactDate,
            Channel = request.NewChannel,
            Result = request.NewResult,
            Notes = request.NewNotes,
            IsActive = true,
            RegisteredBy = request.ChangedBy,
            CreatedAt = DateTime.UtcNow
        };
    }

    public async Task<IEnumerable<AuditLogResponseDto>> GetAuditByPatientIdAsync(Guid patientId, CancellationToken cancellationToken = default)
    {
        var logs = await _contactRepository.GetAuditByPatientIdAsync(patientId, cancellationToken);
        return _mapper.Map<IEnumerable<AuditLogResponseDto>>(logs);
    }

    public async Task<IEnumerable<AuditLogResponseDto>> GetAuditByContactIdAsync(Guid contactId, CancellationToken cancellationToken = default)
    {
        var logs = await _contactRepository.GetAuditByContactIdAsync(contactId, cancellationToken);
        return _mapper.Map<IEnumerable<AuditLogResponseDto>>(logs);
    }
}

public class RegistrationService : IRegistrationService
{
    private readonly IRegistrationLinkRepository _linkRepository;
    private readonly IPatientRepository _patientRepository;
    private readonly IMapper _mapper;
    private readonly IValidator<SelfRegIdentifyRequestDto> _identifyValidator;
    private readonly IValidator<SelfRegCompleteRequestDto> _completeValidator;

    public RegistrationService(
        IRegistrationLinkRepository linkRepository,
        IPatientRepository patientRepository,
        IMapper mapper,
        IValidator<SelfRegIdentifyRequestDto> identifyValidator,
        IValidator<SelfRegCompleteRequestDto> completeValidator)
    {
        _linkRepository = linkRepository;
        _patientRepository = patientRepository;
        _mapper = mapper;
        _identifyValidator = identifyValidator;
        _completeValidator = completeValidator;
    }

    public async Task<RegistrationLinkResponseDto> CreateLinkAsync(RegistrationLinkCreateRequestDto request, CancellationToken cancellationToken = default)
    {
        var (token, expiresAt) = await _linkRepository.CreateAsync(request.CreatedBy, request.ExpiresInDays, cancellationToken);

        return new RegistrationLinkResponseDto
        {
            Token = token,
            Url = $"/autorregistro/{token}",
            Status = "PENDING",
            ExpiresAt = expiresAt
        };
    }

    public async Task<RegistrationLinkResponseDto> ValidateLinkAsync(string token, CancellationToken cancellationToken = default)
    {
        var link = await _linkRepository.ValidateAsync(token, cancellationToken);
        if (link == null)
            throw new NotFoundException("Registration link token was not found.");

        if (link.Status == "USED")
            throw new ConflictException("Registration link has already been used.");

        if (link.ExpiresAt < DateTime.UtcNow)
            throw new Domain.Exceptions.ValidationException("Registration link has expired.");

        return _mapper.Map<RegistrationLinkResponseDto>(link);
    }

    public async Task<SelfRegIdentifyResponseDto> IdentifyPatientAsync(string token, SelfRegIdentifyRequestDto request, CancellationToken cancellationToken = default)
    {
        var validationResult = await _identifyValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());
            throw new Domain.Exceptions.ValidationException(errors);
        }

        var (patientId, sessionToken) = await _linkRepository.IdentifyPatientAsync(token, request.FullName, request.Email, request.CountryCode, cancellationToken);

        return new SelfRegIdentifyResponseDto
        {
            PatientId = patientId,
            SessionToken = sessionToken,
            NextStep = $"/autorregistro/{token}/completar"
        };
    }

    public async Task<PatientResponseDto> CompletePatientAsync(string token, SelfRegCompleteRequestDto request, CancellationToken cancellationToken = default)
    {
        var validationResult = await _completeValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());
            throw new Domain.Exceptions.ValidationException(errors);
        }

        var patientId = await _linkRepository.CompletePatientAsync(
            token, request.DocumentType, request.DocumentNumber, request.Phone,
            request.City, request.TreatmentStart, request.FollowUpDays, request.ConsentDate, cancellationToken);

        var patient = await _patientRepository.GetByIdAsync(patientId, cancellationToken);
        if (patient == null)
            throw new NotFoundException($"Patient with ID {patientId} not found after completion.");

        return _mapper.Map<PatientResponseDto>(patient);
    }
}