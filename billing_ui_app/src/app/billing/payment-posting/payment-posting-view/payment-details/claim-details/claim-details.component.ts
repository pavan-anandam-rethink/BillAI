import { Component } from '@angular/core';
import { ClaimPostingDetails } from '@core/models/billing';
import { ClaimPostingService } from '@core/services/billing';
import { Observable } from 'rxjs';


@Component({
    selector: 'claim-details',
    templateUrl: './claim-details.html',
    styleUrls: ['./claim-details.css']
})
export class ClaimDetailsComponent {
    claimDetails: ClaimPostingDetails;

    constructor(private claimPostingService: ClaimPostingService) {
    }

    setData(claimId: number) {
        this.claimPostingService.getClaimPostingDetails(claimId).subscribe(res => {
            this.claimDetails = res;
        });
    }
}