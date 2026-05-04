import { Component, Input } from '@angular/core';
import { ClaimErrorAlertModel } from '@core/models/billing/claim-errors-alerts';

@Component({
  selector: 'app-encounter-errors-alerts-details',
  templateUrl: './encounter-errors-alerts-details.component.html',
  styleUrls: ['./encounter-errors-alerts-details.component.css']
})
export class EncounterErrorsAlertsDetailsComponent {
  
  @Input('errorOrAlertEl') element: ClaimErrorAlertModel;
  @Input() claimStatus: string;
}
