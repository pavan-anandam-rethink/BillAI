import { Component, ElementRef, Input } from '@angular/core';
import { AuthService } from '@core/services/sso';

@Component({
  selector: 'app-profile-options',
  templateUrl: './profile-options.component.html',
  styleUrls: ['./profile-options.component.css']
})
export class ProfileOptionsComponent {
  @Input() anchor: ElementRef;

  constructor(private authSvc: AuthService) { }

  ngOnInit() {
  }

  logOut(){
    this.authSvc.logout();
  }
}
