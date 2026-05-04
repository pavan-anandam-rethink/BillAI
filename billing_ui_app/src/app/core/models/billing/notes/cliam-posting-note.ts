export interface ClaimNote {
    id: number;
    claimId: number;
    remindDate: Date;
    note: string;
    recievedReminder: boolean;
    createdBy: number;
    modifiedBy: number;

    dateCreated: Date;
    dateLastModified: Date;
    dateDeleted: Date;

    createdByName: string;
    modifiedByName: string;
} 

export interface ClaimNoteGetModel {
    id: number;
    patientId: number;
    patientName: string;
    dateOfService: Date;
    number?: string;
}

export interface ClaimNoteGetAllModel {
    id: number;
    accountInfoId:number;
}

export interface ClaimNotesSaveModel {
    claimNoteModels: ClaimNoteModel[];    
    memberId:number;
}

export interface ClaimNoteModel {
    claimId: number;
    remindDate: Date;
    note: string;
}

export interface ClaimNoteSaveModel {
    claimId: number;
    remindDate: Date;
    note: string;
    memberId:number;
}

export interface ClaimNoteUpdateModel {
    id: number;
    remindDate: Date;
    note: string;
    recievedReminder: boolean;
}

export interface ClaimNoteDeleteModel {
    id: number;
    dateCreated: Date;
    memberId:number;
}
