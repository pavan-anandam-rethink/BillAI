export interface Note {
    id: number;
    noteTypeId: number;
    noteType: string;
    title: string;
    noteDetails: string;
    isPrivate: boolean;
    clientId: number;
    staffId: number;
    createdBy: string;
    createdOn: Date;
    lastUpdatedBy: string;
    lastUpdatedOn: Date | null;
}
