import { ChangeDetectorRef, Component, NgZone, OnInit } from '@angular/core';
import { HubConnectionBuilder } from '@microsoft/signalr';

@Component({
  selector: 'app-dashboard',
  imports: [],
  templateUrl: './dashboard.html',
  styleUrl: './dashboard.css',
})
export class Dashboard implements OnInit{

  constructor(private cd:ChangeDetectorRef){}

  sideBarOn=false;
  Ir_data="";
  toggleSideBar(){
    this.sideBarOn = !this.sideBarOn;
  }

  goLive(){
    let connection = new HubConnectionBuilder()
      .withUrl("http://localhost:5020/sensorHub")
      .build();

    connection.on("ReceiveSensorReading",data=>{
      console.log("Received:", data);
      this.Ir_data = data;
      this.cd.detectChanges();
    });

    connection.start()
      .then(() => console.log("Connected to SignalR"))
      .catch(err => console.error(err));

  }

  ngOnInit(): void {
    this.goLive();
  }


}
