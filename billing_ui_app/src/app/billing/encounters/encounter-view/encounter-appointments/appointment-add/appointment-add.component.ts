import { Component, EventEmitter, Input, OnInit, Output, ViewEncapsulation } from "@angular/core";
import { FormBuilder, FormGroup } from "@angular/forms";
import { ActivatedRoute } from "@angular/router";

import { SelectableAppointment } from "@core/models/billing/appointment";
import { AccountMemberService } from "@core/services/account/account-member.service";
import { AppointmentService } from "@core/services/billing";
import { AppointmentGetRequest } from "@core/services/billing/appointment.service";
import { Observable, Subject } from "rxjs";
import { takeUntil } from 'rxjs/operators';

@Component({
	selector: 'appointment-add',
	templateUrl: './appointment-add.component.html',
	styleUrls: ['./appointment-add.component.css'],
	encapsulation: ViewEncapsulation.None
})

export class AppointmentAddComponent implements OnInit {
	@Input() childProfileId: number;
	@Input() locationId: number;
	@Input() claimId: number;
	@Output() closeDialogEmitter = new EventEmitter();
	readonly unsubscribeAll$ = new Subject();
	appointments: SelectableAppointment[] = [];
	cachedAppointments: SelectableAppointment[] = [];
	allowSave: boolean = false;
	public isAppointLoading$: Observable<boolean>;
	appointmentForm: FormGroup;
	searchString: string;
	isLoading: boolean;

	constructor(private fb: FormBuilder, private appointmentService: AppointmentService,
		private route: ActivatedRoute, private accountService: AccountMemberService) {
		this.appointmentForm = fb.group({
			appointmentName: ['', ''],
		});
	}

	ngOnInit(): void {
		this.isLoading = true;
		this.route.params.pipe(takeUntil(this.unsubscribeAll$)).subscribe(params => {
			if (params && params.id) {
				let fromDate = new Date();
				fromDate.setDate(fromDate.getDate() - 120); //this value to be set from funder setup
				var req : AppointmentGetRequest = { 
					ClaimId: this.claimId, 
					ClientId: this.childProfileId, 
					MemberId: this.accountService.memberDetails.memberId, 
					locationId: this.locationId,
					AccountInfoId: this.accountService.memberDetails.accountInfoId,
					StartDate: fromDate,
					EndDate: new Date()
				}
				this.appointmentService.GetFor(req).subscribe((appointments) => {
					this.cachedAppointments = appointments;
					this.filterValues();
					this.isLoading = false;
				},
			error => {
				this.isLoading = false;	
				this.appointments = [];
				this.cachedAppointments = [];});
			}
		});
	}

	close(): void {
		this.closeDialogEmitter.emit('close');
	}

	filterValues(): void {
		if (this.searchString && this.searchString.length) {
			var lowerStr = this.searchString;
			if (isNaN(+this.searchString)) {
				lowerStr = this.searchString.toLowerCase();
			}
			
			this.appointments = this.cachedAppointments.filter(app => app.staffName.toLowerCase().includes(lowerStr) || 
									app.serviceName?.toLowerCase()?.includes(lowerStr) || app.billingCode?.includes(lowerStr));
		} else {
			this.appointments = this.cachedAppointments;
		}
	}

	appointmentSearchValueChanged(event: any): void {
		this.searchString = event.target.value;
		this.filterValues();
	}

	formCheck(): void {
		this.allowSave = !!this.cachedAppointments.find(a => a.selected == true);
	}

	appointmentClick(event: any, appointmentId: number): void {
		const checkAllEl = <HTMLInputElement>document.getElementById('selectAllCheckbox');
		const selectAllState = checkAllEl.checked;

		if(appointmentId == 0){
			this.cachedAppointments.forEach(app => {
				app.selected = selectAllState;
			})
		}else{
			const selectedAppointment = this.cachedAppointments.find(x => x.id === appointmentId);
			if (selectedAppointment) {
				selectedAppointment.selected = !selectedAppointment.selected;
			}
		}

		this.formCheck();
	}

	createAppointment(): void {
		const saveAppointmentsIds = this.cachedAppointments.filter(x => x.selected).map(x => x.id);

		// this.appointmentService.LinkAppointments(this.claimId, saveAppointmentsIds);
		var data = {
			claimId: this.claimId,
			saveAppointmentsIds: saveAppointmentsIds
		}
		this.closeDialogEmitter.emit(data);
	}

	ngOnDestroy() {
		this.unsubscribeAll$.next(void 0);
		this.unsubscribeAll$.complete();
	}
}