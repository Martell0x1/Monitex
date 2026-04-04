import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Dashboard } from '../../dashboard';

@Component({
  selector: 'app-overview-page',
  imports: [CommonModule],
  templateUrl: './overview-page.html',
  styleUrl: './overview-page.css',
})
export class OverviewPage {
  readonly dashboard = inject(Dashboard);
}
