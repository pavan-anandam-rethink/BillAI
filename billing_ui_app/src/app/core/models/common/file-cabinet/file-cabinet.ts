import { Folder } from './folder';

export interface FileCabinet {
    viewerId: number;
    usersAvailable: SharedContact[];
    fileCabinetId: number;
    folders: Folder[];
}

export interface SharedContact {
    memberId: number;
    userName: string;
    firstName: string;
    lastName: string;
    isUserContact: boolean;
    isUserCaseManager: boolean;
    selected: boolean;

    viewName: string;
}
