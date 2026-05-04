import { Injectable } from '@angular/core';
import { BehaviorSubject, Subject } from 'rxjs';

@Injectable({ providedIn: 'root' })
export class ViewStateService {

  isFooterVisible$ = new BehaviorSubject(true);

  windowResize$ = new Subject();

  constructor() {
    window.addEventListener('resize', e => {
      this.windowResize$.next(e);
    });
  }

}
