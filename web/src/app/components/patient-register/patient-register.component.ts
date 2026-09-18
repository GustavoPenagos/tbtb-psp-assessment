import { CommonModule } from '@angular/common';
import { Component, EventEmitter, Output, inject } from '@angular/core';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { CountryCode, DocumentType, PatientRegisterRequest } from '../../core/models/models';
import { PatientService } from '../../core/services/patient.service';
import { ToastService } from '../../core/services/toast.service';

@Component({
  selector: 'app-patient-register',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './patient-register.component.html',
  styleUrl: './patient-register.component.scss'
})
export class PatientRegisterComponent {
  private readonly fb = inject(FormBuilder);
  private readonly patientService = inject(PatientService);
  private readonly toastService = inject(ToastService);

  @Output() patientRegistered = new EventEmitter<void>();

  public isSubmitting: boolean = false;

  public readonly countries: { code: CountryCode; label: string; prefix: string; docType: DocumentType; flag: string }[] = [
    { code: 'CO', label: 'Colombia', prefix: '+57', docType: 'CC', flag: '🇨🇴' },
    { code: 'PE', label: 'Perú', prefix: '+51', docType: 'DNI', flag: '🇵🇪' },
    { code: 'EC', label: 'Ecuador', prefix: '+593', docType: 'CEDULA', flag: '🇪🇨' }
  ];

  public form: FormGroup = this.fb.group({
    countryCode: ['CO' as CountryCode, [Validators.required]],
    fullName: ['', [Validators.required, Validators.maxLength(200)]],
    documentType: ['CC' as DocumentType, [Validators.required]],
    documentNumber: ['', [Validators.required, Validators.pattern(/^[0-9A-Za-z-]{5,20}$/)]],
    phone: ['+57', [Validators.required, Validators.pattern(/^\+[1-9]\d{6,14}$/)]],
    email: ['', [Validators.required, Validators.email, Validators.maxLength(150)]],
    city: ['', [Validators.required, Validators.maxLength(100)]],
    treatmentStart: [new Date().toISOString().substring(0, 10), [Validators.required]],
    followUpDays: [30, [Validators.required, Validators.min(1)]],
    consentDate: [new Date().toISOString().substring(0, 10)]
  });

  onCountryChange(event: Event): void {
    const selected = (event.target as HTMLSelectElement).value as CountryCode;
    const meta = this.countries.find(c => c.code === selected);
    if (meta) {
      this.form.patchValue({
        documentType: meta.docType,
        phone: meta.prefix
      });
    }
  }

  submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.isSubmitting = true;
    const val = this.form.getRawValue();

    const request: PatientRegisterRequest = {
      countryCode: val.countryCode as CountryCode,
      fullName: val.fullName.trim(),
      documentType: val.documentType as DocumentType,
      documentNumber: val.documentNumber.trim(),
      phone: val.phone.trim(),
      email: val.email.trim().toLowerCase(),
      city: val.city.trim(),
      treatmentStart: val.treatmentStart,
      followUpDays: Number(val.followUpDays),
      consentDate: val.consentDate ? val.consentDate : null,
      createdBy: '11111111-1111-1111-1111-111111111111' // Gestor por defecto
    };

    this.patientService.registerPatient(request).subscribe({
      next: (created) => {
        this.isSubmitting = false;
        this.toastService.success(
          'Registro Exitoso',
          `El paciente ${created.fullName} fue registrado con éxito y asignado a estado ACTIVO.`
        );
        this.resetForm();
        this.patientRegistered.emit();
      },
      error: () => {
        this.isSubmitting = false;
      }
    });
  }

  resetForm(): void {
    this.form.reset({
      countryCode: 'CO',
      fullName: '',
      documentType: 'CC',
      documentNumber: '',
      phone: '+57',
      email: '',
      city: '',
      treatmentStart: new Date().toISOString().substring(0, 10),
      followUpDays: 30,
      consentDate: new Date().toISOString().substring(0, 10)
    });
  }
}
