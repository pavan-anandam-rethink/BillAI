import { Component, ElementRef, HostListener, ViewChild } from '@angular/core';
import { Router } from '@angular/router';
import {
  BreakpointObserver,
  Breakpoints,
  BreakpointState,
} from '@angular/cdk/layout';
import { Observable, map } from 'rxjs';
import { AccountMemberService } from '@core/services/account/account-member.service';

@Component({
  selector: 'app-shell',
  templateUrl: './shell.component.html',
  styleUrls: ['./shell.component.css']
})
export class ShellComponent {
  opened: boolean = true;
  sideMenus: any[];
  showUserProfile = false;

  public selectedItem : string = '';
  public isHandset$: Observable<boolean> = this.breakpointObserver
    .observe(Breakpoints.Handset)
    .pipe(map((result: BreakpointState) => result.matches));

  @ViewChild('drawer') drawer: any;
  @ViewChild('userAnchorEl', { read: ElementRef }) public userAnchor: ElementRef;
  @ViewChild("popup", { read: ElementRef }) public popup: ElementRef;
  
  @HostListener("document:keydown", ["$event"])
  public keydown(event: KeyboardEvent): void {
    if (event.code === "Escape") {
      this.showUserProfile = false;
    }
  }

  @HostListener("document:click", ["$event"])
  public documentClick(event: KeyboardEvent): void {
    if (!this.contains(event.target)) {
      this.showUserProfile = false;
    }
  }
  
  private contains(target: EventTarget): boolean {
    if(this.userAnchor && this.userAnchor.nativeElement)
      {
    return (      
      this.userAnchor.nativeElement.contains(target) ||
      (this.popup ? this.popup.nativeElement.contains(target) : false)
      
    );
    }
  }

  constructor(
    private accountService: AccountMemberService,
    private router: Router,
    private breakpointObserver: BreakpointObserver) {
  }

  ngOnInit(): void {
    this.sideMenus = this.accountService.getSideMenus().filter(menu => menu.Show == true);

  }
  
  isShowSideNav() {
    return !(this.router.url.indexOf('billing/claims/edit') > -1 ||
    this.router.url.indexOf('billing/claims/add') > -1);
  }
}
