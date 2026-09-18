-- ============================================================================
-- SCRIPT 02: PROCEDIMIENTOS ALMACENADOS TRANSACCIONALES (SP)
-- Programa: Acompañamiento a Pacientes (PSP)
-- Autor: Gustavo Penagos
-- Base de Datos: SQL Server / LocalDB
-- Operaciones: Insert, Update, Soft-Delete y Get con control ACID y TRY...CATCH
-- ============================================================================

SET NOCOUNT ON;
GO

-- ============================================================================
-- 1. PACIENTES (CA-1)
-- ============================================================================

-- ----------------------------------------------------------------------------
-- SP: sp_RegisterPatient (Registro completo por gestor)
-- ----------------------------------------------------------------------------
CREATE OR ALTER PROCEDURE dbo.sp_RegisterPatient
    @FullName        NVARCHAR(200),
    @DocumentType    NVARCHAR(10),
    @DocumentNumber  NVARCHAR(20),
    @CountryCode     CHAR(2),
    @Phone           NVARCHAR(20),
    @Email           NVARCHAR(150),
    @City            NVARCHAR(100),
    @TreatmentStart  DATE,
    @FollowUpDays    INT,
    @ConsentDate     DATE,
    @CreatedBy       UNIQUEIDENTIFIER,
    @NewPatientId    UNIQUEIDENTIFIER OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRY
        BEGIN TRANSACTION;

        -- Validar unicidad de documento por país
        IF EXISTS (
            SELECT 1 FROM dbo.patients 
            WHERE country_code = @CountryCode 
              AND document_type = @DocumentType 
              AND document_number = @DocumentNumber
        )
        BEGIN
            THROW 50001, 'Patient with this country and document number already exists.', 1;
        END;

        SET @NewPatientId = NEWSEQUENTIALID();

        INSERT INTO dbo.patients (
            id, full_name, document_type, document_number, country_code,
            phone, email, city, treatment_start, follow_up_days,
            status, registration_source, consent_date, created_by,
            created_at, updated_at
        )
        VALUES (
            @NewPatientId, @FullName, @DocumentType, @DocumentNumber, @CountryCode,
            @Phone, @Email, @City, @TreatmentStart, @FollowUpDays,
            'ACTIVE', 'GESTOR', @ConsentDate, @CreatedBy,
            SYSUTCDATETIME(), SYSUTCDATETIME()
        );

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END;
GO

-- ----------------------------------------------------------------------------
-- SP: sp_GetPatientById (Consulta por identificador único)
-- ----------------------------------------------------------------------------
CREATE OR ALTER PROCEDURE dbo.sp_GetPatientById
    @PatientId UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;

    SELECT id, full_name, document_type, document_number, country_code,
           phone, email, city, treatment_start, follow_up_days,
           status, registration_source, consent_date, created_by,
           created_at, updated_at
    FROM dbo.patients
    WHERE id = @PatientId;
END;
GO

-- ----------------------------------------------------------------------------
-- SP: sp_GetPatients (Consulta con filtros y paginación)
-- ----------------------------------------------------------------------------
CREATE OR ALTER PROCEDURE dbo.sp_GetPatients
    @CountryCode CHAR(2) = NULL,
    @Status NVARCHAR(20) = NULL,
    @PageNumber INT = 1,
    @PageSize INT = 20
AS
BEGIN
    SET NOCOUNT ON;

    SELECT id, full_name, document_type, document_number, country_code,
           phone, email, city, treatment_start, follow_up_days,
           status, registration_source, consent_date, created_by,
           created_at, updated_at,
           COUNT(*) OVER() AS total_count
    FROM dbo.patients
    WHERE (@CountryCode IS NULL OR country_code = @CountryCode)
      AND (@Status IS NULL OR status = @Status)
    ORDER BY created_at DESC
    OFFSET (@PageNumber - 1) * @PageSize ROWS
    FETCH NEXT @PageSize ROWS ONLY;
END;
GO

-- ============================================================================
-- 2. AUTORREGISTRO EN 2 PASOS (CA-1 VARIANTE)
-- ============================================================================

-- ----------------------------------------------------------------------------
-- SP: sp_CreateRegistrationLink (Gestor genera enlace con token)
-- ----------------------------------------------------------------------------
CREATE OR ALTER PROCEDURE dbo.sp_CreateRegistrationLink
    @CreatedBy      UNIQUEIDENTIFIER,
    @ExpiresInDays  INT = 7,
    @Token          NVARCHAR(64) OUTPUT,
    @ExpiresAt      DATETIME2 OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    SET @Token = REPLACE(CONVERT(NVARCHAR(36), NEWID()), '-', '');
    SET @ExpiresAt = DATEADD(DAY, ISNULL(@ExpiresInDays, 7), SYSUTCDATETIME());

    INSERT INTO dbo.registration_links (
        id, token, created_by, status, expires_at, created_at
    )
    VALUES (
        NEWSEQUENTIALID(), @Token, @CreatedBy, 'PENDING', @ExpiresAt, SYSUTCDATETIME()
    );
END;
GO

-- ----------------------------------------------------------------------------
-- SP: sp_ValidateRegistrationLink (Validación de vigencia del token)
-- ----------------------------------------------------------------------------
CREATE OR ALTER PROCEDURE dbo.sp_ValidateRegistrationLink
    @Token NVARCHAR(64)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT id, token, created_by, patient_id, status, expires_at, created_at, used_at,
           CASE 
               WHEN status = 'USED' THEN 'USED'
               WHEN expires_at < SYSUTCDATETIME() THEN 'EXPIRED'
               ELSE status
           END AS computed_status
    FROM dbo.registration_links
    WHERE token = @Token;
END;
GO

-- ----------------------------------------------------------------------------
-- SP: sp_IdentifyPatientSelfReg (Paso 1: Identificación y filtro de acceso)
-- ----------------------------------------------------------------------------
CREATE OR ALTER PROCEDURE dbo.sp_IdentifyPatientSelfReg
    @Token        NVARCHAR(64),
    @FullName     NVARCHAR(200),
    @Email        NVARCHAR(150),
    @CountryCode  CHAR(2),
    @PatientId    UNIQUEIDENTIFIER OUTPUT,
    @SessionToken NVARCHAR(64) OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRY
        BEGIN TRANSACTION;

        DECLARE @LinkId UNIQUEIDENTIFIER, @CreatedBy UNIQUEIDENTIFIER, @LinkStatus NVARCHAR(20), @ExpiresAt DATETIME2;

        SELECT @LinkId = id, @CreatedBy = created_by, @LinkStatus = status, @ExpiresAt = expires_at
        FROM dbo.registration_links
        WHERE token = @Token;

        IF @LinkId IS NULL
            THROW 50010, 'Registration token not found.', 1;

        IF @LinkStatus = 'USED'
            THROW 50011, 'Registration token has already been used.', 1;

        IF @ExpiresAt < SYSUTCDATETIME()
            THROW 50012, 'Registration token has expired.', 1;

        -- Crear registro preliminar de paciente en estado PENDING
        SET @PatientId = NEWSEQUENTIALID();

        INSERT INTO dbo.patients (
            id, full_name, email, country_code, status,
            registration_source, created_by, created_at, updated_at
        )
        VALUES (
            @PatientId, @FullName, @Email, @CountryCode, 'PENDING',
            'SELF', @CreatedBy, SYSUTCDATETIME(), SYSUTCDATETIME()
        );

        -- Actualizar el enlace asociando el paciente y marcando paso 1 completado
        UPDATE dbo.registration_links
        SET patient_id = @PatientId,
            status = 'STEP1_DONE'
        WHERE id = @LinkId;

        SET @SessionToken = @Token;

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END;
GO

-- ----------------------------------------------------------------------------
-- SP: sp_CompletePatientSelfReg (Paso 2: Datos obligatorios y activación)
-- ----------------------------------------------------------------------------
CREATE OR ALTER PROCEDURE dbo.sp_CompletePatientSelfReg
    @Token           NVARCHAR(64),
    @DocumentType    NVARCHAR(10),
    @DocumentNumber  NVARCHAR(20),
    @Phone           NVARCHAR(20),
    @City            NVARCHAR(100),
    @TreatmentStart  DATE,
    @FollowUpDays    INT,
    @ConsentDate     DATE = NULL,
    @PatientId       UNIQUEIDENTIFIER OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRY
        BEGIN TRANSACTION;

        DECLARE @LinkId UNIQUEIDENTIFIER, @CurrentStatus NVARCHAR(20), @ExpiresAt DATETIME2, @CountryCode CHAR(2);

        SELECT @LinkId = l.id, @PatientId = l.patient_id, @CurrentStatus = l.status, 
               @ExpiresAt = l.expires_at, @CountryCode = p.country_code
        FROM dbo.registration_links l
        INNER JOIN dbo.patients p ON l.patient_id = p.id
        WHERE l.token = @Token;

        IF @LinkId IS NULL OR @PatientId IS NULL
            THROW 50020, 'Invalid registration session. Step 1 must be completed first.', 1;

        IF @CurrentStatus <> 'STEP1_DONE'
            THROW 50021, 'Invalid registration status for completing registration.', 1;

        IF @ExpiresAt < SYSUTCDATETIME()
            THROW 50022, 'Registration token has expired.', 1;

        -- Validar unicidad de documento
        IF EXISTS (
            SELECT 1 FROM dbo.patients
            WHERE country_code = @CountryCode
              AND document_type = @DocumentType
              AND document_number = @DocumentNumber
              AND id <> @PatientId
        )
        BEGIN
            THROW 50001, 'Patient with this country and document number already exists.', 1;
        END;

        -- Actualizar datos del paciente a ACTIVE
        UPDATE dbo.patients
        SET document_type = @DocumentType,
            document_number = @DocumentNumber,
            phone = @Phone,
            city = @City,
            treatment_start = @TreatmentStart,
            follow_up_days = @FollowUpDays,
            consent_date = ISNULL(@ConsentDate, SYSUTCDATETIME()),
            status = 'ACTIVE',
            updated_at = SYSUTCDATETIME()
        WHERE id = @PatientId;

        -- Invalidar token (un solo uso)
        UPDATE dbo.registration_links
        SET status = 'USED',
            used_at = SYSUTCDATETIME()
        WHERE id = @LinkId;

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END;
GO

-- ============================================================================
-- 3. CONTACTOS (CA-2)
-- ============================================================================

-- ----------------------------------------------------------------------------
-- SP: sp_CreateContact (Registrar nuevo contacto sobre paciente activo)
-- ----------------------------------------------------------------------------
CREATE OR ALTER PROCEDURE dbo.sp_CreateContact
    @PatientId      UNIQUEIDENTIFIER,
    @ContactDate    DATETIME2,
    @Channel        NVARCHAR(20),
    @Result         NVARCHAR(30),
    @Notes          NVARCHAR(500),
    @RegisteredBy   UNIQUEIDENTIFIER,
    @NewContactId   UNIQUEIDENTIFIER OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRY
        BEGIN TRANSACTION;

        -- Validar que el paciente exista y esté en estado ACTIVE
        DECLARE @PatientStatus NVARCHAR(20);
        SELECT @PatientStatus = status FROM dbo.patients WHERE id = @PatientId;

        IF @PatientStatus IS NULL
            THROW 50030, 'Patient not found.', 1;

        IF @PatientStatus <> 'ACTIVE'
            THROW 50031, 'Cannot register contact: Patient is not in ACTIVE status.', 1;

        SET @NewContactId = NEWSEQUENTIALID();

        INSERT INTO dbo.contacts (
            id, patient_id, contact_date, channel, result, notes,
            is_active, registered_by, created_at
        )
        VALUES (
            @NewContactId, @PatientId, @ContactDate, @Channel, @Result, @Notes,
            1, @RegisteredBy, SYSUTCDATETIME()
        );

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END;
GO

-- ----------------------------------------------------------------------------
-- SP: sp_GetContactsByPatient (Historial de contactos activos del paciente)
-- ----------------------------------------------------------------------------
CREATE OR ALTER PROCEDURE dbo.sp_GetContactsByPatient
    @PatientId UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;

    SELECT c.id, c.patient_id, c.contact_date, c.channel, c.result, c.notes,
           c.is_active, c.registered_by, u.name AS registered_by_name, c.created_at
    FROM dbo.contacts c
    INNER JOIN dbo.users u ON c.registered_by = u.id
    WHERE c.patient_id = @PatientId
      AND c.is_active = 1
    ORDER BY c.contact_date DESC;
END;
GO

-- ============================================================================
-- 4. CORRECCIÓN Y AUDITORÍA INMUTABLE (CA-3)
-- ============================================================================

-- ----------------------------------------------------------------------------
-- SP: sp_CorrectContact (Soft-update atómico con registro en contact_audit_log)
-- ----------------------------------------------------------------------------
CREATE OR ALTER PROCEDURE dbo.sp_CorrectContact
    @OriginalContactId UNIQUEIDENTIFIER,
    @ChangedBy         UNIQUEIDENTIFIER,
    @NewContactDate    DATETIME2,
    @NewChannel        NVARCHAR(20),
    @NewResult         NVARCHAR(30),
    @NewNotes          NVARCHAR(500),
    @Reason            NVARCHAR(300),
    @NewContactId      UNIQUEIDENTIFIER OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRY
        BEGIN TRANSACTION;

        -- 1. Validar motivo obligatorio
        IF @Reason IS NULL OR LTRIM(RTRIM(@Reason)) = ''
            THROW 50040, 'Reason is required for contact correction.', 1;

        -- 2. Validar existencia de contacto original activo
        DECLARE @PatientId UNIQUEIDENTIFIER, @PrevJson NVARCHAR(MAX);
        SELECT @PatientId = patient_id,
               @PrevJson = (SELECT id, patient_id, contact_date, channel, result, notes, registered_by, created_at 
                            FOR JSON PATH, WITHOUT_ARRAY_WRAPPER)
        FROM dbo.contacts 
        WHERE id = @OriginalContactId AND is_active = 1;

        IF @PatientId IS NULL
            THROW 50041, 'Contact not found or has already been corrected.', 1;

        -- 3. Soft-delete del registro original
        UPDATE dbo.contacts 
        SET is_active = 0 
        WHERE id = @OriginalContactId;

        -- 4. Insertar nuevo contacto activo con los datos corregidos
        SET @NewContactId = NEWSEQUENTIALID();
        INSERT INTO dbo.contacts (
            id, patient_id, contact_date, channel, result, notes, 
            is_active, registered_by, created_at
        )
        VALUES (
            @NewContactId, @PatientId, @NewContactDate, @NewChannel, @NewResult, @NewNotes, 
            1, @ChangedBy, SYSUTCDATETIME()
        );

        -- 5. Generar snapshot del nuevo registro e insertar auditoría inmutable
        DECLARE @NewJson NVARCHAR(MAX) = (
            SELECT @NewContactId AS id, @PatientId AS patient_id, @NewContactDate AS contact_date, 
                   @NewChannel AS channel, @NewResult AS result, @NewNotes AS notes, @ChangedBy AS registered_by 
            FOR JSON PATH, WITHOUT_ARRAY_WRAPPER
        );

        INSERT INTO dbo.contact_audit_log (
            id, contact_id, changed_by, changed_at, reason, previous_value, new_value
        )
        VALUES (
            NEWSEQUENTIALID(), @OriginalContactId, @ChangedBy, SYSUTCDATETIME(), @Reason, @PrevJson, @NewJson
        );

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END;
GO