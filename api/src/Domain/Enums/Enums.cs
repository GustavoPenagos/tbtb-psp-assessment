namespace Domain.Enums;

public enum CountryCode
{
    CO,
    PE,
    EC
}

public enum DocumentType
{
    CC,
    DNI,
    CEDULA
}

public enum PatientStatus
{
    PENDING,
    ACTIVE,
    INACTIVE,
    UNREACHABLE
}

public enum RegistrationSource
{
    GESTOR,
    SELF
}

public enum ContactChannel
{
    PHONE,
    WHATSAPP,
    EMAIL
}

public enum ContactResult
{
    SUCCESSFUL_CONTACT,
    NO_ANSWER,
    WRONG_NUMBER,
    REFUSED,
    APPOINTMENT_SCHEDULED
}

public enum LinkStatus
{
    PENDING,
    STEP1_DONE,
    USED,
    EXPIRED
}

public enum UserRole
{
    GESTOR,
    COORDINADORA,
    ADMIN
}