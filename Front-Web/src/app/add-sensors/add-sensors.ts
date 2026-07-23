import { CommonModule } from '@angular/common';
import { ChangeDetectorRef, Component } from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';
import { FormsModule } from '@angular/forms';
import { Notification } from '../notification/notification';
import { SensorDto } from '../../DTOs/SensorDTO';
import { AuthService } from '../../services/AuthService';
import { NotificationService } from '../../services/NotificationService';
import { SensorService } from '../../services/SensorService';

type SensorForm = SensorDto;

@Component({
  selector: 'app-add-sensors',
  imports: [CommonModule, FormsModule, Notification],
  templateUrl: './add-sensors.html',
  styleUrl: './add-sensors.css',
})
export class AddSensors {
  sensors: SensorForm[] = [this.createSensor()];
  submitting = false;

  constructor(
    private sensorService: SensorService,
    private authService: AuthService,
    private notificationService: NotificationService,
    private cd: ChangeDetectorRef
  ) {}

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
    if (this.submitting) {
      return;
    }

    this.sensors.push(this.createSensor());
  }

  removeSensor(index: number): void {
    if (this.submitting || this.sensors.length === 1) {
      return;
    }

    this.sensors.splice(index, 1);
  }

  registerSensors(): void {
    if (this.submitting) {
      return;
    }

    const validSensors = this.sensors
      .map((sensor) => ({
        name: sensor.name.trim(),
        type: sensor.type.trim(),
        location: sensor.location.trim(),
        ipAddress: sensor.ipAddress.trim(),
        description: sensor.description.trim(),
      }))
      .filter((sensor) => sensor.name.length > 0);

    if (!validSensors.length) {
      this.stopSubmitting();
      this.notificationService.showError('Add at least one sensor with a name.');
      return;
    }

    const incomplete = validSensors.find(
      (sensor) => !sensor.type || !sensor.location
    );

    if (incomplete) {
      this.stopSubmitting();
      this.notificationService.showError(
        'Each sensor needs a name, type, and location.'
      );
      return;
    }

    this.submitting = true;
    this.cd.detectChanges();

    this.sensorService.registerSensors(validSensors).subscribe({
      next: () => {
        try {
          this.notificationService.showSuccess('Sensors registered successfully.');
          this.authService.continueOnboardingAfterSensors();
        } catch {
          this.stopSubmitting();
          this.notificationService.showError(
            'Sensors registered but navigation failed. Open the dashboard.'
          );
        }
      },
      error: (error: HttpErrorResponse) => {
        this.stopSubmitting();
        this.notificationService.showError(this.getErrorMessage(error));
      },
    });
  }

  private stopSubmitting(): void {
    this.submitting = false;
    this.cd.detectChanges();
  }

  private getErrorMessage(error: HttpErrorResponse): string {
    if (error.status === 401) {
      return 'Your session expired. Please log in again.';
    }

    if (error.status === 400) {
      return (
        error.error?.message ||
        error.error?.Message ||
        'Invalid sensor details. Check the form and try again.'
      );
    }

    const apiMessage =
      (typeof error.error === 'string' && error.error) ||
      error.error?.message ||
      error.error?.Message;

    if (typeof apiMessage === 'string' && apiMessage.trim()) {
      return apiMessage;
    }

    return 'Unable to register sensors. Please try again.';
  }
}
