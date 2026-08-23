import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Observable, of } from 'rxjs';
import { catchError, map } from 'rxjs/operators';
import { DeviceDto } from '../DTOs/DeviceDTO';
import { AuthService } from './AuthService';
import { apiUrl } from '../environments/api';

@Injectable({
  providedIn: 'root',
})
export class DeviceService {
  private apiBase = apiUrl('/api');

  constructor(
    private http: HttpClient,
    private authService: AuthService
  ) {}

  getDevicesForCurrentUser(): Observable<DeviceDto[]> {
    const userId = this.authService.getUserId();

    if (!userId) {
      return of([]);
    }

    const headers = this.buildHeaders();
    const endpoints = [
      `${this.apiBase}/device/user/${userId}`,
      `${this.apiBase}/devices/user/${userId}`,
      `${this.apiBase}/users/${userId}/devices`,
      `${this.apiBase}/devices/${userId}`,
    ];

    return this.tryEndpoints(endpoints, headers);
  }

  createDevice(deviceName: string): Observable<any> {
    return this.http.post(
      `${this.apiBase}/device/create`,
      { device_name: deviceName },
      { headers: this.buildHeaders() }
    );
  }

  private tryEndpoints(
    endpoints: string[],
    headers: HttpHeaders
  ): Observable<DeviceDto[]> {
    const [current, ...rest] = endpoints;

    if (!current) {
      return of([]);
    }

    return this.http.get<any>(current, { headers }).pipe(
      map((response) => this.normalizeDevices(response)),
      catchError(() => this.tryEndpoints(rest, headers))
    );
  }

  private buildHeaders(): HttpHeaders {
    const token = this.authService.getToken();

    if (!token) {
      return new HttpHeaders();
    }

    return new HttpHeaders({
      Authorization: `Bearer ${token}`,
    });
  }

  private normalizeDevices(response: any): DeviceDto[] {
    const rawDevices = this.extractDeviceCollection(response);

    return rawDevices
      .filter((device) => device && typeof device === 'object')
      .map((device, index) => ({
        id:
          device.device_id ??
          device.Device_id ??
          device.deviceId ??
          device.DeviceId ??
          device.id ??
          index + 1,
        name:
          device.device_name ??
          device.Device_name ??
          device.deviceName ??
          device.DeviceName ??
          device.name ??
          `Device ${index + 1}`,
        type: device.type ?? device.deviceType ?? device.DeviceType ?? 'Smart Device',
        location: device.location ?? device.room ?? 'Unknown location',
        ipAddress:
          device.ipAddress ??
          device.ip_address ??
          device.IpAddress ??
          device.ip ??
          'Unavailable',
        description:
          device.description ??
          device.details ??
          'Live connected smart-home device.',
        status:
          device.device_status ??
          device.Device_status ??
          device.status ??
          device.connectionStatus ??
          'Online',
      }));
  }

  private extractDeviceCollection(response: any): any[] {
    if (Array.isArray(response)) {
      return response;
    }

    if (!response || typeof response !== 'object') {
      return [];
    }

    const candidates = [
      response.devices,
      response.data,
      response.data?.devices,
      response.items,
      response.result,
      response.result?.devices,
      response.user?.devices,
    ];

    for (const candidate of candidates) {
      if (Array.isArray(candidate)) {
        return candidate;
      }
    }

    return [];
  }
}
