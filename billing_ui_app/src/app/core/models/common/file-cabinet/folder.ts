import { SharedContact } from "./file-cabinet";

export interface Folder {
    order: number;
    folderId: number;
    folderName: string;
    sharedFolder: boolean;
    isSystemFolder: boolean;
    contacts: SharedContact[];
}
