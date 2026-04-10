import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Observable } from 'rxjs';
import { SensorDto } from '../DTOs/SensorDTO';
import { SensorSummaryDto } from '../DTOs/SensorSummaryDTO';
import { AuthService } from './AuthService';

@Injectable({
  providedIn: 'root',
})
export class SensorService {
  private api = 'http://localhost:5020/api/sensors';

  constructor(
    private http: HttpClient,
    private authService: AuthService
  ) {}

  registerSensors(sensors: SensorDto[]): Observable<any> {
    return this.http.post(`${this.api}/bulk`, sensors, {
      headers: this.buildHeaders(),
    });
  }

  getSensorsByDevice(deviceId: number | string): Observable<SensorSummaryDto[]> {
    return this.http.get<SensorSummaryDto[]>(
      `${this.api}/device/${deviceId}`,
      { headers: this.buildHeaders() }
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
}
