export interface UploadFilesModel {
  fileCabinetId: number;
  folderId: number;
  files: any; 
  tagIds: number[]; 
  effectiveDate?: Date; 
  expirationDate?: Date;
}