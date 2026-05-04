import { Injectable } from '@angular/core';
import { Observable }  from 'rxjs/internal/Observable';
import { BehaviorSubject }  from 'rxjs/internal/BehaviorSubject';
import { AuthService } from '../sso';
import { AccountPermissions } from '@core/enums/account';

@Injectable({
    providedIn: 'root'
})
export class AccountMemberService {
    private accountMemberSettingsSubject = new BehaviorSubject<AccountMemberSettings | null>(null);
    readonly accountMemberSettings: Observable<AccountMemberSettings | null> = this.accountMemberSettingsSubject.asObservable();
    private sideMenusSubject = new BehaviorSubject<SideMenu[] | null>(null);
    readonly sideMenus: Observable<SideMenu[] | null> = this.sideMenusSubject.asObservable();
    public memberDetails: AccountMemberSettings | null = null;
    private memberPermissions: string[] | null;

    constructor(private authSvc: AuthService) {
        this.resetSettings();
    }

    resetSettings() {
        this.memberDetails = this.authSvc.getUserData();
        this.memberPermissions = this.memberDetails.permissions;
        // this.memberPermissions.remove(AccountPermissions.BillingReopenEncounter);
        this.accountMemberSettingsSubject.next(this.authSvc.getUserData());
        this.sideMenusSubject.next(this.getSideMenus());
    }

    checkPermissionLevel(permission: string): boolean {
        if (!this.memberPermissions) {
            return false;
        }
        return this.memberPermissions && this.memberPermissions.indexOf(permission) !== -1;
    }

    private isImpersonating(): boolean {
        return !!this.memberDetails?.impersonatedUser;
    }


    checkPermissionAnyLevels(permissions: string[]): boolean {
        let result = false;
        if (permissions != null && permissions.length > 0) {
            permissions.forEach((permission) => {
                result = result || this.checkPermissionLevel(permission);
            });
        }
        return result;
    }

    checkPermissionAllLevels(permissions: string[]): boolean {

        let result = true;
        if (permissions != null && permissions.length > 0) {
            permissions.forEach((permission) => {
                result = result && this.checkPermissionLevel(permission);
            });
        }
        return result;
    }

     checkPermissionForAll(permissions: string[]): boolean {

        if (!permissions || permissions.length === 0) {
            return false;
        }
        return permissions.every(permission => this.checkPermissionLevel(permission));
    }

    getSideMenus(): SideMenu[] {
        let smClaim: SideMenu = {
            Name: 'Claims',
            Description: 'View Claims, create, Edit Claims',
            path: 'billing/claims',
            Show: this.checkPermissionLevel(AccountPermissions.BillingView),
            leftSideIcon: "billing-icon"
        };
        let smPP: SideMenu = {
            Name: 'Payment Posting',
            Description: 'View Payments, create, Edit Payments',
            path: 'billing/paymentposting',
            Show: this.checkPermissionLevel(AccountPermissions.BillingPostPayments),
            leftSideIcon: "paymentposting-icon"
        };
        let smPI: SideMenu = {
            Name: 'Patient Invoicing',
            Description: 'View Patient Invoicing, create, Edit Patient Invoicing',
            path: 'billing/patientinvoicing',
            Show: this.checkPermissionLevel(AccountPermissions.BillingReopenEncounter),
            leftSideIcon: "patientinvoicing-icon"
        };
        let smReport: SideMenu = {
            Name: 'Reporting',
            Description: 'View reports',
            path: 'billing/reporting',
            Show: this.checkPermissionLevel(AccountPermissions.BillingCloseEncounters),
            leftSideIcon: "report-icon"
        };
        let smClient: SideMenu = {
            Name: 'Client History',
            Description: 'View Client History',
            path: 'billing/clienthistory',
            Show: this.checkPermissionLevel(AccountPermissions.BillingView),
            leftSideIcon: "clientChargeHistory-icon"
        };
        let smSetting: SideMenu = {
            Name: 'Settings',
            Description: 'View Setting', 
            path: 'billing/settings',
            Show: this.isImpersonating() && 
                  this.checkPermissionLevel(AccountPermissions.BillingReopenEncounter),
            leftSideIcon: "settings-01-icon"
        };


        return [smClaim, smPP, smPI, smReport, smClient,smSetting];
    }
}

export interface AccountMemberSettings {
    accountInfoId: number;
    memberId: number;
    memberName: string;
    memberRole: string;
    clientId?: number;
    accountDetail: string;
    impersonationUserName: string | null;
    impersonatedUser: string | null;
    permissions: string[];
}

export interface SideMenu {
    Name: string;
    Description: string;
    path: string;
    Show: boolean;
    leftSideIcon: string;
}
