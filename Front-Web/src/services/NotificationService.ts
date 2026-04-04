import { Injectable, signal } from '@angular/core';

export type NotificationType = 'error' | 'success' | 'info';

export interface NotificationState {
  type: NotificationType;
  message: string;
}

@Injectable({ providedIn: 'root' })
export class NotificationService {
  readonly notification = signal<NotificationState | null>(null);
  private timerId: ReturnType<typeof setTimeout> | null = null;

  showError(message: string, durationMs = 4000): void {
    this.show('error', message, durationMs);
  }

  showSuccess(message: string, durationMs = 3000): void {
    this.show('success', message, durationMs);
  }

  showInfo(message: string, durationMs = 3000): void {
    this.show('info', message, durationMs);
  }

  clear(): void {
    this.notification.set(null);

    if (this.timerId) {
      clearTimeout(this.timerId);
      this.timerId = null;
    }
  }

  private show(type: NotificationType, message: string, durationMs: number): void {
    this.notification.set({ type, message });

    if (this.timerId) {
      clearTimeout(this.timerId);
    }

    this.timerId = setTimeout(() => {
      this.notification.set(null);
      this.timerId = null;
    }, durationMs);
  }
}
