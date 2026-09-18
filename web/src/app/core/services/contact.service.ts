import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { Contact, ContactCorrectRequest, ContactCreateRequest } from '../models/models';

@Injectable({
  providedIn: 'root'
})
export class ContactService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = 'http://localhost:5000/api/contacts';

  createContact(request: ContactCreateRequest): Observable<Contact> {
    return this.http.post<Contact>(this.apiUrl, request);
  }

  getContactsByPatient(patientId: string): Observable<Contact[]> {
    return this.http.get<Contact[]>(`${this.apiUrl}/patient/${patientId}`);
  }

  correctContact(originalContactId: string, request: ContactCorrectRequest): Observable<Contact> {
    return this.http.put<Contact>(`${this.apiUrl}/${originalContactId}/correct`, request);
  }
}
