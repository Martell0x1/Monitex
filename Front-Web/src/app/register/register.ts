import { ChangeDetectorRef, Component, OnInit } from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { Divider } from '../divider/divider';
import { Notification } from '../notification/notification';
import { AuthService } from '../../services/AuthService';
import { NotificationService } from '../../services/NotificationService';
import RegiterDTO from '../../DTOs/RegisterDTO';

/** Matches backend RegisterDTO / LoginDTO password regex. */
const PASSWORD_PATTERN = /^[A-Z](?=.*[0-9])(?=.*[^A-Za-z0-9]).{7,}$/;

@Component({
  selector: 'app-register',
  imports: [Divider, FormsModule, Notification, RouterLink],
  templateUrl: './register.html',
  styleUrl: './register.css',
})
export class Register implements OnInit {
  username = '';
  email = '';
  password = '';
  confirmPassword = '';
  submitting = false;

  constructor(
    private authService: AuthService,
    private notificationService: NotificationService,
    private router: Router,
    private cd: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.authService.redirectIfLoggedIn();
  }

  goToLogin(event: Event): void {
    event.preventDefault();
    event.stopPropagation();
    void this.router.navigateByUrl('/login');
  }

  register(): void {
    if (this.submitting) {
      return;
    }

    const payload: RegiterDTO = {
      username: this.username.trim(),
      email: this.email.trim(),
      password: this.password,
    };

    const validationError = this.validate(payload);
    if (validationError) {
      this.stopSubmitting();
      this.notify(validationError);
      return;
    }

    this.submitting = true;
    this.cd.detectChanges();

    this.authService.register(payload).subscribe({
      next: (response) => {
        try {
          this.notificationService.showSuccess('Account created successfully.');
          this.authService.handleRegisterSuccess(response);
        } catch {
          this.stopSubmitting();
          this.notify('Account created but navigation failed. Please log in.');
        }
      },
      error: (error: HttpErrorResponse) => {
        this.stopSubmitting();
        this.notify(this.getErrorMessage(error));
      },
    });
  }

  private stopSubmitting(): void {
    this.submitting = false;
    this.cd.detectChanges();
  }

  private validate(payload: RegiterDTO): string | null {
    if (!payload.username || !payload.email || !payload.password) {
      return 'Please fill in username, email, and password.';
    }

    if (payload.username.length > 100) {
      return 'Username must be 100 characters or fewer.';
    }

    if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(payload.email)) {
      return 'Please enter a valid email address.';
    }

    if (!PASSWORD_PATTERN.test(payload.password)) {
      return 'Password must start with an uppercase letter, be at least 8 characters, and include a number and a special character.';
    }

    if (payload.password !== this.confirmPassword) {
      return 'Passwords do not match.';
    }

    return null;
  }

  private notify(message: string): void {
    this.notificationService.showError(message);
  }

  private getErrorMessage(error: HttpErrorResponse): string {
    if (error.status === 409) {
      return 'Email already in use.';
    }

    if (error.status === 400) {
      const validation =
        error.error?.errors ||
        error.error?.title ||
        error.error?.Message ||
        error.error?.message;

      if (typeof validation === 'string' && validation.trim()) {
        return validation;
      }

      return 'Invalid registration details. Check your password requirements and try again.';
    }

    const apiMessage =
      (typeof error.error === 'string' && error.error) ||
      error.error?.message ||
      error.error?.Message ||
      error.error?.error;

    if (typeof apiMessage === 'string' && apiMessage.trim()) {
      return apiMessage;
    }

    return 'Something went wrong while registering. Please try again.';
  }
}
