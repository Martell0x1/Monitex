import { ChangeDetectorRef, Component } from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';
import { FormsModule } from '@angular/forms';
import { Notification } from '../notification/notification';
import { AuthService } from '../../services/AuthService';
import { DeviceService } from '../../services/DeviceService';
import { NotificationService } from '../../services/NotificationService';

@Component({
  selector: 'app-add-device',
  imports: [FormsModule, Notification],
  templateUrl: './add-device.html',
  styleUrl: './add-device.css',
})
export class AddDevice {
  deviceName = '';
  deviceType = '';
  location = '';
  ipAddress = '';
  description = '';
  submitting = false;

  constructor(
    private deviceService: DeviceService,
    private authService: AuthService,
    private notificationService: NotificationService,
    private cd: ChangeDetectorRef
  ) {}

  registerDevice(): void {
    if (this.submitting) {
      return;
    }

    const name = this.deviceName.trim();
    if (!name) {
      this.stopSubmitting();
      this.notificationService.showError('Please enter a device name.');
      return;
    }

    this.submitting = true;
    this.cd.detectChanges();

    this.deviceService.createDevice(name).subscribe({
      next: () => {
        try {
          this.notificationService.showSuccess('Device registered successfully.');
          this.authService.continueOnboardingAfterDevice();
        } catch {
          this.stopSubmitting();
          this.notificationService.showError(
            'Device registered but navigation failed. Please continue to sensors.'
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
    if (error.status === 409) {
      return 'A device with this name already exists, or registration failed.';
    }

    if (error.status === 401) {
      return 'Your session expired. Please log in again.';
    }

    const apiMessage =
      (typeof error.error === 'string' && error.error) ||
      error.error?.message ||
      error.error?.Message;

    if (typeof apiMessage === 'string' && apiMessage.trim()) {
      return apiMessage;
    }

    return 'Unable to register the device. Please try again.';
  }
}
