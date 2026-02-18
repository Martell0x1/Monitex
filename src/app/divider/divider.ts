import { Component, Input } from '@angular/core';

@Component({
  selector: 'app-divider',
  imports: [],
  templateUrl: './divider.html',
  styleUrl: './divider.css',
})
export class Divider {
  @Input() message:string="";
}
