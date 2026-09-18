import { CommonModule } from '@angular/common';
import { Component, inject } from '@angular/core';
import { ToastMessage } from '../../../core/models/models';
import { ToastService } from '../../../core/services/toast.service';

@Component({
  selector: 'app-toast',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './toast.component.html',
  styleUrl: './toast.component.scss'
})
export class ToastComponent {
  private readonly toastService = inject(ToastService);
  public readonly toasts = this.toastService.toasts;

  close(id: string): void {
    this.toastService.remove(id);
  }

  trackByToastId(_index: number, toast: ToastMessage): string {
    return toast.id;
  }
}
