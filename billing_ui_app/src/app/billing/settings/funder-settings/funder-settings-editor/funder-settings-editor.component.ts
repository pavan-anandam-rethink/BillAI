import { Component, EventEmitter, Input, OnInit, Output, ViewChild, ElementRef } from '@angular/core';
import { BillingFunderSettingService } from '../../../../core/services/billing/billing-funder-setting.service';
import { BillingFunderIdRequestModel } from '../../../../core/models/billing/claim-filingIndicator-model';
import { ClaimFilingIndicatorModel } from '../../../../core/models/billing/billingFunderSetting-model';
import { NotificationHandlerService } from '../../../../core/services/common/notification-handler.service';

@Component({
  selector: 'app-funder-settings-editor',
  templateUrl: './funder-settings-editor.component.html',
  styleUrls: ['./funder-settings-editor.component.css']
})
export class FunderSettingsEditorComponent implements OnInit {
@Input() funder: any;
@Input()timeZone: any;
@Input() claimFilingIndicatorModel : ClaimFilingIndicatorModel[];
@Output() close = new EventEmitter<void>();
public selectedFrequency: number = 1;
public selectedFrequencyOption: number = 0;
public includeTaxonomyCode: boolean = false;
public combineCharges: boolean = false;
public lastSavedPayload: any = null;
  public invalidTime: boolean = false;
  public invalidTimeZone: boolean = false;
  public invalidDays: boolean = false;
  public invalidFrequencyOption: boolean = false;
  public invalidClaimFilingIndicatorId: boolean = false;
billingFunderSetting!: BillingFunderIdRequestModel;
selectedDays: string[] = []; 
selectedTime: string = ''; 
selectedTimeZone: number | null = null;
ClaimFilingIndicator: number | null = null;
public isLoadingMoreData = false;

constructor( private billingFunderSettingService: BillingFunderSettingService,
  private notificationService: NotificationHandlerService
)
{}
ngOnInit(): void {
  this.isLoadingMoreData = true;

    this.loadFunderSetting();
     this.isLoadingMoreData = false;
  }

  loadFunderSetting() {
    this.billingFunderSettingService
      .getBillingFunderIdsSetting(this.funder.funderId)
      .subscribe({
        next: (res) => {
          this.billingFunderSetting = res;
          this.selectedFrequency = res.scheduleType || 1;
          this.selectedDays = res.weeklyDays ? res.weeklyDays.split(',') : [];
          this.selectedFrequencyOption = Number(res.monthlyFrequency);
          this.selectedTime = res.scheduleTime || '';
          this.selectedTimeZone = Number(res.scheduleTimeZone);
          this.combineCharges = res.combineChargesForSameClient || false;
          this.ClaimFilingIndicator = res.claimFilingIndicatorId || null;
          this.includeTaxonomyCode = res.includeTaxonomyCode || false;
        },
        error: (err) => {
          console.error('Error loading funder setting', err);
        }
      });
  }

claimCreationFrequencies = [
  { id: 1, name: 'Immediate' },
  { id: 2, name: 'Daily' },
  { id: 3, name: 'Weekly' },
  { id: 4, name: 'Monthly' }
];


onFrequencyChange(value: number) {
  this.selectedFrequency = value;
}


daysOfWeek = [
  { name: 'Monday' },
  { name: 'Tuesday' },
  { name: 'Wednesday' },
  { name: 'Thursday' },
  { name: 'Friday' },
  { name: 'Saturday' },
  { name: 'Sunday' }
];

frequencyOptions = [
  { id: 1, name: 'First Day' },
  { id: 2, name: 'Last Day' }
];
onCancel() {
  this.close.emit();
}
saveSettings() {
  this.isLoadingMoreData = true;
  this.invalidTime = false;
  this.invalidTimeZone = false;
  this.invalidDays = false;
  this.invalidFrequencyOption = false;
  this.invalidClaimFilingIndicatorId = false;
  let missingFields: string[] = [];
  let isValid = true;

   if (this.selectedFrequency != 1) {

    if (this.selectedFrequency == 3 && (!this.selectedDays || this.selectedDays.length === 0)) {
      this.invalidDays = true;
      missingFields.push('Select Days');
      isValid = false;
    }

    if (this.selectedFrequency == 4 && (!this.selectedFrequencyOption || this.selectedFrequencyOption === 0)) {
      this.invalidFrequencyOption = true;
      missingFields.push('Frequency');
      isValid = false;
    }

    if (!this.selectedTime) {
      this.invalidTime = true;
      missingFields.push('Time');
      isValid = false;
    }

    if (!this.selectedTimeZone) {
      this.invalidTimeZone = true;
       missingFields.push('Time Zone');
      isValid = false;
    }
  }

  this.invalidClaimFilingIndicatorId = this.ClaimFilingIndicator === null || this.ClaimFilingIndicator === undefined || this.ClaimFilingIndicator === 0;
  if(this.invalidClaimFilingIndicatorId ){
      missingFields.push('Claim Filing Indicator');
      isValid = false;
  }
  
  if (!isValid) {
    this.notificationService.showNotificationWarning('Please select: ' + missingFields.join(', '));
    return;
  }

  const payload = {
    changedBy: 0,
    data: {
      scheduleType: Number(this.selectedFrequency),
      scheduleTime: this.selectedFrequency !== 1 ? this.selectedTime : null,
      scheduleTimeZone: this.selectedFrequency !== 1 ? Number(this.selectedTimeZone) : null,
      weeklyDays: this.selectedFrequency == 3 ? this.selectedDays.join(',') : null,
      monthlyFrequency: this.selectedFrequency == 4 ? this.selectedFrequencyOption : null,
      combineChargesForSameClient: this.combineCharges
    },
    id: this.billingFunderSetting?.id ?? 0,
    funderId: this.funder?.funderId ?? 0,
    claimFilingIndicatorId: this.ClaimFilingIndicator ?? 0,
    includeTaxonomyCode: this.includeTaxonomyCode,
    accountInfoId: this.billingFunderSetting?.accountInfoId ?? 0,
    funderName: this.funder?.funderName ?? ''
  };

  this.lastSavedPayload = payload;

  console.log('Saving Payload:', payload);

  this.billingFunderSettingService.saveBillingFunderSettings(payload)
    .subscribe({
      next: () => this.onCancel(),
      error: (err) => {
        console.error('Save failed', err);
        this.notificationService.showNotificationError('Save failed. Please try again.');
      }
    });
    this.isLoadingMoreData = false;
}

onClaimCreationFrequencyChange(): void {
   this.selectedDays = [];
  this.daysScrollIndex = 0;
  this.selectedDays = [];
  this.selectedFrequencyOption = 0;
  this.selectedTime = '';
  this.selectedTimeZone = null; 

  this.invalidDays = false;
  this.invalidFrequencyOption = false;
  this.invalidTime = false;
  this.invalidTimeZone = false;
}

daysScrollIndex: number = 0;
visibleDaysCount: number = 5;

get visibleDays(): any[] {
  return this.daysOfWeek.slice(this.daysScrollIndex, this.daysScrollIndex + this.visibleDaysCount);
}

scrollDaysUp(): void {
  if (this.selectedFrequency != 3 || this.daysScrollIndex === 0) return;
  this.daysScrollIndex--;
}

scrollDaysDown(): void {
  if (this.selectedFrequency != 3 || this.daysScrollIndex + this.visibleDaysCount >= this.daysOfWeek.length) return;
  this.daysScrollIndex++;
}


onDayCheckboxChange(dayName: string, event: Event): void {
  const checked = (event.target as HTMLInputElement).checked;
  if (checked) {
    if (!this.selectedDays.includes(dayName)) {
      this.selectedDays.push(dayName);
    }
  } else {
    this.selectedDays = this.selectedDays.filter(d => d !== dayName);
  }
}

@ViewChild('daysSelect') daysSelect: ElementRef;

scrollDays(direction: string): void {
  if (this.selectedFrequency != 3) return;
  
  const selectEl = this.daysSelect.nativeElement;
  const scrollAmount = 25; // approximate height of one option

  if (direction === 'up') {
    selectEl.scrollTop -= scrollAmount;
  } else {
    selectEl.scrollTop += scrollAmount;
  }
}

}
