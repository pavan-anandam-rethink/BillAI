import { Component, Renderer2 } from '@angular/core';

@Component({
  selector: 'app-unauthorized',
  templateUrl: './unauthorized.component.html',
  styleUrls: ['./unauthorized.component.css']
})
export class UnauthorizedComponent {
  headerTitle = "401 Unauthorized";

  constructor(
    private renderer: Renderer2,
  ) 
  {
  }
}