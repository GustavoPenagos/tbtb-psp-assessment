# 01 — Lectura Crítica del PRD
**Programa:** Acompañamiento a Pacientes (PSP)  
**Documento base:** Extracto PRD — caso anonimizado (Páginas 8–9)  
**Autor del análisis:** Gustavo Penagos  
**Fecha:** 2026-09-18  

---

## Hallazgos de la Especificación

> **Nota metodológica:** Se priorizan los hallazgos que bloquean la construcción o que generan riesgos regulatorios en un entorno farmacéutico (GxP / trazabilidad de datos de salud). Los hallazgos cosméticos o de UX se dejan en segundo plano.

---

### Tabla de Hallazgos

| # | Referencia | Tipo | Descripción | Impacto | Pregunta al PO | Supuesto de trabajo |
|---|-----------|------|-------------|---------|----------------|---------------------|
| H-01 | Alcance funcional §1 vs §2 | **Contradicción** | §1 establece que el teléfono es **obligatorio** porque el contacto principal es telefónico. §2 (autorregistro) solo pide nombre y correo: no incluye teléfono. Un paciente que se autoregistra queda sin teléfono y no se le puede agendar ninguna llamada. | El paciente autoregistrado nunca puede ser contactado por el canal principal. El CA-1 exigiría que el gestor complete el teléfono manualmente después, creando un flujo huérfano no documentado. | ¿El paciente autoregistrado debe completar su teléfono en un paso posterior antes de quedar disponible para contacto, o el autoregistro es solo una pre-inscripción que el gestor debe completar? | **Supuesto actualizado:** El gestor proporciona al paciente una URL única de registro. El autorregistro se divide en 2 pasos: (1) el paciente se identifica con nombre y correo (filtro de acceso), y (2) completa todos los datos obligatorios restantes (documento, teléfono, ciudad, fecha de inicio de tratamiento). El paciente solo queda en estado `ACTIVE` al completar el paso 2. La URL contiene un token asociado al gestor para trazabilidad. |
| H-02 | CA-3 / Alcance funcional §4 | **Riesgo regulatorio** | "Si el gestor se equivocó... corrige el registro" implica una operación de UPDATE destructivo sobre el registro original. En la industria farmacéutica (y en cualquier programa de soporte al paciente que reporta a un laboratorio), **no se sobrescriben registros de contacto**: se requiere un registro de auditoría inmutable con el valor anterior, el nuevo valor, el motivo del cambio (*reason for change*), la fecha y el usuario que realizó la corrección. | El reporte de adherencia entregado al laboratorio podría reflejar datos sin evidencia de la corrección, lo cual viola principios GxP básicos. En una auditoría, la trazabilidad de cambios es exigible. | ¿El sistema debe guardar el historial de modificaciones de cada contacto con motivo de cambio? ¿Existe algún requerimiento regulatorio explícito del laboratorio patrocinador sobre trazabilidad de datos? | Se implementa **soft-update con auditoría**: el registro original se marca como inactivo (`is_active = false`) y se crea un nuevo registro activo con los datos corregidos. La tabla `contact_audit_log` guarda: `contact_id`, `changed_by`, `changed_at`, `reason`, `previous_value (JSON)`, `new_value (JSON)`. |
| H-03 | CA-5 / Alcance funcional §3 | **Ambigüedad** | "No contesta tres veces consecutivas" no define: (a) la ventana temporal entre intentos, (b) si los tres intentos deben ser en canales distintos o puede ser el mismo, (c) si el contador se reinicia cuando el paciente sí contesta después, ni (d) quién/qué proceso marca al paciente como ilocalizable (¿automático al guardar el tercer contacto fallido, o es una acción manual de la coordinadora?). | Se puede implementar un contador que nunca resetea correctamente, o que marca como ilocalizables a pacientes que no contestaron dos llamadas seguidas pero sí un WhatsApp. El estado `ILOCALIZABLE` afecta directamente el denominador del reporte de adherencia (CA-6). | ¿"Consecutivas" significa en el orden cronológico de registro independientemente del canal, o solo para llamadas telefónicas? ¿En qué ventana de tiempo deben darse los tres intentos? ¿Quién dispara el cambio de estado: el sistema automáticamente o la coordinadora manualmente? | Se asume: tres contactos registrados con resultado `NO_CONTESTA` en cualquier canal, en orden cronológico sin un contacto exitoso intermedio, disparan automáticamente el cambio de estado del paciente a `UNREACHABLE`. No se define ventana temporal (aplica a cualquier fecha). El estado se puede revertir manualmente por la coordinadora. **Este criterio (CA-5) queda fuera del alcance de entrega** por la ambigüedad no resuelta. |
| H-04 | CA-6 / Notas del documento | **Vacío resuelto** | El reporte de adherencia calcula "porcentaje de pacientes contactados dentro del plazo previsto". Las notas originales indicaban que el calendario estaba pendiente con el área médica, dejando la métrica sin plazo definido. | La adherencia requiere una base temporal concreta por paciente para determinar si el contacto ocurrió "a tiempo". | ¿Cómo define el área médica el calendario de seguimiento para cada paciente? ¿Es global o personalizado? | **Supuesto resuelto:** El área médica define el calendario de seguimiento en días de manera personalizada al registrar el paciente (mediante un campo numérico `follow_up_days` / input number). La fecha límite prevista se calcula como: `treatment_start + follow_up_days`. Esto desbloquea por completo la métrica matemática de CA-6 en el modelo de datos. |
| H-05 | Alcance funcional §1 / Contexto | **Límite no definido** | El sistema opera en Colombia, Perú y Ecuador. Cada país tiene formatos distintos de documento de identidad (CC en Colombia, DNI en Perú, Cédula de Ciudadanía/Identidad en Ecuador), prefijos telefónicos internacionales (+57, +51, +593) y regulaciones de privacidad de datos de salud diferentes (Ley 1581/2012 Colombia, Ley N° 29733 Perú, LOPDP Ecuador). El PRD no especifica cómo se valida el documento, ni si el número de documento es único por país o globalmente único en el sistema. | Se puede registrar el mismo número de documento de dos países distintos como duplicado (error), o no detectar duplicados reales dentro de un mismo país. La validación de formato de teléfono sin prefijo internacional generará datos inconsistentes. | ¿El número de documento identifica univocamente a un paciente junto con el tipo y el país? ¿El sistema exige el prefijo internacional en el teléfono o lo infiere del país del paciente? | Se asume que la clave única de paciente es la combinación `(country_code, document_type, document_number)`. El teléfono se almacena con prefijo internacional obligatorio (formato E.164). El país del paciente se captura como campo obligatorio en el registro. |
| H-06 | Alcance funcional §1 / CA-1 | **Límite no definido** | El PRD menciona "el paciente queda disponible para agendar contactos" pero no define el concepto de **asignación de gestor**. ¿Cualquier gestor puede contactar a cualquier paciente, o cada paciente tiene un gestor asignado? Esto afecta directamente la vista de filtrado por gestor (CA-4) y la extensión móvil offline. | Sin un modelo de asignación claro, el filtro "por gestor" en CA-4 no se puede implementar correctamente. Si cualquier gestor puede contactar a cualquier paciente, el filtro filtra por "gestor que realizó el contacto", no por "gestor responsable del paciente". | ¿Los pacientes se asignan a un gestor específico, o cualquier gestor puede registrar contactos para cualquier paciente? ¿Un paciente puede reasignarse entre gestores? | Se asume que cada contacto registrado lleva el ID del gestor que lo realizó (`registered_by`). No se implementa asignación fija de paciente a gestor en esta entrega. CA-4 filtra por el gestor que realizó el contacto. |
| H-07 | CA-2 / Alcance funcional §3 | **Vacío** | El PRD define los canales como "llamada, WhatsApp o correo", pero no define el catálogo de **resultados posibles** de un contacto. ¿Cuáles son los resultados válidos? Sin este catálogo, cada gestor podría ingresar texto libre, haciendo imposible el análisis estadístico y el cálculo de CA-5 y CA-6. | Los reportes de adherencia y el marcado de ilocalizables dependen de poder clasificar los resultados. Con texto libre no hay forma programática de detectar "no contestó". | ¿Cuál es el catálogo cerrado de resultados posibles para un contacto? ¿Es el mismo para todos los canales, o varía por canal? | Se implementa un **enum cerrado de resultados**: `SUCCESSFUL_CONTACT`, `NO_ANSWER`, `WRONG_NUMBER`, `REFUSED`, `APPOINTMENT_SCHEDULED`. Se valida en el backend que el resultado sea uno de los valores permitidos. |

---

## Resumen de Severidad

| # Hallazgo | Severidad | ¿Bloquea construcción? | Criterio afectado |
|-----------|-----------|------------------------|-------------------|
| H-01 | 🔴 Alta | Sí — flujo roto en autoregistro | CA-1 |
| H-02 | 🔴 Alta | Sí — riesgo regulatorio real | CA-3 |
| H-03 | 🟠 Media | Sí — lógica de negocio indeterminada | CA-5 |
| H-04 | 🟢 Baja (Resuelto) | No — desbloqueado con `follow_up_days` | CA-6 |
| H-05 | 🟠 Media | Sí — validaciones de datos inconsistentes | CA-1, CA-2 |
| H-06 | 🟡 Baja | No — afecta semántica de filtros | CA-4 |
| H-07 | 🔴 Alta | Sí — análisis y CA-5/CA-6 bloqueados | CA-2, CA-5, CA-6 |

---

## Criterios descartados y justificación

| Criterio | Razón de descarte |
|---------|-------------------|
| **CA-5** (Ilocalizable) | Ambigüedad no resuelta sobre ventana temporal y disparador (H-03). Sin aclaración del PO, cualquier implementación sería una suposición de alto riesgo. |
| **CA-6** (Reporte de adherencia) | Regla de negocio desbloqueada y modelada con `follow_up_days` (input numérico definido por el área médica). Se mantiene fuera de la construcción activa de UI/API para respetar la restricción de la prueba de enfocar 2 criterios de punta a punta (CA-1 y CA-2). |

---

## Hallazgos Regulatorios Específicos del Sector Farmacéutico

> Estos hallazgos no aparecen explícitamente en el PRD porque el autor no consideró el contexto regulado.

| Riesgo | Descripción | Recomendación |
|--------|-------------|---------------|
| **Consentimiento informado** | Los datos de salud y contacto de pacientes en programas de soporte requieren consentimiento explícito bajo las leyes de cada país. El PRD no menciona el campo de consentimiento ni la fecha de firma. | Agregar `consent_date` y `consent_type` al modelo de paciente. |
| **Trazabilidad de cambios (GxP)** | Toda modificación a datos clínicos o de seguimiento debe quedar auditada. Un UPDATE directo sin log es inaceptable en entornos regulados. | Patrón de corrección con `audit_log` (contemplado en supuesto H-02). |
| **Retención y eliminación de datos** | Cuando un paciente solicita borrado de sus datos, la regulación puede requerir anonimización en lugar de eliminación física para preservar estadísticas históricas. | El modelo debe soportar `anonymized_at` en lugar de DELETE físico. |

---

*Documento generado como primer entregable antes de cualquier línea de código, cumpliendo el requisito de secuencia de commits establecido en la Parte I de la prueba técnica.*
