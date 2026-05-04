import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';

import { HttpService } from '@core/services';
import { Note, NoteType } from '@core/models/common';
import { MemberType } from '../../models/company-account';


@Injectable({
    providedIn: 'root'
})
export class NotesService {

    constructor(private http: HttpService) { }

    getNotes(memberId: number, field: string, asc: boolean, skip: number, take: number, filter: any,memberType: number): Observable<Note[]> {
        return this.http.post<Note[]>('/core/api/common/notes/getnotes', { memberId, field, asc, skip, take, filter,memberType });
    }

    getNote(noteId: number) {
        return this.http.post('/core/api/common/notes/getnote', noteId);
    }

    saveNote(note: Note): Observable<void> {
        return this.http.post('/core/api/common/notes/savenote', note);
    }

    deleteNote(id: number): Observable<void> {
        return this.http.post('/core/api/common/notes/deleteNote', id);
    }

    getNoteTypes(memberType: MemberType): Observable<NoteType[]>{
        return this.http.post('/core/api/common/notesType/getNoteTypes', memberType);
    }

    deleteNoteType(id: number): Observable<void> {
        return this.http.post('/core/api/common/notesType/deleteNoteType', id);
    }

    CheckUsageOfNoteType(id: number) {
        return this.http.post('/core/api/common/notesType/isInUse', id);
    }

    saveNotetType(noteType: NoteType): Observable<NoteType> {
        return this.http.post<NoteType>('/core/api/common/notesType/saveNoteType', noteType);
    }
}