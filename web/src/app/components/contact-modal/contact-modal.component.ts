import { CommonModule } from '@angular/common';
import { Component, EventEmitter, Input, Output, inject } from '@angular/core';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { ContactChannel, ContactCreateRequest, ContactResult } from '../../core/models/models';
import { ContactService } from '../../core/services/contact.service';
import { ToastService } from '../../core/services/toast.service';

@Component({
  selector: 'app-contact-modal',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './contact-modal.component.html',
  styleUrl: './contact-modal.component.scss'
})
export class ContactModalComponent {
  private readonly fb = inject(FormBuilder);
  private readonly contactService = inject(ContactService);
  private readonly toastService = inject(ToastService);

  @Input() isOpen: boolean = false;
  @Input() patientId: string = '';
  @Input() patientName: string = '';
  @Output() closed = new EventEmitter<void>();
  @Output() contactCreated = new EventEmitter<void>();

  public isSubmitting: boolean = false;

  public readonly channels: ContactChannel[] = ['PHONE', 'WHATSAPP', 'EMAIL'];
  public readonly results: ContactResult[] = [
    'SUCCESSFUL_CONTACT',
    'NO_ANSWER',
    'WRONG_NUMBER',
    'REFUSED',
    'APPOINTMENT_SCHEDULED'
  ];

  public form: FormGroup = this.fb.group({
    contactDate: [new Date().toISOString().substring(0, 16), [Validators.required]],
    channel: ['PHONE' as ContactChannel, [Validators.required]],
    result: ['SUCCESSFUL_CONTACT' as ContactResult, [Validators.required]],
    notes: ['', [Validators.maxLength(500)]]
  });

  close(): void {
    this.form.reset({
      contactDate: new Date().toISOString().substring(0, 16),
      channel: 'PHONE',
      result: 'SUCCESSFUL_CONTACT',
      notes: ''
    });
    this.closed.emit();
  }

  submit(): void {
    if (this.form.invalid || !this.patientId) {
      this.form.markAllAsTouched();
      return;
    }

    this.isSubmitting = true;
    const formVal = this.form.getRawValue();

    const request: ContactCreateRequest = {
      patientId: this.patientId,
      contactDate: new Date(formVal.contactDate).toISOString(),
      channel: formVal.channel as ContactChannel,
      result: formVal.result as ContactResult,
      notes: formVal.notes?.trim() || null,
      registeredBy: '11111111-1111-1111-1111-111111111111' // ID de gestor activo por defecto
    };

    this.contactService.createContact(request).subscribe({
      next: () => {
        this.isSubmitting = false;
        this.toastService.success('Contacto Registrado', 'La interacción con el paciente fue guardada exitosamente.');
        this.contactCreated.emit();
        this.close();
      },
      error: () => {
        this.isSubmitting = false;
      }
    });
  }
}
