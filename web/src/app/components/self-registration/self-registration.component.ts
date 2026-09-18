import { CommonModule } from '@angular/common';
import { Component, OnInit, inject } from '@angular/core';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, RouterModule } from '@angular/router';
import { CountryCode, DocumentType, Patient, RegistrationLinkResponse, SelfRegCompleteRequest, SelfRegIdentifyRequest } from '../../core/models/models';
import { RegistrationService } from '../../core/services/registration.service';
import { ToastService } from '../../core/services/toast.service';

@Component({
  selector: 'app-self-registration',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterModule],
  templateUrl: './self-registration.component.html',
  styleUrl: './self-registration.component.scss'
})
export class SelfRegistrationComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly regService = inject(RegistrationService);
  private readonly toastService = inject(ToastService);
  private readonly fb = inject(FormBuilder);

  public token: string = '';
  public currentStep: 1 | 2 | 3 = 1;
  public isValidatingToken: boolean = true;
  public isTokenValid: boolean = false;
  public tokenError: string = '';
  public isSubmitting: boolean = false;
  public completedPatient: Patient | null = null;

  public readonly countries: { code: CountryCode; label: string; prefix: string; docType: DocumentType; flag: string }[] = [
    { code: 'CO', label: 'Colombia', prefix: '+57', docType: 'CC', flag: '🇨🇴' },
    { code: 'PE', label: 'Perú', prefix: '+51', docType: 'DNI', flag: '🇵🇪' },
    { code: 'EC', label: 'Ecuador', prefix: '+593', docType: 'CEDULA', flag: '🇪🇨' }
  ];

  public step1Form: FormGroup = this.fb.group({
    countryCode: ['CO' as CountryCode, [Validators.required]],
    fullName: ['', [Validators.required, Validators.maxLength(200)]],
    email: ['', [Validators.required, Validators.email, Validators.maxLength(150)]]
  });

  public step2Form: FormGroup = this.fb.group({
    documentType: ['CC' as DocumentType, [Validators.required]],
    documentNumber: ['', [Validators.required, Validators.pattern(/^[0-9A-Za-z-]{5,20}$/)]],
    phonePrefix: ['+57', [Validators.required]],
    phoneNumber: ['', [Validators.required, Validators.pattern(/^[0-9]{7,12}$/)]],
    city: ['', [Validators.required]],
    treatmentStart: [new Date().toISOString().substring(0, 10), [Validators.required]],
    followUpDays: [30, [Validators.required, Validators.min(1)]],
    consentDate: [new Date().toISOString().substring(0, 10)]
  });

  ngOnInit(): void {
    this.token = this.route.snapshot.paramMap.get('token') || '';
    if (!this.token) {
      this.isValidatingToken = false;
      this.tokenError = 'Enlace de autorregistro inválido o ausente.';
      return;
    }

    this.validateToken();
  }

  validateToken(): void {
    this.isValidatingToken = true;
    this.regService.validateLink(this.token).subscribe({
      next: (link: RegistrationLinkResponse) => {
        this.isValidatingToken = false;
        if (link.status === 'USED') {
          this.tokenError = 'Este enlace de autorregistro ya ha sido utilizado previamente.';
          this.isTokenValid = false;
        } else if (link.status === 'EXPIRED') {
          this.tokenError = 'Este enlace de autorregistro ha expirado. Solicite uno nuevo a su gestor del programa.';
          this.isTokenValid = false;
        } else {
          this.isTokenValid = true;
          this.currentStep = 1;
        }
      },
      error: (err: unknown) => {
        this.isValidatingToken = false;
        this.isTokenValid = false;
        this.tokenError = 'El token de autorregistro no existe o ha caducado.';
      }
    });
  }

  onCountryChange(event: Event): void {
    const code = (event.target as HTMLSelectElement).value as CountryCode;
    const meta = this.countries.find(c => c.code === code);
    if (meta) {
      this.step2Form.patchValue({
        documentType: meta.docType,
        phonePrefix: meta.prefix
      });
    }
  }

  onPhonePrefixChange(event: Event): void {
    const selectedPrefix = (event.target as HTMLSelectElement).value;
    const meta = this.countries.find(c => c.prefix === selectedPrefix);
    if (meta) {
      this.step2Form.patchValue({
        documentType: meta.docType
      });
    }
  }

  submitStep1(): void {
    if (this.step1Form.invalid) {
      this.step1Form.markAllAsTouched();
      return;
    }

    this.isSubmitting = true;
    const val = this.step1Form.getRawValue();

    const request: SelfRegIdentifyRequest = {
      countryCode: val.countryCode as CountryCode,
      fullName: val.fullName.trim(),
      email: val.email.trim().toLowerCase()
    };

    this.regService.identify(this.token, request).subscribe({
      next: () => {
        this.isSubmitting = false;
        const meta = this.countries.find(c => c.code === val.countryCode);
        if (meta) {
          this.step2Form.patchValue({
            documentType: meta.docType,
            phonePrefix: meta.prefix
          });
        }
        this.currentStep = 2;
        this.toastService.info('Paso 1 Completado', 'Por favor complete sus datos médicos para finalizar el registro.');
      },
      error: () => {
        this.isSubmitting = false;
      }
    });
  }

  submitStep2(): void {
    if (this.step2Form.invalid) {
      this.step2Form.markAllAsTouched();
      return;
    }

    this.isSubmitting = true;
    const val = this.step2Form.getRawValue();

    const request: SelfRegCompleteRequest = {
      documentType: val.documentType as DocumentType,
      documentNumber: val.documentNumber.trim(),
      phone: `${val.phonePrefix}${val.phoneNumber.trim()}`,
      city: val.city.trim(),
      treatmentStart: val.treatmentStart,
      followUpDays: Number(val.followUpDays),
      consentDate: val.consentDate ? val.consentDate : null
    };

    this.regService.complete(this.token, request).subscribe({
      next: (patient) => {
        this.isSubmitting = false;
        this.completedPatient = patient;
        this.currentStep = 3;
        this.toastService.success(
          '¡Bienvenido al Programa!',
          'Su registro ha sido completado exitosamente y su estado ahora es ACTIVO.'
        );
      },
      error: () => {
        this.isSubmitting = false;
      }
    });
  }
}
