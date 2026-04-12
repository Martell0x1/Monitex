import { Routes } from '@angular/router';
import { DownloadComponent } from './download/download';
import { Home } from './home/home';

export const routes: Routes = [
  {
    path: '',
    component: Home,
    title: 'Monitex | Self-Hosted Sensor Monitoring Dashboard',
  },
  {
    path: 'download',
    component: DownloadComponent,
    title: 'Install Monitex',
  },
  {
    path: '**',
    redirectTo: '',
  },
];
