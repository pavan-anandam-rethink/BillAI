import { Injectable } from "@angular/core";

import { HttpService } from "@core/services";
import { ClaimNote } from "@core/models/billing";
import { ActionResponseResult } from "@core/models/common/action-response-result";
import { ClaimNoteDeleteModel, ClaimNoteGetAllModel, ClaimNoteSaveModel, ClaimNoteUpdateModel, ClaimNotesSaveModel } from "@core/models/billing/notes/cliam-posting-note";
import { environment } from "src/environments/environment";
import { BehaviorSubject, Observable } from "rxjs";

@Injectable()
export class ClaimNotesService {

    constructor(private http: HttpService) { }

    private apiBaseUrl = environment.claimApiBaseUrl;

    private claimNoteCompletedSubject = new BehaviorSubject<number>(null);

    private claimNoteCompleted$ = this.claimNoteCompletedSubject.asObservable();

    setClaimNoteCompleted(data: number): void {
      this.claimNoteCompletedSubject.next(data);
    }

    getClaimNoteCompleted(): Observable<any> {
      return this.claimNoteCompleted$;
    }

    getAll(model: ClaimNoteGetAllModel) {
        return this.http.post<ActionResponseResult<ClaimNote[]>>(this.apiBaseUrl + '/ClaimNote/GetAll', model);
    }

    addNote(model: ClaimNoteSaveModel) {
        return this.http.post<ActionResponseResult>(this.apiBaseUrl + '/ClaimNote/add', model);
    }

    addToSeveral(model: ClaimNotesSaveModel) {
        return this.http.post<ActionResponseResult>(this.apiBaseUrl + '/ClaimNote/AddToSeveral', model);
    }

    updateNote(model: ClaimNoteUpdateModel) {
        return this.http.post<ActionResponseResult>(this.apiBaseUrl + '/ClaimNote/update', model);
    }

    deleteNote(note: ClaimNoteDeleteModel) {
        return this.http.post<ActionResponseResult>(this.apiBaseUrl + '/ClaimNote/delete', note);
    }

}
