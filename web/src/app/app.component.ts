import { CommonModule } from '@angular/common';
import { Component, OnInit, ViewChild, inject } from '@angular/core';
import { NavigationEnd, Router, RouterModule } from '@angular/router';
import { filter } from 'rxjs';
import { AuditViewerComponent } from './components/audit-viewer/audit-viewer.component';
import { ContactCorrectModalComponent } from './components/contact-correct-modal/contact-correct-modal.component';
import { ContactModalComponent } from './components/contact-modal/contact-modal.component';
import { PatientDirectoryComponent } from './components/patient-directory/patient-directory.component';
import { PatientEditModalComponent } from './components/patient-edit-modal/patient-edit-modal.component';
import { PatientRegisterComponent } from './components/patient-register/patient-register.component';
import { Contact, Patient } from './core/models/models';
import { ToastComponent } from './shared/components/toast/toast.component';

type ActiveTab = 'directory' | 'register' | 'audit';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [
    CommonModule,
    RouterModule,
    ToastComponent,
    PatientDirectoryComponent,
    PatientRegisterComponent,
    AuditViewerComponent,
    ContactModalComponent,
    ContactCorrectModalComponent,
    PatientEditModalComponent
  ],
  templateUrl: './app.component.html',
  styleUrl: './app.component.scss'
})
export class AppComponent implements OnInit {
  private readonly router = inject(Router);

  @ViewChild(PatientDirectoryComponent) directoryComp?: PatientDirectoryComponent;
  @ViewChild(AuditViewerComponent) auditViewerComp?: AuditViewerComponent;

  public isPublicRoute: boolean = false;
  public activeTab: ActiveTab = 'directory';

  // Modales
  public isContactModalOpen: boolean = false;
  public isContactCorrectModalOpen: boolean = false;
  public isPatientEditModalOpen: boolean = false;

  public selectedPatientForContact: Patient | null = null;
  public selectedPatientForEdit: Patient | null = null;
  public selectedPatientForAudit: Patient | null = null;
  public selectedContactForCorrection: Contact | null = null;

  ngOnInit(): void {
    this.checkIfPublicRoute(this.router.url);

    this.router.events
      .pipe(filter((event): event is NavigationEnd => event instanceof NavigationEnd))
      .subscribe((event: NavigationEnd) => {
        this.checkIfPublicRoute(event.urlAfterRedirects);
      });
  }

  private checkIfPublicRoute(url: string): void {
    this.isPublicRoute = url.includes('/autorregistro');
  }

  setTab(tab: ActiveTab): void {
    this.activeTab = tab;
  }

  // Handlers para abrir modales
  openContact(p: Patient): void {
    this.selectedPatientForContact = p;
    this.isContactModalOpen = true;
  }

  openEdit(p: Patient): void {
    this.selectedPatientForEdit = p;
    this.isPatientEditModalOpen = true;
  }

  openAudit(p: Patient): void {
    this.selectedPatientForAudit = p;
    this.activeTab = 'audit';
  }

  openCorrection(c: Contact): void {
    this.selectedContactForCorrection = c;
    this.isContactCorrectModalOpen = true;
  }

  onPatientRegistered(): void {
    this.activeTab = 'directory';
    this.directoryComp?.loadPatients();
  }

  onPatientUpdated(): void {
    this.directoryComp?.loadPatients();
    if (this.selectedPatientForAudit && this.selectedPatientForEdit?.id === this.selectedPatientForAudit.id) {
      this.auditViewerComp?.loadData();
    }
  }

  onContactCreated(): void {
    this.auditViewerComp?.loadData();
  }

  onContactCorrected(): void {
    this.auditViewerComp?.loadData();
  }
}
