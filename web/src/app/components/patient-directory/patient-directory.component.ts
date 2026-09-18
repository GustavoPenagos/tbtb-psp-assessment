import { CommonModule } from '@angular/common';
import { Component, EventEmitter, OnInit, Output, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { CountryCode, Patient, PatientStatus } from '../../core/models/models';
import { PatientService } from '../../core/services/patient.service';
import { RegistrationService } from '../../core/services/registration.service';
import { ToastService } from '../../core/services/toast.service';

@Component({
  selector: 'app-patient-directory',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './patient-directory.component.html',
  styleUrl: './patient-directory.component.scss'
})
export class PatientDirectoryComponent implements OnInit {
  private readonly patientService = inject(PatientService);
  private readonly registrationService = inject(RegistrationService);
  private readonly toastService = inject(ToastService);

  @Output() openContactModal = new EventEmitter<Patient>();
  @Output() openEditModal = new EventEmitter<Patient>();
  @Output() openAuditViewer = new EventEmitter<Patient>();

  public patients: Patient[] = [];
  public filteredPatients: Patient[] = [];
  public isLoading: boolean = false;
  public totalCount: number = 0;

  public searchFilter: string = '';
  public selectedCountry: string = '';
  public selectedStatus: string = '';
  public pageNumber: number = 1;
  public pageSize: number = 15;
  public totalPages: number = 1;

  ngOnInit(): void {
    this.loadPatients();
  }

  loadPatients(): void {
    this.isLoading = true;
    this.patientService.getPatients(
      this.selectedCountry || undefined,
      this.selectedStatus || undefined,
      this.pageNumber,
      this.pageSize
    ).subscribe({
      next: (res) => {
        this.patients = res.items;
        this.totalCount = res.totalCount;
        this.totalPages = Math.ceil(this.totalCount / this.pageSize) || 1;
        this.applySearchFilter();
        this.isLoading = false;
      },
      error: () => {
        this.isLoading = false;
      }
    });
  }

  onFilterChange(): void {
    this.pageNumber = 1;
    this.loadPatients();
  }

  goToPage(page: number): void {
    if (page < 1 || page > this.totalPages || page === this.pageNumber) {
      return;
    }
    this.pageNumber = page;
    this.loadPatients();
  }

  nextPage(): void {
    if (this.pageNumber < this.totalPages) {
      this.pageNumber++;
      this.loadPatients();
    }
  }

  prevPage(): void {
    if (this.pageNumber > 1) {
      this.pageNumber--;
      this.loadPatients();
    }
  }

  firstPage(): void {
    if (this.pageNumber !== 1) {
      this.pageNumber = 1;
      this.loadPatients();
    }
  }

  lastPage(): void {
    if (this.pageNumber !== this.totalPages) {
      this.pageNumber = this.totalPages;
      this.loadPatients();
    }
  }

  getStartIndex(): number {
    if (this.totalCount === 0) return 0;
    return (this.pageNumber - 1) * this.pageSize + 1;
  }

  getEndIndex(): number {
    return Math.min(this.pageNumber * this.pageSize, this.totalCount);
  }

  getVisiblePages(): number[] {
    const pages: number[] = [];
    const maxPagesToShow = 5;
    let startPage = Math.max(1, this.pageNumber - Math.floor(maxPagesToShow / 2));
    let endPage = Math.min(this.totalPages, startPage + maxPagesToShow - 1);

    if (endPage - startPage + 1 < maxPagesToShow) {
      startPage = Math.max(1, endPage - maxPagesToShow + 1);
    }

    for (let i = startPage; i <= endPage; i++) {
      pages.push(i);
    }
    return pages;
  }

  applySearchFilter(): void {
    const q = this.searchFilter.trim().toLowerCase();
    if (!q) {
      this.filteredPatients = [...this.patients];
      return;
    }

    this.filteredPatients = this.patients.filter(p =>
      p.fullName.toLowerCase().includes(q) ||
      (p.documentNumber && p.documentNumber.toLowerCase().includes(q)) ||
      p.email.toLowerCase().includes(q) ||
      (p.phone && p.phone.includes(q)) ||
      (p.city && p.city.toLowerCase().includes(q))
    );
  }

  generateSelfRegLink(): void {
    this.registrationService.createLink({
      createdBy: '11111111-1111-1111-1111-111111111111',
      expiresInDays: 7
    }).subscribe({
      next: (res) => {
        const fullUrl = `${window.location.origin}/autorregistro/${res.token}`;
        navigator.clipboard.writeText(fullUrl).then(() => {
          this.toastService.success(
            'Enlace Copiado',
            `El enlace de autorregistro temporal fue copiado al portapapeles: ${fullUrl}`
          );
        });
      }
    });
  }

  trackByPatientId(_index: number, patient: Patient): string {
    return patient.id;
  }
}
