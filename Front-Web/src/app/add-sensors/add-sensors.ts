import { CommonModule } from '@angular/common';
import { Component } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { SensorService } from '../../services/SensorService';

type SensorForm = {
  name: string;
  type: string;
  location: string;
  ipAddress: string;
  description: string;
};

@Component({
  selector: 'app-add-sensors',
  imports: [CommonModule, FormsModule],
  templateUrl: './add-sensors.html',
  styleUrl: './add-sensors.css',
})
export class AddSensors {
  sensors: SensorForm[] = [this.createSensor()];

  constructor(private sensorService:SensorService){}

  private createSensor(): SensorForm {
    return {
      name: '',
      type: '',
      location: '',
      ipAddress: '',
      description: '',
    };
  }

  addSensor(): void {
    this.sensors.push(this.createSensor());
  }

  removeSensor(index: number): void {
    if (this.sensors.length === 1) {
      return;
    }

    this.sensors.splice(index, 1);
  }

  registerSensors(): void {
    const validSensors = this.sensors.filter((sensor) => sensor.name.trim().length > 0);

    if(!validSensors.length) return;

    this.sensorService.registerSensors(validSensors).subscribe({
      next:()=>console.log('Sensors Registerd !'),
      error:(err)=> console.log("Failed",err)
    });

    console.log('Sensors to register:', validSensors);
  }
}
