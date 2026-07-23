import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { Dashboard } from '../../dashboard';
import { AuthService } from '../../../../services/AuthService';

@Component({
  selector: 'app-settings-page',
  imports: [CommonModule, RouterLink],
  templateUrl: './settings-page.html',
  styleUrl: './settings-page.css',
})
export class SettingsPage {
  readonly dashboard = inject(Dashboard);
  private readonly authService = inject(AuthService);

  logout(): void {
    this.authService.logout();
  }
}
