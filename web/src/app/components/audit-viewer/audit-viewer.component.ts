import { CommonModule } from '@angular/common';
import { Component, EventEmitter, Input, OnChanges, Output, SimpleChanges, inject } from '@angular/core';
import { AuditLogItem, Contact, Patient } from '../../core/models/models';
import { ContactService } from '../../core/services/contact.service';
import { PatientService } from '../../core/services/patient.service';

interface FieldDiff {
  field: string;
  oldValue: string;
  newValue: string;
}

interface ParsedAuditLog extends AuditLogItem {
  diffs: FieldDiff[];
}

@Component({
  selector: 'app-audit-viewer',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './audit-viewer.component.html',
  styleUrl: './audit-viewer.component.scss'
})
export class AuditViewerComponent implements OnChanges {
  private readonly patientService = inject(PatientService);
  private readonly contactService = inject(ContactService);

  @Input() patient: Patient | null = null;
  @Output() close = new EventEmitter<void>();
  @Output() openCorrectionModal = new EventEmitter<Contact>();

  public patientAudits: ParsedAuditLog[] = [];
  public contactAudits: ParsedAuditLog[] = [];
  public contacts: Contact[] = [];
  public isLoading: boolean = false;
  public activeTab: 'patient-audit' | 'contact-audit' | 'contacts' = 'contact-audit';

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['patient'] && this.patient) {
      this.loadData();
    }
  }

  loadData(): void {
    if (!this.patient) return;
    this.isLoading = true;

    // 1. Cargar historial de auditoría de paciente (patient_audit_log)
    this.patientService.getPatientAuditHistory(this.patient.id).subscribe({
      next: (logs) => {
        this.patientAudits = logs.map(l => ({
          ...l,
          diffs: this.computeDiff(l.previousValue, l.newValue, l)
        }));
      },
      error: () => {}
    });

    // 2. Cargar historial de auditoría de contactos (contact_audit_log)
    this.contactService.getContactAuditByPatient(this.patient.id).subscribe({
      next: (logs) => {
        this.contactAudits = logs.map(l => ({
          ...l,
          diffs: this.computeDiff(l.previousValue, l.newValue, l)
        }));
        this.isLoading = false;
      },
      error: () => {
        this.isLoading = false;
      }
    });

    // 3. Cargar lista de contactos
    this.contactService.getContactsByPatient(this.patient.id).subscribe({
      next: (contacts) => {
        this.contacts = contacts;
      }
    });
  }

  private readonly FIELD_LABELS: Record<string, string> = {
    contact_date: 'Fecha de contacto',
    channel: 'Canal',
    result: 'Resultado',
    notes: 'Notas / Observaciones',
    registered_by: 'Registrado por',
    phone: 'Teléfono',
    city: 'Ciudad',
    follow_up_days: 'Días de seguimiento',
    status: 'Estado',
    full_name: 'Nombre completo',
    document_type: 'Tipo de documento',
    document_number: 'Número de documento',
    treatment_start: 'Inicio de tratamiento',
    consent_date: 'Fecha de consentimiento'
  };

  private computeDiff(prevJson: string, newJson: string, logItem?: AuditLogItem): FieldDiff[] {
    try {
      const prev = JSON.parse(prevJson) as Record<string, string | number | null>;
      const next = JSON.parse(newJson) as Record<string, string | number | null>;
      const diffs: FieldDiff[] = [];

      const allKeys = Array.from(new Set([...Object.keys(prev), ...Object.keys(next)]));
      for (const key of allKeys) {
        // OMITIR estrictamente patient_id, id, is_active, updated_at y created_at
        if (
          key === 'patient_id' ||
          key === 'id' ||
          key === 'is_active' ||
          key === 'updated_at' ||
          key === 'created_at'
        ) {
          continue;
        }

        let val1 = String(prev[key] ?? '');
        let val2 = String(next[key] ?? '');

        // Si es registered_by y viene un ID/GUID, resolverlo con el nombre de quien realiza el registro
        if (key === 'registered_by') {
          if (logItem?.changedByName) {
            if (val2 && (val2 === logItem.changedBy || this.isGuid(val2))) {
              val2 = logItem.changedByName;
            }
            if (val1 && (val1 === logItem.changedBy || this.isGuid(val1))) {
              val1 = logItem.changedByName;
            }
          }
        }

        if (val1 !== val2) {
          diffs.push({
            field: this.FIELD_LABELS[key] || key,
            oldValue: val1 || '(vacío)',
            newValue: val2 || '(vacío)'
          });
        }
      }
      return diffs;
    } catch {
      return [{ field: 'Modificación', oldValue: prevJson, newValue: newJson }];
    }
  }

  private isGuid(value: string): boolean {
    return /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i.test(value);
  }

  correct(c: Contact): void {
    this.openCorrectionModal.emit(c);
  }
}
