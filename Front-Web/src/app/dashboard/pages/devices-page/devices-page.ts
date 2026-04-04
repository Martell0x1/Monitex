import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Dashboard } from '../../dashboard';

@Component({
  selector: 'app-devices-page',
  imports: [CommonModule],
  templateUrl: './devices-page.html',
  styleUrl: './devices-page.css',
})
export class DevicesPage {
  readonly dashboard = inject(Dashboard);
}
