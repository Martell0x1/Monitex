import { Component, signal } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { Notification } from './notification/notification';
@Component({
  selector: 'app-root',
  imports: [RouterOutlet, Notification],
  templateUrl: './app.html',
  styleUrl: './app.css'
})
export class App {
  protected readonly title = signal('smart_home');
}
