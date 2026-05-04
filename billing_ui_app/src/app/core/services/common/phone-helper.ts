import { Injectable } from '@angular/core';

@Injectable({
    providedIn: 'root'
})
export class PhoneHelper {

    static format(str: string): string {
        let cleaned = ('' + str).replace(/\D/g, '');
        let match = cleaned.match(/^(\d{3})?(\d{3})(\d{4})(\d{0,3})$/);

        if (match) {
            let last = match[4];
            last += '___'.slice(last.length);

            return '(' + match[1] + ') ' + match[2] + '-' + match[3] + ' x' + last;
        };

        return str;
    };

    static phonePattern = ['(', /[0-9+]/, /\d/, /\d/, ')', ' ', /[0-9 ]/, /\d/, /\d/, '-', /\d/, /\d/, /\d/, /\d/, ' ', 'x', /\d/, /\d/, /\d/];
    static phonePatternShort = ['(', /[0-9+]/, /\d/, /\d/, ')', ' ', /[0-9 ]/, /\d/, /\d/, '-', /\d/, /\d/, /\d/, /\d/];

    constructor() { }
}