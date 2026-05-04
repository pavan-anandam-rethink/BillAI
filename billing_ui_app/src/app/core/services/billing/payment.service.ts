import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import { HttpService } from '../http.service';
import { NewPayment, Payment, PaymentOptions } from '@core/models/billing';
import { environment } from 'src/environments/environment';


@Injectable({
    providedIn: 'root'
})
export class PaymentService {
    private apiBaseUrl = environment.claimApiBaseUrl;

    constructor(private http: HttpService) { }

    applyPayment(payment: NewPayment): Observable<NewPayment> {
        return this.http.post<NewPayment>(this.apiBaseUrl + '/chargepayment/save', payment);
    }

    getByClaimId(claimId: number): Observable<Payment[]> {
        return this.http.post<Payment[]>(this.apiBaseUrl + '/chargepayment/getforclaim', claimId)
            .pipe(map(payments => payments.map(payment => { return { ...payment, date: new Date(payment.date) }; })));
    }

    getPaymentOptions(claimId: number): Observable<PaymentOptions> {
        return this.http.post<PaymentOptions>(this.apiBaseUrl + '/chargepayment/getpaymentoptions', claimId);
    }

    getRemainingAmount(chargeId: number): Observable<number> {
        return this.http.post<number>(this.apiBaseUrl + '/chargepayment/getremainingamount', chargeId);
    }

    Delete(payment: Payment): Observable<Payment> {
        return this.http.post<Payment>(this.apiBaseUrl + '/chargepayment/Delete', payment).pipe(map(result => { return { ...result }; }));
    }
}