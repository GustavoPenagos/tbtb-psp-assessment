-- ============================================================================
-- SCRIPT 01: CREACIÓN DE TABLAS, RESTRICCIONES E ÍNDICES
-- Programa: Acompañamiento a Pacientes (PSP)
-- Autor: Gustavo Penagos
-- Base de Datos: SQL Server / LocalDB
-- ============================================================================

SET NOCOUNT ON;

-- ----------------------------------------------------------------------------
-- 1. TABLA: users (Gestores, coordinadores y administradores del programa)
-- ----------------------------------------------------------------------------
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'users')
BEGIN
    CREATE TABLE dbo.users (
        id          UNIQUEIDENTIFIER NOT NULL DEFAULT NEWSEQUENTIALID(),
        name        NVARCHAR(200)    NOT NULL,
        email       NVARCHAR(150)    NOT NULL,
        role        NVARCHAR(20)     NOT NULL,
        created_at  DATETIME2        NOT NULL DEFAULT SYSUTCDATETIME(),

        CONSTRAINT PK_users PRIMARY KEY CLUSTERED (id),
        CONSTRAINT UQ_users_email UNIQUE (email),
        CONSTRAINT CK_users_role CHECK (role IN ('GESTOR', 'COORDINADORA', 'ADMIN'))
    );
END;
GO

-- ----------------------------------------------------------------------------
-- 2. TABLA: patients (Pacientes del programa en CO, PE y EC)
-- ----------------------------------------------------------------------------
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'patients')
BEGIN
    CREATE TABLE dbo.patients (
        id                   UNIQUEIDENTIFIER NOT NULL DEFAULT NEWSEQUENTIALID(),
        full_name            NVARCHAR(200)    NOT NULL,
        document_type        NVARCHAR(10)     NULL,
        document_number      NVARCHAR(20)     NULL,
        country_code         CHAR(2)          NOT NULL,
        phone                NVARCHAR(20)     NULL,
        email                NVARCHAR(150)    NOT NULL,
        city                 NVARCHAR(100)    NULL,
        treatment_start      DATE             NULL,
        follow_up_days       INT              NULL,
        status               NVARCHAR(20)     NOT NULL DEFAULT 'PENDING',
        registration_source  NVARCHAR(20)     NOT NULL DEFAULT 'GESTOR',
        consent_date         DATE             NULL,
        created_by           UNIQUEIDENTIFIER NOT NULL,
        created_at           DATETIME2        NOT NULL DEFAULT SYSUTCDATETIME(),
        updated_at           DATETIME2        NOT NULL DEFAULT SYSUTCDATETIME(),

        CONSTRAINT PK_patients PRIMARY KEY CLUSTERED (id),
        CONSTRAINT FK_patients_created_by FOREIGN KEY (created_by) REFERENCES dbo.users (id),
        CONSTRAINT CK_patients_country CHECK (country_code IN ('CO', 'PE', 'EC')),
        CONSTRAINT CK_patients_doc_type CHECK (document_type IS NULL OR document_type IN ('CC', 'DNI', 'CEDULA')),
        CONSTRAINT CK_patients_status CHECK (status IN ('PENDING', 'ACTIVE', 'INACTIVE', 'UNREACHABLE')),
        CONSTRAINT CK_patients_source CHECK (registration_source IN ('GESTOR', 'SELF'))
    );
END;
GO

-- ----------------------------------------------------------------------------
-- 3. TABLA: registration_links (Tokens generados por el gestor para autorregistro)
-- ----------------------------------------------------------------------------
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'registration_links')
BEGIN
    CREATE TABLE dbo.registration_links (
        id          UNIQUEIDENTIFIER NOT NULL DEFAULT NEWSEQUENTIALID(),
        token       NVARCHAR(64)     NOT NULL,
        created_by  UNIQUEIDENTIFIER NOT NULL,
        patient_id  UNIQUEIDENTIFIER NULL,
        status      NVARCHAR(20)     NOT NULL DEFAULT 'PENDING',
        expires_at  DATETIME2        NOT NULL,
        created_at  DATETIME2        NOT NULL DEFAULT SYSUTCDATETIME(),
        used_at     DATETIME2        NULL,

        CONSTRAINT PK_registration_links PRIMARY KEY CLUSTERED (id),
        CONSTRAINT UQ_registration_links_token UNIQUE (token),
        CONSTRAINT FK_reg_links_created_by FOREIGN KEY (created_by) REFERENCES dbo.users (id),
        CONSTRAINT FK_reg_links_patient FOREIGN KEY (patient_id) REFERENCES dbo.patients (id),
        CONSTRAINT CK_reg_links_status CHECK (status IN ('PENDING', 'STEP1_DONE', 'USED', 'EXPIRED'))
    );
END;
GO

-- ----------------------------------------------------------------------------
-- 4. TABLA: contacts (Historial de interacciones multicanal con pacientes)
-- ----------------------------------------------------------------------------
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'contacts')
BEGIN
    CREATE TABLE dbo.contacts (
        id             UNIQUEIDENTIFIER NOT NULL DEFAULT NEWSEQUENTIALID(),
        patient_id     UNIQUEIDENTIFIER NOT NULL,
        contact_date   DATETIME2        NOT NULL,
        channel        NVARCHAR(20)     NOT NULL,
        result         NVARCHAR(30)     NOT NULL,
        notes          NVARCHAR(500)    NULL,
        is_active      BIT              NOT NULL DEFAULT 1,
        registered_by  UNIQUEIDENTIFIER NOT NULL,
        created_at     DATETIME2        NOT NULL DEFAULT SYSUTCDATETIME(),

        CONSTRAINT PK_contacts PRIMARY KEY CLUSTERED (id),
        CONSTRAINT FK_contacts_patient FOREIGN KEY (patient_id) REFERENCES dbo.patients (id),
        CONSTRAINT FK_contacts_registered_by FOREIGN KEY (registered_by) REFERENCES dbo.users (id),
        CONSTRAINT CK_contacts_channel CHECK (channel IN ('PHONE', 'WHATSAPP', 'EMAIL')),
        CONSTRAINT CK_contacts_result CHECK (result IN ('SUCCESSFUL_CONTACT', 'NO_ANSWER', 'WRONG_NUMBER', 'REFUSED', 'APPOINTMENT_SCHEDULED'))
    );
END;
GO

-- ----------------------------------------------------------------------------
-- 5. TABLA: contact_audit_log (Registro inmutable de auditoría para correcciones GxP)
-- ----------------------------------------------------------------------------
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'contact_audit_log')
BEGIN
    CREATE TABLE dbo.contact_audit_log (
        id              UNIQUEIDENTIFIER NOT NULL DEFAULT NEWSEQUENTIALID(),
        contact_id      UNIQUEIDENTIFIER NOT NULL,
        changed_by      UNIQUEIDENTIFIER NOT NULL,
        changed_at      DATETIME2        NOT NULL DEFAULT SYSUTCDATETIME(),
        reason          NVARCHAR(300)    NOT NULL,
        previous_value  NVARCHAR(MAX)    NOT NULL,
        new_value       NVARCHAR(MAX)    NOT NULL,

        CONSTRAINT PK_contact_audit_log PRIMARY KEY CLUSTERED (id),
        CONSTRAINT FK_audit_log_changed_by FOREIGN KEY (changed_by) REFERENCES dbo.users (id)
    );
END;
GO

-- ============================================================================
-- ÍNDICES DE RENDIMIENTO E INTEGRIDAD
-- ============================================================================

-- Clave única filtrada para documentos de pacientes:
-- Permite pacientes en autorregistro paso 1 (NULL) sin colisionar
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'UX_patient_doc_filtered' AND object_id = OBJECT_ID('dbo.patients'))
BEGIN
    CREATE UNIQUE NONCLUSTERED INDEX UX_patient_doc_filtered
    ON dbo.patients (country_code, document_type, document_number)
    WHERE document_type IS NOT NULL AND document_number IS NOT NULL;
END;
GO

-- Clave única para email de paciente (cada paciente debe ser único)
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'UX_patients_email' AND object_id = OBJECT_ID('dbo.patients'))
BEGIN
    CREATE UNIQUE NONCLUSTERED INDEX UX_patients_email
    ON dbo.patients (email);
END;
GO

-- Clave única filtrada para teléfono de paciente (formato E.164 único, permite NULL en paso 1)
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'UX_patients_phone_filtered' AND object_id = OBJECT_ID('dbo.patients'))
BEGIN
    CREATE UNIQUE NONCLUSTERED INDEX UX_patients_phone_filtered
    ON dbo.patients (phone)
    WHERE phone IS NOT NULL;
END;
GO

-- Índice para historial de contactos por paciente ordenado cronológicamente
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_contacts_patient_date' AND object_id = OBJECT_ID('dbo.contacts'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_contacts_patient_date
    ON dbo.contacts (patient_id, contact_date DESC);
END;
GO

-- Índice para optimizar consultas de contactos activos y resultados
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_contacts_active_result' AND object_id = OBJECT_ID('dbo.contacts'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_contacts_active_result
    ON dbo.contacts (patient_id, is_active, result);
END;
GO

-- Índice de cobertura para consultas por gestor y rango de fechas (CA-4 / Reportes)
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_contacts_gestor_date' AND object_id = OBJECT_ID('dbo.contacts'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_contacts_gestor_date
    ON dbo.contacts (registered_by, contact_date)
    INCLUDE (patient_id, channel, result, is_active);
END;
GO