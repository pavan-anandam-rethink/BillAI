import { Injectable } from '@angular/core';
import { ToastrService } from "ngx-toastr";

@Injectable({
  providedIn: 'root'
})
export class NotificationHandlerService {
  constructor(private toastrservice: ToastrService) {
  }
  
  showNotificationError(content: string) {
    this.toastrservice.error(content);
  }

  showNotificationSuccess(content: string) {
    this.toastrservice.success(content);
  }

  showNotificationWarning(content: string) {
    this.toastrservice.warning(content);
  }

}