import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import {
  Patient,
  RegistrationLinkCreateRequest,
  RegistrationLinkResponse,
  SelfRegCompleteRequest,
  SelfRegIdentifyRequest,
  SelfRegIdentifyResponse
} from '../models/models';

@Injectable({
  providedIn: 'root'
})
export class RegistrationService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = 'http://localhost:5000/api/registration-links';

  createLink(request: RegistrationLinkCreateRequest): Observable<RegistrationLinkResponse> {
    return this.http.post<RegistrationLinkResponse>(this.apiUrl, request);
  }

  validateLink(token: string): Observable<RegistrationLinkResponse> {
    return this.http.get<RegistrationLinkResponse>(`${this.apiUrl}/${token}/validate`);
  }

  identify(token: string, request: SelfRegIdentifyRequest): Observable<SelfRegIdentifyResponse> {
    return this.http.post<SelfRegIdentifyResponse>(`${this.apiUrl}/${token}/identify`, request);
  }

  complete(token: string, request: SelfRegCompleteRequest): Observable<Patient> {
    return this.http.post<Patient>(`${this.apiUrl}/${token}/complete`, request);
  }
}
