import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { SensorDto } from '../DTOs/SensorDTO';



@Injectable({
  providedIn: 'root',
})
export class SensorService {
  private api = 'http://localhost:8080/api/sensors/create'; // change to your backend URL

  constructor(private http: HttpClient) {}

  registerSensors(sensors: SensorDto[]): Observable<any> {
    return this.http.post(`${this.api}/bulk`, sensors);
  }
}
