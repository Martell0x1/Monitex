import { CommonModule } from '@angular/common';
import { Component } from '@angular/core';
import { NotificationService } from '../../services/NotificationService';

@Component({
  selector: 'app-notification',
  imports: [CommonModule],
  templateUrl: './notification.html',
  styleUrl: './notification.css'
})
export class Notification {
  constructor(
    protected notificationService: NotificationService
  ) {}

  clearNotification(): void {
    this.notificationService.clear();
  }
}
