import { CommonModule } from '@angular/common';
import { Component, EventEmitter, Input, OnChanges, Output, SimpleChanges, inject } from '@angular/core';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { Patient, PatientStatus, PatientUpdateRequest } from '../../core/models/models';
import { PatientService } from '../../core/services/patient.service';
import { ToastService } from '../../core/services/toast.service';

@Component({
  selector: 'app-patient-edit-modal',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './patient-edit-modal.component.html',
  styleUrl: './patient-edit-modal.component.scss'
})
export class PatientEditModalComponent implements OnChanges {
  private readonly fb = inject(FormBuilder);
  private readonly patientService = inject(PatientService);
  private readonly toastService = inject(ToastService);

  @Input() isOpen: boolean = false;
  @Input() patient: Patient | null = null;
  @Output() closed = new EventEmitter<void>();
  @Output() updated = new EventEmitter<void>();

  public isSubmitting: boolean = false;

  public readonly statuses: PatientStatus[] = ['ACTIVE', 'INACTIVE', 'UNREACHABLE', 'PENDING'];

  public form: FormGroup = this.fb.group({
    phone: ['', [Validators.required, Validators.pattern(/^\+[1-9]\d{6,14}$/)]],
    city: ['', [Validators.required]],
    followUpDays: [30, [Validators.required, Validators.min(1)]],
    status: ['ACTIVE' as PatientStatus, [Validators.required]],
    reason: ['', [Validators.required, Validators.minLength(10)]]
  });

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['patient'] && this.patient) {
      this.form.patchValue({
        phone: this.patient.phone || '',
        city: this.patient.city || '',
        followUpDays: this.patient.followUpDays || 30,
        status: this.patient.status,
        reason: ''
      });
    }
  }

  get reasonLength(): number {
    return (this.form.get('reason')?.value || '').trim().length;
  }

  get isReasonValid(): boolean {
    return this.reasonLength >= 10;
  }

  close(): void {
    this.form.reset();
    this.closed.emit();
  }

  submit(): void {
    if (this.form.invalid || !this.isReasonValid || !this.patient) {
      this.form.markAllAsTouched();
      return;
    }

    this.isSubmitting = true;
    const val = this.form.getRawValue();

    const request: PatientUpdateRequest = {
      phone: val.phone.trim(),
      city: val.city.trim(),
      followUpDays: Number(val.followUpDays),
      status: val.status as PatientStatus,
      changedBy: '11111111-1111-1111-1111-111111111111',
      reason: val.reason.trim()
    };

    this.patientService.updatePatientWithAudit(this.patient.id, request).subscribe({
      next: () => {
        this.isSubmitting = false;
        this.toastService.success(
          'Paciente Actualizado',
          'Los datos del paciente fueron actualizados y auditados bajo normativa GxP.'
        );
        this.updated.emit();
        this.close();
      },
      error: () => {
        this.isSubmitting = false;
      }
    });
  }
}
