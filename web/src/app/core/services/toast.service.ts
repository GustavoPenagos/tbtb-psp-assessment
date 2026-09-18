import { Injectable, signal } from '@angular/core';
import { ToastMessage } from '../models/models';

@Injectable({
  providedIn: 'root'
})
export class ToastService {
  private readonly _toasts = signal<ToastMessage[]>([]);
  public readonly toasts = this._toasts.asReadonly();

  show(type: 'success' | 'error' | 'info' | 'warning', title: string, message: string, durationMs: number = 5000): void {
    const id = Math.random().toString(36).substring(2, 9);
    const toast: ToastMessage = { id, type, title, message, durationMs };

    this._toasts.update(current => [...current, toast]);

    if (durationMs > 0) {
      setTimeout(() => {
        this.remove(id);
      }, durationMs);
    }
  }

  success(title: string, message: string): void {
    this.show('success', title, message);
  }

  error(title: string, message: string): void {
    this.show('error', title, message, 8000);
  }

  warning(title: string, message: string): void {
    this.show('warning', title, message);
  }

  info(title: string, message: string): void {
    this.show('info', title, message);
  }

  remove(id: string): void {
    this._toasts.update(current => current.filter(t => t.id !== id));
  }
}
