import { ChangeDetectorRef, Component, OnDestroy, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { HubConnection, HubConnectionBuilder, LogLevel } from '@microsoft/signalr';
import { NavigationEnd, Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { Subscription } from 'rxjs';
import { DeviceDto } from '../../DTOs/DeviceDTO';
import { SensorSummaryDto } from '../../DTOs/SensorSummaryDTO';
import { DeviceService } from '../../services/DeviceService';
import { AuthService } from '../../services/AuthService';
import { SensorService } from '../../services/SensorService';
import { NotificationService } from '../../services/NotificationService';

type LiveSensorCard = {
  sensorKey: string;
  label: string;
  value: string;
  updatedAt: string;
  updatedAtMs: number;
  history: number[];
  accentColor: string;
  type: string;
  location: string;
  description: string;
  hasLiveValue: boolean;
};

type DeviceHealthSnapshot = {
  deviceId: string;
  deviceName: string;
  ipAddress: string | null;
  score: number;
  state: 'Healthy' | 'Warning' | 'Critical';
  reasons: string[];
  updatedAt: string;
  updatedAtMs: number;
  wifiRssi: number | null;
  freeHeapBytes: number | null;
  minFreeHeapBytes: number | null;
  uptimeSeconds: number | null;
};

type AnomalyNotification = {
  deviceId: string;
  deviceName: string;
  sensorId: string | null;
  sensorName: string | null;
  sensorType: string | null;
  severity: 'info' | 'warning' | 'critical';
  title: string;
  message: string;
  value: number | null;
  threshold: number | null;
  ipAddress: string | null;
  timestamp: string;
  updatedAt: string;
  updatedAtMs: number;
};

type DashboardRealtimeSnapshot = {
  connectionState: string;
  liveMetrics: Record<string, string>;
  liveSensorReadings: LiveSensorCard[];
  selectedDeviceSensors: SensorSummaryDto[];
  selectedDevice: DeviceDto | null;
  devices: DeviceDto[];
  deviceHealthByDevice: Array<[string, DeviceHealthSnapshot]>;
  sensorReadingsByDevice: Array<
    [
      string,
      Array<
        [
          string,
          {
            sensorKey: string;
            label: string;
            value: string;
            updatedAt: string;
            updatedAtMs: number;
            history: number[];
          }
        ]
      >
    ]
  >;
};

@Component({
  selector: 'app-dashboard',
  imports: [CommonModule, RouterLink, RouterLinkActive, RouterOutlet],
  templateUrl: './dashboard.html',
  styleUrl: './dashboard.css',
})
export class Dashboard implements OnInit, OnDestroy {
  readonly zoneMetrics = [
    { label: 'North Wing', value: '98.2%', state: 'Stable' },
    { label: 'Lab Sensors', value: '12', state: 'Streaming' },
    { label: 'Alert Queue', value: '03', state: 'Needs review' },
  ];

  constructor(
    private cd: ChangeDetectorRef,
    private deviceService: DeviceService,
    private router: Router,
    private authService: AuthService,
    private sensorService: SensorService,
    private notificationService: NotificationService
  ) {}

  sideBarOn = true;
  devices: DeviceDto[] = [];
  selectedDevice: DeviceDto | null = null;
  liveMetrics: Record<string, string> = {
    label: 'Waiting for live data',
    value: '--',
    updatedAt: 'Not connected yet',
  };
  liveSensorReadings: LiveSensorCard[] = [];
  selectedDeviceSensors: SensorSummaryDto[] = [];
  isLoadingDevices = true;
  isLoadingSensors = false;
  deviceLoadError = '';
  sensorLoadError = '';
  connectionState = 'Connecting';
  pageEyebrow = 'Operations Overview';
  pageTitle = 'Sensor Monitoring Dashboard';
  private connection?: HubConnection;
  private routeSubscription?: Subscription;
  private hasStartedRealtime = false;
  private previousRealtimeSnapshot: DashboardRealtimeSnapshot | null = null;
  private liveFeedTimeoutId: ReturnType<typeof setTimeout> | null = null;
  private hasReceivedRealtimePayload = false;
  private readonly liveFeedTimeoutMs = 10000;
  private deviceHealthByDevice = new Map<string, DeviceHealthSnapshot>();
  anomalyNotifications: AnomalyNotification[] = [];
  private sensorReadingsByDevice = new Map<
    string,
    Map<string, { sensorKey: string; label: string; value: string; updatedAt: string; updatedAtMs: number; history: number[] }>
  >();
  private readonly sensorPalette = ['#f4a261', '#7dd3fc', '#34d399', '#f472b6', '#facc15', '#a78bfa'];

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

  get activeAnomalyCount(): number {
    return this.anomalyNotifications.length;
  }

  get openAlertCount(): number {
    return this.activeAnomalyCount + this.alertCount + this.offlineDeviceCount;
  }

  get averageHealthScore(): string {
    if (this.deviceHealthByDevice.size) {
      const scores = Array.from(this.deviceHealthByDevice.values()).map((health) => health.score);
      const averageScore = Math.round(
        scores.reduce((total, current) => total + current, 0) / scores.length
      );

      return `${averageScore}%`;
    }

    if (!this.devices.length) {
      return '--';
    }

    const score = Math.max(
      72,
      Math.min(99, 100 - this.offlineDeviceCount * 14 - this.alertCount * 6)
    );

    return `${score}%`;
  }

  get averageHealthTone(): 'healthy' | 'warning' | 'critical' {
    const selectedHealth = this.selectedDeviceHealth;
    const fallbackScore = Number.parseInt(this.averageHealthScore, 10);
    const score = selectedHealth?.score ?? fallbackScore;

    if (!Number.isFinite(score)) {
      return 'warning';
    }

    if (score >= 85) {
      return 'healthy';
    }

    if (score >= 60) {
      return 'warning';
    }

    return 'critical';
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
    this.loadSensorsForSelectedDevice();
  }

  private loadDevices(): void {
    this.isLoadingDevices = true;
    this.deviceLoadError = '';

    this.deviceService.getDevicesForCurrentUser().subscribe({
      next: (devices) => {
        this.devices = devices;
        this.selectedDevice = devices[0] ?? null;
        this.isLoadingDevices = false;
        this.loadSensorsForSelectedDevice(true);
        this.cd.detectChanges();
      },
      error: () => {
        this.devices = [];
        this.selectedDevice = null;
        this.isLoadingDevices = false;
        this.deviceLoadError = 'Unable to load your devices right now.';
        this.startRealtimeIfNeeded();
        this.cd.detectChanges();
      },
    });
  }

  private loadSensorsForSelectedDevice(startRealtimeAfterLoad = false): void {
    const selectedDevice = this.selectedDevice;

    if (!selectedDevice) {
      this.selectedDeviceSensors = [];
      this.sensorLoadError = '';
      this.isLoadingSensors = false;
      this.syncSelectedDeviceFeed();
      this.startRealtimeIfNeeded(startRealtimeAfterLoad);
      this.cd.detectChanges();
      return;
    }

    this.isLoadingSensors = true;
    this.sensorLoadError = '';

    this.sensorService.getSensorsByDevice(selectedDevice.id).subscribe({
      next: (sensors) => {
        this.selectedDeviceSensors = sensors;
        this.isLoadingSensors = false;
        this.syncSelectedDeviceFeed();
        this.startRealtimeIfNeeded(startRealtimeAfterLoad);
        this.cd.detectChanges();
      },
      error: () => {
        this.selectedDeviceSensors = [];
        this.isLoadingSensors = false;
        this.sensorLoadError = 'Unable to load sensors for the selected device.';
        this.syncSelectedDeviceFeed();
        this.startRealtimeIfNeeded(startRealtimeAfterLoad);
        this.cd.detectChanges();
      },
    });
  }

  private startRealtimeIfNeeded(shouldStart = true): void {
    if (!shouldStart || this.hasStartedRealtime) {
      return;
    }

    this.hasStartedRealtime = true;
    this.captureRealtimeSnapshot();
    this.goLive();
  }

  goLive() {
    const token = this.authService.getToken();

    if (!token) {
      this.restoreRealtimeSnapshot('Sensor reading stopped because authentication is no longer available.');
      this.connectionState = 'Offline';
      this.liveMetrics = { label: 'Authentication required', value: '--', updatedAt: 'No access token available' };
      this.cd.detectChanges();
      return;
    }

    this.connection = new HubConnectionBuilder()
      .withUrl('http://monitex.local:5020/sensorHub',{
        accessTokenFactory: () => token
      })
      .configureLogging(LogLevel.Information)
      .withAutomaticReconnect()
      .build();

    for (const eventName of this.realtimeEventNames) {
      this.connection.on(eventName, (data: unknown) => {
        console.log(`[SignalR] ${eventName}`, data);
        this.processIncomingSensorPayload(data);
      });
    }

    for (const eventName of this.healthEventNames) {
      this.connection.on(eventName, (data: unknown) => {
        console.log(`[SignalR] ${eventName}`, data);
        this.processIncomingHealthPayload(data);
      });
    }

    for (const eventName of this.anomalyEventNames) {
      this.connection.on(eventName, (data: unknown) => {
        console.log(`[SignalR] ${eventName}`, data);
        this.processIncomingAnomalyPayload(data);
      });
    }

    this.connection.onreconnecting(() => {
      this.connectionState = 'Connecting';
      this.cd.detectChanges();
    });

    this.connection.onreconnected(() => {
      this.connectionState = 'Live';
      this.cd.detectChanges();
    });

    this.connection.onclose(() => {
      this.restoreRealtimeSnapshot('Sensor reading stopped. The dashboard was restored to its previous state.');
    });

    this.connection
      .start()
      .then(() => {
        this.connectionState = 'Live';
        this.cd.detectChanges();
      })
      .catch(() => {
        this.restoreRealtimeSnapshot('Unable to start sensor reading. The dashboard was restored to its previous state.');
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
  }

  ngOnDestroy(): void {
    this.routeSubscription?.unsubscribe();
    this.clearLiveFeedTimeout();
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

  get selectedDeviceSensorCount(): number {
    return this.selectedDeviceSensors.length || this.liveSensorReadings.length;
  }

  get sensorCards(): LiveSensorCard[] {
    return this.liveSensorReadings;
  }

  get selectedDeviceHealth(): DeviceHealthSnapshot | null {
    const selectedKeys = this.getSelectedDeviceKeys();

    for (const deviceKey of selectedKeys) {
      const health = this.deviceHealthByDevice.get(deviceKey);
      if (health) {
        return health;
      }
    }

    return null;
  }

  get selectedDeviceHealthReasons(): string[] {
    return this.selectedDeviceHealth?.reasons ?? ['Waiting for a device heartbeat.'];
  }

  private get realtimeEventNames(): string[] {
    return [
      'ReceiveSensorData',
      'receiveSensorData',
      'SensorDataReceived',
      'sensorDataReceived',
      'ReceiveTelemetry',
      'receiveTelemetry',
      'TelemetryReceived',
      'telemetryReceived',
      'ReceiveDeviceTelemetry',
      'receiveDeviceTelemetry',
    ];
  }

  private get healthEventNames(): string[] {
    return [
      'ReceiveDeviceHealth',
      'receiveDeviceHealth',
      'DeviceHealthReceived',
      'deviceHealthReceived',
    ];
  }

  private get anomalyEventNames(): string[] {
    return [
      'ReceiveAnomalyNotification',
      'receiveAnomalyNotification',
      'AnomalyNotificationReceived',
      'anomalyNotificationReceived',
    ];
  }

  private processIncomingSensorPayload(data: unknown): void {
    const readings = this.extractReadings(data);

    if (!readings.length) {
      console.warn('[SignalR] payload received but no readings could be extracted', data);
      return;
    }

    this.markRealtimeFeedAsActive();

    for (const reading of readings) {
      this.storeReading(reading);
      this.syncSelectedDeviceIp(reading);
    }

    this.syncSelectedDeviceFeed();
    this.cd.detectChanges();
  }

  private processIncomingHealthPayload(data: unknown): void {
    const healthSnapshot = this.normalizeHealthPayload(data);

    if (!healthSnapshot) {
      console.warn('[SignalR] health payload received but could not be normalized', data);
      return;
    }

    this.storeDeviceHealth(healthSnapshot);
    this.syncSelectedDeviceIp({
      deviceId: healthSnapshot.deviceId,
      ipAddress: healthSnapshot.ipAddress,
    });
    this.cd.detectChanges();
  }

  private processIncomingAnomalyPayload(data: unknown): void {
    const anomaly = this.normalizeAnomalyPayload(data);

    if (!anomaly) {
      console.warn('[SignalR] anomaly payload received but could not be normalized', data);
      return;
    }

    const notificationMessage = anomaly.title
      ? `${anomaly.title}: ${anomaly.message}`
      : anomaly.message;

    if (anomaly.severity === 'critical') {
      this.notificationService.showError(notificationMessage, 6000);
    } else if (anomaly.severity === 'info') {
      this.notificationService.showInfo(notificationMessage, 5000);
    } else {
      this.notificationService.showInfo(notificationMessage, 6000);
    }

    this.storeAnomalyNotification(anomaly);

    this.syncSelectedDeviceIp({
      deviceId: anomaly.deviceId,
      ipAddress: anomaly.ipAddress,
    });

    this.cd.detectChanges();
  }

  selectDeviceByLookup(deviceLookup: string | null): void {
    if (!deviceLookup) {
      return;
    }

    const normalizedLookup = this.normalizeLookupValue(deviceLookup);
    const matchedDevice = this.devices.find((device) => {
      return this.getDeviceLookupKeys(device).includes(normalizedLookup);
    });

    if (!matchedDevice) {
      return;
    }

    this.selectDevice(matchedDevice);
  }

  private extractReadings(data: unknown): Array<{
    deviceId: string | null;
    sensorKey: string;
    label: string;
    value: string;
    updatedAt: string;
    updatedAtMs: number;
    ipAddress: string | null;
  }> {
    if (Array.isArray(data)) {
      return data.flatMap((entry) => this.extractReadings(entry));
    }

    if (data && typeof data === 'object') {
      const payload = data as Record<string, unknown>;
      const nestedCollections = [
        payload['sensors'],
        payload['sensorReadings'],
        payload['readings'],
        payload['data'],
        payload['items'],
        payload['values'],
      ];

      for (const nestedCollection of nestedCollections) {
        if (Array.isArray(nestedCollection)) {
          const parentDeviceId = this.extractDeviceId(payload);

          return nestedCollection.flatMap((entry) => {
            const normalized = this.normalizeReading(entry, parentDeviceId);
            return normalized ? [normalized] : [];
          });
        }
      }
    }

    const normalized = this.normalizeReading(data);
    return normalized ? [normalized] : [];
  }

  private storeReading(reading: {
    deviceId: string | null;
    sensorKey: string;
    label: string;
    value: string;
    updatedAt: string;
    updatedAtMs: number;
    ipAddress: string | null;
  }): void {
    const deviceBucketKey = reading.deviceId ?? this.getSelectedDeviceKeys()[0] ?? '__unscoped__';
    const deviceBucket =
      this.sensorReadingsByDevice.get(deviceBucketKey) ??
      new Map<string, { sensorKey: string; label: string; value: string; updatedAt: string; updatedAtMs: number; history: number[] }>();

    const existingReading = deviceBucket.get(reading.sensorKey);
    const numericValue = this.parseNumericValue(reading.value);
    const history = existingReading?.history ? [...existingReading.history] : [];

    if (numericValue !== null) {
      history.push(numericValue);
    }

    const trimmedHistory = history.slice(-12);

    deviceBucket.set(reading.sensorKey, {
      sensorKey: reading.sensorKey,
      label: reading.label,
      value: reading.value,
      updatedAt: reading.updatedAt,
      updatedAtMs: reading.updatedAtMs,
      history: trimmedHistory,
    });

    this.sensorReadingsByDevice.set(deviceBucketKey, deviceBucket);
  }

  private syncSelectedDeviceFeed(): void {
    const deviceKeys = this.getSelectedDeviceKeys();
    const mergedReadings = new Map<
      string,
      { sensorKey: string; label: string; value: string; updatedAt: string; updatedAtMs: number; history: number[] }
    >();

    for (const deviceKey of deviceKeys) {
      const deviceReadings = this.sensorReadingsByDevice.get(deviceKey);

      if (!deviceReadings) {
        continue;
      }

      for (const [sensorKey, reading] of deviceReadings.entries()) {
        mergedReadings.set(sensorKey, reading);
      }
    }

    const cards: LiveSensorCard[] = [];

    for (const [index, sensor] of this.selectedDeviceSensors.entries()) {
      const reading = this.findReadingForSensor(sensor, mergedReadings);

      cards.push({
        sensorKey: this.getSensorLookupKeys(sensor)[0] ?? `sensor-${sensor.sensorId}`,
        label: sensor.name || sensor.type || `Sensor ${index + 1}`,
        value: reading?.value ?? '--',
        updatedAt: reading?.updatedAt ?? 'Awaiting live reading',
        updatedAtMs: reading?.updatedAtMs ?? 0,
        history: reading?.history?.length ? reading.history : [0, 0, 0, 0, 0, 0],
        accentColor: this.getSensorAccent(index),
        type: sensor.type || 'Sensor',
        location: sensor.location || this.selectedDevice?.location || '--',
        description: sensor.description || 'Registered sensor on the selected device.',
        hasLiveValue: Boolean(reading),
      });
    }

    for (const [index, reading] of Array.from(mergedReadings.values()).entries()) {
      if (cards.some((card) => card.sensorKey === reading.sensorKey || card.label === reading.label)) {
        continue;
      }

      cards.push({
        sensorKey: reading.sensorKey,
        label: reading.label,
        value: reading.value,
        updatedAt: reading.updatedAt,
        updatedAtMs: reading.updatedAtMs,
        history: reading.history.length ? reading.history : [0, 0, 0, 0, 0, 0],
        accentColor: this.getSensorAccent(this.selectedDeviceSensors.length + index),
        type: 'Live sensor',
        location: this.selectedDevice?.location || '--',
        description: 'Detected from the realtime stream.',
        hasLiveValue: true,
      });
    }

    this.liveSensorReadings = cards.sort((left, right) => right.updatedAtMs - left.updatedAtMs);

    const [latestReading] = this.liveSensorReadings;

    if (!latestReading) {
      this.liveMetrics = {
        label: this.selectedDeviceSensors.length ? `${this.selectedDeviceSensors.length} sensors registered` : 'No sensors registered',
        value: this.selectedDeviceSensors.length ? 'Live readings will appear here' : '--',
        updatedAt: this.isLoadingSensors ? 'Loading sensors' : 'No readings yet',
      };
      return;
    }

    const previewValues = this.sensorCards
      .slice(0, 3)
      .map((reading) => `${reading.label}: ${reading.value}`)
      .join(' | ');

    this.liveMetrics = {
      label: this.sensorCards.length > 1 ? `${this.sensorCards.length} sensors in view` : latestReading.label,
      value: previewValues,
      updatedAt: latestReading.updatedAt,
    };
  }

  getSensorChartPointHeight(point: number, history: number[]): string {
    const maxValue = Math.max(...history, 1);
    const minValue = Math.min(...history, 0);
    const normalized = maxValue === minValue ? 0.65 : (point - minValue) / (maxValue - minValue);

    return `${Math.max(18, Math.round(normalized * 100))}%`;
  }

  private findReadingForSensor(
    sensor: SensorSummaryDto,
    readings: Map<string, { sensorKey: string; label: string; value: string; updatedAt: string; updatedAtMs: number; history: number[] }>
  ) {
    for (const lookupKey of this.getSensorLookupKeys(sensor)) {
      const reading = readings.get(lookupKey);
      if (reading) {
        return reading;
      }
    }

    return Array.from(readings.values()).find((reading) =>
      this.normalizeLookupValue(reading.label) === this.normalizeLookupValue(sensor.name) ||
      this.normalizeLookupValue(reading.label) === this.normalizeLookupValue(sensor.type)
    );
  }

  private getSensorLookupKeys(sensor: SensorSummaryDto): string[] {
    const keys = new Set<string>();
    const addKey = (value: unknown) => {
      const normalized = this.normalizeLookupValue(value);
      if (normalized) {
        keys.add(normalized);
      }
    };

    addKey(sensor.sensorId);
    addKey(sensor.name);
    addKey(sensor.type);
    addKey(`${sensor.name}-${sensor.type}`);

    return Array.from(keys);
  }

  private getSensorAccent(index: number): string {
    return this.sensorPalette[index % this.sensorPalette.length];
  }

  private parseNumericValue(value: string): number | null {
    const parsedValue = Number.parseFloat(value);
    return Number.isFinite(parsedValue) ? parsedValue : null;
  }

  private syncSelectedDeviceIp(reading: { deviceId: string | null; ipAddress: string | null }): void {
    if (!this.selectedDevice || !reading.ipAddress) {
      return;
    }

    const selectedKeys = new Set(this.getSelectedDeviceKeys());
    const readingDeviceKey = this.normalizeLookupValue(reading.deviceId);

    if (readingDeviceKey && !selectedKeys.has(readingDeviceKey)) {
      return;
    }

    this.selectedDevice = {
      ...this.selectedDevice,
      ipAddress: reading.ipAddress,
    };

    this.devices = this.devices.map((device) =>
      device.id === this.selectedDevice?.id
        ? { ...device, ipAddress: reading.ipAddress ?? device.ipAddress }
        : device
    );
  }

  private storeDeviceHealth(healthSnapshot: DeviceHealthSnapshot): void {
    const lookupKeys = [
      healthSnapshot.deviceId,
      healthSnapshot.deviceName,
      healthSnapshot.ipAddress,
    ].map((value) => this.normalizeLookupValue(value));

    for (const key of lookupKeys) {
      if (!key) {
        continue;
      }

      this.deviceHealthByDevice.set(key, healthSnapshot);
    }
  }

  private storeAnomalyNotification(anomaly: AnomalyNotification): void {
    const dedupeKey = [
      anomaly.deviceId,
      anomaly.sensorId,
      anomaly.title,
      anomaly.message,
      anomaly.updatedAtMs,
    ].join('|');

    const nextFeed = [
      anomaly,
      ...this.anomalyNotifications.filter((entry) => {
        const entryKey = [
          entry.deviceId,
          entry.sensorId,
          entry.title,
          entry.message,
          entry.updatedAtMs,
        ].join('|');

        return entryKey !== dedupeKey;
      }),
    ];

    this.anomalyNotifications = nextFeed
      .sort((left, right) => right.updatedAtMs - left.updatedAtMs)
      .slice(0, 30);
  }

  private normalizeHealthPayload(data: unknown): DeviceHealthSnapshot | null {
    if (!data || typeof data !== 'object') {
      return null;
    }

    const payload = data as Record<string, unknown>;
    const deviceId =
      this.pickPayloadValue(payload, 'deviceId', 'DeviceId')?.toString() ?? '';
    const deviceName =
      this.pickPayloadValue(payload, 'deviceName', 'DeviceName')?.toString() ?? '';
    const state =
      this.pickPayloadValue(payload, 'state', 'State')?.toString() ?? 'Warning';
    const reasons = this.pickPayloadValue(payload, 'reasons', 'Reasons');
    const rawTimestamp =
      this.pickPayloadValue(payload, 'timestamp', 'Timestamp')?.toString() ?? new Date().toISOString();
    const parsedDate = new Date(rawTimestamp);
    const score = Number(this.pickPayloadValue(payload, 'score', 'Score'));

    if (!deviceId && !deviceName) {
      return null;
    }

    return {
      deviceId: this.normalizeLookupValue(deviceId || deviceName),
      deviceName,
      ipAddress: this.pickPayloadValue(payload, 'ipAddress', 'IpAddress')?.toString() ?? null,
      score: Number.isFinite(score) ? score : 0,
      state: this.normalizeHealthState(state),
      reasons: Array.isArray(reasons)
        ? reasons.map((reason) => String(reason))
        : ['Waiting for a health explanation from the backend.'],
      updatedAt: Number.isNaN(parsedDate.getTime())
        ? rawTimestamp
        : parsedDate.toLocaleTimeString(),
      updatedAtMs: Number.isNaN(parsedDate.getTime()) ? Date.now() : parsedDate.getTime(),
      wifiRssi: this.pickNumberPayloadValue(payload, 'wifiRssi', 'WifiRssi'),
      freeHeapBytes: this.pickNumberPayloadValue(payload, 'freeHeapBytes', 'FreeHeapBytes'),
      minFreeHeapBytes: this.pickNumberPayloadValue(payload, 'minFreeHeapBytes', 'MinFreeHeapBytes'),
      uptimeSeconds: this.pickNumberPayloadValue(payload, 'uptimeSeconds', 'UptimeSeconds'),
    };
  }

  private normalizeHealthState(value: string): 'Healthy' | 'Warning' | 'Critical' {
    const normalizedValue = value.trim().toLowerCase();

    if (normalizedValue === 'healthy') {
      return 'Healthy';
    }

    if (normalizedValue === 'critical') {
      return 'Critical';
    }

    return 'Warning';
  }

  private normalizeAnomalyPayload(data: unknown): AnomalyNotification | null {
    if (!data || typeof data !== 'object') {
      return null;
    }

    const payload = data as Record<string, unknown>;
    const deviceId =
      this.pickPayloadValue(payload, 'deviceId', 'DeviceId')?.toString() ?? '';
    const deviceName =
      this.pickPayloadValue(payload, 'deviceName', 'DeviceName')?.toString() ?? '';
    const severity = this.normalizeAnomalySeverity(
      this.pickPayloadValue(payload, 'severity', 'Severity')?.toString() ?? 'warning'
    );
    const title =
      this.pickPayloadValue(payload, 'title', 'Title')?.toString() ?? 'Anomaly detected';
    const message =
      this.pickPayloadValue(payload, 'message', 'Message')?.toString() ??
      'The backend reported an anomaly.';
    const rawTimestamp =
      this.pickPayloadValue(payload, 'timestamp', 'Timestamp')?.toString() ??
      new Date().toISOString();
    const parsedDate = new Date(rawTimestamp);

    if (!deviceId && !deviceName) {
      return null;
    }

    return {
      deviceId: this.normalizeLookupValue(deviceId || deviceName),
      deviceName,
      sensorId: this.pickPayloadValue(payload, 'sensorId', 'SensorId')?.toString() ?? null,
      sensorName: this.pickPayloadValue(payload, 'sensorName', 'SensorName')?.toString() ?? null,
      sensorType: this.pickPayloadValue(payload, 'sensorType', 'SensorType')?.toString() ?? null,
      severity,
      title,
      message,
      value: this.pickNumberPayloadValue(payload, 'value', 'Value'),
      threshold: this.pickNumberPayloadValue(payload, 'threshold', 'Threshold'),
      ipAddress: this.pickPayloadValue(payload, 'ipAddress', 'IpAddress')?.toString() ?? null,
      timestamp: rawTimestamp,
      updatedAt: Number.isNaN(parsedDate.getTime())
        ? rawTimestamp
        : parsedDate.toLocaleTimeString(),
      updatedAtMs: Number.isNaN(parsedDate.getTime()) ? Date.now() : parsedDate.getTime(),
    };
  }

  private normalizeAnomalySeverity(value: string): 'info' | 'warning' | 'critical' {
    const normalizedValue = value.trim().toLowerCase();

    if (normalizedValue === 'critical' || normalizedValue === 'error' || normalizedValue === 'high') {
      return 'critical';
    }

    if (normalizedValue === 'info' || normalizedValue === 'low') {
      return 'info';
    }

    return 'warning';
  }

  private pickNumberPayloadValue(payload: Record<string, unknown>, ...keys: string[]): number | null {
    const rawValue = this.pickPayloadValue(payload, ...keys);
    const numericValue = Number(rawValue);

    return Number.isFinite(numericValue) ? numericValue : null;
  }

  private normalizeLookupValue(value: unknown): string {
    return value?.toString().trim().toLowerCase() ?? '';
  }

  private getSelectedDeviceKeys(): string[] {
    if (!this.selectedDevice) {
      return [];
    }

    const keys = new Set<string>();
    const addKey = (value: unknown) => {
      const normalized = value?.toString().trim().toLowerCase();
      if (normalized) {
        keys.add(normalized);
      }
    };

    addKey(this.selectedDevice.id);
    addKey(this.selectedDevice.name);
    addKey(this.selectedDevice.ipAddress);

    return Array.from(keys);
  }

  private getDeviceLookupKeys(device: DeviceDto): string[] {
    const keys = new Set<string>();
    const addKey = (value: unknown) => {
      const normalized = value?.toString().trim().toLowerCase();
      if (normalized) {
        keys.add(normalized);
      }
    };

    addKey(device.id);
    addKey(device.name);
    addKey(device.ipAddress);

    return Array.from(keys);
  }

  private normalizeReading(
    data: unknown,
    fallbackDeviceId?: string | null
  ): {
    deviceId: string | null;
    sensorKey: string;
    label: string;
    value: string;
    updatedAt: string;
    updatedAtMs: number;
    ipAddress: string | null;
  } | null {
    if (data && typeof data === 'object') {
      const payload = data as Record<string, unknown>;
      const rawValue =
        this.pickPayloadValue(payload, 'value', 'Value') ??
        this.pickPayloadValue(payload, 'reading', 'Reading') ??
        this.pickPayloadValue(payload, 'readingValue', 'ReadingValue') ??
        this.pickPayloadValue(payload, 'data', 'Data');
      const label =
        this.pickPayloadValue(payload, 'sensorName', 'SensorName') ??
        this.pickPayloadValue(payload, 'sensorType', 'SensorType') ??
        this.pickPayloadValue(payload, 'label', 'Label') ??
        this.pickPayloadValue(payload, 'name', 'Name') ??
        'Live reading';
      const deviceId = this.extractDeviceId(payload) ?? fallbackDeviceId ?? null;
      const sensorKey =
        this.pickPayloadValue(payload, 'sensorId', 'SensorId')?.toString() ??
        this.pickPayloadValue(payload, 'id', 'Id')?.toString() ??
        this.pickPayloadValue(payload, 'sensorName', 'SensorName')?.toString() ??
        this.pickPayloadValue(payload, 'sensorType', 'SensorType')?.toString() ??
        this.pickPayloadValue(payload, 'label', 'Label')?.toString() ??
        label.toString();
      const { displayValue: updatedAt, timestampMs: updatedAtMs } = this.extractUpdatedAt(payload);

      return {
        deviceId: deviceId?.toString().trim().toLowerCase() ?? null,
        sensorKey: this.normalizeLookupValue(sensorKey),
        label: String(label),
        value: String(rawValue ?? '--'),
        updatedAt,
        updatedAtMs,
        ipAddress: this.pickPayloadValue(payload, 'ipAddress', 'IpAddress')?.toString() ?? null,
      };
    }

    return {
      deviceId: fallbackDeviceId ?? null,
      sensorKey: 'live-reading',
      label: 'Live reading',
      value: String(data ?? '--'),
      updatedAt: new Date().toLocaleTimeString(),
      updatedAtMs: Date.now(),
      ipAddress: null,
    };
  }

  private extractDeviceId(payload: Record<string, unknown>): string | null {
    const candidate =
      this.pickPayloadValue(payload, 'deviceId', 'DeviceId') ??
      this.pickPayloadValue(payload, 'deviceName', 'DeviceName') ??
      this.pickPayloadValue(payload, 'deviceKey', 'DeviceKey') ??
      this.pickPayloadValue(payload, 'device', 'Device') ??
      this.pickPayloadValue(payload, 'ipAddress', 'IpAddress');

    if (candidate == null) {
      return null;
    }

    return candidate.toString().trim().toLowerCase() || null;
  }

  private extractUpdatedAt(payload: Record<string, unknown>): {
    displayValue: string;
    timestampMs: number;
  } {
    const rawTimestamp =
      this.pickPayloadValue(payload, 'updatedAt', 'UpdatedAt') ??
      this.pickPayloadValue(payload, 'timestamp', 'Timestamp') ??
      this.pickPayloadValue(payload, 'createdAt', 'CreatedAt') ??
      this.pickPayloadValue(payload, 'time', 'Time');

    if (rawTimestamp == null) {
      return {
        displayValue: new Date().toLocaleTimeString(),
        timestampMs: Date.now(),
      };
    }

    const parsedDate = new Date(String(rawTimestamp));

    if (Number.isNaN(parsedDate.getTime())) {
      return {
        displayValue: String(rawTimestamp),
        timestampMs: Date.now(),
      };
    }

    return {
      displayValue: parsedDate.toLocaleTimeString(),
      timestampMs: parsedDate.getTime(),
    };
  }

  private pickPayloadValue(payload: Record<string, unknown>, ...keys: string[]): unknown {
    for (const key of keys) {
      if (payload[key] !== undefined && payload[key] !== null) {
        return payload[key];
      }
    }

    return null;
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

  private captureRealtimeSnapshot(): void {
    this.previousRealtimeSnapshot = {
      connectionState: this.connectionState,
      liveMetrics: { ...this.liveMetrics },
      liveSensorReadings: this.liveSensorReadings.map((reading) => ({
        ...reading,
        history: [...reading.history],
      })),
      selectedDeviceSensors: this.selectedDeviceSensors.map((sensor) => ({ ...sensor })),
      selectedDevice: this.selectedDevice ? { ...this.selectedDevice } : null,
      devices: this.devices.map((device) => ({ ...device })),
      deviceHealthByDevice: Array.from(this.deviceHealthByDevice.entries()).map(([key, health]) => [
        key,
        {
          ...health,
          reasons: [...health.reasons],
        },
      ]),
      sensorReadingsByDevice: Array.from(this.sensorReadingsByDevice.entries()).map(([deviceKey, readings]) => [
        deviceKey,
        Array.from(readings.entries()).map(([sensorKey, reading]) => [
          sensorKey,
          {
            ...reading,
            history: [...reading.history],
          },
        ]),
      ]),
    };
  }

  private restoreRealtimeSnapshot(message: string): void {
    this.hasReceivedRealtimePayload = false;
    this.clearLiveFeedTimeout();

    if (this.previousRealtimeSnapshot) {
      this.connectionState = this.previousRealtimeSnapshot.connectionState;
      this.liveMetrics = { ...this.previousRealtimeSnapshot.liveMetrics };
      this.liveSensorReadings = this.previousRealtimeSnapshot.liveSensorReadings.map((reading) => ({
        ...reading,
        history: [...reading.history],
      }));
      this.selectedDeviceSensors = this.previousRealtimeSnapshot.selectedDeviceSensors.map((sensor) => ({
        ...sensor,
      }));
      this.selectedDevice = this.previousRealtimeSnapshot.selectedDevice
        ? { ...this.previousRealtimeSnapshot.selectedDevice }
        : null;
      this.devices = this.previousRealtimeSnapshot.devices.map((device) => ({ ...device }));
      this.deviceHealthByDevice = new Map(
        this.previousRealtimeSnapshot.deviceHealthByDevice.map(([key, health]) => [
          key,
          {
            ...health,
            reasons: [...health.reasons],
          },
        ])
      );
      this.sensorReadingsByDevice = new Map(
        this.previousRealtimeSnapshot.sensorReadingsByDevice.map(([deviceKey, readings]) => [
          deviceKey,
          new Map(
            readings.map(([sensorKey, reading]) => [
              sensorKey,
              {
                ...reading,
                history: [...reading.history],
              },
            ])
          ),
        ])
      );
    } else {
      this.connectionState = 'Offline';
      this.deviceHealthByDevice = new Map();
      this.sensorReadingsByDevice = new Map();
      this.syncSelectedDeviceFeed();
    }

    this.notificationService.showError(message);
    this.cd.detectChanges();
  }

  private markRealtimeFeedAsActive(): void {
    this.hasReceivedRealtimePayload = true;
    this.clearLiveFeedTimeout();
    this.liveFeedTimeoutId = setTimeout(() => {
      if (!this.hasReceivedRealtimePayload) {
        return;
      }

      this.restoreRealtimeSnapshot(
        'Sensor readings stopped. The dashboard was restored to its previous state.'
      );
    }, this.liveFeedTimeoutMs);
  }

  private clearLiveFeedTimeout(): void {
    if (this.liveFeedTimeoutId) {
      clearTimeout(this.liveFeedTimeoutId);
      this.liveFeedTimeoutId = null;
    }
  }
}
