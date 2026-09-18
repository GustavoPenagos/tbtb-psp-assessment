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

  public auditHistory: ParsedAuditLog[] = [];
  public contacts: Contact[] = [];
  public isLoading: boolean = false;
  public activeTab: 'audits' | 'contacts' = 'audits';

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['patient'] && this.patient) {
      this.loadData();
    }
  }

  loadData(): void {
    if (!this.patient) return;
    this.isLoading = true;

    // Cargar historial de auditoría de paciente
    this.patientService.getPatientAuditHistory(this.patient.id).subscribe({
      next: (logs) => {
        this.auditHistory = logs.map(l => ({
          ...l,
          diffs: this.computeDiff(l.previousValue, l.newValue)
        }));
        this.isLoading = false;
      },
      error: () => {
        this.isLoading = false;
      }
    });

    // Cargar contactos
    this.contactService.getContactsByPatient(this.patient.id).subscribe({
      next: (contacts) => {
        this.contacts = contacts;
      }
    });
  }

  private computeDiff(prevJson: string, newJson: string): FieldDiff[] {
    try {
      const prev = JSON.parse(prevJson) as Record<string, string | number | null>;
      const next = JSON.parse(newJson) as Record<string, string | number | null>;
      const diffs: FieldDiff[] = [];

      const allKeys = Array.from(new Set([...Object.keys(prev), ...Object.keys(next)]));
      for (const key of allKeys) {
        const val1 = String(prev[key] ?? '');
        const val2 = String(next[key] ?? '');
        if (val1 !== val2 && key !== 'updated_at' && key !== 'id') {
          diffs.push({
            field: key,
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

  correct(c: Contact): void {
    this.openCorrectionModal.emit(c);
  }
}
