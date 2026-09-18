using Application.DTOs;
using Application.Interfaces;
using Application.Mappings;
using Application.Services;
using Application.Validators;
using AutoMapper;
using Domain.Entities;
using Domain.Exceptions;
using FluentAssertions;
using Moq;
using Xunit;

namespace Application.UnitTests;

public class CA1_PatientRegistrationTests
{
    private readonly IMapper _mapper;
    private readonly Mock<IPatientRepository> _patientRepoMock;
    private readonly PatientRegisterRequestValidator _registerValidator;
    private readonly PatientUpdateRequestValidator _updateValidator;
    private readonly PatientService _patientService;

    public CA1_PatientRegistrationTests()
    {
        _mapper = TestMapperFactory.Create();
        _patientRepoMock = new Mock<IPatientRepository>();
        _registerValidator = new PatientRegisterRequestValidator();
        _updateValidator = new PatientUpdateRequestValidator();

        _patientService = new PatientService(
            _patientRepoMock.Object,
            _mapper,
            _registerValidator,
            _updateValidator);
    }

    [Fact]
    public async Task CA1_RegisterPatient_ValidData_ReturnsActivePatient()
    {
        // Arrange
        var request = new PatientRegisterRequestDto
        {
            FullName = "Carlos Alberto Morales",
            CountryCode = "CO",
            DocumentType = "CC",
            DocumentNumber = "1020304050",
            Phone = "+573001234567",
            Email = "carlos.morales@hospital.co",
            City = "Bogotá",
            TreatmentStart = DateTime.UtcNow.Date,
            FollowUpDays = 30,
            ConsentDate = DateTime.UtcNow.Date,
            CreatedBy = Guid.NewGuid()
        };

        var createdId = Guid.NewGuid();
        _patientRepoMock
            .Setup(r => r.RegisterAsync(It.IsAny<Patient>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(createdId);

        _patientRepoMock
            .Setup(r => r.GetByIdAsync(createdId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Patient
            {
                Id = createdId,
                FullName = request.FullName,
                CountryCode = request.CountryCode,
                DocumentType = request.DocumentType,
                DocumentNumber = request.DocumentNumber,
                Phone = request.Phone,
                Email = request.Email,
                City = request.City,
                TreatmentStart = request.TreatmentStart,
                FollowUpDays = request.FollowUpDays,
                Status = "ACTIVE",
                RegistrationSource = "GESTOR",
                ConsentDate = request.ConsentDate,
                CreatedBy = request.CreatedBy,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });

        // Act
        var result = await _patientService.RegisterPatientAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(createdId);
        result.Status.Should().Be("ACTIVE");
        result.RegistrationSource.Should().Be("GESTOR");
        result.Phone.Should().Be("+573001234567");
        result.Email.Should().Be("carlos.morales@hospital.co");

        _patientRepoMock.Verify(r => r.RegisterAsync(It.Is<Patient>(p => p.FullName == request.FullName), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CA1_RegisterPatient_InvalidPhone_ThrowsValidationException()
    {
        // Arrange (invalid phone missing country prefix or invalid format)
        var request = new PatientRegisterRequestDto
        {
            FullName = "Carlos Alberto Morales",
            CountryCode = "CO",
            DocumentType = "CC",
            DocumentNumber = "1020304050",
            Phone = "3001234567", // Non-E.164 without '+'
            Email = "carlos.morales@hospital.co",
            City = "Bogotá",
            TreatmentStart = DateTime.UtcNow.Date,
            FollowUpDays = 30,
            CreatedBy = Guid.NewGuid()
        };

        // Act & Assert
        var act = () => _patientService.RegisterPatientAsync(request);
        await act.Should().ThrowAsync<ValidationException>()
            .Where(e => e.Errors.ContainsKey("Phone"));
    }

    [Fact]
    public async Task CA1_RegisterPatient_DuplicateDocument_ThrowsConflictException()
    {
        // Arrange
        var request = new PatientRegisterRequestDto
        {
            FullName = "Carlos Alberto Morales",
            CountryCode = "CO",
            DocumentType = "CC",
            DocumentNumber = "1020304050",
            Phone = "+573001234567",
            Email = "carlos.morales@hospital.co",
            City = "Bogotá",
            TreatmentStart = DateTime.UtcNow.Date,
            FollowUpDays = 30,
            CreatedBy = Guid.NewGuid()
        };

        _patientRepoMock
            .Setup(r => r.RegisterAsync(It.IsAny<Patient>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ConflictException("Patient with this country and document number already exists."));

        // Act & Assert
        var act = () => _patientService.RegisterPatientAsync(request);
        await act.Should().ThrowAsync<ConflictException>();
    }

    [Fact]
    public async Task CA1_SelfReg_IdentifyPatient_ValidData_ReturnsSessionToken()
    {
        // Arrange
        var linkRepoMock = new Mock<IRegistrationLinkRepository>();
        var identifyValidator = new SelfRegIdentifyRequestValidator();
        var completeValidator = new SelfRegCompleteRequestValidator();

        var regService = new RegistrationService(
            linkRepoMock.Object,
            _patientRepoMock.Object,
            _mapper,
            identifyValidator,
            completeValidator);

        var token = "abc123token";
        var request = new SelfRegIdentifyRequestDto
        {
            FullName = "María Elena Rossi",
            Email = "maria.rossi@outlook.pe",
            CountryCode = "PE"
        };

        var patientId = Guid.NewGuid();
        linkRepoMock
            .Setup(r => r.IdentifyPatientAsync(token, request.FullName, request.Email, request.CountryCode, It.IsAny<CancellationToken>()))
            .ReturnsAsync((patientId, token));

        // Act
        var result = await regService.IdentifyPatientAsync(token, request);

        // Assert
        result.Should().NotBeNull();
        result.PatientId.Should().Be(patientId);
        result.SessionToken.Should().Be(token);
        result.NextStep.Should().Contain("/completar");
    }

    [Fact]
    public async Task CA1_SelfReg_CompletePatient_ValidData_ActivatesPatient()
    {
        // Arrange
        var linkRepoMock = new Mock<IRegistrationLinkRepository>();
        var identifyValidator = new SelfRegIdentifyRequestValidator();
        var completeValidator = new SelfRegCompleteRequestValidator();

        var regService = new RegistrationService(
            linkRepoMock.Object,
            _patientRepoMock.Object,
            _mapper,
            identifyValidator,
            completeValidator);

        var token = "abc123token";
        var patientId = Guid.NewGuid();
        var request = new SelfRegCompleteRequestDto
        {
            DocumentType = "DNI",
            DocumentNumber = "71829304",
            Phone = "+51987654321",
            City = "Lima",
            TreatmentStart = DateTime.UtcNow.Date,
            FollowUpDays = 15,
            ConsentDate = DateTime.UtcNow.Date
        };

        linkRepoMock
            .Setup(r => r.CompletePatientAsync(
                token, request.DocumentType, request.DocumentNumber, request.Phone,
                request.City, request.TreatmentStart, request.FollowUpDays, request.ConsentDate, It.IsAny<CancellationToken>()))
            .ReturnsAsync(patientId);

        _patientRepoMock
            .Setup(r => r.GetByIdAsync(patientId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Patient
            {
                Id = patientId,
                FullName = "María Elena Rossi",
                CountryCode = "PE",
                DocumentType = request.DocumentType,
                DocumentNumber = request.DocumentNumber,
                Phone = request.Phone,
                Email = "maria.rossi@outlook.pe",
                City = request.City,
                TreatmentStart = request.TreatmentStart,
                FollowUpDays = request.FollowUpDays,
                Status = "ACTIVE",
                RegistrationSource = "SELF",
                ConsentDate = request.ConsentDate,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });

        // Act
        var result = await regService.CompletePatientAsync(token, request);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(patientId);
        result.Status.Should().Be("ACTIVE");
        result.RegistrationSource.Should().Be("SELF");
        result.Phone.Should().Be("+51987654321");
    }
}
