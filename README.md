# PSP Assessment — TBTB Global Healthcare

**Programa de Acompañamiento a Pacientes (PSP) para Colombia, Perú y Ecuador**  
**Autor:** Gustavo Penagos  
**Stack Tecnológico:** .NET 8 WebApi • Angular 18 Standalone • SQL Server / LocalDB (Transaccional con Stored Procedures)  
**Estándares Regulatorios:** GxP • ALCOA+ • 21 CFR Part 11 • OWASP Top 10 • RFC 7807 (ProblemDetails)

---

## 1. Arquitectura de la Solución

El proyecto implementa una arquitectura desacoplada basada en Clean Architecture en el backend y arquitectura orientada a componentes independientes (Standalone) en el frontend:

```
                                  ARQUITECTURA DE LA SOLUCIÓN
┌────────────────────────────────────────────────────────────────────────────────────────┐
│                                 FRONTEND: ANGULAR 18                                   │
│  [Standalone Components] ─── [Typed Reactive Forms] ─── [Services Inyectados]         │
│  • Aside 1: Registro CA-1  • Aside 2: Directorio      • Aside 3: Auditoría GxP        │
│  • Modales: Corrección CA-3 & Edición Paciente (Motivo obligatorio >= 10 chars)        │
│  • Ruta Pública Aislada: /autorregistro/:token (Wizard Paso 1 -> Paso 2)               │
│  • Interceptor HTTP: ProblemDetails RFC 7807 & Sanitización contextual                │
└─────────────────────────────────────────┬──────────────────────────────────────────────┘
                                          │ HTTPS / CORS (localhost:4200)
┌─────────────────────────────────────────▼──────────────────────────────────────────────┐
│                                BACKEND: .NET 8 WEBAPI                                  │
│  ┌──────────────────────────────────────────────────────────────────────────────────┐  │
│  │ WebApi: Security Headers, CORS, Controllers (try-catch), ExceptionMiddleware     │  │
│  │ Configuration: appsettings.json (DefaultConnection + LoggingConfig)              │  │
│  │ FileLoggerService: Registro forense thread-safe en E:\logs\logs_TBTB.PSP.text   │  │
│  └──────────────────────────────────────┬───────────────────────────────────────────┘  │
│  ┌──────────────────────────────────────▼───────────────────────────────────────────┐  │
│  │ Application: DTOs, AutoMapper Profiles, FluentValidation (Regex + XSS strip)     │  │
│  └──────────────────────────────────────┬───────────────────────────────────────────┘  │
│  ┌──────────────────────────────────────▼───────────────────────────────────────────┐  │
│  │ Domain: Entidades de Negocio, Enums cerrados, Excepciones de Dominio             │  │
│  └──────────────────────────────────────┬───────────────────────────────────────────┘  │
│  ┌──────────────────────────────────────▼───────────────────────────────────────────┐  │
│  │ Infrastructure: SqlConnectionFactory, Repositorios consumiendo solo SPs          │  │
│  └──────────────────────────────────────┬───────────────────────────────────────────┘  │
└─────────────────────────────────────────┼──────────────────────────────────────────────┘
                                          │ TDS / ADO.NET (DynamicParameters fuertemente tipados)
┌─────────────────────────────────────────▼──────────────────────────────────────────────┐
│                          BASE DE DATOS: SQL SERVER / LOCALDB                           │
│  • Tablas: users, patients, registration_links, contacts, contact_audit_log,          │
│            patient_audit_log                                                           │
│  • Stored Procedures Transaccionales (TRY...CATCH + XACT_ABORT + ROLLBACK):            │
│    sp_RegisterPatient, sp_UpdatePatientWithAudit, sp_GetPatientAuditHistory,           │
│    sp_CreateContact, sp_CorrectContact, sp_IdentifyPatientSelfReg, etc.                │
└────────────────────────────────────────────────────────────────────────────────────────┘
```

---

## 2. Alcance Cerrado e Implementación de Criterios (Regla del PDF)

Siguiendo la indicación estricta de la prueba técnica (**máximo 2 a 3 Criterios de Aceptación implementados de punta a punta**):

| Criterio | Descripción | Nivel de Implementación |
|---|---|---|
| **CA-1** | **Registro de Paciente por Gestor y Autorregistro en 2 Pasos:** Validación multi-país (CO, PE, EC), teléfono en formato E.164 obligatorio, unicidad compuesta e índice único filtrado. Enlace de autorregistro generado por gestor que crea registro en `PENDING` (Paso 1) y activa a `ACTIVE` invalidando el token (Paso 2). | **100% Punta a Punta** (BD, API REST, UI Angular, Tests) |
| **CA-2** | **Registro de Contactos:** Interacciones asociadas exclusivamente a pacientes en estado `ACTIVE`. Canales cerrados (`PHONE`, `WHATSAPP`, `EMAIL`) y catálogo de resultados (`SUCCESSFUL_CONTACT`, `NO_ANSWER`, etc.). | **100% Punta a Punta** (BD, API REST, UI Angular, Tests) |
| **CA-3** | **Auditoría Inmutable y Soft-Update GxP:** Corrección de contactos y actualización de pacientes sin destrucción de datos. Registro de snapshot JSON anterior y nuevo en `contact_audit_log` y `patient_audit_log`. Justificación regulatoria (`reason`) obligatoria con contador dinámico $\ge 10$ caracteres. Visualización comparativa con chips rojo tachado vs verde. | **100% Punta a Punta** (BD, API REST, UI Angular, Tests) |
| **CA-4, CA-5, CA-6** | Excluidos deliberadamente de la interfaz de usuario para priorizar la profundidad y solidez técnica exigida en el PDF. Diseñados a nivel de datos e índices, con formulación matemática documentada en `02-plan.md`. | **Diseño y Modelo de Datos** |

---

## 3. Guía de Puesta en Marcha

### Prerrequisitos
- **.NET SDK 8.0** instalado.
- **Node.js 18+** y **npm** instalados.
- **SQL Server** o **SQL Server LocalDB** (`(localdb)\MSSQLLocalDB`).

---

### Paso 1: Base de Datos

1. Crear la base de datos `PSP_TBTB` y ejecutar los scripts en orden:
   ```powershell
   sqlcmd -S "(localdb)\MSSQLLocalDB" -Q "CREATE DATABASE PSP_TBTB;"
   sqlcmd -S "(localdb)\MSSQLLocalDB" -d PSP_TBTB -i "scripts\01_schema.sql"
   sqlcmd -S "(localdb)\MSSQLLocalDB" -d PSP_TBTB -i "scripts\02_stored_procedures.sql"
   sqlcmd -S "(localdb)\MSSQLLocalDB" -d PSP_TBTB -i "scripts\03_seed.sql"
   ```
2. El script `03_seed.sql` precarga:
   - 3 usuarios gestores (`gp@tbtb.com`, `admin@tbtb.com`, `op@tbtb.com`).
   - 5 pacientes de prueba (Colombia, Perú y Ecuador) + 400 pacientes generados con unicidad estricta para pruebas de volumen real.
   - Contactos e historial de trazabilidad GxP.

---

### Paso 2: Backend (.NET 8 WebApi)

1. Verificar la cadena de conexión en `api/src/WebApi/appsettings.json`:
   ```json
   {
     "ConnectionStrings": {
       "DefaultConnection": "Server=(localdb)\\MSSQLLocalDB;Database=PSP_TBTB;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true;"
     },
     "LoggingConfig": {
       "LogDirectory": "E:\\logs",
       "LogFileName": "logs_TBTB.PSP.text"
     },
     "AllowedOrigins": [
       "http://localhost:4200"
     ]
   }
   ```
   *(Nota: Existe también una plantilla sanitizada de referencia en `api/src/WebApi/appsettings.Example.json`).*

2. Compilar la solución completa:
   ```bash
   dotnet build api/TBTB.PSP.sln
   ```

3. Ejecutar las pruebas unitarias automatizadas:
   ```bash
   dotnet test api/TBTB.PSP.sln --logger "console;verbosity=detailed"
   ```
   *(15 pruebas unitarias xUnit cubren CA-1, CA-2 y CA-3 al 100% en verde).*

4. Iniciar la API REST:
   ```bash
   dotnet run --project api/src/WebApi/WebApi.csproj --urls "http://localhost:5000"
   ```
   - **Swagger UI:** `http://localhost:5000/` o `http://localhost:5000/swagger`

---

### Paso 3: Frontend (Angular 18 Standalone)

1. Ingresar a la carpeta `web/` e instalar dependencias (si aplica):
   ```bash
   cd web
   npm install
   ```

2. Compilar en modo producción para verificar tipado estricto y cero errores de estilos:
   ```bash
   npm run build
   ```

3. Iniciar el servidor de desarrollo:
   ```bash
   npm start
   ```
   - La aplicación estará disponible en `http://localhost:4200`.

---

## 4. Estándares Técnicos y Normativos Cumplidos

1. **Persistencia 100% Transaccional:** Cero SQL embebido en código C#. Todas las operaciones de consulta y mutación se ejecutan exclusivamente mediante Stored Procedures con `SET XACT_ABORT ON` y control transaccional ACID (`BEGIN TRY...COMMIT...BEGIN CATCH...ROLLBACK`).
2. **AutoMapper Desacoplado:** Transformación bidireccional entre DTOs (`PatientRegisterRequestDto`, `ContactCreateRequestDto`, etc.) y las Entidades de BD mediante `MappingProfile`.
3. **Manejo de Errores RFC 7807 (ProblemDetails):**
   - 400 Bad Request: Diccionario de errores de validación por campo.
   - 404 Not Found: Recurso no encontrado.
   - 409 Conflict: Duplicidad de documentos, correos o teléfonos.
   - 422 Unprocessable Entity: Regla de negocio infringida (ej. contactar paciente no activo).
   - 500 Internal Server Error: Sanitizado (evita CWE-209) con registro forense en disco (`E:\logs\logs_TBTB.PSP.text`).
4. **Regla de Oro en UI:**
   - Todo componente cuenta con su tríada desacoplada: `*.component.html`, `*.component.ts`, `*.component.scss`. Cero HTML o CSS inline.
   - Prohibición absoluta de `any` en TypeScript (`"noImplicitAny": true`).
   - Contador dinámico $X/10$ caracteres para la justificación de cambio (`reason`) en correcciones y ediciones, con botón de confirmación inhabilitado hasta alcanzar $\ge 10$ caracteres válidos.
   - Columna de **correo electrónico visible** en la tabla del directorio de pacientes.
   - Ruta pública aislada `/autorregistro/:token` para el flujo guiado de pacientes sin exponer controles del gestor.

---

## 5. Documentación de Soporte y Bitácora
- `01-hallazgos.md`: Auditoría de especificaciones y vacíos críticos (H-01 a H-07).
- `02-plan.md`: Plan maestro de arquitectura, modelos y análisis de CAs.
- `03-bitacora.md`: Matriz de trazabilidad, registro de decisiones y bitácora de interacción con IA.
