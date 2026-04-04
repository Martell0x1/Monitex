import { Component, OnInit } from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';
import { FormsModule } from '@angular/forms';
import { Divider } from '../divider/divider';
import { AuthService } from '../../services/AuthService';
import LoginDto from '../../DTOs/LoginDTO';
import { Notification } from '../notification/notification';
import { NotificationService } from '../../services/NotificationService';

@Component({
  selector: 'app-login',
  imports: [Divider, FormsModule, Notification],
  templateUrl: './login.html',
  styleUrl: './login.css',
})
export class Login implements OnInit {
  constructor(
    private authService: AuthService,
    private notificationService: NotificationService
  ) {}

  email = '';
  password = '';

  ngOnInit(): void {
    this.authService.redirectIfLoggedIn();
  }

  login(): void {
    const loginDto: LoginDto = {
      email: this.email.trim(),
      password: this.password
    };

    if (!loginDto.email || !loginDto.password) {
      this.notify('Please enter both email and password.');
      return;
    }

    this.authService.login(loginDto).subscribe({
      next: (response) => {
        console.log("Login data",response);
        this.authService.handleLoginSuccess(response);
      },
      error: (error: HttpErrorResponse) => {
        this.notify(this.getErrorMessage(error));
      }
    });
  }

  private notify(message: string): void {
    this.notificationService.showError(message);
  }

  private getErrorMessage(error: HttpErrorResponse): string {
    const apiMessage =
      (typeof error.error === 'string' && error.error) ||
      error.error?.message ||
      error.error?.error;

    if (typeof apiMessage === 'string' && apiMessage.trim()) {
      return apiMessage;
    }

    return 'Something went wrong while logging in. Please try again.';
  }
}
