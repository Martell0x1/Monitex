import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { DeviceDto } from '../../../../DTOs/DeviceDTO';
import { Dashboard } from '../../dashboard';

@Component({
  selector: 'app-alerts-page',
  imports: [CommonModule],
  templateUrl: './alerts-page.html',
  styleUrl: './alerts-page.css',
})
export class AlertsPage {
  readonly dashboard = inject(Dashboard);

  get flaggedDevices(): DeviceDto[] {
    return this.dashboard.devices.filter((device) => {
      const tone = this.dashboard.getDeviceStatusTone(device);
      return tone === 'warning' || tone === 'offline';
    });
  }

  get hasCriticalAlerts(): boolean {
    return this.flaggedDevices.some(
      (device) => this.dashboard.getDeviceStatusTone(device) === 'offline'
    );
  }
}
