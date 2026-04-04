import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { Dashboard } from '../../dashboard';

@Component({
  selector: 'app-settings-page',
  imports: [CommonModule, RouterLink],
  templateUrl: './settings-page.html',
  styleUrl: './settings-page.css',
})
export class SettingsPage {
  readonly dashboard = inject(Dashboard);
}
