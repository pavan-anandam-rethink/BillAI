import { Injectable } from '@angular/core';
import { Observable, BehaviorSubject } from 'rxjs';
import { map } from 'rxjs/operators';

import { HttpService } from '../http.service';
import { ProviderInformationModel, TimeZones, Provider, LogoUrl, FileUploadSuccess } from '@core/models/company-account';


@Injectable({
  providedIn: 'root'
})
export class BasicInformationService {
  isLogoDownloaded = false;
  isTimeZonesDownloaded = false;

  private basicInformationSource = new BehaviorSubject<Provider>(new Provider());
  basicInformation = this.basicInformationSource.asObservable();

  private downloadLogoUrlSource = new BehaviorSubject<LogoUrl>(new LogoUrl());
  downloadLogoUrl = this.downloadLogoUrlSource.asObservable();

  private timeZonesSource = new BehaviorSubject<TimeZones>(new TimeZones());
  timeZones = this.timeZonesSource.asObservable();

  constructor(private http: HttpService, public httpClient: HttpService) { }


  getBasicInformation(): void {
    this.http.post('/core/api/Provider/Provider/GetInformation', {})
      .subscribe((x: any) => {
        this.basicInformationSource.next(x);
      },
        err => console.error(err)
      );
  }

  getDownloadLogoUrl(fullPath: string, reload: boolean = false): Observable<LogoUrl> {
    if (!this.isLogoDownloaded || reload) {
      return this.http.post<LogoUrl>('/core/api/Provider/Provider/GetDownloadUrl', { fullPath }).pipe(
        map((response: LogoUrl) => {
          this.downloadLogoUrlSource.next(response);
          this.isLogoDownloaded = true;
          return response;
        }
        )
      );
    } else {
      return this.downloadLogoUrl;
    }
  }

  getTimeZones(): Observable<TimeZones> {
    if (!this.isTimeZonesDownloaded) {
      return this.http.post<TimeZones>(`/core/api/Provider/Provider/GetTimeZones`, {}).pipe(map((response: TimeZones) => {
        this.timeZonesSource.next(response);
        this.isTimeZonesDownloaded = true;
        return response;
      }));
    } else {
      return this.timeZones;
    }
  }

  saveBasicInformation(data: ProviderInformationModel): Observable<void> {
    const path = `/core/api/Provider/Provider/SaveProviderDetailsAsync`;
    return this.http.post<void>(path, { ...data });
  }

  uploadPicture(file: File): Observable<FileUploadSuccess> {
    const formData: FormData = new FormData();
    formData.append('Name', file.name);
    formData.append('Length', file.size.toString());
    formData.append('FileName', file.name);
    formData.append('ContentType', file.type);
    formData.append('File', file);

    const path = `/core/api/Provider/Provider/UploadImageAsync`;
    return this.httpClient.post<FileUploadSuccess>(path, formData);
  }

  setDomainTypeForAccount(useCustomDomains: boolean) {
    const path = `/core/api/Provider/Provider/SetDomainTypeForAccount`;
    return this.http.post<any>(path, { useCustomDomains });
  }
}
