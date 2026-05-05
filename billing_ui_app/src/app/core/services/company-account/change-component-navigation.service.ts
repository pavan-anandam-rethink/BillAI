import { Injectable } from '@angular/core';
import { BehaviorSubject } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class ChangeComponentNavigationService {

  private basicInformationDetailsSource = new BehaviorSubject<any>(null);
  basicInformationDetails = this.basicInformationDetailsSource.asObservable();

  setBasicDetailsComponent(): void {
    this.basicInformationDetailsSource.next(null);
  }

}