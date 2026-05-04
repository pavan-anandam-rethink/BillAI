import { Component, Renderer2 } from '@angular/core';

@Component({
  selector: 'app-not-found',
  templateUrl: './not-found.component.html',
  styleUrls: ['./not-found.component.css']
})
export class NotFoundComponent {
  headerTitle = "404 Page Not Found";

  constructor(
    private renderer: Renderer2,
  ) 
  {
  }
}