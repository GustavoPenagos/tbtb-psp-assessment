import { CommonModule } from '@angular/common';
import { Component, EventEmitter, Input, OnChanges, Output, SimpleChanges, inject } from '@angular/core';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { CountryCode, Patient, PatientStatus, PatientUpdateRequest } from '../../core/models/models';
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

  public readonly countries: { code: CountryCode; label: string; prefix: string; flag: string }[] = [
    { code: 'CO', label: 'Colombia', prefix: '+57', flag: '🇨🇴' },
    { code: 'PE', label: 'Perú', prefix: '+51', flag: '🇵🇪' },
    { code: 'EC', label: 'Ecuador', prefix: '+593', flag: '🇪🇨' }
  ];

  public form: FormGroup = this.fb.group({
    phonePrefix: ['+57', [Validators.required]],
    phoneNumber: ['', [Validators.required, Validators.pattern(/^[0-9]{7,12}$/)]],
    city: ['', [Validators.required]],
    followUpDays: [30, [Validators.required, Validators.min(1)]],
    status: ['ACTIVE' as PatientStatus, [Validators.required]],
    reason: ['', [Validators.required, Validators.minLength(10)]]
  });

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['patient'] && this.patient) {
      const { prefix, number } = this.parsePhone(this.patient.phone, this.patient.countryCode);
      this.form.patchValue({
        phonePrefix: prefix,
        phoneNumber: number,
        city: this.patient.city || '',
        followUpDays: this.patient.followUpDays || 30,
        status: this.patient.status,
        reason: ''
      });
    }
  }

  private parsePhone(phone: string | null, countryCode?: string): { prefix: string; number: string } {
    if (!phone) {
      const defaultMeta = this.countries.find(c => c.code === countryCode) || this.countries[0];
      return { prefix: defaultMeta.prefix, number: '' };
    }
    const clean = phone.trim();
    if (clean.startsWith('+593')) {
      return { prefix: '+593', number: clean.substring(4) };
    }
    if (clean.startsWith('+57')) {
      return { prefix: '+57', number: clean.substring(3) };
    }
    if (clean.startsWith('+51')) {
      return { prefix: '+51', number: clean.substring(3) };
    }
    const meta = this.countries.find(c => c.code === countryCode) || this.countries[0];
    return { prefix: meta.prefix, number: clean.replace(/^\+/, '') };
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
      phone: `${val.phonePrefix}${val.phoneNumber.trim()}`,
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
