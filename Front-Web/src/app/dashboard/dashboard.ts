import { ChangeDetectorRef, Component, OnDestroy, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { HubConnection, HubConnectionBuilder } from '@microsoft/signalr';
import { DeviceDto } from '../../DTOs/DeviceDTO';
import { DeviceService } from '../../services/DeviceService';

@Component({
  selector: 'app-dashboard',
  imports: [CommonModule],
  templateUrl: './dashboard.html',
  styleUrl: './dashboard.css',
})
export class Dashboard implements OnInit, OnDestroy {
  constructor(
    private cd: ChangeDetectorRef,
    private deviceService: DeviceService
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
  private connection?: HubConnection;

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

    this.connection.on('ReceiveSensorReading', (data) => {
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
    this.loadDevices();
    this.goLive();
  }

  ngOnDestroy(): void {
    void this.connection?.stop();
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
}
