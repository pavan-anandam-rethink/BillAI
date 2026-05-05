import { Injectable } from '@angular/core';
import { BehaviorSubject, Observable } from 'rxjs';
import { tap } from 'rxjs/operators';

import { HttpService } from '..';
import { BasicOption } from '@core/models/common';


@Injectable({
  providedIn: 'root'
})
export class LookupService {
  private _staffMembers: BehaviorSubject<BasicOption[]>;

  get staffMembers(): Observable<BasicOption[]> {
    if (this._staffMembers) {
      return this._staffMembers.asObservable();
    }

    return this.http.post<BasicOption[]>('', {}).pipe(tap(data => {
      this._staffMembers = new BehaviorSubject(data);
    }));
  }

  constructor(private http: HttpService) { }
}