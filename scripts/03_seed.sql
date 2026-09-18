-- ============================================================================
-- SCRIPT 03: DATOS SEMILLA DE PRUEBA (SEED)
-- Programa: Acompañamiento a Pacientes (PSP)
-- Autor: Gustavo Penagos
-- Base de Datos: SQL Server / LocalDB
-- Carga: Gestores, pacientes multi-país (CO/PE/EC), contactos, auditoría y tokens
-- ============================================================================

SET NOCOUNT ON;
GO

-- ----------------------------------------------------------------------------
-- 1. USUARIOS (Coordinadora y Gestores)
-- ----------------------------------------------------------------------------
DECLARE @AdminId  UNIQUEIDENTIFIER = '11111111-1111-1111-1111-111111111111';
DECLARE @Gestor1Id UNIQUEIDENTIFIER = '22222222-2222-2222-2222-222222222222';
DECLARE @Gestor2Id UNIQUEIDENTIFIER = '33333333-3333-3333-3333-333333333333';

IF NOT EXISTS (SELECT 1 FROM dbo.users WHERE id = @AdminId)
    INSERT INTO dbo.users (id, name, email, role, created_at)
    VALUES (@AdminId, 'Dra. Ana Martínez (Coordinadora)', 'ana.martinez@psp-tbtb.com', 'COORDINADORA', SYSUTCDATETIME());

IF NOT EXISTS (SELECT 1 FROM dbo.users WHERE id = @Gestor1Id)
    INSERT INTO dbo.users (id, name, email, role, created_at)
    VALUES (@Gestor1Id, 'Juan Carlos Gestor', 'juan.gestor@psp-tbtb.com', 'GESTOR', SYSUTCDATETIME());

IF NOT EXISTS (SELECT 1 FROM dbo.users WHERE id = @Gestor2Id)
    INSERT INTO dbo.users (id, name, email, role, created_at)
    VALUES (@Gestor2Id, 'Laura Gómez Gestora', 'laura.gestora@psp-tbtb.com', 'GESTOR', SYSUTCDATETIME());

-- ----------------------------------------------------------------------------
-- 2. PACIENTES EN COLOMBIA, PERÚ Y ECUADOR
-- ----------------------------------------------------------------------------
DECLARE @PatientCo1Id UNIQUEIDENTIFIER = '44444444-4444-4444-4444-444444444441';
DECLARE @PatientCo2Id UNIQUEIDENTIFIER = '44444444-4444-4444-4444-444444444442';
DECLARE @PatientPe1Id UNIQUEIDENTIFIER = '44444444-4444-4444-4444-444444444443';
DECLARE @PatientEc1Id UNIQUEIDENTIFIER = '44444444-4444-4444-4444-444444444444';
DECLARE @PatientPendingId UNIQUEIDENTIFIER = '44444444-4444-4444-4444-444444444445';

-- Paciente 1 (Colombia - Activo)
IF NOT EXISTS (SELECT 1 FROM dbo.patients WHERE id = @PatientCo1Id)
    INSERT INTO dbo.patients (
        id, full_name, document_type, document_number, country_code,
        phone, email, city, treatment_start, follow_up_days,
        status, registration_source, consent_date, created_by, created_at
    )
    VALUES (
        @PatientCo1Id, 'Carlos Eduardo Restrepo', 'CC', '1017123456', 'CO',
        '+573105551234', 'carlos.restrepo@example.com', 'Medellín', '2026-08-01', 30,
        'ACTIVE', 'GESTOR', '2026-08-01', @Gestor1Id, SYSUTCDATETIME()
    );

-- Paciente 2 (Colombia - Activo)
IF NOT EXISTS (SELECT 1 FROM dbo.patients WHERE id = @PatientCo2Id)
    INSERT INTO dbo.patients (
        id, full_name, document_type, document_number, country_code,
        phone, email, city, treatment_start, follow_up_days,
        status, registration_source, consent_date, created_by, created_at
    )
    VALUES (
        @PatientCo2Id, 'María Fernanda Morales', 'CC', '52899123', 'CO',
        '+573007778899', 'maria.morales@example.com', 'Bogotá', '2026-08-15', 45,
        'ACTIVE', 'GESTOR', '2026-08-15', @Gestor1Id, SYSUTCDATETIME()
    );

-- Paciente 3 (Perú - Activo)
IF NOT EXISTS (SELECT 1 FROM dbo.patients WHERE id = @PatientPe1Id)
    INSERT INTO dbo.patients (
        id, full_name, document_type, document_number, country_code,
        phone, email, city, treatment_start, follow_up_days,
        status, registration_source, consent_date, created_by, created_at
    )
    VALUES (
        @PatientPe1Id, 'Jorge Luis Quispe', 'DNI', '45891234', 'PE',
        '+51987654321', 'jorge.quispe@example.com', 'Lima', '2026-07-20', 30,
        'ACTIVE', 'GESTOR', '2026-07-20', @Gestor2Id, SYSUTCDATETIME()
    );

-- Paciente 4 (Ecuador - Activo)
IF NOT EXISTS (SELECT 1 FROM dbo.patients WHERE id = @PatientEc1Id)
    INSERT INTO dbo.patients (
        id, full_name, document_type, document_number, country_code,
        phone, email, city, treatment_start, follow_up_days,
        status, registration_source, consent_date, created_by, created_at
    )
    VALUES (
        @PatientEc1Id, 'Andrea Estefanía Noboa', 'CEDULA', '1712345678', 'EC',
        '+593991234567', 'andrea.noboa@example.com', 'Quito', '2026-09-01', 15,
        'ACTIVE', 'SELF', '2026-09-01', @Gestor2Id, SYSUTCDATETIME()
    );

-- Paciente 5 (Autorregistro en curso - Paso 1 completado, pendiente paso 2)
IF NOT EXISTS (SELECT 1 FROM dbo.patients WHERE id = @PatientPendingId)
    INSERT INTO dbo.patients (
        id, full_name, document_type, document_number, country_code,
        phone, email, city, treatment_start, follow_up_days,
        status, registration_source, consent_date, created_by, created_at
    )
    VALUES (
        @PatientPendingId, 'Felipe Andrés Saldarriaga', NULL, NULL, 'CO',
        NULL, 'felipe.saldarriaga@example.com', NULL, NULL, NULL,
        'PENDING', 'SELF', NULL, @Gestor1Id, SYSUTCDATETIME()
    );

-- ----------------------------------------------------------------------------
-- 3. ENLACES DE AUTORREGISTRO (TOKENS)
-- ----------------------------------------------------------------------------
-- Token activo listo para usar en prueba manual
IF NOT EXISTS (SELECT 1 FROM dbo.registration_links WHERE token = 'TOKENPRUEBA2026ACTIVO00000000001')
    INSERT INTO dbo.registration_links (
        id, token, created_by, patient_id, status, expires_at, created_at
    )
    VALUES (
        NEWSEQUENTIALID(), 'TOKENPRUEBA2026ACTIVO00000000001', @Gestor1Id, NULL, 
        'PENDING', DATEADD(DAY, 7, SYSUTCDATETIME()), SYSUTCDATETIME()
    );

-- Token con paso 1 completado (asociado al paciente pendiente)
IF NOT EXISTS (SELECT 1 FROM dbo.registration_links WHERE token = 'TOKENPRUEBAPASO1COMPLETO00000002')
    INSERT INTO dbo.registration_links (
        id, token, created_by, patient_id, status, expires_at, created_at
    )
    VALUES (
        NEWSEQUENTIALID(), 'TOKENPRUEBAPASO1COMPLETO00000002', @Gestor1Id, @PatientPendingId, 
        'STEP1_DONE', DATEADD(DAY, 7, SYSUTCDATETIME()), SYSUTCDATETIME()
    );

-- Token expirado para pruebas de error 400
IF NOT EXISTS (SELECT 1 FROM dbo.registration_links WHERE token = 'TOKENPRUEBAEXPIRADO0000000000003')
    INSERT INTO dbo.registration_links (
        id, token, created_by, patient_id, status, expires_at, created_at
    )
    VALUES (
        NEWSEQUENTIALID(), 'TOKENPRUEBAEXPIRADO0000000000003', @Gestor2Id, NULL, 
        'PENDING', DATEADD(DAY, -1, SYSUTCDATETIME()), DATEADD(DAY, -8, SYSUTCDATETIME())
    );

-- ----------------------------------------------------------------------------
-- 4. CONTACTOS Y AUDITORÍA INMUTABLE (CA-2 y CA-3)
-- ----------------------------------------------------------------------------
DECLARE @Contact1Id UNIQUEIDENTIFIER = '55555555-5555-5555-5555-555555555551';
DECLARE @Contact2Id UNIQUEIDENTIFIER = '55555555-5555-5555-5555-555555555552';
DECLARE @ContactOriginalId UNIQUEIDENTIFIER = '55555555-5555-5555-5555-555555555553';
DECLARE @ContactCorrectedId UNIQUEIDENTIFIER = '55555555-5555-5555-5555-555555555554';

-- Contacto 1: Llamada exitosa a Carlos Restrepo
IF NOT EXISTS (SELECT 1 FROM dbo.contacts WHERE id = @Contact1Id)
    INSERT INTO dbo.contacts (
        id, patient_id, contact_date, channel, result, notes, is_active, registered_by, created_at
    )
    VALUES (
        @Contact1Id, @PatientCo1Id, '2026-08-10T14:30:00Z', 'PHONE', 'SUCCESSFUL_CONTACT',
        'Paciente confirmó recepción de medicamentos. Refiere buena tolerancia sin efectos adversos.',
        1, @Gestor1Id, '2026-08-10T14:35:00Z'
    );

-- Contacto 2: WhatsApp con María Morales
IF NOT EXISTS (SELECT 1 FROM dbo.contacts WHERE id = @Contact2Id)
    INSERT INTO dbo.contacts (
        id, patient_id, contact_date, channel, result, notes, is_active, registered_by, created_at
    )
    VALUES (
        @Contact2Id, @PatientCo2Id, '2026-08-20T10:00:00Z', 'WHATSAPP', 'APPOINTMENT_SCHEDULED',
        'Se coordina cita de seguimiento médico para el próximo mes.',
        1, @Gestor1Id, '2026-08-20T10:15:00Z'
    );

-- Contacto original corregido (is_active = 0 para simular CA-3)
IF NOT EXISTS (SELECT 1 FROM dbo.contacts WHERE id = @ContactOriginalId)
    INSERT INTO dbo.contacts (
        id, patient_id, contact_date, channel, result, notes, is_active, registered_by, created_at
    )
    VALUES (
        @ContactOriginalId, @PatientPe1Id, '2026-08-12T09:00:00Z', 'PHONE', 'NO_ANSWER',
        'No contesta llamada en primer intento.',
        0, @Gestor2Id, '2026-08-12T09:05:00Z'
    );

-- Contacto activo corregido (reemplazo del original)
IF NOT EXISTS (SELECT 1 FROM dbo.contacts WHERE id = @ContactCorrectedId)
    INSERT INTO dbo.contacts (
        id, patient_id, contact_date, channel, result, notes, is_active, registered_by, created_at
    )
    VALUES (
        @ContactCorrectedId, @PatientPe1Id, '2026-08-12T09:00:00Z', 'WHATSAPP', 'SUCCESSFUL_CONTACT',
        'Corrección: El contacto se realizó exitosamente por WhatsApp, no por llamada.',
        1, @Gestor2Id, '2026-08-12T09:20:00Z'
    );

-- Registro de auditoría inmutable correspondiente (CA-3 GxP)
IF NOT EXISTS (SELECT 1 FROM dbo.contact_audit_log WHERE contact_id = @ContactOriginalId)
    INSERT INTO dbo.contact_audit_log (
        id, contact_id, changed_by, changed_at, reason, previous_value, new_value
    )
    VALUES (
        NEWSEQUENTIALID(), @ContactOriginalId, @Gestor2Id, '2026-08-12T09:20:00Z',
        'Error al seleccionar el canal en el formulario inicial; la interacción real fue por WhatsApp.',
        '{"id":"55555555-5555-5555-5555-555555555553","channel":"PHONE","result":"NO_ANSWER"}',
        '{"id":"55555555-5555-5555-5555-555555555554","channel":"WHATSAPP","result":"SUCCESSFUL_CONTACT"}'
    );
GO

-- ----------------------------------------------------------------------------
-- 5. CARGA MASIVA OPCIONAL: 400 PACIENTES (SEGÚN NOTA DEL PRD: "El programa arranca con alrededor de cuatrocientos pacientes")
-- ----------------------------------------------------------------------------
-- Variable configurable: 1 = Activar lote de 400 pacientes para pruebas de volumen, 0 = Solo seed básico inicial
DECLARE @Generate400Patients BIT = 1;

IF @Generate400Patients = 1
BEGIN
    PRINT 'Iniciando generación opcional de 400 pacientes según nota del PRD...';

    DECLARE @FirstNames TABLE (idx INT IDENTITY(1,1), name NVARCHAR(50));
    INSERT INTO @FirstNames (name) VALUES 
    ('Alejandro'), ('Beatriz'), ('Camilo'), ('Diana'), ('Esteban'), ('Fernanda'), ('Gabriel'), ('Helena'),
    ('Ignacio'), ('Juliana'), ('Lucas'), ('Mariana'), ('Nicolás'), ('Olga'), ('Pablo'), ('Raquel'),
    ('Santiago'), ('Tatiana'), ('Valentina'), ('William');

    DECLARE @LastNames TABLE (idx INT IDENTITY(1,1), name NVARCHAR(50));
    INSERT INTO @LastNames (name) VALUES 
    ('Rodríguez'), ('González'), ('Martínez'), ('López'), ('Gómez'), ('Hernández'), ('Pérez'), ('Castro'),
    ('Sánchez'), ('Ramírez'), ('Torres'), ('Flores'), ('Díaz'), ('Vargas'), ('Morales'), ('Rojas'),
    ('Mendoza'), ('Guerrero'), ('Ortiz'), ('Silva');

    DECLARE @i INT = 1;
    WHILE @i <= 400
    BEGIN
        DECLARE @DocNum NVARCHAR(20) = CAST(200000000 + @i AS NVARCHAR(20));
        DECLARE @ModCountry INT = @i % 3;
        DECLARE @Country CHAR(2);
        DECLARE @DocType NVARCHAR(10);
        DECLARE @Phone NVARCHAR(20);
        DECLARE @City NVARCHAR(100);

        IF @ModCountry = 0
        BEGIN
            SET @Country = 'CO';
            SET @DocType = 'CC';
            SET @Phone = '+573' + RIGHT('00000000' + CAST(10000000 + @i AS VARCHAR(10)), 9);
            SET @City = CASE @i % 4 WHEN 0 THEN 'Bogotá' WHEN 1 THEN 'Medellín' WHEN 2 THEN 'Cali' ELSE 'Barranquilla' END;
        END
        ELSE IF @ModCountry = 1
        BEGIN
            SET @Country = 'PE';
            SET @DocType = 'DNI';
            SET @Phone = '+519' + RIGHT('0000000' + CAST(8000000 + @i AS VARCHAR(10)), 8);
            SET @City = CASE @i % 3 WHEN 0 THEN 'Lima' WHEN 1 THEN 'Arequipa' ELSE 'Trujillo' END;
        END
        ELSE
        BEGIN
            SET @Country = 'EC';
            SET @DocType = 'CEDULA';
            SET @Phone = '+5939' + RIGHT('0000000' + CAST(9000000 + @i AS VARCHAR(10)), 8);
            SET @City = CASE @i % 2 WHEN 0 THEN 'Quito' ELSE 'Guayaquil' END;
        END;

        DECLARE @FnIndex INT = (@i % 20) + 1;
        DECLARE @LnIndex INT = ((@i * 7) % 20) + 1;
        DECLARE @Fn NVARCHAR(50) = (SELECT name FROM @FirstNames WHERE idx = @FnIndex);
        DECLARE @Ln NVARCHAR(50) = (SELECT name FROM @LastNames WHERE idx = @LnIndex);
        DECLARE @FullPatientName NVARCHAR(200) = @Fn + ' ' + @Ln;
        DECLARE @PatientEmail NVARCHAR(150) = LOWER(@Fn) + '.' + LOWER(@Ln) + CAST(@i AS NVARCHAR(10)) + '@psp-paciente.org';

        -- Verificación estricta: ninguno de los tres campos (documento, email, teléfono) puede repetirse
        IF NOT EXISTS (
            SELECT 1 FROM dbo.patients 
            WHERE document_number = @DocNum 
               OR email = @PatientEmail 
               OR phone = @Phone
        )
        BEGIN
            DECLARE @TreatDate DATE = DATEADD(DAY, -((@i * 3) % 180), '2026-09-01');
            DECLARE @FollowDays INT = CASE @i % 4 WHEN 0 THEN 15 WHEN 1 THEN 30 WHEN 2 THEN 45 ELSE 60 END;
            DECLARE @PatientStatus NVARCHAR(20) = CASE WHEN @i % 20 = 0 THEN 'UNREACHABLE' WHEN @i % 40 = 0 THEN 'PENDING' ELSE 'ACTIVE' END;
            DECLARE @AssignedGestor UNIQUEIDENTIFIER = '22222222-2222-2222-2222-222222222222';
            IF @i % 2 = 1
                SET @AssignedGestor = '33333333-3333-3333-3333-333333333333';

            DECLARE @NewGenPatId UNIQUEIDENTIFIER = NEWSEQUENTIALID();

            INSERT INTO dbo.patients (
                id, full_name, document_type, document_number, country_code,
                phone, email, city, treatment_start, follow_up_days,
                status, registration_source, consent_date, created_by, created_at
            )
            VALUES (
                @NewGenPatId, @FullPatientName, @DocType, @DocNum, @Country,
                @Phone, @PatientEmail, @City, @TreatDate, @FollowDays,
                @PatientStatus, 'GESTOR', @TreatDate, @AssignedGestor, SYSUTCDATETIME()
            );

            -- Generar interacciones para pacientes activos (para pruebas de consultas y reportes)
            IF @PatientStatus = 'ACTIVE' AND (@i % 3 = 0)
            BEGIN
                DECLARE @ChannelGen NVARCHAR(20) = CASE @i % 3 WHEN 0 THEN 'PHONE' WHEN 1 THEN 'WHATSAPP' ELSE 'EMAIL' END;
                DECLARE @ResultGen NVARCHAR(30) = CASE @i % 4 WHEN 0 THEN 'SUCCESSFUL_CONTACT' WHEN 1 THEN 'NO_ANSWER' WHEN 2 THEN 'APPOINTMENT_SCHEDULED' ELSE 'SUCCESSFUL_CONTACT' END;
                DECLARE @ContactGenDate DATETIME2 = DATEADD(DAY, 10, CAST(@TreatDate AS DATETIME2));

                INSERT INTO dbo.contacts (
                    id, patient_id, contact_date, channel, result, notes, is_active, registered_by, created_at
                )
                VALUES (
                    NEWSEQUENTIALID(), @NewGenPatId, @ContactGenDate, @ChannelGen, @ResultGen,
                    'Contacto automático de seguimiento según protocolo inicial.', 1, @AssignedGestor, SYSUTCDATETIME()
                );
            END;
        END;

        SET @i = @i + 1;
    END;

    PRINT 'Generación opcional de 400 pacientes completada exitosamente.';
END;
GO