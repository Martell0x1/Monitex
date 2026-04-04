import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Observable, of } from 'rxjs';
import { catchError, map } from 'rxjs/operators';
import { DeviceDto } from '../DTOs/DeviceDTO';
import { AuthService } from './AuthService';

@Injectable({
  providedIn: 'root',
})
export class DeviceService {
  private apiBase = 'http://localhost:5020/api';

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
      `${this.apiBase}/devices/user/${userId}`,
      `${this.apiBase}/device/user/${userId}`,
      `${this.apiBase}/users/${userId}/devices`,
      `${this.apiBase}/devices/${userId}`,
    ];

    return this.tryEndpoints(endpoints, headers);
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
        id: device.id ?? device.deviceId ?? index + 1,
        name: device.name ?? device.deviceName ?? `Device ${index + 1}`,
        type: device.type ?? device.deviceType ?? 'Smart Device',
        location: device.location ?? device.room ?? 'Unknown location',
        ipAddress: device.ipAddress ?? device.ip ?? 'Unavailable',
        description:
          device.description ??
          device.details ??
          'Live connected smart-home device.',
        status: device.status ?? device.connectionStatus ?? 'Online',
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
