export interface PaymentNote {
    id: number;
    paymentId: number;
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

export interface PaymentNoteSaveModel {
    paymentId: number;
    remindDate: Date;
    note: string;
    memberId:number;
}

export interface PaymentNoteDeleteModel {
    id: number;
    dateCreated: Date;
    memberId:number;
}
