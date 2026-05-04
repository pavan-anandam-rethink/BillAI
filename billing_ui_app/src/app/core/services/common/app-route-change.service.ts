import { EventEmitter, Injectable } from '@angular/core';
import { Router, NavigationEnd, ActivatedRoute } from '@angular/router';
import { BehaviorSubject } from 'rxjs/internal/BehaviorSubject';
import { of }  from 'rxjs/internal/observable/of';
import { Observable } from 'rxjs/internal/Observable';
import { filter, mergeMap, startWith } from 'rxjs/operators';

@Injectable({
    providedIn: 'root'
})

export class AppRouteChangeService {

    private showMenuSubject = new BehaviorSubject<boolean>(true);
    readonly showMenu: Observable<boolean> = this.showMenuSubject.asObservable();
    private showLoginMenuSubject = new BehaviorSubject<boolean>(true);
    readonly showLoginMenu: Observable<boolean> = this.showLoginMenuSubject.asObservable();
    readonly tertiaryMenuTitle = new EventEmitter();
    readonly editTitle = new EventEmitter();

    tertiaryTitle = new EventEmitter();

    init(data: any) {
        if (data) {
            this.showMenuSubject.next(!(data.hideMenu || false));
            this.showLoginMenuSubject.next(!(data.hideLoginMenu || false));
            this.tertiaryTitle.emit(data.title || '');
        } else {
            this.showMenuSubject.next(true);
            this.showLoginMenuSubject.next(true);
            this.tertiaryTitle.emit('');
        }
    }

    getRouteData(route: ActivatedRoute, router: Router) {
        return router.events.pipe(
            filter(event => event instanceof NavigationEnd),
            startWith(() => route.firstChild ? route.firstChild.data : of(null)),
            mergeMap(() => route.firstChild ? route.firstChild.data : of(null))
        )
            .subscribe((data: any) => {
                this.init(data);
            });
    }

} 