import { Routes } from '@angular/router';
import { SelfRegistrationComponent } from './components/self-registration/self-registration.component';

export const routes: Routes = [
  {
    path: 'autorregistro/:token',
    component: SelfRegistrationComponent
  }
];
