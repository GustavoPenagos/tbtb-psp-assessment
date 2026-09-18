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

public class CA3_AuditAndCorrectionTests
{
    private readonly IMapper _mapper;
    private readonly Mock<IContactRepository> _contactRepoMock;
    private readonly Mock<IPatientRepository> _patientRepoMock;
    private readonly ContactService _contactService;
    private readonly PatientService _patientService;

    public CA3_AuditAndCorrectionTests()
    {
        _mapper = TestMapperFactory.Create();
        _contactRepoMock = new Mock<IContactRepository>();
        _patientRepoMock = new Mock<IPatientRepository>();

        _contactService = new ContactService(
            _contactRepoMock.Object,
            _patientRepoMock.Object,
            _mapper,
            new ContactCreateRequestValidator(),
            new ContactCorrectRequestValidator());

        _patientService = new PatientService(
            _patientRepoMock.Object,
            _mapper,
            new PatientRegisterRequestValidator(),
            new PatientUpdateRequestValidator());
    }

    [Fact]
    public async Task CA3_CorrectContact_ValidData_ReturnsNewActiveContact()
    {
        // Arrange
        var originalContactId = Guid.NewGuid();
        var newContactId = Guid.NewGuid();
        var changedBy = Guid.NewGuid();

        var request = new ContactCorrectRequestDto
        {
            NewContactDate = DateTime.UtcNow,
            NewChannel = "WHATSAPP",
            NewResult = "SUCCESSFUL_CONTACT",
            NewNotes = "Paciente confirmó que la cita fue reprogramada correctamente.",
            Reason = "Error en el canal digitado originalmente por el gestor", // >= 10 chars
            ChangedBy = changedBy
        };

        _contactRepoMock
            .Setup(r => r.CorrectAsync(
                originalContactId, changedBy, request.NewContactDate,
                request.NewChannel, request.NewResult, request.NewNotes, request.Reason, It.IsAny<CancellationToken>()))
            .ReturnsAsync(newContactId);

        // Act
        var result = await _contactService.CorrectContactAsync(originalContactId, request);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(newContactId);
        result.Channel.Should().Be("WHATSAPP");
        result.Result.Should().Be("SUCCESSFUL_CONTACT");
        result.IsActive.Should().BeTrue();
        result.RegisteredBy.Should().Be(changedBy);

        _contactRepoMock.Verify(r => r.CorrectAsync(
            originalContactId, changedBy, request.NewContactDate,
            request.NewChannel, request.NewResult, request.NewNotes, request.Reason, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CA3_CorrectContact_ReasonUnder10Chars_ThrowsValidationException()
    {
        // Arrange
        var originalContactId = Guid.NewGuid();
        var request = new ContactCorrectRequestDto
        {
            NewContactDate = DateTime.UtcNow,
            NewChannel = "PHONE",
            NewResult = "NO_ANSWER",
            Reason = "Error", // Only 5 characters (< 10)
            ChangedBy = Guid.NewGuid()
        };

        // Act & Assert
        var act = () => _contactService.CorrectContactAsync(originalContactId, request);
        await act.Should().ThrowAsync<ValidationException>()
            .Where(e => e.Errors.ContainsKey("Reason"));
    }

    [Fact]
    public async Task CA3_UpdatePatient_ValidData_ExecutesSuccessfully()
    {
        // Arrange
        var patientId = Guid.NewGuid();
        var changedBy = Guid.NewGuid();

        var request = new PatientUpdateRequestDto
        {
            Phone = "+573119876543",
            City = "Medellín",
            FollowUpDays = 45,
            Status = "ACTIVE",
            Reason = "Actualización de número telefónico por solicitud del paciente", // >= 10 chars
            ChangedBy = changedBy
        };

        _patientRepoMock
            .Setup(r => r.GetByIdAsync(patientId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Patient
            {
                Id = patientId,
                FullName = "Andrés Felipe Castro",
                CountryCode = "CO",
                Phone = "+573001112233",
                Email = "andres.castro@hospital.co",
                City = "Bogotá",
                FollowUpDays = 30,
                Status = "ACTIVE"
            });

        _patientRepoMock
            .Setup(r => r.UpdateWithAuditAsync(
                patientId, request.Phone, request.City, request.FollowUpDays, request.Status,
                changedBy, request.Reason, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _patientService.UpdatePatientWithAuditAsync(patientId, request);

        // Assert
        _patientRepoMock.Verify(r => r.UpdateWithAuditAsync(
            patientId, request.Phone, request.City, request.FollowUpDays, request.Status,
            changedBy, request.Reason, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CA3_UpdatePatient_ReasonUnder10Chars_ThrowsValidationException()
    {
        // Arrange
        var patientId = Guid.NewGuid();
        var request = new PatientUpdateRequestDto
        {
            Phone = "+573119876543",
            City = "Medellín",
            FollowUpDays = 45,
            Status = "ACTIVE",
            Reason = "Cambio", // Only 6 characters (< 10)
            ChangedBy = Guid.NewGuid()
        };

        // Act & Assert
        var act = () => _patientService.UpdatePatientWithAuditAsync(patientId, request);
        await act.Should().ThrowAsync<ValidationException>()
            .Where(e => e.Errors.ContainsKey("Reason"));
    }

    [Fact]
    public async Task CA3_UpdatePatient_InvalidStatus_ThrowsValidationException()
    {
        // Arrange
        var patientId = Guid.NewGuid();
        var request = new PatientUpdateRequestDto
        {
            Phone = "+573119876543",
            City = "Medellín",
            FollowUpDays = 45,
            Status = "INVALID_STATUS_VALUE",
            Reason = "Actualización con estado no permitido",
            ChangedBy = Guid.NewGuid()
        };

        // Act & Assert
        var act = () => _patientService.UpdatePatientWithAuditAsync(patientId, request);
        await act.Should().ThrowAsync<ValidationException>()
            .Where(e => e.Errors.ContainsKey("Status"));
    }

    [Fact]
    public async Task CA3_GetContactAuditByPatient_ReturnsAuditLogs()
    {
        // Arrange
        var patientId = Guid.NewGuid();
        var contactId = Guid.NewGuid();
        var changedBy = Guid.NewGuid();

        var logs = new List<ContactAuditLog>
        {
            new ContactAuditLog
            {
                Id = Guid.NewGuid(),
                ContactId = contactId,
                ChangedBy = changedBy,
                ChangedByName = "Gestor Clínico",
                ChangedAt = DateTime.UtcNow,
                Reason = "Corrección de resultado de contacto reportado erróneamente",
                PreviousValue = "{\"channel\":\"PHONE\",\"result\":\"NO_ANSWER\"}",
                NewValue = "{\"channel\":\"PHONE\",\"result\":\"SUCCESSFUL_CONTACT\"}"
            }
        };

        _contactRepoMock
            .Setup(r => r.GetAuditByPatientIdAsync(patientId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(logs);

        // Act
        var result = await _contactService.GetAuditByPatientIdAsync(patientId);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(1);
        result.First().Reason.Should().Be("Corrección de resultado de contacto reportado erróneamente");
        result.First().ChangedByName.Should().Be("Gestor Clínico");
    }

    [Fact]
    public async Task CA3_GetContactAuditByContactId_ReturnsAuditLogs()
    {
        // Arrange
        var contactId = Guid.NewGuid();
        var changedBy = Guid.NewGuid();

        var logs = new List<ContactAuditLog>
        {
            new ContactAuditLog
            {
                Id = Guid.NewGuid(),
                ContactId = contactId,
                ChangedBy = changedBy,
                ChangedByName = "Gestor Clínico",
                ChangedAt = DateTime.UtcNow,
                Reason = "Registro inicial de contacto con paciente",
                PreviousValue = "{}",
                NewValue = "{\"channel\":\"PHONE\",\"result\":\"SUCCESSFUL_CONTACT\"}"
            }
        };

        _contactRepoMock
            .Setup(r => r.GetAuditByContactIdAsync(contactId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(logs);

        // Act
        var result = await _contactService.GetAuditByContactIdAsync(contactId);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(1);
        result.First().Reason.Should().Be("Registro inicial de contacto con paciente");
    }
}
