# 02 — Plan y Cierre de Alcance
**Programa:** Acompañamiento a Pacientes (PSP)  
**Autor:** Gustavo Penagos  
**Fecha:** 2026-09-18  
**Base:** Hallazgos documentados en `01-hallazgos.md` + supuestos adoptados

> Este documento cumple el requisito de secuencia: existe en el historial de Git **antes** del primer commit de código.

---

## 1. Alcance cerrado

Cubro **dos criterios de aceptación de punta a punta** (BD → API → UI) más el patrón de corrección de CA-3 implementado en el backend. Elegí CA-1 y CA-2 porque son la columna vertebral del sistema: sin paciente registrado no hay contacto posible, y sin contacto registrado el programa no tiene razón de existir. CA-3 se incorpora como patrón arquitectónico porque su omisión implicaría construir sobre una base que viola las reglas del sector.

| Criterio | Qué cubre esta entrega | Capas |
|---------|----------------------|-------|
| **CA-1** | Registro de paciente por gestor (nombre, documento, teléfono, correo, ciudad, fecha de inicio, país y días de seguimiento definidos por el área médica). Validación de unicidad por `(país, tipo_doc, número_doc)` y formato E.164 en teléfono. | BD + API + UI |
| **CA-1 — Autorregistro** | Flujo de 2 pasos via URL con token generado por el gestor. Paso 1: el paciente se identifica con nombre y correo (estado `PENDING`). Paso 2: completa documento, teléfono, ciudad, fecha de inicio y días de seguimiento (estado `ACTIVE`). | BD + API + UI |
| **CA-2** | Registro de contacto asociado a un paciente: fecha, canal (enum `PHONE/WHATSAPP/EMAIL`), resultado (enum cerrado), notas y gestor que lo registra. Solo se permiten contactos sobre pacientes en estado `ACTIVE`. | BD + API + UI |
| **CA-3 (patrón de auditoría)** | Endpoint de corrección de contacto con soft-update: el registro original queda `is_active = false` y se crea uno nuevo activo. La tabla `contact_audit_log` almacena snapshot JSON anterior/nuevo, motivo (obligatorio), gestor y timestamp. Sin pantalla UI dedicada en esta entrega. | BD + API |

---

## 2. Fuera de alcance

Esta sección pesa igual que la anterior: dejar algo fuera con criterio vale tanto como decidir qué entra.

| Criterio / elemento | Justificación |
|--------------------|---------------|
| **CA-4** — Vista filtrada del mes | Requiere CA-1 y CA-2 completamente funcionales. La query JOIN (`contacts ⟶ patients ⟶ users`) está diseñada en el modelo y en los índices para que añadirla en la siguiente iteración no requiera cambios de esquema. Se omite para garantizar profundidad sobre anchura. |
| **CA-5** — Paciente ilocalizable | Bloqueado por H-03: "tres veces consecutivas" no define ventana temporal, si aplica por canal o entre canales, ni quién dispara el cambio de estado. Cualquier implementación sería un supuesto no validado con el PO. El campo `status` en `patients` queda preparado para el valor `UNREACHABLE`. |
| **CA-6** — Reporte de adherencia | Desbloqueado conceptualmente: el área médica define los días de seguimiento (`follow_up_days`) al registrar cada paciente. La fecha límite prevista se calcula como: `treatment_start + follow_up_days`, permitiendo obtener la fórmula: $\text{Adherencia} = (\text{contactados en plazo} / \text{total con plazo vencido}) \times 100$. El campo queda persistido en BD. Se excluye de la UI/API de esta entrega para cumplir la directriz estricta de la prueba de enfocar **máximo 2 criterios de punta a punta** (CA-1 y CA-2). |
| **Autenticación y roles** | El stack de la prueba no lo exige. Se usa un `userId` del seed de prueba para identificar al gestor en cada operación. En producción se añadiría JWT + middleware de autorización por rol. |
| **Pantalla de corrección (CA-3 UI)** | El endpoint existe y tiene pruebas. La pantalla se omite para respetar el tope de 2 criterios de punta a punta en UI y asegurar que los que entran quedan completos. |

---

## 3. Modelo de datos

### Entidades, campos, tipos y restricciones

```sql
-- TABLA: users (simplificada — sin auth en esta entrega)
users (
  id       UNIQUEIDENTIFIER  PK  DEFAULT NEWSEQUENTIALID(),
  name     NVARCHAR(200)     NOT NULL,
  email    NVARCHAR(150)     NOT NULL  UNIQUE,
  role     NVARCHAR(20)      NOT NULL   -- GESTOR | COORDINADORA | ADMIN
)

-- TABLA: registration_links (tokens de autorregistro generados por el gestor)
registration_links (
  id          UNIQUEIDENTIFIER  PK  DEFAULT NEWSEQUENTIALID(),
  token       NVARCHAR(64)      NOT NULL  UNIQUE,            -- GUID sin guiones
  created_by  UNIQUEIDENTIFIER  NOT NULL  REFERENCES users(id),
  patient_id  UNIQUEIDENTIFIER  NULL      REFERENCES patients(id),  -- NULL hasta paso 1
  status      NVARCHAR(20)      NOT NULL  DEFAULT 'PENDING', -- PENDING | STEP1_DONE | USED | EXPIRED
  expires_at  DATETIME2         NOT NULL,
  created_at  DATETIME2         NOT NULL  DEFAULT SYSUTCDATETIME(),
  used_at     DATETIME2         NULL
)

-- TABLA: patients
patients (
  id                   UNIQUEIDENTIFIER  PK  DEFAULT NEWSEQUENTIALID(),
  full_name            NVARCHAR(200)     NOT NULL,
  document_type        NVARCHAR(10)      NULL,   -- CC | DNI | CEDULA  (NULL en paso 1 de autorregistro)
  document_number      NVARCHAR(20)      NULL,   -- NULL en paso 1 de autorregistro
  country_code         CHAR(2)           NOT NULL,   -- CO | PE | EC
  phone                NVARCHAR(20)      NULL,   -- Formato E.164 (+57...). NULL en paso 1
  email                NVARCHAR(150)     NOT NULL,
  city                 NVARCHAR(100)     NULL,   -- NULL en paso 1 de autorregistro
  treatment_start      DATE              NULL,   -- NULL en paso 1 de autorregistro
  follow_up_days       INT               NULL,   -- Días de seguimiento según área médica (input number). Obligatorio al activar
  status               NVARCHAR(20)      NOT NULL  DEFAULT 'PENDING',  -- PENDING | ACTIVE | INACTIVE | UNREACHABLE
  registration_source  NVARCHAR(20)      NOT NULL  DEFAULT 'GESTOR',   -- GESTOR | SELF
  consent_date         DATE              NULL,   -- Campo regulatorio: fecha de consentimiento informado
  created_by           UNIQUEIDENTIFIER  NOT NULL  REFERENCES users(id),
  created_at           DATETIME2         NOT NULL  DEFAULT SYSUTCDATETIME(),
  updated_at           DATETIME2         NOT NULL  DEFAULT SYSUTCDATETIME(),

  CONSTRAINT UQ_patient_doc UNIQUE (country_code, document_type, document_number)
  -- Nota: SQL Server permite múltiples filas con NULLs en un UNIQUE constraint.
  -- Se refuerza con un índice filtrado:
  --   CREATE UNIQUE INDEX UX_patient_doc_filtered ON patients
  --   (country_code, document_type, document_number)
  --   WHERE document_type IS NOT NULL;
)

-- TABLA: contacts
contacts (
  id             UNIQUEIDENTIFIER  PK  DEFAULT NEWSEQUENTIALID(),
  patient_id     UNIQUEIDENTIFIER  NOT NULL  REFERENCES patients(id),
  contact_date   DATETIME2         NOT NULL,
  channel        NVARCHAR(20)      NOT NULL,   -- PHONE | WHATSAPP | EMAIL
  result         NVARCHAR(30)      NOT NULL,   -- SUCCESSFUL_CONTACT | NO_ANSWER | WRONG_NUMBER | REFUSED | APPOINTMENT_SCHEDULED
  notes          NVARCHAR(500)     NULL,
  is_active      BIT               NOT NULL  DEFAULT 1,  -- 0 = corregido (soft-delete para CA-3)
  registered_by  UNIQUEIDENTIFIER  NOT NULL  REFERENCES users(id),
  created_at     DATETIME2         NOT NULL  DEFAULT SYSUTCDATETIME()
)

-- TABLA: contact_audit_log (CA-3 — inmutable, nunca se elimina ni actualiza)
contact_audit_log (
  id              UNIQUEIDENTIFIER  PK  DEFAULT NEWSEQUENTIALID(),
  contact_id      UNIQUEIDENTIFIER  NOT NULL,   -- ID del contacto original corregido
  changed_by      UNIQUEIDENTIFIER  NOT NULL  REFERENCES users(id),
  changed_at      DATETIME2         NOT NULL  DEFAULT SYSUTCDATETIME(),
  reason          NVARCHAR(300)     NOT NULL,   -- Motivo obligatorio (reason for change)
  previous_value  NVARCHAR(MAX)     NOT NULL,   -- JSON snapshot del registro antes de la corrección
  new_value       NVARCHAR(MAX)     NOT NULL    -- JSON snapshot del nuevo registro activo
)
```

### Índices y justificación

| Índice | Columnas | Justificación |
|--------|---------|---------------|
| `UX_patient_doc_filtered` | `(country_code, document_type, document_number) WHERE document_type IS NOT NULL` | Clave de negocio única por país. Con ~400 pacientes en 3 países, un UNIQUE index compuesto detecta duplicados en O(log n) sin colisionar registros PENDING (NULL). |
| `UQ_reg_link_token` | `(token)` | Lookup O(1) del token en cada petición del formulario de autorregistro. Se consulta en cada renderizado del formulario. |
| `IX_contacts_patient_date` | `(patient_id, contact_date DESC)` | Toda consulta de historial de contactos filtra primero por paciente. El orden DESC evita un sort en memoria para las vistas de actividad reciente. |
| `IX_contacts_active_result` | `(patient_id, is_active, result)` | Necesario para CA-5 futuro: contar los últimos N contactos sin éxito de un paciente. El include de `is_active` elimina del resultado los registros corregidos. |
| `IX_contacts_gestor_date` | `(registered_by, contact_date)` | Soporte preventivo para CA-4 (filtro por gestor y mes). Diseñado ahora para que añadir CA-4 no requiera migración de esquema. |

### Estrategia de transaccionalidad e integridad con Stored Procedures (SP)

Para garantizar la integridad referencial, el aislamiento transaccional y evitar que la capa de aplicación ejecute operaciones directas sin sincronizar las tablas dependientes (`patients`, `contacts`, `contact_audit_log`, `registration_links`), **todas las transacciones de modificación y consulta (Insert, Update, Delete [lógico/soft-delete] y Get) se encapsulan en Stored Procedures (SP) en la base de datos**.

#### Justificación técnica de seguridad:
1. **Atomicidad estricta (`TRY...CATCH` y `TRANSACTION`):** Las operaciones que afectan más de una tabla se ejecutan dentro de bloques `BEGIN TRANSACTION ... COMMIT TRANSACTION`. Ante cualquier fallo o violación de restricción, el bloque `CATCH` ejecuta `ROLLBACK TRANSACTION`, impidiendo inconsistencias parciales.
2. **Protección contra desincronización entre tablas:** Ningún proceso externo puede modificar una tabla aislada sin ejecutar las validaciones cruzadas (por ejemplo: validar que un paciente esté en estado `ACTIVE` antes de insertar un contacto, o actualizar el estado del token a `USED` al momento de activar el paciente).
3. **Seguridad y rendimiento:** Se mitigan riesgos de SQL Injection, se reutilizan planes de ejecución en SQL Server y se centraliza la regla de negocio transaccional.

#### Catálogo de Stored Procedures:

| Tipo | Stored Procedure | Tablas Afectadas | Propósito y Control Transaccional |
|------|------------------|------------------|-----------------------------------|
| **Insert** | `sp_RegisterPatient` | `patients` | Registra paciente por gestor validando unicidad `(country_code, document_type, document_number)`. Asigna estado `ACTIVE`. |
| **Insert** | `sp_IdentifyPatientSelfReg` | `patients`, `registration_links` | Paso 1 autorregistro: Inserta paciente preliminar en estado `PENDING` y actualiza el token a `STEP1_DONE` en una sola transacción. |
| **Update** | `sp_CompletePatientSelfReg` | `patients`, `registration_links` | Paso 2 autorregistro: En transacción atómica completa los datos obligatorios (`follow_up_days`, doc, tel), pasa el paciente a `ACTIVE` y marca el token como `USED` (`used_at = SYSUTCDATETIME()`). |
| **Get** | `sp_GetPatientById` | `patients` | Consulta de detalle de paciente por identificador único. |
| **Get** | `sp_GetPatients` | `patients` | Consulta paginada con filtros por país y estado. |
| **Insert** | `sp_CreateContact` | `contacts`, `patients` | Valida que el paciente exista y tenga `status = 'ACTIVE'`. Si es válido, inserta el nuevo contacto. Si no, genera error y no inserta. |
| **Get** | `sp_GetContactsByPatient` | `contacts` | Consulta el historial de contactos activos (`is_active = 1`) ordenados por fecha descendente. |
| **Update / Soft-Delete** | `sp_CorrectContact` | `contacts`, `contact_audit_log` | **Transacción atómica (CA-3):** Realiza soft-delete (`is_active = 0`) del contacto original, inserta el nuevo contacto corregido e inserta el snapshot de auditoría inmutable en `contact_audit_log`. |
| **Insert** | `sp_CreateRegistrationLink` | `registration_links` | Inserta nuevo token de autorregistro vinculado al gestor. |
| **Get** | `sp_ValidateRegistrationLink` | `registration_links` | Obtiene el estado, vigencia y validez del token de autorregistro. |

---

### Cómo se trata la corrección de un registro ya guardado (CA-3) mediante SP

El sistema **no realiza UPDATE destructivo sobre el registro original**. La corrección se delega exclusivamente al Stored Procedure `sp_CorrectContact`:

```sql
CREATE PROCEDURE dbo.sp_CorrectContact
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

        -- 1. Validar que el motivo sea provisto
        IF @Reason IS NULL OR LTRIM(RTRIM(@Reason)) = ''
            THROW 50001, 'Reason is required for contact correction.', 1;

        -- 2. Obtener y validar el contacto original activo
        DECLARE @PatientId UNIQUEIDENTIFIER, @PrevJson NVARCHAR(MAX);
        SELECT @PatientId = patient_id,
               @PrevJson = (SELECT id, patient_id, contact_date, channel, result, notes, registered_by, created_at 
                            FOR JSON PATH, WITHOUT_ARRAY_WRAPPER)
        FROM contacts WHERE id = @OriginalContactId AND is_active = 1;

        IF @PatientId IS NULL
            THROW 50002, 'Contact not found or has already been corrected.', 1;

        -- 3. Soft-delete: desactivar contacto anterior
        UPDATE contacts SET is_active = 0 WHERE id = @OriginalContactId;

        -- 4. Insertar nuevo contacto activo
        SET @NewContactId = NEWSEQUENTIALID();
        INSERT INTO contacts (id, patient_id, contact_date, channel, result, notes, is_active, registered_by, created_at)
        VALUES (@NewContactId, @PatientId, @NewContactDate, @NewChannel, @NewResult, @NewNotes, 1, @ChangedBy, SYSUTCDATETIME());

        -- 5. Insertar snapshot inmutable en tabla de auditoría
        DECLARE @NewJson NVARCHAR(MAX) = (
            SELECT @NewContactId AS id, @PatientId AS patient_id, @NewContactDate AS contact_date, 
                   @NewChannel AS channel, @NewResult AS result, @NewNotes AS notes, @ChangedBy AS registered_by 
            FOR JSON PATH, WITHOUT_ARRAY_WRAPPER
        );

        INSERT INTO contact_audit_log (id, contact_id, changed_by, changed_at, reason, previous_value, new_value)
        VALUES (NEWSEQUENTIALID(), @OriginalContactId, @ChangedBy, SYSUTCDATETIME(), @Reason, @PrevJson, @NewJson);

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END;
```

- El registro original queda con `is_active = 0` y **nunca se borra**.
- La atomicidad garantiza que nunca existirá un contacto desactivado sin su correspondiente nuevo contacto y su entrada en `contact_audit_log`.
- Cumple con las normativas GxP y trazabilidad regulatoria del sector salud.

---

## 4. Contrato de la interfaz

**Base URL:** `/api/v1`  
**Formato de errores:** RFC 7807 `ProblemDetails` en todos los casos de error.

```json
// Ejemplo de error 400 (ProblemDetails):
{
  "type": "https://api.tbtb.com/errors/validation",
  "title": "One or more validation errors occurred.",
  "status": 400,
  "errors": {
    "phone": ["Phone must be in E.164 format (e.g. +573001234567)"]
  }
}
```

---

### Patients

#### `POST /api/v1/patients` — Registro por gestor (CA-1)
**Entrada:**
```json
{
  "fullName":       "María García López",
  "documentType":   "CC",
  "documentNumber": "1032456789",
  "countryCode":    "CO",
  "phone":          "+573001234567",
  "email":          "maria@email.com",
  "city":           "Bogotá",
  "treatmentStart": "2026-09-01",
  "followUpDays":   30,
  "consentDate":    "2026-09-01"
}
```
**Salida 201:**
```json
{
  "id": "uuid", "fullName": "María García López",
  "status": "ACTIVE", "countryCode": "CO",
  "phone": "+573001234567", "email": "maria@email.com",
  "city": "Bogotá", "treatmentStart": "2026-09-01", "followUpDays": 30, "createdAt": "..."
}
```
**Errores:**
- `400` — Campos inválidos (`errors.phone`, `errors.email`, etc.)
- `409` — `"Patient with document CO/CC/1032456789 already exists."`

#### `GET /api/v1/patients/{id}` — Detalle de paciente
- `200` — Objeto paciente completo
- `404` — `"Patient not found."`

#### `GET /api/v1/patients?country=CO&status=ACTIVE&page=1&pageSize=20`
- `200` — `{ items: [...], totalCount, page, pageSize }`

---

### Registration Links — Autorregistro (CA-1 variante)

#### `POST /api/v1/registration-links` — Gestor genera enlace
**Entrada:** `{ "expiresInDays": 7 }` (opcional, default 7)  
**Salida 201:** `{ "token": "...", "url": "https://app.tbtb.com/register/{token}", "expiresAt": "..." }`

#### `GET /api/v1/registration-links/{token}/validate` — Valida token antes del formulario
- `200` — `{ "valid": true, "status": "PENDING" | "STEP1_DONE", "expiresAt": "..." }`
- `400` — Token expirado
- `404` — Token no existe
- `410` — Token ya utilizado

#### `POST /api/v1/registration-links/{token}/identify` — Paso 1: identificación
**Entrada:** `{ "fullName": "...", "email": "...", "countryCode": "CO" }`  
**Salida 200:** `{ "sessionToken": "...", "patientId": "uuid", "nextStep": "/register/{token}/complete" }`  
**Errores:** `400` validación · `404` token no existe o expirado

#### `PATCH /api/v1/registration-links/{token}/complete` — Paso 2: datos completos
**Headers:** `Authorization: Bearer {sessionToken}`  
**Entrada:** `{ "documentType": "CC", "documentNumber": "...", "phone": "+57...", "city": "Cali", "treatmentStart": "2026-09-01", "followUpDays": 30 }`  
**Salida 200:** Objeto paciente completo con `"status": "ACTIVE"`, `"followUpDays": 30` y `"registrationSource": "SELF"`  
**Errores:** `400` campos · `409` documento duplicado · `422` sessionToken inválido o expirado

---

### Contacts (CA-2)

#### `POST /api/v1/patients/{patientId}/contacts` — Registro de contacto
**Entrada:**
```json
{
  "contactDate": "2026-09-18T10:30:00Z",
  "channel":     "PHONE",
  "result":      "SUCCESSFUL_CONTACT",
  "notes":       "Paciente confirmó toma del medicamento."
}
```
**Salida 201:**
```json
{
  "id": "uuid", "patientId": "uuid",
  "contactDate": "2026-09-18T10:30:00Z",
  "channel": "PHONE", "result": "SUCCESSFUL_CONTACT",
  "notes": "...", "registeredBy": { "id": "uuid", "name": "Juan Gestor" },
  "createdAt": "..."
}
```
**Errores:**
- `400` — Campos inválidos
- `404` — `"Patient not found."`
- `422` — `"Invalid channel value. Allowed: PHONE, WHATSAPP, EMAIL."`
- `422` — `"Cannot register contact: Patient status is PENDING. Complete registration first."`

#### `GET /api/v1/patients/{patientId}/contacts`
- `200` — `{ items: [{ id, contactDate, channel, result, notes, registeredBy, isActive, createdAt }], totalCount }`
- `404` — Paciente no encontrado

---

### Contacts — Corrección (CA-3)

#### `PUT /api/v1/contacts/{id}` — Corrección con auditoría inmutable
**Entrada:**
```json
{
  "contactDate": "2026-09-18T11:00:00Z",
  "channel":     "WHATSAPP",
  "result":      "NO_ANSWER",
  "notes":       "Corrección: el canal registrado fue incorrecto.",
  "reason":      "Se seleccionó PHONE por error, el intento fue por WhatsApp."
}
```
**Salida 200:** Nuevo registro activo con `"correctedFrom": { "originalContactId": "uuid" }`  
**Errores:**
- `400` — `"Reason is required for contact correction."`
- `404` — Contacto no encontrado
- `409` — `"This contact has already been corrected."`

---

## 5. Secuencia de trabajo

| # | Tarea | Tiempo |
|---|-------|--------|
| 1 | Commit inicial: `01-hallazgos.md` + `02-plan.md` (este archivo) | 5 min |
| 2 | `scripts/01_schema.sql` — Tablas, constraints e índices | 20 min |
| 3 | `scripts/02_stored_procedures.sql` — SPs de Insert, Update, Soft-Delete y Get con transacciones seguras | 30 min |
| 4 | `scripts/03_seed.sql` — Datos iniciales (gestores, pacientes CO/PE/EC, contactos, tokens) | 15 min |
| 5 | Estructura solución .NET 8: `dotnet new` + 3 proyectos (API, Application, Infrastructure) | 20 min |
| 6 | Capa de persistencia: ejecución de Stored Procedures vía Dapper / EF Core + LocalDB | 20 min |
| 7 | `PatientService` + `PatientRepository` + `PatientsController` (CA-1 vía SPs) | 35 min |
| 8 | `RegistrationService` + `RegistrationLinkRepository` + `RegistrationLinksController` (autorregistro vía SPs) | 35 min |
| 9 | `ContactService` + `ContactRepository` + `ContactsController` (CA-2 y CA-3 vía `sp_CreateContact` y `sp_CorrectContact`) | 35 min |
| 10 | `GlobalExceptionMiddleware` + `ProblemDetails` RFC 7807 | 10 min |
| 11 | Pruebas xUnit: CA-1 gestor (3 tests) + CA-1 autorregistro (5 tests) + CA-2 (4 tests) + CA-3 (3 tests) | 45 min |
| 12 | Angular 17+: estructura de proyecto + servicios + interceptor de errores | 20 min |
| 13 | UI: formulario registro paciente por gestor + lista de pacientes (CA-1) | 20 min |
| 14 | UI: autorregistro paso 1 (identificación) + paso 2 (datos completos y días de seguimiento) | 30 min |
| 15 | UI: formulario registro contacto + historial de contactos (CA-2) | 25 min |
| 16 | `README.md` + `03-bitacora.md` + commit final | 20 min |
| | **Total estimado** | **≈ 6 h 25 min** |

---

## 6. Riesgos

| Riesgo | Probabilidad | Impacto | Mitigación |
|--------|-------------|---------|------------|
| SQL Server no disponible en la máquina de evaluación | Media | Alto | Se usa SQL Server Express LocalDB (`(localdb)\MSSQLLocalDB`), incluida en VS 2022 sin instalación adicional. La cadena de conexión va en `appsettings.Development.json` y se documenta en README. |
| Tiempo insuficiente para completar el frontend | Media | Medio | Se prioriza el orden: (1) registro gestor, (2) registro contacto, (3) autorregistro. Los tres son formularios simples. Si el tiempo se agota antes del paso 3, los dos primeros demuestran CA-1 y CA-2 de punta a punta. |
| Seguridad del token de autorregistro | Baja | Medio | En esta entrega se usa `Guid.NewGuid().ToString("N")` (32 chars hex). Es suficiente para la prueba. En producción se reemplazaría por un token generado con `RandomNumberGenerator` (CSPRNG). Se documenta como decisión consciente en la bitácora. |
| Constraint UNIQUE con NULLs en `patients` | Baja | Bajo | SQL Server permite múltiples NULLs en un UNIQUE constraint. Se añade un índice filtrado (`WHERE document_type IS NOT NULL`) para cubrir solo los registros con documento completo. |
| Conflicto EF Core migrations vs. scripts .sql | Baja | Bajo | Se opta por scripts `.sql` versionados y numerados (más explícitos y auditables). El `AppDbContext` usa `EnsureCreated()` solo en entorno de desarrollo para no duplicar el schema. |
| Deuda de `sessionToken` en autorregistro sin JWT | Baja | Bajo | El sessionToken del paso 1 se almacena implícitamente como `registration_links.status = 'STEP1_DONE'` + `patient_id`. No es un JWT real. Se documenta en la bitácora como simplificación acordada para la prueba. |

---

## 7. Extensión móvil

> Media página conceptual, sin código. Se evalúa el criterio, no la implementación.

**Escenario:** El mismo gestor que usa la web también accede desde su celular en campo, frecuentemente en zonas con cobertura intermitente o sin señal.

### ¿Qué guardaría en el dispositivo?

Solo lo necesario para la jornada del día:
- **La lista de pacientes asignados a la ruta del gestor** (no todos los 400 del programa, solo los de su agenda del día, descargados al inicio de la jornada). Campos suficientes para registrar un contacto: id, nombre, ciudad, estado.
- **La cola de contactos creados offline**, persistida en SQLite local (Capacitor SQLite Plugin). Cada elemento de la cola tiene un `local_id` temporal, el `patient_id`, fecha, canal, resultado y notas.
- **El catálogo de enumerados** (canales y resultados válidos), cacheado con TTL de 24 horas para validar localmente antes de sincronizar.
- **Los tokens de registro generados offline** para compartir con pacientes que se van a autoregistrar. Se sincronizan al recuperar red antes de que el gestor los comparta.

### ¿Cuándo sincronizo?

- **Al iniciar la jornada** (con red): descarga los pacientes del día y los enumerados.
- **Al recuperar conectividad** (automático): la app escucha el evento de red (`Network Information API` / Capacitor Network Plugin) y dispara la sincronización de la cola pendiente.
- **Al cerrar la jornada** (manual): botón "Sincronizar y cerrar" que garantiza que todo quedó subido antes de que el gestor se desconecte intencionalmente.
- **En foreground con red**: los registros van directo al servidor sin pasar por la cola local.

### ¿Qué hago si el mismo registro se modificó en el servidor mientras el teléfono estaba desconectado?

Adopto la estrategia **Server Wins with Audit Trail**, que es coherente con el principio GxP de trazabilidad que ya aplica en CA-3:

1. Al sincronizar, el cliente envía cada contacto de la cola junto con la versión del paciente que tenía en caché (`patient_etag`).
2. El servidor compara el `etag`:
   - **Sin conflicto** (el paciente no cambió): acepta el contacto → 201, sin más acción.
   - **Con conflicto** (el paciente fue modificado en el servidor): el contacto **se acepta igual** — es una inserción nueva, no una edición — y la respuesta incluye el paciente actualizado para que el cliente refresque su caché local.
3. **Las correcciones de contacto (CA-3) no se permiten en modo offline**: modificar un contacto ya guardado requiere verificar el estado actual en el servidor y generar el `audit_log` en tiempo real. La UI muestra el botón de corrección deshabilitado cuando no hay conexión, con el mensaje "Requiere conexión para mantener la trazabilidad del registro".

Este diseño garantiza que no se pierden contactos registrados offline y que la regla de auditoría inmutable no puede violarse desde el dispositivo móvil.
