import { Injectable } from '@angular/core';

@Injectable({
    providedIn: 'root'
})
export class ValidationService {
    self = {};

    isValidCVV(cvv: string): boolean {
        let re = /^[0-9]{3,4}$/;
        return re.test(cvv);
    }

    isValidZipCode(input: string): boolean {
        let re = /^\d{5}$/;
        return re.test(input);
    }
    isValidCCExp(ccexp: string): boolean {
        let match = ccexp.match(/^\s*(0?[1-9]|1[0-2])\/(\d\d|\d{4})\s*$/);
        if (!match) {
            return false;
        }
        let exp = new Date(this.normalizeYear(1 * +match[2]), 1 * +match[1] - 1, 1).valueOf();
        let now = new Date();
        let currMonth = new Date(now.getFullYear(), now.getMonth(), 1).valueOf();

        if (exp < currMonth) {
            return false;
        } else {
            return true;
        };
    }
    isValidCreditCard(ccnumber: string, cctype: string): boolean {
        if (cctype === "") {
            return false;
        }
        // remove non-numerics
        let v = "0123456789";
        let w = "";
        for (let i = 0; i < ccnumber.length; i++) {
            let x = ccnumber.charAt(i);
            if (v.indexOf(x, 0) != -1)
                w += x;
        }
        ccnumber = w;
        let re = /^[0-9]{15,16}$/;
        if (!re.test(ccnumber)) {
            return false;
        }

        return true;
    }
    isValidCard(s: string): boolean {
        // remove non-numerics
        let v = "0123456789";
        let w = "";
        for (let i = 0; i < s.length; i++) {
            let x = s.charAt(i);
            if (v.indexOf(x, 0) != -1)
                w += x;
        }

        // validate number
        let j = w.length / 2;
        if (j < 6.5 || j > 8 || j == 7)
            return false;

        let k = Math.floor(j);
        let m = Math.ceil(j) - k;
        let c = 0;
        for (let i = 0; i < k; i++) {
            let a = +w.charAt(i * 2 + m) * 2;
            c += a > 9 ? Math.floor(a / 10 + a % 10) : a;
        }

        for (let i = 0; i < k + m; i++)
            c += +w.charAt(i * 2 + 1 - m) * 1;

        return (c % 10 === 0);
    }
    isValidUserName(input: string): boolean {
        let re = /^[a-zA-Z0-9@.]*$/;
        return re.test(input);
    }
    isValidPhone(input: string): boolean {
        let re = /^\d{10}$/;
        return re.test(input);
    }
    validateEmail(email: string): boolean {
        let re = /^([\w-]+(?:\.[\w-]+)*)@((?:[\w-]+\.)*\w[\w-]{0,66})\.([a-z]{2,6}(?:\.[a-z]{2})?)$/i;
        return re.test(email);
    }
    compareDates(date1: string, date2: string): number {

        let val1 = new Date(date1).getTime();
        let val2 = new Date(date2).getTime();


        if (val1 == val2) {
            return 0;
        }

        if (val1 > val2) {
            return 1;
        }

        if (val1 < val2) {
            return 2;
        }

        return -1;
    }

    normalizeYear(year: number): number {
        // Century fix
        let YEARS_AHEAD = 20;
        if (year < 100) {
            let nowYear = new Date().getFullYear();
            year += Math.floor(nowYear / 100) * 100;
            if (year > nowYear + YEARS_AHEAD) {
                year -= 100;
            } else if (year <= nowYear - 100 + YEARS_AHEAD) {
                year += 100;
            }
        }
        return year;
    }
}

