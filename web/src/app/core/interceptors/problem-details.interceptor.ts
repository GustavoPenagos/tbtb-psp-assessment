import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, throwError } from 'rxjs';
import { ProblemDetails } from '../models/models';
import { ToastService } from '../services/toast.service';

export const problemDetailsInterceptor: HttpInterceptorFn = (req, next) => {
  const toastService = inject(ToastService);

  return next(req).pipe(
    catchError((error: unknown) => {
      if (error instanceof HttpErrorResponse) {
        if (error.error && typeof error.error === 'object' && 'title' in error.error) {
          const problem = error.error as ProblemDetails;
          let message = problem.detail || problem.title;

          if (problem.errors && Object.keys(problem.errors).length > 0) {
            const fieldErrors = Object.entries(problem.errors)
              .map(([field, msgs]) => `${field}: ${msgs.join(', ')}`)
              .join(' | ');
            message = `${message} — ${fieldErrors}`;
          }

          toastService.error(`Error (${problem.status})`, message);
        } else if (error.status === 0) {
          toastService.error(
            'Error de Conexión',
            'No fue posible comunicarse con la API de .NET (localhost:5000/https:5001). Verifique que el backend esté en ejecución.'
          );
        } else {
          toastService.error(`Error HTTP ${error.status}`, error.message || 'Error inesperado en la solicitud');
        }
      } else {
        toastService.error('Error del Sistema', 'Ocurrió un error no controlado en la aplicación web.');
      }

      return throwError(() => error);
    })
  );
};
