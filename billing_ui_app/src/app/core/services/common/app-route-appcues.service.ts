import { Injectable } from '@angular/core';
import { Router, NavigationEnd, ActivatedRoute } from '@angular/router';
import { filter } from 'rxjs/operators';

@Injectable({
    providedIn: 'root'
})

export class AppRouteAppcuesService {
    constructor(private router: Router, private route: ActivatedRoute) {
        router.events
            .pipe(
                filter(event => event instanceof NavigationEnd)
            )
            .subscribe(event => {
                (window as any).Appcues && (window as any).Appcues.page();
            });
    }
}