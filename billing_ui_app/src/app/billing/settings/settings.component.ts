import { Component } from '@angular/core';
import { AccountMemberService } from '@core/services/account/account-member.service';
import { AccountPermissions } from '@core/enums/account';

@Component({
  selector: 'app-settings',
  templateUrl: './settings.component.html',
  styleUrls: ['./settings.component.css']
})
export class SettingsComponent {
  headerTitle: string = 'Settings';
  selectedTab: number = 0;
  canEditPatientInvoicing = false;

  constructor(private accountService: AccountMemberService) {
    this.canEditPatientInvoicing = this.accountService.checkPermissionLevel(AccountPermissions.BillingEditPatientInvoicing);
  }

  selectedTabChanged(index: number): void {
    this.selectedTab = index;
  }
}
