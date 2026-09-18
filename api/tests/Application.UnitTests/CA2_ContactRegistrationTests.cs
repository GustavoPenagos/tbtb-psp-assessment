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

public class CA2_ContactRegistrationTests
{
    private readonly IMapper _mapper;
    private readonly Mock<IContactRepository> _contactRepoMock;
    private readonly Mock<IPatientRepository> _patientRepoMock;
    private readonly ContactCreateRequestValidator _createValidator;
    private readonly ContactCorrectRequestValidator _correctValidator;
    private readonly ContactService _contactService;

    public CA2_ContactRegistrationTests()
    {
        _mapper = TestMapperFactory.Create();
        _contactRepoMock = new Mock<IContactRepository>();
        _patientRepoMock = new Mock<IPatientRepository>();
        _createValidator = new ContactCreateRequestValidator();
        _correctValidator = new ContactCorrectRequestValidator();

        _contactService = new ContactService(
            _contactRepoMock.Object,
            _patientRepoMock.Object,
            _mapper,
            _createValidator,
            _correctValidator);
    }

    [Fact]
    public async Task CA2_CreateContact_ActivePatient_ReturnsCreatedContact()
    {
        // Arrange
        var patientId = Guid.NewGuid();
        var request = new ContactCreateRequestDto
        {
            PatientId = patientId,
            ContactDate = DateTime.UtcNow,
            Channel = "PHONE",
            Result = "SUCCESSFUL_CONTACT",
            Notes = "Paciente reporta buena tolerancia al medicamento.",
            RegisteredBy = Guid.NewGuid()
        };

        _patientRepoMock
            .Setup(r => r.GetByIdAsync(patientId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Patient
            {
                Id = patientId,
                FullName = "Laura Gómez",
                Status = "ACTIVE",
                CountryCode = "CO",
                Email = "laura@example.com"
            });

        var createdContactId = Guid.NewGuid();
        _contactRepoMock
            .Setup(r => r.CreateAsync(It.IsAny<Contact>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(createdContactId);

        // Act
        var result = await _contactService.CreateContactAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(createdContactId);
        result.Channel.Should().Be("PHONE");
        result.Result.Should().Be("SUCCESSFUL_CONTACT");
        result.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task CA2_CreateContact_PendingPatient_ThrowsUnprocessableEntityException()
    {
        // Arrange (Patient is in PENDING status, cannot register contact)
        var patientId = Guid.NewGuid();
        var request = new ContactCreateRequestDto
        {
            PatientId = patientId,
            ContactDate = DateTime.UtcNow,
            Channel = "WHATSAPP",
            Result = "SUCCESSFUL_CONTACT",
            RegisteredBy = Guid.NewGuid()
        };

        _patientRepoMock
            .Setup(r => r.GetByIdAsync(patientId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Patient
            {
                Id = patientId,
                FullName = "Incomplete Registration Patient",
                Status = "PENDING",
                CountryCode = "PE",
                Email = "pending@example.com"
            });

        // Act & Assert
        var act = () => _contactService.CreateContactAsync(request);
        await act.Should().ThrowAsync<UnprocessableEntityException>()
            .WithMessage("*ACTIVE*");
    }

    [Fact]
    public async Task CA2_CreateContact_PatientNotFound_ThrowsNotFoundException()
    {
        // Arrange
        var patientId = Guid.NewGuid();
        var request = new ContactCreateRequestDto
        {
            PatientId = patientId,
            ContactDate = DateTime.UtcNow,
            Channel = "EMAIL",
            Result = "APPOINTMENT_SCHEDULED",
            RegisteredBy = Guid.NewGuid()
        };

        _patientRepoMock
            .Setup(r => r.GetByIdAsync(patientId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Patient?)null);

        // Act & Assert
        var act = () => _contactService.CreateContactAsync(request);
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task CA2_CreateContact_InvalidChannel_ThrowsValidationException()
    {
        // Arrange (Channel not in closed enum: PHONE, WHATSAPP, EMAIL)
        var request = new ContactCreateRequestDto
        {
            PatientId = Guid.NewGuid(),
            ContactDate = DateTime.UtcNow,
            Channel = "SMS_INVALID",
            Result = "SUCCESSFUL_CONTACT",
            RegisteredBy = Guid.NewGuid()
        };

        // Act & Assert
        var act = () => _contactService.CreateContactAsync(request);
        await act.Should().ThrowAsync<ValidationException>()
            .Where(e => e.Errors.ContainsKey("Channel"));
    }

    [Fact]
    public async Task CA2_CreateContact_InvalidResult_ThrowsValidationException()
    {
        // Arrange (Result not in closed enum)
        var request = new ContactCreateRequestDto
        {
            PatientId = Guid.NewGuid(),
            ContactDate = DateTime.UtcNow,
            Channel = "PHONE",
            Result = "CALLED_BACK_LATER", // Invalid result enum
            RegisteredBy = Guid.NewGuid()
        };

        // Act & Assert
        var act = () => _contactService.CreateContactAsync(request);
        await act.Should().ThrowAsync<ValidationException>()
            .Where(e => e.Errors.ContainsKey("Result"));
    }
}
