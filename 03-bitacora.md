# 03 — Trazabilidad y Bitácora de Decisiones

**Programa:** Acompañamiento a Pacientes (PSP)  
**Autor:** Gustavo Penagos  
**Fecha de inicio:** 2026-09-18  
**Herramientas declaradas:** Google Antigravity / Agentic Coding Assistant  

---

## 1. Matriz de Trazabilidad

> Esta matriz mapea cada criterio de aceptación del PRD con su implementación técnica, las pruebas automatizadas asociadas y su estado de cobertura verificado.

| Criterio de Aceptación | Commits / Archivos de Implementación | Prueba Automatizada (xUnit) | Estado |
|---|---|---|---|
| **CA-1:** Registro de paciente por gestor (multi-país CO/PE/EC, validación E.164, unicidad) | `scripts/01_schema.sql`<br>`scripts/02_stored_procedures.sql` (`sp_RegisterPatient`)<br>`PatientService.cs`<br>`PatientsController.cs`<br>`patient-register.component.ts/.html/.scss` | `CA1_RegisterPatient_ValidData_ReturnsActivePatient`<br>`CA1_RegisterPatient_DuplicateDocument_ThrowsConflictException`<br>`CA1_RegisterPatient_InvalidPhone_ThrowsValidationException` | **100% Implementado y Verificado** (Tests en verde) |
| **CA-1 (Variante):** Autorregistro en 2 pasos vía URL con token | `scripts/02_stored_procedures.sql` (`sp_IdentifyPatientSelfReg`, `sp_CompletePatientSelfReg`)<br>`RegistrationService.cs`<br>`RegistrationLinksController.cs`<br>`self-registration.component.ts/.html/.scss` | `CA1_SelfReg_IdentifyPatient_ValidData_ReturnsSessionToken`<br>`CA1_SelfReg_CompletePatient_ValidData_ActivatesPatient` | **100% Implementado y Verificado** (Tests en verde) |
| **CA-2:** Registro de contacto asociado a paciente activo (canales y catálogo de resultados) | `scripts/02_stored_procedures.sql` (`sp_CreateContact`, `sp_GetContactsByPatient`)<br>`ContactService.cs`<br>`ContactsController.cs`<br>`contact-modal.component.ts/.html/.scss` | `CA2_CreateContact_ActivePatient_ReturnsCreatedContact`<br>`CA2_CreateContact_PendingPatient_ThrowsUnprocessableEntityException`<br>`CA2_CreateContact_PatientNotFound_ThrowsNotFoundException`<br>`CA2_CreateContact_InvalidChannel_ThrowsValidationException`<br>`CA2_CreateContact_InvalidResult_ThrowsValidationException` | **100% Implementado y Verificado** (Tests en verde) |
| **CA-3 (Patrón y UI):** Corrección de contacto y actualización de paciente con soft-update y auditoría inmutable (GxP) | `scripts/01_schema.sql`<br>`scripts/02_stored_procedures.sql` (`sp_CorrectContact`, `sp_UpdatePatientWithAudit`)<br>`ContactService.cs`<br>`PatientService.cs`<br>`ContactsController.cs`<br>`PatientsController.cs`<br>`contact-correct-modal.component.ts`<br>`patient-edit-modal.component.ts`<br>`audit-viewer.component.ts/.html/.scss` | `CA3_CorrectContact_ValidData_ReturnsNewActiveContact`<br>`CA3_CorrectContact_ReasonUnder10Chars_ThrowsValidationException`<br>`CA3_UpdatePatient_ValidData_ExecutesSuccessfully`<br>`CA3_UpdatePatient_ReasonUnder10Chars_ThrowsValidationException`<br>`CA3_UpdatePatient_InvalidStatus_ThrowsValidationException` | **100% Implementado y Verificado** (Tests en verde) |
| **CA-4:** Vista filtrada del mes por gestor | Diseñado en modelo de datos e índices (`IX_contacts_gestor_date`). Endpoint omitido en UI/API activa. | N/A (Fuera de alcance) | **Fuera de alcance** (Justificado en `02-plan.md`) |
| **CA-5:** Paciente ilocalizable (3 intentos fallidos) | Estado `UNREACHABLE` contemplado en el enum de `patients`. Lógica de contador omitida por ambigüedad no resuelta (H-03). | N/A (Fuera de alcance) | **Fuera de alcance** (Justificado en `02-plan.md`) |
| **CA-6:** Reporte de adherencia | Desbloqueado con campo `follow_up_days` en `patients`. Fórmula matemática formalizada en `02-plan.md`. | N/A (Fuera de alcance) | **Fuera de alcance** (Justificado en `02-plan.md`) |

---

## 2. Registro de Decisiones y Uso de Inteligencia Artificial

> Registro cronológico de decisiones arquitectónicas y técnicas tomadas durante el proyecto. Se especifica el origen de la propuesta y los puntos críticos donde el desarrollador corrigió o definió las reglas de negocio.

### Decisión 1 — Delimitación de alcance a 2 criterios de punta a punta (CA-1 y CA-2) + patrón CA-3
- **Propuesto por:** Asistente IA (basado en el requerimiento del PRD).
- **Decisión del desarrollador:** Aceptada.
- **Justificación:** La especificación técnica limita deliberadamente la entrega a 2 o 3 criterios de punta a punta. Se prioriza profundidad sobre amplitud: construir CA-1 (pacientes) y CA-2 (contactos) abarcando base de datos, API REST con `ProblemDetails` y frontend Angular, complementado con el patrón de auditoría inmutable de CA-3 en backend y UI.

### Decisión 2 — Corrección del flujo de autorregistro en 2 pasos con URL y token del gestor
- **Propuesto por:** Asistente IA.
- **Decisión del desarrollador:** ❌ **Rechazado y corregido por el desarrollador.**
- **Motivo de la corrección:** La IA propuso inicialmente un formulario tradicional plano de autorregistro o dejar el teléfono para completarse a posteriori en una vista huérfana. El desarrollador intervino y definió la regla de negocio: el gestor genera una URL con token único y el autorregistro se divide en **dos pasos obligatorios**:
  1. *Paso 1 (Filtro de acceso/identificación):* El paciente ingresa nombre y correo con el token, quedando registrado de forma preliminar (`PENDING`).
  2. *Paso 2 (Completitud):* El paciente ingresa los datos obligatorios restantes (documento, teléfono E.164, ciudad, fecha inicio y días de seguimiento), momento en el cual se activa (`ACTIVE`) y el token pasa a `USED`.

### Decisión 3 — Resolución de CA-6 mediante parámetro numérico de seguimiento (`follow_up_days`)
- **Propuesto por:** Asistente IA.
- **Decisión del desarrollador:** ❌ **Rechazado y corregido por el desarrollador.**
- **Motivo de la corrección:** La IA había clasificado el hallazgo H-04 como un "Vacío crítico insalvable" y descartado por completo CA-6 porque el PRD indicaba que el área médica no había confirmado el calendario de seguimiento. El desarrollador corrigió este enfoque indicando que el área médica no necesita un calendario rígido global, sino que **define el calendario de seguimiento en días de forma personalizada al registrar al paciente** (mediante un `input number` para `follow_up_days`). Esto resolvió H-04, habilitó el cálculo de la fecha prevista (`treatment_start + follow_up_days`) y dejó la fórmula de adherencia lista en base de datos.

### Decisión 4 — Estrategia de persistencia y transaccionalidad mandatoria con Stored Procedures
- **Propuesto por:** Asistente IA.
- **Decisión del desarrollador:** ❌ **Rechazado y corregido por el desarrollador.**
- **Motivo de la corrección:** La IA propuso realizar las inserciones y actualizaciones directamente desde la capa de aplicación con Entity Framework Core. El desarrollador rechazó esta aproximación y exigió que **todas las operaciones de modificación y consulta (Insert, Update, Soft-Delete y Get) se ejecuten a través de Stored Procedures**. El motivo es garantizar la seguridad y la integridad referencial entre tablas dependientes (por ejemplo, sincronizar `patients` y `registration_links` en autorregistro, o ejecutar el soft-update atómico entre `contacts` y `contact_audit_log` dentro de un bloque transaccional `BEGIN TRANSACTION ... COMMIT ... CATCH ROLLBACK` en base de datos).

### Decisión 5 — Implementación de soft-update inmutable con snapshot JSON para CA-3 (GxP)
- **Propuesto por:** Desarrollador / Asistente IA alineados.
- **Decisión del desarrollador:** Aceptada.
- **Justificación:** En entornos farmacéuticos regulados por normativas GxP y 21 CFR Part 11, los registros de interacción con pacientes nunca se destruyen ni se sobrescriben con `UPDATE`. Se implementa una tabla inmutable `contact_audit_log` que guarda el snapshot JSON anterior, el nuevo snapshot, fecha, gestor y el campo obligatorio `reason`.

### Decisión 6 — Consulta con criterio multi-tabla e índices para volumen real (Requisito Parte III)
- **Propuesto por:** Desarrollador.
- **Justificación técnica:** Para el análisis de contactos y supervisión de gestores (CA-4 / Reportes), se diseñó la consulta JOIN entre `contacts`, `patients` y `users`:
  ```sql
  SELECT c.id, c.contact_date, c.channel, c.result, p.full_name AS patient_name,
         p.country_code, u.name AS gestor_name
  FROM contacts c
  INNER JOIN patients p ON c.patient_id = p.id
  INNER JOIN users u ON c.registered_by = u.id
  WHERE c.is_active = 1
    AND c.registered_by = @GestorId
    AND c.contact_date >= @StartDate AND c.contact_date < @EndDate
  ORDER BY c.contact_date DESC;
  ```
  - Con este índice, SQL Server realiza un *Index Seek* directo por el gestor y rango de fechas, evitando el costoso *Clustered Index Scan* y eliminando la necesidad de un operador de ordenamiento en memoria (*Sort*). La unión con `patients` y `users` se resuelve mediante *Nested Loops* o *Hash Match* sobre sus claves primarias agrupadas.

### Decisión 7 — Auditoría simétrica para edición de datos maestros de paciente (`patient_audit_log`)
- **Propuesto por:** Desarrollador.
- **Decisión del desarrollador:** Aceptada e incorporada.
- **Motivo de la decisión:** Si bien CA-3 aborda formalmente la corrección de contactos, en la operación diaria del programa los pacientes cambian de teléfono, correo o ciudad. En un entorno farmacéutico GxP, cualquier mutación de datos de salud debe preservar su estado anterior. Se implementó la tabla `patient_audit_log` y el Stored Procedure transaccional `sp_UpdatePatientWithAudit`, exigiendo una justificación obligatoria $\ge 10$ caracteres y registrando el snapshot JSON antes y después del cambio.

### Decisión 8 — Cadena de conexión desacoplada en `appsettings.json` y plantilla sanitizada
- **Propuesto por:** Desarrollador.
- **Decisión del desarrollador:** Aceptada e implementada.
- **Motivo de la decisión:** Garantizar portabilidad técnica y cumplimiento de estándares de desarrollo en .NET 8. La conexión a base de datos se centraliza en `appsettings.json` bajo la clave `ConnectionStrings:DefaultConnection`, inyectada a través de `ISqlConnectionFactory` / `SqlConnectionFactory`. Se creó además `appsettings.Example.json` sin credenciales sensibles para replicabilidad inmediata por parte del evaluador.

### Decisión 9 — Uso sistemático de AutoMapper para transformación DTOs <-> Entidades
- **Propuesto por:** Desarrollador.
- **Decisión del desarrollador:** Aceptada e implementada.
- **Motivo de la decisión:** Desacoplar estrictamente el contrato público de la API de la estructura interna de persistencia. Se implementó `MappingProfile` con mapeos bidireccionales (`PatientRegisterRequestDto` $\leftrightarrow$ `Patient`, `ContactCreateRequestDto` $\leftrightarrow$ `Contact`, `AuditLog` $\rightarrow$ `AuditLogResponseDto`), asegurando que ningún controlador o servicio de aplicación asigne propiedades manualmente.

### Decisión 10 — Modularización de Inyección de Dependencias en `DependencyInjection/`
- **Propuesto por:** Desarrollador.
- **Decisión del desarrollador:** Aceptada e implementada.
- **Motivo de la decisión:** Reducir la sobrecarga en `Program.cs` y cumplir con los principios de Clean Architecture y Single Responsibility Principle. Se extrajo la configuración de contenedores IoC hacia una clase de extensión dedicada `WebApi.DependencyInjection.DependencyInjection` en la carpeta `DependencyInjection/`, organizando los servicios por capa (`AddInfrastructureServices`, `AddApplicationServices`, `AddWebApiServices`, `AddSwaggerDocumentation` y `AddCorsPolicy`), dejando `Program.cs` enfocado exclusivamente en la tubería de middleware HTTP.

---

## 3. Matriz de Cumplimiento de Buenas Prácticas y Restricciones del PDF

| Buena Práctica / Regla del PDF | Criterio de Auditoría | Evidencia en el Repositorio | Estado |
| :--- | :--- | :--- | :--- |
| **1. Tope Estricto de Alcance** | Cumplir con el límite estricto de máximo 2 a 3 CAs (Pág. 4 del PDF) para priorizar profundidad sobre superficialidad. | Alcance cerrado exclusivamente a CA-1 y CA-2 de punta a punta, más el patrón de auditoría inmutable de CA-3. CA-4, CA-5 y CA-6 quedan excluidos formalmente con justificación técnica y matemática en `02-plan.md`. | **Cumplido** |
| **2. Secuencia Histórica de Commits** | El commit de `01-hallazgos.md` y `02-plan.md` debe ser anterior a cualquier código o script (Pág. 3). | Commit inicial `54b9ada` registra exclusivamente los documentos de análisis antes de la creación de scripts o código fuente. | **Cumplido** |
| **3. Persistencia Versionada** | Esquema creado con scripts `.sql` numerados en `scripts/`, nunca desde herramienta gráfica (Pág. 4). | `scripts/01_schema.sql`, `02_stored_procedures.sql` y `03_seed.sql` versionados e idempotentes, ejecutados en LocalDB `PSP_TBTB`. | **Cumplido** |
| **4. Separación de Responsabilidades** | La lógica de negocio no vive en el controlador ni en el componente (Pág. 4). | Lógica encapsulada en Stored Procedures transaccionales y en la capa de servicios de aplicación (`PatientService`, `ContactService`, `RegistrationService`). | **Cumplido** |
| **5. Consumo por Servicio Inyectado** | Prohibidas llamadas directas a `HttpClient` desde componentes de Angular (Pág. 4). | Todo consumo de la API REST se realiza a través de servicios inyectados (`PatientService`, `ContactService`, `RegistrationService`). | **Cumplido** |
| **6. Tipado Estricto (Zero `any`)** | Prohibido el uso de `any` en TypeScript y `dynamic` en C# para garantizar robustez y trazabilidad. | `"noImplicitAny": true` activo en Angular. Interfaces fuertemente tipadas en `models.ts`. DTOs propios en .NET 8 sin exponer entidades de BD. Cero uso de `any`. | **Cumplido** |
| **7. Seguridad OWASP (CORS, SQLi, XSS)** | Mitigación activa contra inyecciones y accesos no autorizados. | CORS restrictivo por configuración, 100% de consultas parametrizadas con Stored Procedures, sanitización de entrada en FluentValidation y cero uso de `[innerHTML]`. | **Cumplido** |
| **8. Manejo de Errores Visible** | Error tratado de punta a punta desde la BD/API hasta la pantalla del usuario (Pág. 4). | Excepciones capturadas con `ProblemDetails` (RFC 7807) en el backend y desplegadas en notificaciones visuales legibles (`ToastComponent`) en frontend. | **Cumplido** |
| **9. Datos de Prueba (Seed Data)** | Script para probar la solución sin inventar datos (Pág. 4). | `scripts/03_seed.sql` con usuarios gestores, pacientes iniciales, contactos y generador de 400 pacientes únicos. | **Cumplido** |
| **10. Tríada de Componentes Desacoplada** | Estricta separación de templates y estilos en archivos dedicados. | Todos los componentes de Angular cuentan con archivos independientes `*.component.html`, `*.component.ts` y `*.component.scss`. Cero estilos o templates inline. | **Cumplido** |
| **11. Logging Forense en Disco** | Trazabilidad de errores no controlados y base de datos persistida físicamente. | `FileLoggerService` thread-safe que escribe en `E:\logs\logs_TBTB.PSP.text` ante excepciones inesperadas. | **Cumplido** |