import { Injectable } from "@angular/core";
import { HttpService } from "@core/services";
import { PaymentNote } from "@core/models/billing";
import { PaymentNoteDeleteModel, PaymentNoteSaveModel } from "@core/models/billing/notes/payment-posting-note";
import { environment } from "src/environments/environment";

@Injectable()
export class PaymentNotesService {
    private apiBaseUrl = environment.claimApiBaseUrl;

    constructor(private http: HttpService) { }

    getAll(paymentId: number) {
        return this.http.post<PaymentNote[]>(this.apiBaseUrl + '/PaymentNote/getAll', paymentId, {}, true);
    }

    addNote(model: PaymentNoteSaveModel) {
        return this.http.post<number>(this.apiBaseUrl + '/PaymentNote/add', model, {}, true);
    }

    addToSeveral(model: PaymentNoteSaveModel[]) {
        return this.http.post<number>(this.apiBaseUrl + '/PaymentNote/AddToSeveral', model);
    }

    deleteNote(note: PaymentNoteDeleteModel) {
        return this.http.post<number>(this.apiBaseUrl + '/PaymentNote/delete', note);
    }

}