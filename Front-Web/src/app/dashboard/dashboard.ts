import { ChangeDetectorRef, Component, OnDestroy, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { HubConnection, HubConnectionBuilder } from '@microsoft/signalr';
import { NavigationEnd, Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { Subscription } from 'rxjs';
import { DeviceDto } from '../../DTOs/DeviceDTO';
import { DeviceService } from '../../services/DeviceService';

@Component({
  selector: 'app-dashboard',
  imports: [CommonModule, RouterLink, RouterLinkActive, RouterOutlet],
  templateUrl: './dashboard.html',
  styleUrl: './dashboard.css',
})
export class Dashboard implements OnInit, OnDestroy {
  readonly telemetrySeries = [42, 58, 47, 63, 55, 71, 68, 76, 62, 84, 73, 88];
  readonly zoneMetrics = [
    { label: 'North Wing', value: '98.2%', state: 'Stable' },
    { label: 'Lab Sensors', value: '12', state: 'Streaming' },
    { label: 'Alert Queue', value: '03', state: 'Needs review' },
  ];

  constructor(
    private cd: ChangeDetectorRef,
    private deviceService: DeviceService,
    private router: Router
  ) {}

  sideBarOn = true;
  devices: DeviceDto[] = [];
  selectedDevice: DeviceDto | null = null;
  liveMetrics: Record<string, string> = {
    label: 'Waiting for live data',
    value: '--',
    updatedAt: 'Not connected yet',
  };
  isLoadingDevices = true;
  deviceLoadError = '';
  connectionState = 'Connecting';
  pageEyebrow = 'Operations Overview';
  pageTitle = 'Sensor Monitoring Dashboard';
  private connection?: HubConnection;
  private routeSubscription?: Subscription;

  get activeDeviceCount(): number {
    return this.devices.length;
  }

  get offlineDeviceCount(): number {
    return this.devices.filter((device) => this.getDeviceStatusTone(device) === 'offline')
      .length;
  }

  get alertCount(): number {
    return this.devices.filter((device) => this.getDeviceStatusTone(device) === 'warning')
      .length;
  }

  get averageHealthScore(): string {
    if (!this.devices.length) {
      return '--';
    }

    const score = Math.max(
      72,
      Math.min(99, 100 - this.offlineDeviceCount * 14 - this.alertCount * 6)
    );

    return `${score}%`;
  }

  get connectionTone(): 'live' | 'offline' | 'pending' {
    if (this.connectionState === 'Live') {
      return 'live';
    }

    if (this.connectionState === 'Offline') {
      return 'offline';
    }

    return 'pending';
  }

  get selectedDeviceStatusTone(): 'live' | 'warning' | 'offline' {
    if (!this.selectedDevice) {
      return 'offline';
    }

    return this.getDeviceStatusTone(this.selectedDevice);
  }

  toggleSideBar() {
    this.sideBarOn = !this.sideBarOn;
  }

  selectDevice(device: DeviceDto): void {
    this.selectedDevice = device;
    this.liveMetrics = {
      ...this.liveMetrics,
      label: 'Waiting for live data',
      value: '--',
    };
  }

  private loadDevices(): void {
    this.isLoadingDevices = true;
    this.deviceLoadError = '';

    this.deviceService.getDevicesForCurrentUser().subscribe({
      next: (devices) => {
        this.devices = devices;
        this.selectedDevice = devices[0] ?? null;
        this.isLoadingDevices = false;
        this.cd.detectChanges();
      },
      error: () => {
        this.devices = [];
        this.selectedDevice = null;
        this.isLoadingDevices = false;
        this.deviceLoadError = 'Unable to load your devices right now.';
        this.cd.detectChanges();
      },
    });
  }

  goLive() {
    this.connection = new HubConnectionBuilder()
      .withUrl('http://localhost:5020/sensorHub')
      .build();

    this.connection.on('ReceiveSensorReading', (data: unknown) => {
      const reading = this.normalizeReading(data);
      const selectedDeviceId = this.selectedDevice?.id?.toString();

      if (
        selectedDeviceId &&
        reading.deviceId &&
        reading.deviceId !== selectedDeviceId
      ) {
        return;
      }

      this.liveMetrics = {
        label: reading.label,
        value: reading.value,
        updatedAt: new Date().toLocaleTimeString(),
      };
      this.cd.detectChanges();
    });

    this.connection
      .start()
      .then(() => {
        this.connectionState = 'Live';
        this.cd.detectChanges();
      })
      .catch(() => {
        this.connectionState = 'Offline';
        this.cd.detectChanges();
      });
  }

  ngOnInit(): void {
    this.syncPageMeta(this.router.url);
    this.routeSubscription = this.router.events.subscribe((event) => {
      if (event instanceof NavigationEnd) {
        this.syncPageMeta(event.urlAfterRedirects);
      }
    });

    this.loadDevices();
    this.goLive();
  }

  ngOnDestroy(): void {
    this.routeSubscription?.unsubscribe();
    void this.connection?.stop();
  }

  getDeviceStatusTone(device: DeviceDto): 'live' | 'warning' | 'offline' {
    const status = device.status?.toLowerCase() ?? 'online';

    if (
      status.includes('offline') ||
      status.includes('disconnected') ||
      status.includes('inactive')
    ) {
      return 'offline';
    }

    if (
      status.includes('warning') ||
      status.includes('alert') ||
      status.includes('maintenance')
    ) {
      return 'warning';
    }

    return 'live';
  }

  getTelemetryHeight(value: number): string {
    return `${Math.max(22, Math.min(100, value))}%`;
  }

  private normalizeReading(data: any): {
    deviceId: string | null;
    label: string;
    value: string;
  } {
    if (data && typeof data === 'object') {
      const rawValue =
        data.value ??
        data.reading ??
        data.sensorValue ??
        data.data ??
        data.message ??
        '--';

      return {
        deviceId:
          data.deviceId?.toString() ??
          data.id?.toString() ??
          data.sensorId?.toString() ??
          null,
        label: data.label ?? data.sensorType ?? data.type ?? 'Live reading',
        value: String(rawValue),
      };
    }

    return {
      deviceId: null,
      label: 'Live reading',
      value: String(data ?? '--'),
    };
  }

  private syncPageMeta(url: string): void {
    if (url.includes('/dashboard/alerts')) {
      this.pageEyebrow = 'Alert Center';
      this.pageTitle = 'Operational Alerts';
      return;
    }

    if (url.includes('/dashboard/devices')) {
      this.pageEyebrow = 'Device Directory';
      this.pageTitle = 'Connected Devices';
      return;
    }

    if (url.includes('/dashboard/settings')) {
      this.pageEyebrow = 'Control Settings';
      this.pageTitle = 'System Preferences';
      return;
    }

    this.pageEyebrow = 'Operations Overview';
    this.pageTitle = 'Sensor Monitoring Dashboard';
  }
}
