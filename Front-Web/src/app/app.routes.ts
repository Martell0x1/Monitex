import { Routes } from '@angular/router';
import { Login } from './login/login';
import { Dashboard } from './dashboard/dashboard';
import { AddDevice } from './add-device/add-device';
import { AddSensors } from './add-sensors/add-sensors';

export const routes: Routes = [
  { path: '', pathMatch: 'full', redirectTo: 'login' },
  { path: 'login', component: Login },
  { path: 'dashboard', component: Dashboard },
  { path: 'add-device', component: AddDevice },
  { path: 'add-sensors', component: AddSensors },
  { path: '**', redirectTo: 'login' },
];
