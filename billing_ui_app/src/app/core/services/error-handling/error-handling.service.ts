import { ErrorHandler, Injectable } from '@angular/core';
import { ErrorPopupService } from '../common';
import { NotificationHandlerService } from '../common/notification-handler.service';

@Injectable({
  providedIn: 'root'
})
export class ErrorHandlingService implements ErrorHandler {
  public errormessage = "Error occurred while processing the request.";
  constructor(private errorService: ErrorPopupService,private notify: NotificationHandlerService) {}

  handleError(error: any): void {
    this.notify.showNotificationError(error ?? this.errormessage);
  }
  showPopup(error: any): void {
    if (!this.errorService.isLoading()) {
      var errorObj = {
        isShow: true,
        message: 'Validating Claim(s) may take some time. Please wait...'
    }
      this.errorService.show(errorObj);
    }
  }
  hidePopup() {
    this.errorService.hide();
  }
}
