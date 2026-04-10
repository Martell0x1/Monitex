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

  get anomalyFeed() {
    return this.dashboard.anomalyNotifications;
  }

  get flaggedDevices(): DeviceDto[] {
    return this.dashboard.devices.filter((device) => {
      const tone = this.dashboard.getDeviceStatusTone(device);
      return tone === 'warning' || tone === 'offline';
    });
  }

  get hasCriticalAlerts(): boolean {
    return this.anomalyFeed.some((alert) => alert.severity === 'critical') || this.flaggedDevices.some(
      (device) => this.dashboard.getDeviceStatusTone(device) === 'offline'
    );
  }

  openAlertDevice(deviceLookup: string | null): void {
    this.dashboard.selectDeviceByLookup(deviceLookup);
  }

  getSeverityTone(severity: 'info' | 'warning' | 'critical'): 'live' | 'warning' | 'offline' {
    if (severity === 'critical') {
      return 'offline';
    }

    if (severity === 'warning') {
      return 'warning';
    }

    return 'live';
  }

  getSeverityLabel(severity: 'info' | 'warning' | 'critical'): string {
    if (severity === 'critical') {
      return 'Critical';
    }

    if (severity === 'warning') {
      return 'Warning';
    }

    return 'Info';
  }
}
