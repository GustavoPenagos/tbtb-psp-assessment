# Plataforma de Acompañamiento a Pacientes (PSP) · TBTB Global Healthcare

> **Solución Técnica de Ingreso · Desarrollador Full-Stack**  
> **Autor:** Gustavo Penagos  
> **Fecha:** Septiembre de 2026  
> **Entorno Regulado:** Industria Farmacéutica (Principios GxP, 21 CFR Part 11, ALCOA+, OWASP Top 10, RFC 7807 ProblemDetails)  
> **Stack Principal:** SQL Server (Transaccional) • .NET 8 (Clean Architecture) • Angular 18 (Standalone Components)

---

## 1. Resumen Ejecutivo y Alcance de la Solución

El presente proyecto implementa la plataforma de software para el **Programa de Acompañamiento a Pacientes (PSP)** que apoya a personas en tratamiento crónico en **Colombia, Perú y Ecuador**, asegurando la trazabilidad, inmutabilidad y reporte determinista de adherencia a laboratorios farmacéuticos patrocinadores.

Cumpliendo con la directriz de evaluación del documento de la prueba técnica (**enfoque riguroso y de punta a punta en máximo 2 a 3 Criterios de Aceptación**):

| Criterio | Alcance Implementado de Punta a Punta | Estado Técnico |
| :--- | :--- | :---: |
| **CA-1** | **Registro de Paciente por Gestor y Autorregistro Asistido en 2 Pasos:** Validación multi-país (CO `+57`, PE `+51`, EC `+593`), teléfono obligatorio en formato E.164, validación de unicidad compuesta por `(country_code, document_type, document_number)`. Generación de enlaces con tokens criptográficos temporales (`registration_links`) que crean al paciente en `PENDING` (Paso 1) y lo activan a `ACTIVE` completando datos obligatorios (Paso 2). | **100% Cubierto y Probado** (BD + API + UI + xUnit) |
| **CA-2** | **Registro de Contactos e Interacciones:** Registro transaccional asociado exclusivamente a pacientes en estado `ACTIVE`. Catálogo cerrado y tipado de canales (`PHONE`, `WHATSAPP`, `EMAIL`) y resultados (`SUCCESSFUL_CONTACT`, `NO_ANSWER`, `WRONG_NUMBER`, `REFUSED`, `APPOINTMENT_SCHEDULED`). | **100% Cubierto y Probado** (BD + API + UI + xUnit) |
| **CA-3** | **Trazabilidad Inmutable y Soft-Update GxP:** Modificación de contactos y actualización de pacientes sin destrucción física de datos. Registro de snapshots JSON anterior y nuevo en `contact_audit_log` y `patient_audit_log`. Justificación regulatoria (`reason`) obligatoria con contador dinámico $\ge 10$ caracteres. Visualización comparativa en UI con chips visuales (rojo tachado vs verde nuevo). | **100% Cubierto y Probado** (BD + API + UI + xUnit) |
| **CA-4, CA-5, CA-6** | Excluidos deliberadamente de la interfaz de usuario para garantizar profundidad sobre volumen superficial. CA-4 cuenta con Stored Procedure e índice de cobertura optimizado; CA-5 y CA-6 quedan modelados en BD y resueltos conceptualmente con el parámetro médico `follow_up_days` en `02-plan.md`. | **Modelo de Datos y Justificación en Plan** |

---

## 2. Arquitectura Global y Metodología

La solución adopta una arquitectura desacoplada y orientada al dominio:

```
                                  ARQUITECTURA DE LA SOLUCIÓN
┌────────────────────────────────────────────────────────────────────────────────────────┐
│                                 FRONTEND: ANGULAR 18                                   │
│  [Standalone Components] ─── [Typed Reactive Forms] ─── [Services Inyectados]         │
│  • Aside 1: Registro CA-1  • Aside 2: Directorio      • Aside 3: Auditoría GxP        │
│  • Modales: Corrección CA-3 & Edición Paciente (Motivo obligatorio >= 10 chars)        │
│  • Ruta Pública Aislada: /autorregistro/:token (Wizard Paso 1 -> Paso 2)               │
│  • Interceptor HTTP: ProblemDetails RFC 7807 & Sanitización contextual                │
│  • Cero 'any': 'noImplicitAny' estricto en TypeScript                                 │
└─────────────────────────────────────────┬──────────────────────────────────────────────┘
                                          │ HTTPS / CORS Restrictivo (localhost:4200)
┌─────────────────────────────────────────▼──────────────────────────────────────────────┐
│                                BACKEND: .NET 8 WEBAPI                                  │
│  ┌──────────────────────────────────────────────────────────────────────────────────┐  │
│  │ WebApi Layer: Controllers con try-catch, Security Headers, ExceptionMiddleware   │  │
│  │ FileLoggerService: Registro forense thread-safe en E:\logs\logs_TBTB.PSP.text   │  │
│  │ Swagger OpenAPI 3.0: Documentación viva con comentarios XML y esquemas tipados   │  │
│  └──────────────────────────────────────┬───────────────────────────────────────────┘  │
│  ┌──────────────────────────────────────▼───────────────────────────────────────────┐  │
│  │ Application Layer: DTOs, AutoMapper Profiles, FluentValidation (Regex + XSS strip)│  │
│  └──────────────────────────────────────┬───────────────────────────────────────────┘  │
│  ┌──────────────────────────────────────▼───────────────────────────────────────────┐  │
│  │ Domain Layer: Entidades, Enums cerrados legibles, Excepciones de Negocio          │  │
│  └──────────────────────────────────────┬───────────────────────────────────────────┘  │
│  ┌──────────────────────────────────────▼───────────────────────────────────────────┐  │
│  │ Infrastructure Layer: Dapper con DynamicParameters fuertemente tipados          │  │
│  └──────────────────────────────────────┬───────────────────────────────────────────┘  │
└─────────────────────────────────────────┼──────────────────────────────────────────────┘
                                          │ TDS / ADO.NET (100% Stored Procedures)
┌─────────────────────────────────────────▼──────────────────────────────────────────────┐
│                          BASE DE DATOS: SQL SERVER / LOCALDB                           │
│  • Tablas: users, patients, registration_links, contacts, contact_audit_log,          │
│            patient_audit_log                                                           │
│  • Stored Procedures Transaccionales (TRY...CATCH + SET XACT_ABORT ON + ROLLBACK):     │
│    sp_RegisterPatient, sp_UpdatePatientWithAudit, sp_GetPatientAuditHistory,           │
│    sp_CreateContact, sp_CorrectContact, sp_IdentifyPatientSelfReg, etc.                │
└────────────────────────────────────────────────────────────────────────────────────────┘
```

---

## 3. Módulo de Base de Datos (SQL Server)

La persistencia se gestiona íntegramente mediante scripts DDL/DML versionados en la carpeta `scripts/`, cumpliendo la restricción de **no realizar modificaciones manuales desde interfaces gráficas**.

### A. Tablas del Sistema y Restricciones de Integridad

1. **`users`:** Gestores, coordinadores y administradores del programa.
   - `id` (UNIQUEIDENTIFIER, PK).
   - `name` (NVARCHAR(200)), `email` (NVARCHAR(150), UNIQUE), `role` (NVARCHAR(20), CHECK `GESTOR`, `COORDINADORA`, `ADMIN`).
2. **`patients`:** Registro maestro de pacientes.
   - `id` (UNIQUEIDENTIFIER, PK).
   - `full_name` (NVARCHAR(200)), `document_type` (NVARCHAR(10), CHECK `CC`, `DNI`, `CEDULA`).
   - `document_number` (NVARCHAR(20)), `country_code` (CHAR(2), CHECK `CO`, `PE`, `EC`).
   - `phone` (NVARCHAR(20), formato E.164), `email` (NVARCHAR(150), UNIQUE).
   - `city` (NVARCHAR(100)), `treatment_start` (DATE), `follow_up_days` (INT, parámetro médico para adherencia).
   - `status` (NVARCHAR(20), CHECK `PENDING`, `ACTIVE`, `INACTIVE`, `UNREACHABLE`).
   - `registration_source` (NVARCHAR(20), CHECK `GESTOR`, `SELF`), `consent_date` (DATE).
3. **`registration_links`:** Gestión de tokens seguros para autorregistro asistido.
   - `id` (UNIQUEIDENTIFIER, PK), `token` (NVARCHAR(64), UNIQUE GUID sin guiones), `created_by` (FK `users`).
   - `patient_id` (FK `patients`, NULL hasta completar Paso 1), `status` (CHECK `PENDING`, `STEP1_DONE`, `USED`, `EXPIRED`).
   - `expires_at` (DATETIME2), `created_at` (DATETIME2), `used_at` (DATETIME2, NULL).
4. **`contacts`:** Historial de interacciones multicanal.
   - `id` (UNIQUEIDENTIFIER, PK), `patient_id` (FK `patients`), `contact_date` (DATETIME2 UTC).
   - `channel` (NVARCHAR(20), CHECK `PHONE`, `WHATSAPP`, `EMAIL`).
   - `result` (NVARCHAR(30), CHECK `SUCCESSFUL_CONTACT`, `NO_ANSWER`, `WRONG_NUMBER`, `REFUSED`, `APPOINTMENT_SCHEDULED`).
   - `notes` (NVARCHAR(500)), `is_active` (BIT, Default 1 para soft-update), `registered_by` (FK `users`).
5. **`contact_audit_log`:** Trazabilidad inmutable de corrección de contactos (CA-3 GxP).
   - `id` (UNIQUEIDENTIFIER, PK), `contact_id` (UNIQUEIDENTIFIER), `changed_by` (FK `users`), `changed_at` (DATETIME2 UTC).
   - `reason` (NVARCHAR(300), Justificación obligatoria $\ge 10$ caracteres).
   - `previous_value` (NVARCHAR(MAX), snapshot JSON), `new_value` (NVARCHAR(MAX), snapshot JSON).
6. **`patient_audit_log`:** Trazabilidad inmutable de edición de datos de paciente.
   - `id` (UNIQUEIDENTIFIER, PK), `patient_id` (FK `patients`), `changed_by` (FK `users`), `changed_at` (DATETIME2 UTC).
   - `reason` (NVARCHAR(300), Justificación obligatoria $\ge 10$ caracteres).
   - `previous_value` (NVARCHAR(MAX), snapshot JSON), `new_value` (NVARCHAR(MAX), snapshot JSON).

### B. Índices de Rendimiento e Integridad

- **`UX_patient_doc_filtered`:** Índice único filtrado `(country_code, document_type, document_number) WHERE document_type IS NOT NULL AND document_number IS NOT NULL` (permite autorregistros preliminares en Paso 1 sin colisiones).
- **`UX_patients_email`:** Índice único sobre `email` de pacientes.
- **`UX_patients_phone_filtered`:** Índice único filtrado sobre `phone WHERE phone IS NOT NULL`.
- **`IX_contacts_patient_date`:** Índice agrupado por `(patient_id, contact_date DESC)` para historial clínico rápido.
- **`IX_contacts_gestor_date`:** Índice de cobertura `(registered_by, contact_date) INCLUDE (patient_id, channel, result, is_active)` para reportes y supervisión con más de 100.000 registros sin *Clustered Index Scan*.
- **`IX_patient_audit_log_patient_date`:** Índice para auditoría cronológica de pacientes.

### C. Procedimientos Almacenados Transaccionales (`02_stored_procedures.sql`)

Todos los SPs cuentan con `SET XACT_ABORT ON` y encapsulación en bloques `BEGIN TRY ... BEGIN TRANSACTION ... COMMIT ... END TRY BEGIN CATCH ... ROLLBACK ... THROW`:
- `sp_RegisterPatient`: Registro integral de pacientes por gestor con validación de unicidad.
- `sp_UpdatePatientWithAudit`: Actualización de datos maestros con snapshot JSON y justificación obligatoria $\ge 10$ caracteres.
- `sp_GetPatientAuditHistory`: Consulta histórica de modificaciones de pacientes.
- `sp_CreateContact`: Registro de interacción validando que el paciente esté en estado `ACTIVE`.
- `sp_CorrectContact`: Soft-update atómico; desactiva el contacto previo (`is_active = 0`), inserta el nuevo contacto activo y genera la traza en `contact_audit_log`.
- `sp_GetContactsByPatient` y `sp_GetContactAuditHistory`: Consultas de interacciones y auditorías.
- `sp_CreateRegistrationLink`, `sp_ValidateRegistrationLink`, `sp_IdentifyPatientSelfReg`, `sp_CompletePatientSelfReg`: Flujo de autorregistro en 2 pasos con tokens.

---

## 4. Módulo de Backend (.NET 8 Clean Architecture)

### A. Librerías y Dependencias Instaladas

| Paquete NuGet | Versión | Propósito Arquitectónico |
| :--- | :---: | :--- |
| **Dapper** | `2.1.86` | Micro-ORM de alto rendimiento para ejecución de Stored Procedures con tipado estricto. |
| **Microsoft.Data.SqlClient** | `7.1.0` | Driver nativo y seguro para conexión a SQL Server. |
| **AutoMapper** | `16.2.0` | Mapeo bidireccional desacoplado entre Entidades de Dominio y DTOs de frontera. |
| **FluentValidation** | `12.1.1` | Validación declarativa de reglas de negocio y sanitización anti-XSS. |
| **FluentValidation.DependencyInjectionExtensions** | `12.1.1` | Inyección automática de validadores en el contenedor de dependencias. |
| **Swashbuckle.AspNetCore** | `6.6.2` | Generación de documentación interactiva OpenAPI 3.0 / Swagger UI. |
| **xUnit** | `2.8.1` | Framework de pruebas unitarias automatizadas. |
| **Moq** | `4.20.70` | Mocking de dependencias e interfaces de infraestructura. |
| **FluentAssertions** | `6.12.0` | Aserciones expresivas y legibles en las pruebas unitarias. |

### B. Controladores y Contratos de la API REST

Todos los endpoints producen respuestas JSON y documentan códigos de estado HTTP estandarizados:

#### 1. `PatientsController` (`/api/patients`) — Tag: `1. Pacientes (CA-1 / CA-3)`

* **`POST /api/patients` (CA-1: Registro de Paciente por Gestor):**
  - **Request Body (`PatientRegisterRequestDto`):**
    ```json
    {
      "fullName": "Carlos Andrés Gómez Pérez",
      "documentType": "CC",
      "documentNumber": "1020304050",
      "countryCode": "CO",
      "phone": "+573001234567",
      "email": "carlos.gomez@email.com",
      "city": "Bogotá",
      "treatmentStart": "2026-09-01",
      "followUpDays": 30,
      "consentDate": "2026-09-01",
      "createdBy": "11111111-1111-1111-1111-111111111111"
    }
    ```
  - **Respuestas:**
    - `201 Created` $\rightarrow$ `PatientResponseDto` con URL en cabecera `Location`.
    - `400 Bad Request` $\rightarrow$ `ProblemDetails` con errores de validación de campos.
    - `409 Conflict` $\rightarrow$ `ProblemDetails` por duplicidad de documento, teléfono o correo.
* **`GET /api/patients` (Directorio Paginado de Pacientes):**
  - **Query Params:** `countryCode` (opcional), `status` (opcional), `pageNumber` (def: 1), `pageSize` (def: 15).
  - **Respuesta:** `200 OK` $\rightarrow$ `PatientListResponseDto` (`items`, `totalCount`, `pageNumber`, `pageSize`, `totalPages`).
* **`GET /api/patients/{id}` (Detalle de Paciente por GUID):**
  - **Respuestas:** `200 OK` $\rightarrow$ `PatientResponseDto` | `404 Not Found`.
* **`PUT /api/patients/{id}` (CA-3: Edición Auditada de Paciente):**
  - **Request Body (`PatientUpdateRequestDto`):**
    ```json
    {
      "phone": "+573109876543",
      "email": "carlos.nuevo@email.com",
      "city": "Medellín",
      "status": "ACTIVE",
      "reason": "Actualización de residencia y número de contacto reportado por el paciente",
      "changedBy": "11111111-1111-1111-1111-111111111111"
    }
    ```
  - **Respuestas:** `204 NoContent` | `400 BadRequest` (motivo < 10 chars o inválido) | `404 NotFound` | `409 Conflict`.
* **`GET /api/patients/{id}/audit` (Historial Inmutable de Auditoría de Paciente):**
  - **Respuesta:** `200 OK` $\rightarrow$ `IEnumerable<AuditLogResponseDto>`.

---

#### 2. `ContactsController` (`/api/contacts`) — Tag: `2. Contactos e Interacciones (CA-2 / CA-3)`

* **`POST /api/contacts` (CA-2: Registrar Interacción con Paciente Activo):**
  - **Request Body (`ContactCreateRequestDto`):**
    ```json
    {
      "patientId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      "contactDate": "2026-09-18T14:30:00Z",
      "channel": "PHONE",
      "result": "SUCCESSFUL_CONTACT",
      "notes": "Paciente confirma toma diaria de medicamento sin eventos adversos.",
      "registeredBy": "11111111-1111-1111-1111-111111111111"
    }
    ```
  - **Respuestas:**
    - `201 Created` $\rightarrow$ `ContactResponseDto`.
    - `400 Bad Request` $\rightarrow$ Fecha futura o canal/resultado no contemplado.
    - `404 Not Found` $\rightarrow$ Paciente inexistente.
    - `422 Unprocessable Entity` $\rightarrow$ Paciente en estado diferente a `ACTIVE`.
* **`GET /api/contacts/patient/{patientId}` (Historial de Contactos de un Paciente):**
  - **Respuesta:** `200 OK` $\rightarrow$ `IEnumerable<ContactResponseDto>`.
* **`PUT /api/contacts/{id}/correct` (CA-3: Corrección Auditada con Soft-Update):**
  - **Request Body (`ContactCorrectRequestDto`):**
    ```json
    {
      "contactDate": "2026-09-18T14:30:00Z",
      "channel": "WHATSAPP",
      "result": "SUCCESSFUL_CONTACT",
      "notes": "Se corrigió canal de PHONE a WHATSAPP por error de transcripción en campo.",
      "reason": "El gestor seleccionó llamada por error en lugar de mensaje de WhatsApp verificado.",
      "changedBy": "11111111-1111-1111-1111-111111111111"
    }
    ```
  - **Respuestas:** `200 OK` $\rightarrow$ `ContactResponseDto` del nuevo contacto activo | `400 BadRequest` | `404 NotFound`.
* **`GET /api/contacts/patient/{patientId}/audit` y `GET /api/contacts/{id}/audit`:**
  - **Respuesta:** `200 OK` $\rightarrow$ `IEnumerable<AuditLogResponseDto>`.

---

#### 3. `RegistrationLinksController` (`/api/registration-links`) — Tag: `3. Enlaces y Autorregistro (CA-1)`

* **`POST /api/registration-links` (Gestor Genera Enlace con Token):**
  - **Request:** `{ "createdBy": "GUID", "expiresInDays": 7 }` $\rightarrow$ **Respuesta:** `201 Created` con token único.
* **`GET /api/registration-links/{token}/validate`:**
  - **Respuesta:** `200 OK` (retorna `status`, `expiresAt`, `computedStatus`).
* **`POST /api/registration-links/{token}/identify` (Paso 1: Filtro de Acceso):**
  - **Request:** `{ "fullName": "...", "email": "...", "countryCode": "CO" }` $\rightarrow$ Crea paciente en `PENDING`.
* **`POST /api/registration-links/{token}/complete` (Paso 2: Activación Final):**
  - **Request:** `{ "documentType": "CC", "documentNumber": "...", "phone": "+57300...", "city": "...", "treatmentStart": "...", "followUpDays": 30, "consentDate": "..." }` $\rightarrow$ Activa paciente a `ACTIVE` e invalida el token a `USED`.

---

### C. Políticas de Seguridad, CORS y Registro Forense

1. **CORS Restrictivo:** Prohibición absoluta de `.AllowAnyOrigin()`. Configurado en `Program.cs` para admitir exclusivamente orígenes de la lista blanca de `appsettings.json` (`http://localhost:4200`).
2. **Cabeceras de Seguridad HTTP:** Middleware global que inyecta `X-Content-Type-Options: nosniff` y `X-Frame-Options: DENY`.
3. **Manejo Centralizado de Excepciones y ProblemDetails (RFC 7807):**
   - El cliente HTTP recibe únicamente respuestas sanitizadas sin stack traces (mitigación CWE-209).
4. **Registro Forense Thread-Safe en Archivo Físico (`E:\logs\logs_TBTB.PSP.text`):**
   - Implementado mediante `FileLoggerService` con mecanismo de bloqueo (`lock`) para concurrencia. Registra: Fecha UTC, Endpoint, Método HTTP, TraceId, Tipo de Excepción, Mensaje y Stack Trace completo.

### D. Pruebas Unitarias Automatizadas (xUnit)

La suite de pruebas en `tests/Application.UnitTests` cuenta con **17 pruebas automatizadas pasando al 100%**, estructuradas con nombres atados explícitamente a los Criterios de Aceptación:
- **`CA1_PatientRegistrationTests.cs`:**
  - `RegisterPatient_ValidData_ReturnsActivePatient`
  - `RegisterPatient_DuplicateDocument_ThrowsConflictException`
  - `RegisterPatient_DuplicateEmail_ThrowsConflictException`
  - `RegisterPatient_DuplicatePhone_ThrowsConflictException`
  - `RegisterPatient_MissingRequiredFields_ThrowsValidationException`
  - `RegisterPatient_InvalidPhoneFormat_ThrowsValidationException`
  - `GetPatientsPaged_ReturnsPagedResult`
- **`CA2_ContactRegistrationTests.cs`:**
  - `CreateContact_ActivePatient_ReturnsCreatedContact`
  - `CreateContact_PendingPatient_ThrowsUnprocessableEntityException`
  - `CreateContact_NonExistentPatient_ThrowsNotFoundException`
  - `CreateContact_FutureDate_ThrowsValidationException`
- **`CA3_AuditAndCorrectionTests.cs`:**
  - `CorrectContact_ValidData_ReturnsNewContactAndCreatesAudit`
  - `CorrectContact_ReasonLessThan10Chars_ThrowsValidationException`
  - `CorrectContact_NonExistentContact_ThrowsNotFoundException`
  - `UpdatePatient_ValidData_UpdatesPatientAndCreatesAudit`
  - `UpdatePatient_ReasonLessThan10Chars_ThrowsValidationException`
  - `UpdatePatient_DuplicateEmail_ThrowsConflictException`

---

## 5. Módulo de Frontend (Angular 18 Standalone)

### A. Dependencias y Herramientas

- **Angular Core & Common:** `v18.2.0` (Arquitectura Standalone sin NgModules).
- **Angular Forms:** `v18.2.0` (`ReactiveFormsModule` fuertemente tipado).
- **Angular Router:** `v18.2.0` (Rutas tipadas con lazy loading y layout desacoplado).
- **RxJS:** `~7.8.0` (Gestión asíncrona mediante Observables y operadores `catchError`, `tap`).
- **TypeScript:** `~5.5.2` (Configurado con `"noImplicitAny": true`).
- **SCSS:** Preprocesador para diseño visual modular y responsivo.

### B. Arquitectura de Componentes Desacoplados (Cero Código Inline)

Cada componente está estructurado en tres archivos físicos independientes:
```text
src/app/components/
├── audit-viewer/           # *.component.html | *.component.ts | *.component.scss
├── contact-correct-modal/  # *.component.html | *.component.ts | *.component.scss
├── contact-modal/          # *.component.html | *.component.ts | *.component.scss
├── patient-directory/      # *.component.html | *.component.ts | *.component.scss
├── patient-edit-modal/     # *.component.html | *.component.ts | *.component.scss
├── patient-register/       # *.component.html | *.component.ts | *.component.scss
└── self-registration/      # *.component.html | *.component.ts | *.component.scss
```

### C. Experiencia de Usuario Clínica (GxP UI) y Reglas de Negocio en Frontend

1. **Diseño Visual Médico y Colores Suaves:**
   - Paleta sobria farmacéutica: Azul marino/médico (`#1e3a8a`, `#0f766e`), fondo gris perla suave (`#f8fafc`), estados en esmeralda clínico (`#ecfdf5` / `#059669`) y alertas en carmesí suave (`#fef2f2` / `#dc2626`).
   - Totalmente responsivo para operación en escritorio y tabletas de campo.
2. **Navegación Modular por Asides:**
   - **Aside 1 (Registro CA-1):** Formulario reactivo con selector dinámico de país que asigna automáticamente el prefijo telefónico internacional (`+57` Colombia, `+51` Perú, `+593` Ecuador).
   - **Aside 2 (Directorio de Pacientes):** Tabla reactiva con buscador en tiempo real, filtros por país y estado, **columna obligatoria de correo electrónico visible**, estado del paciente, contador de contactos y botones de acción (`+ Contactar`, `✎ Editar`, `📜 Trazabilidad`, `🔗 Enlace`).
   - **Aside 3 (Trazabilidad GxP / Audit Viewer):** Visualización del historial inmutable de auditoría con comparador visual: **Chip rojo con tachado** para el Valor Anterior y **Chip verde** para el Valor Nuevo, junto a la justificación, usuario modificador y timestamp UTC.
3. **Regla de Oro Transaccional en Modales:**
   - En los modales de corrección de contacto y edición de paciente, el campo **Motivo de Cambio** es mandatorio.
   - Cuenta con un **contador dinámico de caracteres** (`X / 10 caracteres mínimos`). El botón de confirmación permanece estrictamente deshabilitado (`disabled`) hasta que la justificación alcance $\ge 10$ caracteres válidos. No es posible enviar peticiones incompletas al backend.
4. **Módulo Desacoplado de Autorregistro (`/autorregistro/:token`):**
   - Vista pública aislada (sin barra de navegación ni controles de gestor) que guía al paciente en un Wizard de 2 pasos a partir de la validación criptográfica del enlace.
5. **Cero Tolerancia a `any` y Consumo por Servicios:**
   - Cero uso de `any` en TypeScript. Todas las respuestas HTTP y formularios reactivos están tipados contra modelos de interfaz (`patient.model.ts`, `contact.model.ts`, `audit.model.ts`, `registration-link.model.ts`, `api-error.model.ts`).
   - Cero llamadas directas a `HttpClient` desde componentes; todo consumo se realiza a través de servicios inyectados (`PatientService`, `ContactService`, `RegistrationLinkService`).

---

## 6. Guía de Puesta en Marcha en Máquina Limpia

### A. Prerrequisitos del Sistema

| Herramienta | Versión Mínima | Comando de Verificación |
| :--- | :---: | :---: |
| **.NET SDK** | `8.0.x` | `dotnet --version` |
| **Node.js** | `v18.x` o `v20.x+` | `node -v` |
| **npm** | `v9.x+` | `npm -v` |
| **SQL Server** | 2019+, Developer o LocalDB | `sqlcmd -?` |

---

### B. Inicialización de Base de Datos (SQL Server)

Ejecutar en orden los 3 scripts versionados ubicados en `scripts/`:

```powershell
# 1. Crear base de datos
sqlcmd -S "(localdb)\MSSQLLocalDB" -Q "IF NOT EXISTS (SELECT 1 FROM sys.databases WHERE name = 'PSP_TBTB') CREATE DATABASE PSP_TBTB;"

# 2. Ejecutar esquema DDL, Stored Procedures y Seed inicial
sqlcmd -S "(localdb)\MSSQLLocalDB" -d PSP_TBTB -i "scripts\01_schema.sql"
sqlcmd -S "(localdb)\MSSQLLocalDB" -d PSP_TBTB -i "scripts\02_stored_procedures.sql"
sqlcmd -S "(localdb)\MSSQLLocalDB" -d PSP_TBTB -i "scripts\03_seed.sql"
```

*(El script `03_seed.sql` incluye usuarios gestores precargados, pacientes iniciales en CO, PE y EC, contactos con auditoría y un generador opcional de 400 pacientes para pruebas de rendimiento).*

---

### C. Ejecución del Backend (.NET 8 WebApi)

1. Ajustar la cadena de conexión en `api/src/WebApi/appsettings.json` según la instancia local utilizada (existe plantilla de ejemplo en `appsettings.Example.json`).
2. Ejecutar la suite de pruebas automatizadas:
   ```bash
   cd api
   dotnet test TBTB.PSP.sln --logger "console;verbosity=detailed"
   ```
   *Resultado esperado: 17 pruebas pasadas (0 fallidas).*
3. Iniciar el servicio WebApi:
   ```bash
   dotnet run --project src/WebApi/WebApi.csproj --urls "http://localhost:5000"
   ```
4. Acceder a la documentación Swagger interactiva:
   - 🔗 **[http://localhost:5000](http://localhost:5000)** (Redirección directa a Swagger UI).
   - 🔗 **[http://localhost:5000/swagger](http://localhost:5000/swagger)**.

---

### D. Ejecución del Frontend (Angular 18)

En una nueva terminal:
```bash
cd web
npm install
npm run build
npm start
```
- La aplicación estará disponible en: 🔗 **[http://localhost:4200](http://localhost:4200)**.

---

## 7. Estructura del Repositorio

```text
tbtb-psp-assessment/
├── 01-hallazgos.md           # Lectura crítica del PRD (7 hallazgos bloqueantes)
├── 02-plan.md                # Cierre de alcance, modelos, contratos y extensión móvil
├── 03-bitacora.md            # Matriz de trazabilidad y registro de decisiones con IA
├── README.md                 # Documentación técnica completa y puesta en marcha
├── scripts/                  # Scripts SQL versionados GxP
│   ├── 01_schema.sql         # Tablas, llaves foráneas e índices de rendimiento
│   ├── 02_stored_procedures.sql # Stored procedures transaccionales (TRY...CATCH)
│   └── 03_seed.sql           # Datos maestros, usuarios y generador de 400 pacientes
├── api/                      # Backend .NET 8 (Clean Architecture)
│   ├── TBTB.PSP.sln
│   ├── src/
│   │   ├── Domain/           # Entidades (Patient, Contact, User), Enums cerrados
│   │   ├── Application/      # DTOs, MappingProfile, FluentValidation
│   │   ├── Infrastructure/   # SqlConnectionFactory, Dapper Repositories
│   │   └── WebApi/           # Controllers con try-catch, Security Headers, FileLogger
│   └── tests/
│       └── Application.UnitTests/ # 17 pruebas xUnit (CA-1, CA-2, CA-3)
└── web/                      # Frontend Angular 18 Standalone
    ├── angular.json
    ├── package.json
    ├── tsconfig.json         # Configurado con "noImplicitAny": true
    └── src/app/
        ├── core/             # Modelos de dominio e Interceptor ProblemDetails
        ├── services/         # PatientService, ContactService, RegistrationLinkService
        └── components/       # Componentes con archivos independientes (.html, .ts, .scss)
            ├── patient-register/
            ├── patient-directory/
            ├── patient-edit-modal/
            ├── contact-modal/
            ├── contact-correct-modal/
            ├── audit-viewer/
            └── self-registration/
```

---

## 8. Documentos de Auditoría y Trazabilidad

Para profundizar en el análisis previo a la construcción del código, consulte los artefactos obligatorios ubicados en la raíz del repositorio:
- **`01-hallazgos.md`:** Identificación de contradicciones en el PRD, análisis de riesgo regulatorio farmacéutico y formulación de supuestos.
- **`02-plan.md`:** Plan de trabajo y justificación de exclusiones, diseño del modelo relacional, contratos de interfaz, estimaciones y estrategia de sincronización offline para aplicaciones móviles en campo.
- **`03-bitacora.md`:** Matriz de trazabilidad que vincula cada criterio de aceptación con sus archivos y pruebas xUnit, junto al registro cronológico de decisiones y correcciones explícitas realizadas sobre las propuestas del asistente de inteligencia artificial.

