import { Component, HostListener, OnDestroy, OnInit, ViewChild, ElementRef } from '@angular/core';
import { FormBuilder, FormGroup, Validators, AbstractControl, ValidationErrors } from '@angular/forms';
import { Router, NavigationStart } from '@angular/router';
import { Subscription } from 'rxjs';
import { NotificationHandlerService } from '@core/services/common/notification-handler.service';
import { BillingFunderSettingService } from '@core/services/billing/billing-funder-setting.service';
import { BillingSettingInformationModel, BillingDefaultModel, billingProviderAddress, SaveBillingSettingRequest } from '../../../core/models/billing/claim-filingIndicator-model';
import { StateInformation } from "@core/models/billing";
import { ClaimService } from '@core/services/billing/claim.service';

@Component({
  selector: 'app-invoicing-statements-settings',
  templateUrl: './invoicing-statements-settings.component.html',
  styleUrls: ['./invoicing-statements-settings.component.css']
})
export class InvoicingStatementsSettingsComponent implements OnInit, OnDestroy {

  @ViewChild('globalMessageTextarea', { read: ElementRef }) globalMessageTextarea!: ElementRef;
  @ViewChild('dunningMessageTextarea', { read: ElementRef }) dunningMessageTextarea!: ElementRef;

  form: FormGroup;
  isDirty = false;
  isEditMode = false;
  routerSub!: Subscription;
  isOtherSelected = false;

  billingSettingInfo!: BillingSettingInformationModel;
  billingProviderAddress!: billingProviderAddress;
  billingDefaultAddress!: BillingDefaultModel;
  isDefaultAddressEmpty: boolean = false;
  originalFormData!: any;

  payToOptions = [
    { text: 'Billing Provider Location', value: 'Billing Provider Location' },
    { text: 'Other', value: 'Other' }
  ];

  cities: any[] = [];
  states: StateInformation[] = [];
  statesLoaded: boolean = false;

  constructor(
    private fb: FormBuilder,
    private router: Router,
    private billingFunderSettingService: BillingFunderSettingService,
    private notificationService: NotificationHandlerService,
    private claimService: ClaimService
  ) {
    this.form = this.fb.group({
      payToOverride: [''],
      companyName: [''],
      address1: [''],
      address2: [''],
      city: [''],
      state: [''],
      zip: [''],
      zipExtension: [''],
      globalMessage: [''],
      dunningMessage: ['']
    });
  }

  ngOnInit(): void {
    this.loadBillingSettings();
    this.loadBHSettings();

    this.form.valueChanges.subscribe(value => {
      (!value) ? this.isDirty = false : this.isDirty = true;
    });

    this.form.get('payToOverride')?.valueChanges.subscribe(value => {
      if (!value) return;
      this.onPayToChange(value);
    });

    this.routerSub = this.router.events.subscribe(event => {
      if (event instanceof NavigationStart && this.isDirty) {
        const confirmLeave = confirm('You have unsaved changes. Do you really want to leave?');
        if (!confirmLeave) {
          this.router.navigateByUrl(this.router.url);
        }
      }
    });

    this.loadStates();

    this.form.get('state')?.valueChanges.subscribe(stateCode => {
      if (!stateCode) {
        this.form.patchValue({ state: null }, { emitEvent: false });
      }
    });

    this.form.get('city')?.valueChanges.subscribe(cityCode => {
      if (!cityCode) {
        this.form.patchValue({ city: null }, { emitEvent: false });
      }
    });
  }

  // ================= LOAD DATA =================

  loadBHSettings(): void {
    this.billingFunderSettingService.getDefaultBilling().subscribe({
      next: (x: BillingSettingInformationModel) => {
        this.billingSettingInfo = x;

        this.billingDefaultAddress = {
          payToOverride: x.payToAddressOverrideOption.toString(),
          companyName: x.companyName,
          address1: x.addressLine1,
          address2: x.addressLine2,
          city: x.city,
          state: x.state,
          zip: x.zip,
          zipExtension: x.zipExtension,
          globalMessage: x.globalMessage ?? '',
          dunningMessage: x.dunningMessage ?? ''
        };
      },
      error: (err) => {
        console.error('Error loading default billing', err);
      }
    });
  }

  loadBillingSettings(): void {
    this.billingFunderSettingService
      .getBillingSettingInformation()
      .subscribe((x: BillingSettingInformationModel) => {

        this.billingSettingInfo = x;

        this.billingProviderAddress = {
          companyName: x.companyName,
          addressLine1: x.addressLine1,
          addressLine2: x.addressLine2,
          city: x.city,
          state: x.state,
          zip: x.zip,
          zipExtension: x.zipExtension
        };
        this.isDefaultAddressEmpty = this.isDefaultBillingAddressEmpty(this.billingProviderAddress);
        this.form.patchValue({
          payToOverride: x.payToAddressOverrideOption === 1 ? 'Billing Provider Location' : 'Other',
          globalMessage: x.globalMessage ?? '',
          dunningMessage: x.dunningMessage ?? ''
        }, { emitEvent: false });

        this.applyBillingProviderAddress();
        if (x.payToAddressOverrideOption === 0) { this.setOtherValidators(); }
        this.originalFormData = { ...this.form.getRawValue() };
      });
  }

  private loadStates(): void {
    if (this.statesLoaded) return;

    this.claimService.getStateInformation().subscribe({
      next: (data) => {
        this.states = data || [];
        this.statesLoaded = true;
      },
      error: (error) => {
        console.error('Error loading states', error);
        this.states = [];
      }
    });
  }

  // ================= TEXTAREA DIMENSION HELPERS =================

  private resetTextareaDimensions(): void {
    [this.globalMessageTextarea, this.dunningMessageTextarea].forEach(ref => {
      if (ref?.nativeElement) {
        const textarea = ref.nativeElement.querySelector('textarea');
        if (textarea) {
          textarea.style.removeProperty('height');
          textarea.style.removeProperty('width');
          textarea.style.removeProperty('min-height');
        }
      }
    });
  }

  // ================= PAYTO CHANGE =================

  onPayToChange(value: string): void {
    this.isDefaultAddressEmpty = false;
    this.resetTextareaDimensions();
    if (!this.originalFormData) return;

    if (value === 'Billing Provider Location') {

      this.applyBillingProviderAddress();

      if (this.originalFormData?.payToOverride === 'Billing Provider Location') {
        this.form.reset(this.originalFormData, { emitEvent: false });
      } else {
        this.form.reset(this.billingDefaultAddress, { emitEvent: false });
        this.isDefaultAddressEmpty = this.isDefaultBillingAddressEmpty(this.billingDefaultAddress);
        this.form.patchValue(
          { payToOverride: 'Billing Provider Location' },
          { emitEvent: false }
        );
      }

      this.clearOtherValidators();
      this.isOtherSelected = false;

    } else if (value === 'Other') {

      this.enableAddressFields();

      if (this.originalFormData?.payToOverride === 'Other') {
        this.form.reset(this.originalFormData, { emitEvent: false });
      } else {
        this.clearAddressFields();
      }

      this.setOtherValidators();
      this.isOtherSelected = true;
    }
  }

  isDefaultBillingAddressEmpty(items: any): boolean {
    if (!items) return true;

    return (
      items.companyName === '' ||
      items.address1 === '' || items.addressLine1 === '' ||
      items.city === '' ||
      items.state === '' ||
      items.zip === ''
    );
  }

  // ================= VALIDATION =================

  private setOtherValidators(): void {
    this.form.get('companyName')?.setValidators([Validators.required]);
    this.form.get('address1')?.setValidators([Validators.required]);
    this.form.get('city')?.setValidators([Validators.required]);
    this.form.get('state')?.setValidators([Validators.required]);
    this.form.get('zip')?.setValidators([Validators.required, Validators.pattern(/^\d{5}$/)]);
    this.form.get('zipExtension')?.setValidators([this.zipExtensionValidator]);

    this.updateValidity();
  }

  private clearOtherValidators(): void {
    ['companyName', 'address1', 'city', 'state', 'zip'].forEach(field => {
      this.form.get(field)?.clearValidators();
    });

    this.updateValidity();
  }

  private updateValidity(): void {
    ['companyName', 'address1', 'city', 'state', 'zip', 'zipExtension']
      .forEach(field => {
        this.form.get(field)?.updateValueAndValidity({ emitEvent: false });
      });
  }

  private zipExtensionValidator(control: AbstractControl): ValidationErrors | null {
    if (!control.value) return null;
    return /^\d{4}$/.test(control.value) ? null : { invalidZipExtension: true };
  }

  // ================= ADDRESS HELPERS =================

  applyBillingProviderAddress(): void {
    if (!this.billingProviderAddress) return;

    this.form.patchValue({
      companyName: this.billingProviderAddress.companyName,
      address1: this.billingProviderAddress.addressLine1,
      address2: this.billingProviderAddress.addressLine2,
      city: this.billingProviderAddress.city,
      state: this.billingProviderAddress.state === 'State' ? '' : this.billingProviderAddress.state,
      zip: this.billingProviderAddress.zip,
      zipExtension: this.billingProviderAddress.zipExtension
    }, { emitEvent: false });

    if (this.isEditMode) this.disableAddressFields();
  }

  clearAddressFields(): void {
    this.form.patchValue({
      companyName: '',
      address1: '',
      address2: '',
      city: '',
      state: '',
      zip: '',
      zipExtension: '',
      dunningMessage: '',
      globalMessage: ''
    }, { emitEvent: false });

    this.isOtherSelected = true;
    this.isDirty = true;
  }

  enableAddressFields(): void {
    ['companyName', 'address1', 'address2', 'city', 'state', 'zip', 'zipExtension']
      .forEach(field => this.form.get(field)?.enable({ emitEvent: false }));
  }

  disableAddressFields(): void {
    ['companyName', 'address1', 'address2', 'city', 'state', 'zip', 'zipExtension']
      .forEach(field => this.form.get(field)?.disable({ emitEvent: false }));

    this.isOtherSelected = false;
  }

  // ================= SAVE =================

  save(): void {
    this.enableAddressFields();
    const formValue = this.form.getRawValue();

    const request: SaveBillingSettingRequest = {
      accountId: 0,
      payToAddressOverrideOption: formValue.payToOverride === 'Billing Provider Location' ? 1 : 0,
      companyName: formValue.companyName,
      addressLine1: formValue.address1,
      addressLine2: formValue.address2,
      city: formValue.city,
      state: formValue.state,
      zip: formValue.zip,
      zipExtension: formValue.zipExtension,
      dunningMessage: formValue.dunningMessage,
      globalMessage: formValue.globalMessage
    };

    if (
      request.payToAddressOverrideOption === 1 &&
      [request.companyName, request.addressLine1, request.city, request.state, request.zip].some(field => !field)
    ) {
      this.notificationService.showNotificationError(
        'Please enter all required information in billing provider location before saving.'
      );
      this.disableAddressFields();
      return;
    }
    if (request.payToAddressOverrideOption === 1) { this.disableAddressFields() }
    this.billingFunderSettingService.SaveBillingSettings(request).subscribe({
      next: () => {
        this.notificationService.showNotificationSuccess('Saved successfully.');
        this.isDirty = false;

        this.loadBillingSettings();

        this.isEditMode = false;
        this.isOtherSelected = false;
        this.enableAddressFields();
        this.form.patchValue(
          { payToOverride: formValue.payToOverride },
          { emitEvent: false }
        );
      },
      error: () => {
        this.notificationService.showNotificationError('Save failed.');
        this.isDirty = false;
      }
    });
  }

  // ================= EDIT / CANCEL =================

  edit(): void {
    this.isEditMode = true;

    const value = this.form.getRawValue().payToOverride;
    if (value === 'Billing Provider Location') {
      this.disableAddressFields();
    } else {
      this.enableAddressFields();
      this.isOtherSelected = value === 'Other';
    }
  }

  cancel(): void {
    if (this.originalFormData) {
      this.form.reset(this.originalFormData, { emitEvent: false });
    }

    this.isEditMode = false;
    this.isOtherSelected = this.originalFormData?.payToOverride === 'Other';
    this.enableAddressFields();
    this.isDirty = false;
  }

  // ================= OTHER =================

  onMessageChange(field: string, value: string): void {
    this.form.get(field)?.setValue(value);
  }

  @HostListener('window:beforeunload', ['$event'])
  unloadNotification(event: BeforeUnloadEvent): void {
    if (this.isDirty) {
      event.returnValue = true;
    }
  }

  ngOnDestroy(): void {
    this.routerSub?.unsubscribe();
  }
}
