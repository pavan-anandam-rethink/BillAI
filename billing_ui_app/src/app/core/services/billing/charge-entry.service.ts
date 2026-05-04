import { Injectable } from '@angular/core';
import { HttpService } from '../http.service';
import { ClaimNoteAddModel, ClaimNoteDetailsModel } from '@core/models/billing';
import { environment } from 'src/environments/environment';

@Injectable()
export class ChargeEntryService {
    private apiBaseUrl = environment.claimApiBaseUrl;
    constructor(private http: HttpService) {
    }

    addChargeNote(note: ClaimNoteAddModel) {
        return this.http.post<ClaimNoteDetailsModel>(this.apiBaseUrl + '/ChargeEntry/AddNote', note, {}, true);
    }
    
    deleteChargeNote(chargeId: number){
        return this.http.post<any>(this.apiBaseUrl + '/ChargeEntry/DeleteNote', chargeId, {}, true);
    }
}