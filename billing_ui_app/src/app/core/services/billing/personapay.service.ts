import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Observable } from 'rxjs';
import { PersonaPayWebTokenRequest } from '../../models/billing/personapay-web-token.request';
import { environment } from 'src/environments/environment';

@Injectable({
  providedIn: 'root'
})
export class PersonaPayService {

  private baseUrl: string;
  constructor(private http: HttpClient) {
    this.baseUrl = environment.personaBaseUrl;
  }

  createWebToken(
    request: PersonaPayWebTokenRequest
  ): Observable<string> {

    const headers = new HttpHeaders({
      'accept': 'text/plain',
      'Content-Type': 'application/json-patch+json',
      'x-application-key': environment.personaApiKey
    });

    return this.http.post<string>(
      `${this.baseUrl}/web-token`,
      request,
      { headers }
    );
  }
}
 