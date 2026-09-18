import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { AuditLogItem, Patient, PatientListResponse, PatientRegisterRequest, PatientUpdateRequest } from '../models/models';

@Injectable({
  providedIn: 'root'
})
export class PatientService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = 'http://localhost:5000/api/patients';

  getPatients(
    countryCode?: string,
    status?: string,
    pageNumber: number = 1,
    pageSize: number = 20
  ): Observable<PatientListResponse> {
    let params = new HttpParams()
      .set('pageNumber', pageNumber.toString())
      .set('pageSize', pageSize.toString());

    if (countryCode && countryCode.trim() !== '') {
      params = params.set('countryCode', countryCode);
    }
    if (status && status.trim() !== '') {
      params = params.set('status', status);
    }

    return this.http.get<PatientListResponse>(this.apiUrl, { params });
  }

  getPatientById(id: string): Observable<Patient> {
    return this.http.get<Patient>(`${this.apiUrl}/${id}`);
  }

  registerPatient(request: PatientRegisterRequest): Observable<Patient> {
    return this.http.post<Patient>(this.apiUrl, request);
  }

  updatePatientWithAudit(id: string, request: PatientUpdateRequest): Observable<void> {
    return this.http.put<void>(`${this.apiUrl}/${id}`, request);
  }

  getPatientAuditHistory(id: string): Observable<AuditLogItem[]> {
    return this.http.get<AuditLogItem[]>(`${this.apiUrl}/${id}/audit`);
  }
}
