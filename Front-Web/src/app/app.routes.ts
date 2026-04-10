import { Routes } from '@angular/router';
import { Login } from './login/login';
import { Dashboard } from './dashboard/dashboard';
import { AddDevice } from './add-device/add-device';
import { AddSensors } from './add-sensors/add-sensors';
import { AlertsPage } from './dashboard/pages/alerts-page/alerts-page';
import { DevicesPage } from './dashboard/pages/devices-page/devices-page';
import { OverviewPage } from './dashboard/pages/overview-page/overview-page';
import { SettingsPage } from './dashboard/pages/settings-page/settings-page';
import { AuthGuard } from './auth-guard';

export const routes: Routes = [
  { path: '', pathMatch: 'full', redirectTo: 'login' },
  { path: 'login', component: Login },
  {
    path: 'dashboard',
    component: Dashboard,
    children: [
      { path: '', pathMatch: 'full', redirectTo: 'overview' },
      { path: 'overview', component: OverviewPage },
      { path: 'alerts', component: AlertsPage },
      { path: 'devices', component: DevicesPage },
      { path: 'settings', component: SettingsPage },
    ],
    canActivate: [AuthGuard],
  },
  { path: 'add-device', component: AddDevice ,canActivate: [AuthGuard],},
  { path: 'add-sensors', component: AddSensors ,canActivate: [AuthGuard],},
  { path: '**', redirectTo: 'login' },
];
