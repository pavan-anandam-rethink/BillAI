import { EventEmitter, Injectable } from '@angular/core';
import { BehaviorSubject } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class BackCancelService {
  public allowBackCancel = new BehaviorSubject<boolean>(true);
  isAllowBackCancel = this.allowBackCancel.asObservable();

  backCancelEmmiter = new EventEmitter();

  constructor() { }

  onClaimFormChange(saveBtnEnabled: boolean): void {
      this.allowBackCancel.next(!saveBtnEnabled);
  }

  emitBackCancel(): void {
      this.backCancelEmmiter.emit(this.allowBackCancel.value);
  }
}