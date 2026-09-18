import { CommonModule } from '@angular/common';
import { Component, EventEmitter, Input, OnChanges, Output, SimpleChanges, inject } from '@angular/core';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { Contact, ContactChannel, ContactCorrectRequest, ContactResult } from '../../core/models/models';
import { ContactService } from '../../core/services/contact.service';
import { ToastService } from '../../core/services/toast.service';

@Component({
  selector: 'app-contact-correct-modal',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './contact-correct-modal.component.html',
  styleUrl: './contact-correct-modal.component.scss'
})
export class ContactCorrectModalComponent implements OnChanges {
  private readonly fb = inject(FormBuilder);
  private readonly contactService = inject(ContactService);
  private readonly toastService = inject(ToastService);

  @Input() isOpen: boolean = false;
  @Input() contact: Contact | null = null;
  @Output() closed = new EventEmitter<void>();
  @Output() corrected = new EventEmitter<void>();

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
    newContactDate: ['', [Validators.required]],
    newChannel: ['PHONE' as ContactChannel, [Validators.required]],
    newResult: ['SUCCESSFUL_CONTACT' as ContactResult, [Validators.required]],
    newNotes: ['', [Validators.maxLength(500)]],
    reason: ['', [Validators.required, Validators.minLength(10)]]
  });

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['contact'] && this.contact) {
      const dateVal = this.contact.contactDate
        ? new Date(this.contact.contactDate).toISOString().substring(0, 16)
        : new Date().toISOString().substring(0, 16);

      this.form.patchValue({
        newContactDate: dateVal,
        newChannel: this.contact.channel,
        newResult: this.contact.result,
        newNotes: this.contact.notes || '',
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
    if (this.form.invalid || !this.isReasonValid || !this.contact) {
      this.form.markAllAsTouched();
      return;
    }

    this.isSubmitting = true;
    const val = this.form.getRawValue();

    const request: ContactCorrectRequest = {
      newContactDate: new Date(val.newContactDate).toISOString(),
      newChannel: val.newChannel as ContactChannel,
      newResult: val.newResult as ContactResult,
      newNotes: val.newNotes?.trim() || null,
      reason: val.reason.trim(),
      changedBy: '11111111-1111-1111-1111-111111111111'
    };

    this.contactService.correctContact(this.contact.id, request).subscribe({
      next: () => {
        this.isSubmitting = false;
        this.toastService.success(
          'Corrección Registrada',
          'El contacto fue actualizado de forma atómica y auditado en la bitácora inmutable.'
        );
        this.corrected.emit();
        this.close();
      },
      error: () => {
        this.isSubmitting = false;
      }
    });
  }
}
