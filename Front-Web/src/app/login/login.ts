import { ChangeDetectorRef, Component, OnInit } from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { Divider } from '../divider/divider';
import { AuthService } from '../../services/AuthService';
import LoginDto from '../../DTOs/LoginDTO';
import { Notification } from '../notification/notification';
import { NotificationService } from '../../services/NotificationService';

@Component({
  selector: 'app-login',
  imports: [Divider, FormsModule, Notification, RouterLink],
  templateUrl: './login.html',
  styleUrl: './login.css',
})
export class Login implements OnInit {
  constructor(
    private authService: AuthService,
    private notificationService: NotificationService,
    private router: Router,
    private cd: ChangeDetectorRef
  ) {}

  email = '';
  password = '';
  submitting = false;

  ngOnInit(): void {
    this.authService.redirectIfLoggedIn();
  }

  goToRegister(event: Event): void {
    event.preventDefault();
    event.stopPropagation();
    void this.router.navigateByUrl('/register');
  }

  login(): void {
    if (this.submitting) {
      return;
    }

    const loginDto: LoginDto = {
      email: this.email.trim(),
      password: this.password
    };

    if (!loginDto.email || !loginDto.password) {
      this.stopSubmitting();
      this.notify('Please enter both email and password.');
      return;
    }

    this.submitting = true;
    this.cd.detectChanges();

    this.authService.login(loginDto).subscribe({
      next: (response) => {
        try {
          this.authService.handleLoginSuccess(response);
        } catch {
          this.stopSubmitting();
          this.notify('Login succeeded but navigation failed. Please try again.');
        }
      },
      error: (error: HttpErrorResponse) => {
        this.stopSubmitting();
        this.notify(this.getErrorMessage(error));
      }
    });
  }

  private stopSubmitting(): void {
    this.submitting = false;
    this.cd.detectChanges();
  }

  private notify(message: string): void {
    this.notificationService.showError(message);
  }

  private getErrorMessage(error: HttpErrorResponse): string {
    if (error.status === 401) {
      return 'Invalid email or password.';
    }

    const apiMessage =
      (typeof error.error === 'string' && error.error) ||
      error.error?.message ||
      error.error?.Message ||
      error.error?.error;

    if (typeof apiMessage === 'string' && apiMessage.trim()) {
      return apiMessage;
    }

    return 'Something went wrong while logging in. Please try again.';
  }
}
