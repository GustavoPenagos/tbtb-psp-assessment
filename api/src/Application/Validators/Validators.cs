using System.Text.RegularExpressions;
using FluentValidation;
using Application.DTOs;

namespace Application.Validators;

public static class ValidationHelpers
{
    public static readonly Regex E164PhoneRegex = new(@"^\+(57|51|593)[0-9]{8,10}$", RegexOptions.Compiled);
    public static readonly Regex EmailRegex = new(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled);
    public static readonly Regex HtmlScriptTagRegex = new(@"<[^>]*script[^>]*>", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public static bool DoesNotContainScriptTags(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return true;
        return !HtmlScriptTagRegex.IsMatch(value) && !value.Contains("<script", StringComparison.OrdinalIgnoreCase);
    }
}

public class PatientRegisterRequestValidator : AbstractValidator<PatientRegisterRequestDto>
{
    public PatientRegisterRequestValidator()
    {
        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("Full name is required.")
            .MaximumLength(200).WithMessage("Full name must not exceed 200 characters.")
            .Must(ValidationHelpers.DoesNotContainScriptTags).WithMessage("Full name contains invalid characters or HTML tags.");

        RuleFor(x => x.CountryCode)
            .NotEmpty().WithMessage("Country code is required.")
            .Must(c => c is "CO" or "PE" or "EC").WithMessage("Country code must be CO, PE, or EC.");

        RuleFor(x => x.DocumentType)
            .NotEmpty().WithMessage("Document type is required.")
            .Must((dto, docType) =>
            {
                return dto.CountryCode switch
                {
                    "CO" => docType == "CC",
                    "PE" => docType == "DNI",
                    "EC" => docType == "CEDULA",
                    _ => false
                };
            }).WithMessage("Document type must match country rules (CO: CC, PE: DNI, EC: CEDULA).");

        RuleFor(x => x.DocumentNumber)
            .NotEmpty().WithMessage("Document number is required.")
            .MaximumLength(20).WithMessage("Document number must not exceed 20 characters.");

        RuleFor(x => x.Phone)
            .NotEmpty().WithMessage("Phone is required because the primary channel is phone.")
            .Matches(ValidationHelpers.E164PhoneRegex).WithMessage("Phone must be in E.164 format with country prefix (+57, +51, or +593).");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .Matches(ValidationHelpers.EmailRegex).WithMessage("Email must be in a valid format.")
            .MaximumLength(150).WithMessage("Email must not exceed 150 characters.");

        RuleFor(x => x.FollowUpDays)
            .GreaterThan(0).WithMessage("Follow-up days defined by medical area must be greater than 0.");

        RuleFor(x => x.City)
            .NotEmpty().WithMessage("City is required.")
            .MaximumLength(100).WithMessage("City must not exceed 100 characters.");
    }
}

public class PatientUpdateRequestValidator : AbstractValidator<PatientUpdateRequestDto>
{
    public PatientUpdateRequestValidator()
    {
        RuleFor(x => x.Reason)
            .NotEmpty().WithMessage("Reason is required for auditing changes.")
            .MinimumLength(10).WithMessage("Reason is required and must be at least 10 characters long.")
            .Must(ValidationHelpers.DoesNotContainScriptTags).WithMessage("Reason contains invalid script tags.");

        RuleFor(x => x.Phone)
            .NotEmpty().WithMessage("Phone is required.")
            .Matches(ValidationHelpers.E164PhoneRegex).WithMessage("Phone must be in valid E.164 format.");

        RuleFor(x => x.FollowUpDays)
            .GreaterThan(0).WithMessage("Follow-up days must be greater than 0.");

        RuleFor(x => x.Status)
            .NotEmpty().WithMessage("Status is required.")
            .Must(s => s is "ACTIVE" or "INACTIVE" or "UNREACHABLE" or "PENDING")
            .WithMessage("Status must be ACTIVE, INACTIVE, UNREACHABLE, or PENDING.");
    }
}

public class ContactCreateRequestValidator : AbstractValidator<ContactCreateRequestDto>
{
    public ContactCreateRequestValidator()
    {
        RuleFor(x => x.PatientId)
            .NotEmpty().WithMessage("Patient ID is required.");

        RuleFor(x => x.Channel)
            .NotEmpty().WithMessage("Channel is required.")
            .Must(c => c is "PHONE" or "WHATSAPP" or "EMAIL").WithMessage("Channel must be PHONE, WHATSAPP, or EMAIL.");

        RuleFor(x => x.Result)
            .NotEmpty().WithMessage("Result is required.")
            .Must(r => r is "SUCCESSFUL_CONTACT" or "NO_ANSWER" or "WRONG_NUMBER" or "REFUSED" or "APPOINTMENT_SCHEDULED")
            .WithMessage("Result must be a valid closed enum value.");

        RuleFor(x => x.Notes)
            .MaximumLength(500).WithMessage("Notes must not exceed 500 characters.")
            .Must(ValidationHelpers.DoesNotContainScriptTags).WithMessage("Notes contain invalid script tags.");
    }
}

public class ContactCorrectRequestValidator : AbstractValidator<ContactCorrectRequestDto>
{
    public ContactCorrectRequestValidator()
    {
        RuleFor(x => x.Reason)
            .NotEmpty().WithMessage("Reason is required for contact correction.")
            .MinimumLength(10).WithMessage("Reason is required and must be at least 10 characters long.")
            .Must(ValidationHelpers.DoesNotContainScriptTags).WithMessage("Reason contains invalid script tags.");

        RuleFor(x => x.NewChannel)
            .NotEmpty().WithMessage("Channel is required.")
            .Must(c => c is "PHONE" or "WHATSAPP" or "EMAIL").WithMessage("Channel must be PHONE, WHATSAPP, or EMAIL.");

        RuleFor(x => x.NewResult)
            .NotEmpty().WithMessage("Result is required.")
            .Must(r => r is "SUCCESSFUL_CONTACT" or "NO_ANSWER" or "WRONG_NUMBER" or "REFUSED" or "APPOINTMENT_SCHEDULED")
            .WithMessage("Result must be a valid closed enum value.");

        RuleFor(x => x.NewNotes)
            .MaximumLength(500).WithMessage("Notes must not exceed 500 characters.")
            .Must(ValidationHelpers.DoesNotContainScriptTags).WithMessage("Notes contain invalid script tags.");
    }
}

public class SelfRegIdentifyRequestValidator : AbstractValidator<SelfRegIdentifyRequestDto>
{
    public SelfRegIdentifyRequestValidator()
    {
        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("Full name is required.")
            .MaximumLength(200).WithMessage("Full name must not exceed 200 characters.")
            .Must(ValidationHelpers.DoesNotContainScriptTags).WithMessage("Full name contains invalid script tags.");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .Matches(ValidationHelpers.EmailRegex).WithMessage("Email must be in a valid format.")
            .MaximumLength(150).WithMessage("Email must not exceed 150 characters.");

        RuleFor(x => x.CountryCode)
            .NotEmpty().WithMessage("Country code is required.")
            .Must(c => c is "CO" or "PE" or "EC").WithMessage("Country code must be CO, PE, or EC.");
    }
}

public class SelfRegCompleteRequestValidator : AbstractValidator<SelfRegCompleteRequestDto>
{
    public SelfRegCompleteRequestValidator()
    {
        RuleFor(x => x.DocumentType)
            .NotEmpty().WithMessage("Document type is required.")
            .Must(d => d is "CC" or "DNI" or "CEDULA").WithMessage("Document type must be CC, DNI, or CEDULA.");

        RuleFor(x => x.DocumentNumber)
            .NotEmpty().WithMessage("Document number is required.")
            .MaximumLength(20).WithMessage("Document number must not exceed 20 characters.");

        RuleFor(x => x.Phone)
            .NotEmpty().WithMessage("Phone is required.")
            .Matches(ValidationHelpers.E164PhoneRegex).WithMessage("Phone must be in E.164 format with country prefix.");

        RuleFor(x => x.City)
            .NotEmpty().WithMessage("City is required.")
            .MaximumLength(100).WithMessage("City must not exceed 100 characters.");

        RuleFor(x => x.FollowUpDays)
            .GreaterThan(0).WithMessage("Follow-up days must be greater than 0.");
    }
}