export type CountryCode = 'CO' | 'PE' | 'EC';
export type DocumentType = 'CC' | 'DNI' | 'CEDULA';
export type PatientStatus = 'PENDING' | 'ACTIVE' | 'INACTIVE' | 'UNREACHABLE';
export type RegistrationSource = 'GESTOR' | 'SELF';
export type ContactChannel = 'PHONE' | 'WHATSAPP' | 'EMAIL';
export type ContactResult =
  | 'SUCCESSFUL_CONTACT'
  | 'NO_ANSWER'
  | 'WRONG_NUMBER'
  | 'REFUSED'
  | 'APPOINTMENT_SCHEDULED';

export interface Patient {
  id: string;
  fullName: string;
  documentType: DocumentType | null;
  documentNumber: string | null;
  countryCode: CountryCode;
  phone: string | null;
  email: string;
  city: string | null;
  treatmentStart: string | null;
  followUpDays: number | null;
  status: PatientStatus;
  registrationSource: RegistrationSource;
  consentDate: string | null;
  createdBy: string;
  createdAt: string;
  updatedAt: string;
}

export interface PatientListResponse {
  items: Patient[];
  totalCount: number;
  pageNumber: number;
  pageSize: number;
}

export interface PatientRegisterRequest {
  fullName: string;
  documentType: DocumentType;
  documentNumber: string;
  countryCode: CountryCode;
  phone: string;
  email: string;
  city: string;
  treatmentStart: string;
  followUpDays: number;
  consentDate?: string | null;
  createdBy: string;
}

export interface PatientUpdateRequest {
  phone: string;
  city: string;
  followUpDays: number;
  status: PatientStatus;
  changedBy: string;
  reason: string;
}

export interface Contact {
  id: string;
  patientId: string;
  contactDate: string;
  channel: ContactChannel;
  result: ContactResult;
  notes?: string | null;
  isActive: boolean;
  registeredBy: string;
  registeredByName?: string;
  createdAt: string;
}

export interface ContactCreateRequest {
  patientId: string;
  contactDate: string;
  channel: ContactChannel;
  result: ContactResult;
  notes?: string | null;
  registeredBy: string;
}

export interface ContactCorrectRequest {
  newContactDate: string;
  newChannel: ContactChannel;
  newResult: ContactResult;
  newNotes?: string | null;
  reason: string;
  changedBy: string;
}

export interface AuditLogItem {
  id: string;
  entityId: string;
  changedBy: string;
  changedByName?: string;
  changedAt: string;
  reason: string;
  previousValue: string;
  newValue: string;
}

export interface RegistrationLinkResponse {
  token: string;
  url: string;
  status: string;
  expiresAt: string;
}

export interface RegistrationLinkCreateRequest {
  createdBy: string;
  expiresInDays: number;
}

export interface SelfRegIdentifyRequest {
  fullName: string;
  email: string;
  countryCode: CountryCode;
}

export interface SelfRegIdentifyResponse {
  patientId: string;
  sessionToken: string;
  nextStep: string;
}

export interface SelfRegCompleteRequest {
  documentType: DocumentType;
  documentNumber: string;
  phone: string;
  city: string;
  treatmentStart: string;
  followUpDays: number;
  consentDate?: string | null;
}

export interface ProblemDetails {
  type?: string;
  title: string;
  status: number;
  detail?: string;
  instance?: string;
  errors?: Record<string, string[]>;
  traceId?: string;
}

export interface ToastMessage {
  id: string;
  type: 'success' | 'error' | 'info' | 'warning';
  title: string;
  message: string;
  durationMs?: number;
}
