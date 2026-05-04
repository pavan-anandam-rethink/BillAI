import { EventEmitter, Injectable } from '@angular/core';
import { FileCabinet, Folder, MemberFile, RequestResult } from '@core/models/common';
import { BehaviorSubject }  from 'rxjs/internal/BehaviorSubject';
import { Observable }  from 'rxjs/internal/Observable';
import { map } from 'rxjs/operators';
import { FilesInfo, UploadFilesModel } from '../../models/common/file-cabinet';
import { HttpService } from '../http.service';
import { HttpResponse } from '@angular/common/http';
import { combineLatest }  from 'rxjs/internal/observable/combineLatest';

@Injectable({
    providedIn: 'root'
})
export class FileCabinetService {
    public $reloadFileList: EventEmitter<boolean> = new EventEmitter<boolean>();
    private fileCabinetSubject = new BehaviorSubject<any | null>(null);
    readonly fileCabinet: Observable<FileCabinet> = this.fileCabinetSubject.asObservable();
    loadingRequest: { clientId: any; staffId: number; };

    public static readonly FileRestrictionList: string[] = [
        ".gif",
        ".png",
        ".jpeg",
        ".jpg",
        ".bmp",
        ".tif",
        ".jfif",
        ".heic",
        ".mp4",
        ".mov",
        ".avi",
        ".mpg",
        ".mpeg",
        ".doc",
        ".docx",
        ".ppt",
        ".pptx",
        ".pps",
        ".csv",
        ".txt",
        ".rtf",
        ".pdf",
        ".xls",
        ".xlsx"];


    constructor(private http: HttpService) {
        this.$reloadFileList = new EventEmitter();
    }

    createFolder(folder: Folder, fileCabinetId: number): Observable<RequestResult> {
        return this.http.post('/core/api/common/filecabinet/addnewfolder', { ...folder, fileCabinetId });
    }

    copyFile(fileIds: number[], folderId: number, mode: string): Observable<any> {
        return this.http.post('/core/api/common/filecabinet/copyfile', { fileIds: fileIds, folderId, mode });
    }

    renameFile(fileId: number, newName: string, origName: string): Observable<any> {
        return this.http.post('/core/api/common/filecabinet/RenameFile', { fileId, newName, origName });
    }

    deleteFiles(fileIds: number[]): Observable<RequestResult> {
        return this.http.post('/core/api/common/filecabinet/deletefiles', { fileIds: fileIds });
    }

    deleteFolder(folderId: number): Observable<RequestResult> {
        return this.http.post('/core/api/common/filecabinet/deletefolder', folderId);
    }

    getAllFiles(fileCabinetId: number) {
        return this.http.post<FilesInfo>('/core/api/common/filecabinet/getmemberfiles', fileCabinetId)
            .pipe(
                map((model: any) => {
                    model.forEach((file: any) => {
                        file.dateUploaded = new Date(file.dateUploaded);
                        file.expirationDate = file.expirationDate && new Date(file.expirationDate);
                        file.effectiveDate = file.effectiveDate && new Date(file.effectiveDate);
                    });
                    return {
                        files: model,
                        folderInfo: {}
                    };
                })
                //map(files => files.map(file => { return { ...file, dateUploaded: new Date(file.dateUploaded) }; })),
            );
    }

    getDownloadUrl(fullPath: string, fileName: string, fileId: number, isView: boolean): Observable<any> {
        return this.http.post('/core/api/common/filecabinet/getdownloadurl', { fullPath, containerType: 1, fileName, fileId, isView });
    }

    getBlobFromUrl(url: string): Observable<HttpResponse<Blob>> {
        return this.http.get(url, { responseType: 'blob' as 'json' });
    }

    downloadFileGroup(files: number[]): Observable<string> {
        const f = files.join(',');
        const url = '/core/api/common/filecabinet/DownloadAllSelectedFiles?fileIds=' + f + '';

        return this.http.get(url);
    }

    getFile(fileId: number): Observable<MemberFile> {
        return this.http.post('/core/api/common/filecabinet/getfileinfobyid', fileId);
    }

    getFiles(fileCabinetId: number, folderId: number) {
        return this.http.post<FilesInfo>('/core/api/common/filecabinet/getfolderfiles', { fileCabinetId, folderId })
            .pipe(
                map((model: FilesInfo) => {
                    model.files.forEach((file: MemberFile) => {
                        file.dateUploaded = new Date(file.dateUploaded);
                        file.expirationDate = file.expirationDate && new Date(file.expirationDate);
                        file.effectiveDate = file.effectiveDate && new Date(file.effectiveDate);
                    });
                    return model;
                })
            );
    }

    getFolder(folderId: number): Observable<Folder> {
        return this.http.post('/core/api/common/filecabinet/getfolderinfobyid', folderId);
    }

    getFolders(clientId: number, staffId: number, force = false): Observable<FileCabinet> {
        if (!force && this.loadingRequest && (clientId || 0) === (this.loadingRequest.clientId || 0) && (staffId || 0) === (this.loadingRequest.staffId || 0)) {
            return this.fileCabinet;
        }

        this.loadingRequest = { clientId, staffId };

        const result = this.http.post<FileCabinet>('/core/api/common/filecabinet/getfolderlist', { childProfileId: clientId, memberId: staffId })
            .pipe(map(fc => {
                this.fileCabinetSubject.next(fc);
                return fc;
            }))
        return result;
    }

    updateFolder(folder: Folder, fileCabinetId: number): Observable<RequestResult> {
        return this.http.post('/core/api/common/filecabinet/updatefolder', { ...folder, fileCabinetId });
    }

    uploadFile(model: UploadFilesModel): Observable<any> {
        const requests: any[] = [];

        if ((window as any).Appcues != null) {
            (window as any).Appcues.track("Staff Add File to Staff/Client File Cabinet", {});
        }

        model.files.each((file: any) => {
            const data = new FormData();
            data.append('fileCabinetId', model.fileCabinetId.toString());
            data.append('folderId', model.folderId.toString());
            data.append('file', file.rawFile);
            model.effectiveDate && data.append('effectiveDate', model.effectiveDate.toJSON());
            model.expirationDate && data.append('expirationDate', model.expirationDate.toJSON());
            model.tagIds.forEach(id => data.append('tagIds[]', id.toString()));
            requests.push(this.http.post('/core/api/common/filecabinet/uploadfile', data))
        });

        return combineLatest(requests)
    }

    updateFolderOrder(folders: Folder[]) {
        return this.http.post('/core/api/common/filecabinet/updatefolderOrder', { folders: folders });
    }
}