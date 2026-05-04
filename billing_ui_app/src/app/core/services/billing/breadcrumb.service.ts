import { Injectable } from '@angular/core';
import { ActivatedRoute, NavigationEnd, Router } from '@angular/router';
import { filter } from 'rxjs';

export interface Breadcrumb {
  label: string;
  url: string;
}

@Injectable({
  providedIn: 'root'
})

export class BreadcrumbService {

  breadcrumbs: Breadcrumb[] = [];
  constructor(private router: Router, private route: ActivatedRoute) {
    this.router.events
      .pipe(filter(event => event instanceof NavigationEnd))
      .subscribe(() => {
        this.breadcrumbs = this.buildBreadcrumbs(this.route.root);
      });
  }
  private buildBreadcrumbs(route: ActivatedRoute, url: string = '', breadcrumbs: Breadcrumb[] = []): Breadcrumb[] {
    let children = route.children;

    for (let child of children) {
      if (child.snapshot.routeConfig && child.snapshot.routeConfig.path) {
        const routeURL = child.snapshot.url.map(segment => segment.path).join('/');
        url += `/${routeURL}`;

        const label = child.snapshot.data['breadcrumb'] ??
          child.snapshot.routeConfig.path.replace(/-/g, ' ');

        if (label && routeURL.length > 0) {
          breadcrumbs.push({ label, url });
        }
      }
      return this.buildBreadcrumbs(child, url, breadcrumbs);
    }

    return breadcrumbs;
  }

  getBreadcrumbs() {
    return this.breadcrumbs;
  }
}
