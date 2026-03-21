import { Component } from '@angular/core';

@Component({
  selector: 'app-dashboard',
  imports: [],
  templateUrl: './dashboard.html',
  styleUrl: './dashboard.css',
})
export class Dashboard {
  sideBarOn=false;
  toggleSideBar(){
    this.sideBarOn = !this.sideBarOn;
  }
}
