import { Injectable } from "@angular/core";

@Injectable({
    providedIn: 'root'
})
export class Locale {
    private dateFormats = {
        "af-ZA": "yyyy/MM/dd",
        "am-ET": "dd/MM/yyyy",
        "ar-AE": "dd/MM/yyyy",
        "ar-BH": "dd/MM/yyyy",
        "ar-DZ": "dd-MM-yyyy",
        "ar-EG": "dd/MM/yyyy",
        "ar-IQ": "dd/MM/yyyy",
        "ar-JO": "dd/MM/yyyy",
        "ar-KW": "dd/MM/yyyy",
        "ar-LB": "dd/MM/yyyy",
        "ar-LY": "dd/MM/yyyy",
        "ar-MA": "dd-MM-yyyy",
        "ar-OM": "dd/MM/yyyy",
        "ar-QA": "dd/MM/yyyy",
        "ar-SA": "dd/MM/yyyy",
        "ar-SY": "dd/MM/yyyy",
        "ar-TN": "dd-MM-yyyy",
        "ar-YE": "dd/MM/yyyy",
        "arn-CL": "dd-MM-yyyy",
        "as-IN": "dd-MM-yyyy",
        "az-Cyrl-AZ": "dd.MM.yyyy",
        "az-Latn-AZ": "dd.MM.yyyy",
        "ba-RU": "dd.MM.yyyy",
        "be-BY": "dd.MM.yyyy",
        "bg-BG": "dd.MM.yyyy",
        "bn-BD": "dd-MM-yyyy",
        "bn-IN": "dd-MM-yyyy",
        "bo-CN": "yyyy/MM/dd",
        "br-FR": "dd/MM/yyyy",
        "bs-Cyrl-BA": "dd.MM.yyyy",
        "bs-Latn-BA": "dd.MM.yyyy",
        "ca-ES": "dd/MM/yyyy",
        "co-FR": "dd/MM/yyyy",
        "cs-CZ": "dd.MM.yyyy",
        "cy-GB": "dd/MM/yyyy",
        "da-DK": "dd-MM-yyyy",
        "de-AT": "dd.MM.yyyy",
        "de-CH": "dd.MM.yyyy",
        "de-DE": "dd.MM.yyyy",
        "de-LI": "dd.MM.yyyy",
        "de-LU": "dd.MM.yyyy",
        "dsb-DE": "dd. MM. yyyy",
        "dv-MV": "dd/MM/yyyy",
        "el-GR": "dd/M/yyyy",
        "en-029": "MM/dd/yyyy",
        "en-AU": "dd/MM/yyyy",
        "en-BZ": "dd/MM/yyyy",
        "en-CA": "dd/MM/yyyy",
        "en-GB": "dd/MM/yyyy",
        "en-IE": "dd/MM/yyyy",
        "en-IN": "dd-MM-yyyy",
        "en-JM": "dd/MM/yyyy",
        "en-MY": "dd/MM/yyyy",
        "en-NZ": "dd/MM/yyyy",
        "en-PH": "MM/dd/yyyy",
        "en-SG": "dd/MM/yyyy",
        "en-TT": "dd/MM/yyyy",
        "en-US": "MM/dd/yyyy",
        "en-ZA": "yyyy/MM/dd",
        "en-ZW": "MM/dd/yyyy",
        "es-AR": "dd/MM/yyyy",
        "es-BO": "dd/MM/yyyy",
        "es-CL": "dd-MM-yyyy",
        "es-CO": "dd/MM/yyyy",
        "es-CR": "dd/MM/yyyy",
        "es-DO": "dd/MM/yyyy",
        "es-EC": "dd/MM/yyyy",
        "es-ES": "dd/MM/yyyy",
        "es-GT": "dd/MM/yyyy",
        "es-HN": "dd/MM/yyyy",
        "es-MX": "dd/MM/yyyy",
        "es-NI": "dd/MM/yyyy",
        "es-PA": "MM/dd/yyyy",
        "es-PE": "dd/MM/yyyy",
        "es-PR": "dd/MM/yyyy",
        "es-PY": "dd/MM/yyyy",
        "es-SV": "dd/MM/yyyy",
        "es-US": "MM/dd/yyyy",
        "es-UY": "dd/MM/yyyy",
        "es-VE": "dd/MM/yyyy",
        "et-EE": "dd.MMM.yyyy",
        "eu-ES": "yyyy/MM/dd",
        "fa-IR": "MM/dd/yyyy",
        "fi-FI": "dd.MM.yyyy",
        "fil-PH": "MM/dd/yyyy",
        "fo-FO": "dd-MM-yyyy",
        "fr-BE": "dd/MM/yyyy",
        "fr-CA": "yyyy-MM-dd",
        "fr-CH": "dd.MM.yyyy",
        "fr-FR": "dd/MM/yyyy",
        "fr-LU": "dd/MM/yyyy",
        "fr-MC": "dd/MM/yyyy",
        "fy-NL": "dd-MM-yyyy",
        "ga-IE": "dd/MM/yyyy",
        "gd-GB": "dd/MM/yyyy",
        "gl-ES": "dd/MM/yyyy",
        "gsw-FR": "dd/MM/yyyy",
        "gu-IN": "dd-MM-yy",
        "ha-Latn-NG": "dd/MM/yyyy",
        "he-IL": "dd/MM/yyyy",
        "hi-IN": "dd-MM-yyyy",
        "hr-BA": "dd.MM.yyyy.",
        "hr-HR": "dd.MM.yyyy",
        "hsb-DE": "dd. MM. yyyy",
        "hu-HU": "yyyy. MM. dd.",
        "hy-AM": "dd.MM.yyyy",
        "id-ID": "dd/MM/yyyy",
        "ig-NG": "dd/MM/yyyy",
        "ii-CN": "yyyy/MM/dd",
        "is-IS": "dd.MM.yyyy",
        "it-CH": "dd.MM.yyyy",
        "it-IT": "dd/MM/yyyy",
        "iu-Cans-CA": "dd/MM/yyyy",
        "iu-Latn-CA": "dd/MM/yyyy",
        "ja-JP": "yyyy/MM/dd",
        "ka-GE": "dd.MM.yyyy",
        "kk-KZ": "dd.MM.yyyy",
        "kl-GL": "dd-MM-yyyy",
        "km-KH": "yyyy-MM-dd",
        "kn-IN": "dd-MM-yyyy",
        "ko-KR": "yyyy-MM-dd",
        "kok-IN": "dd-MM-yyyy",
        "ky-KG": "dd.MM.yyyy",
        "lb-LU": "dd/MM/yyyy",
        "lo-LA": "dd/MM/yyyy",
        "lt-LT": "yyyy.MM.dd",
        "lv-LV": "yyyy.MM.dd.",
        "mi-NZ": "dd/MM/yyyy",
        "mk-MK": "dd.MM.yyyy",
        "ml-IN": "dd-MM-yy",
        "mn-MN": "yy.MM.dd",
        "mn-Mong-CN": "yyyy/MM/dd",
        "moh-CA": "MM/dD/yyyy",
        "mr-IN": "dd-MM-yyyy",
        "ms-BN": "dd/MM/yyyy",
        "ms-MY": "dd/MM/yyyy",
        "mt-MT": "dd/MM/yyyy",
        "nb-NO": "dd.MM.yyyy",
        "ne-NP": "MM/dd/yyyy",
        "nl-BE": "dd/MM/yyyy",
        "nl-NL": "dd-MM-yyyy",
        "nn-NO": "dd.MM.yyyy",
        "nso-ZA": "yyyy/MM/dd",
        "oc-FR": "dd/MM/yyyy",
        "or-IN": "dd-MM-yyyy",
        "pa-IN": "dd-MM-yyyy",
        "pl-PL": "yyyy-MM-dd",
        "prs-AF": "dd/MM/yyyy",
        "ps-AF": "dd/MM/yyyy",
        "pt-BR": "dd/MM/yyyy",
        "pt-PT": "dd-MM-yyyy",
        "qut-GT": "dd/MM/yyyy",
        "quz-BO": "dd/MM/yyyy",
        "quz-EC": "dd/MM/yyyy",
        "quz-PE": "dd/MM/yyyy",
        "rm-CH": "dd/MM/yyyy",
        "ro-RO": "dd.MM.yyyy",
        "ru-RU": "dd.MM.yyyy",
        "rw-RW": "MM/dd/yyyy",
        "sa-IN": "dd-MM-yyyy",
        "sah-RU": "MM.dd.yyyy",
        "se-FI": "dd.MM.yyyy",
        "se-NO": "dd.MM.yyyy",
        "se-SE": "yyyy-MM-dd",
        "si-LK": "yyyy-MM-dd",
        "sk-SK": "dd. MM. yyyy",
        "sl-SI": "dd.MM.yyyy",
        "sma-NO": "dd.MM.yyyy",
        "sma-SE": "yyyy-MM-dd",
        "smj-NO": "dd.MM.yyyy",
        "smj-SE": "yyyy-MM-dd",
        "smn-FI": "dd.MM.yyyy",
        "sms-FI": "dd.MM.yyyy",
        "sq-AL": "yyyy-MM-dd",
        "sr-Cyrl-BA": "dd.MM.yyyy",
        "sr-Cyrl-CS": "dd.MM.yyyy",
        "sr-Cyrl-ME": "dd.MM.yyyy",
        "sr-Cyrl-RS": "dd.MM.yyyy",
        "sr-Latn-BA": "dd.MM.yyyy",
        "sr-Latn-CS": "dd.MM.yyyy",
        "sr-Latn-ME": "dd.MM.yyyy",
        "sr-Latn-RS": "dd.MM.yyyy",
        "sv-FI": "dd.MM.yyyy",
        "sv-SE": "yyyy-MM-dd",
        "sw-KE": "MM/dd/yyyy",
        "syr-SY": "dd/MM/yyyy",
        "ta-IN": "dd-MM-yyyy",
        "te-IN": "dd-MM-yyyy",
        "tg-Cyrl-TJ": "dd.MM.yyyy",
        "th-TH": "dd/MM/yyyy",
        "tk-TM": "dd.MM.yyyy",
        "tn-ZA": "yyyy/MM/dd",
        "tr-TR": "dd.MM.yyyy",
        "tt-RU": "dd.MM.yyyy",
        "tzm-Latn-DZ": "dd-MM-yyyy",
        "ug-CN": "yyyy-MM-dd",
        "uk-UA": "dd.MM.yyyy",
        "ur-PK": "dd/MM/yyyy",
        "uz-Cyrl-UZ": "dd.MM.yyyy",
        "uz-Latn-UZ": "dd/MM yyyy",
        "vi-VN": "dd/MM/yyyy",
        "wo-SN": "dd/MM/yyyy",
        "xh-ZA": "yyyy/MM/dd",
        "yo-NG": "dd/MM/yyyy",
        "zh-CN": "yyyy/MM/dd",
        "zh-HK": "dd/MM/yyyy",
        "zh-MO": "dd/MM/yyyy",
        "zh-SG": "dd/MM/yyyy",
        "zh-TW": "yyyy/MM/dd",
        "zu-ZA": "yyyy/MM/dd",
    };
            /*var dateFormats = {
    "af-ZA": "yyyy/MM/dd",
    "am-ET": "d/M/yyyy",
    "ar-AE": "dd/MM/yyyy",
    "ar-BH": "dd/MM/yyyy",
    "ar-DZ": "dd-MM-yyyy",
    "ar-EG": "dd/MM/yyyy",
    "ar-IQ": "dd/MM/yyyy",
    "ar-JO": "dd/MM/yyyy",
    "ar-KW": "dd/MM/yyyy",
    "ar-LB": "dd/MM/yyyy",
    "ar-LY": "dd/MM/yyyy",
    "ar-MA": "dd-MM-yyyy",
    "ar-OM": "dd/MM/yyyy",
    "ar-QA": "dd/MM/yyyy",
    "ar-SA": "dd/MM/yy",
    "ar-SY": "dd/MM/yyyy",
    "ar-TN": "dd-MM-yyyy",
    "ar-YE": "dd/MM/yyyy",
    "arn-CL": "dd-MM-yyyy",
    "as-IN": "dd-MM-yyyy",
    "az-Cyrl-AZ": "dd.MM.yyyy",
    "az-Latn-AZ": "dd.MM.yyyy",
    "ba-RU": "dd.MM.yy",
    "be-BY": "dd.MM.yyyy",
    "bg-BG": "dd.M.yyyy",
    "bn-BD": "dd-MM-yy",
    "bn-IN": "dd-MM-yy",
    "bo-CN": "yyyy/M/d",
    "br-FR": "dd/MM/yyyy",
    "bs-Cyrl-BA": "d.M.yyyy",
    "bs-Latn-BA": "d.M.yyyy",
    "ca-ES": "dd/MM/yyyy",
    "co-FR": "dd/MM/yyyy",
    "cs-CZ": "d.M.yyyy",
    "cy-GB": "dd/MM/yyyy",
    "da-DK": "dd-MM-yyyy",
    "de-AT": "dd.MM.yyyy",
    "de-CH": "dd.MM.yyyy",
    "de-DE": "dd.MM.yyyy",
    "de-LI": "dd.MM.yyyy",
    "de-LU": "dd.MM.yyyy",
    "dsb-DE": "d. M. yyyy",
    "dv-MV": "dd/MM/yy",
    "el-GR": "d/M/yyyy",
    "en-029": "MM/dd/yyyy",
    "en-AU": "d/MM/yyyy",
    "en-BZ": "dd/MM/yyyy",
    "en-CA": "dd/MM/yyyy",
    "en-GB": "dd/MM/yyyy",
    "en-IE": "dd/MM/yyyy",
    "en-IN": "dd-MM-yyyy",
    "en-JM": "dd/MM/yyyy",
    "en-MY": "d/M/yyyy",
    "en-NZ": "d/MM/yyyy",
    "en-PH": "M/d/yyyy",
    "en-SG": "d/M/yyyy",
    "en-TT": "dd/MM/yyyy",
    "en-US": "M/d/yyyy",
    "en-ZA": "yyyy/MM/dd",
    "en-ZW": "M/d/yyyy",
    "es-AR": "dd/MM/yyyy",
    "es-BO": "dd/MM/yyyy",
    "es-CL": "dd-MM-yyyy",
    "es-CO": "dd/MM/yyyy",
    "es-CR": "dd/MM/yyyy",
    "es-DO": "dd/MM/yyyy",
    "es-EC": "dd/MM/yyyy",
    "es-ES": "dd/MM/yyyy",
    "es-GT": "dd/MM/yyyy",
    "es-HN": "dd/MM/yyyy",
    "es-MX": "dd/MM/yyyy",
    "es-NI": "dd/MM/yyyy",
    "es-PA": "MM/dd/yyyy",
    "es-PE": "dd/MM/yyyy",
    "es-PR": "dd/MM/yyyy",
    "es-PY": "dd/MM/yyyy",
    "es-SV": "dd/MM/yyyy",
    "es-US": "M/d/yyyy",
    "es-UY": "dd/MM/yyyy",
    "es-VE": "dd/MM/yyyy",
    "et-EE": "d.MM.yyyy",
    "eu-ES": "yyyy/MM/dd",
    "fa-IR": "MM/dd/yyyy",
    "fi-FI": "d.M.yyyy",
    "fil-PH": "M/d/yyyy",
    "fo-FO": "dd-MM-yyyy",
    "fr-BE": "d/MM/yyyy",
    "fr-CA": "yyyy-MM-dd",
    "fr-CH": "dd.MM.yyyy",
    "fr-FR": "dd/MM/yyyy",
    "fr-LU": "dd/MM/yyyy",
    "fr-MC": "dd/MM/yyyy",
    "fy-NL": "d-M-yyyy",
    "ga-IE": "dd/MM/yyyy",
    "gd-GB": "dd/MM/yyyy",
    "gl-ES": "dd/MM/yy",
    "gsw-FR": "dd/MM/yyyy",
    "gu-IN": "dd-MM-yy",
    "ha-Latn-NG": "d/M/yyyy",
    "he-IL": "dd/MM/yyyy",
    "hi-IN": "dd-MM-yyyy",
    "hr-BA": "d.M.yyyy.",
    "hr-HR": "d.M.yyyy",
    "hsb-DE": "d. M. yyyy",
    "hu-HU": "yyyy. MM. dd.",
    "hy-AM": "dd.MM.yyyy",
    "id-ID": "dd/MM/yyyy",
    "ig-NG": "d/M/yyyy",
    "ii-CN": "yyyy/M/d",
    "is-IS": "d.M.yyyy",
    "it-CH": "dd.MM.yyyy",
    "it-IT": "dd/MM/yyyy",
    "iu-Cans-CA": "d/M/yyyy",
    "iu-Latn-CA": "d/MM/yyyy",
    "ja-JP": "yyyy/MM/dd",
    "ka-GE": "dd.MM.yyyy",
    "kk-KZ": "dd.MM.yyyy",
    "kl-GL": "dd-MM-yyyy",
    "km-KH": "yyyy-MM-dd",
    "kn-IN": "dd-MM-yy",
    "ko-KR": "yyyy-MM-dd",
    "kok-IN": "dd-MM-yyyy",
    "ky-KG": "dd.MM.yy",
    "lb-LU": "dd/MM/yyyy",
    "lo-LA": "dd/MM/yyyy",
    "lt-LT": "yyyy.MM.dd",
    "lv-LV": "yyyy.MM.dd.",
    "mi-NZ": "dd/MM/yyyy",
    "mk-MK": "dd.MM.yyyy",
    "ml-IN": "dd-MM-yy",
    "mn-MN": "yy.MM.dd",
    "mn-Mong-CN": "yyyy/M/d",
    "moh-CA": "M/d/yyyy",
    "mr-IN": "dd-MM-yyyy",
    "ms-BN": "dd/MM/yyyy",
    "ms-MY": "dd/MM/yyyy",
    "mt-MT": "dd/MM/yyyy",
    "nb-NO": "dd.MM.yyyy",
    "ne-NP": "M/d/yyyy",
    "nl-BE": "d/MM/yyyy",
    "nl-NL": "d-M-yyyy",
    "nn-NO": "dd.MM.yyyy",
    "nso-ZA": "yyyy/MM/dd",
    "oc-FR": "dd/MM/yyyy",
    "or-IN": "dd-MM-yy",
    "pa-IN": "dd-MM-yy",
    "pl-PL": "yyyy-MM-dd",
    "prs-AF": "dd/MM/yy",
    "ps-AF": "dd/MM/yy",
    "pt-BR": "d/M/yyyy",
    "pt-PT": "dd-MM-yyyy",
    "qut-GT": "dd/MM/yyyy",
    "quz-BO": "dd/MM/yyyy",
    "quz-EC": "dd/MM/yyyy",
    "quz-PE": "dd/MM/yyyy",
    "rm-CH": "dd/MM/yyyy",
    "ro-RO": "dd.MM.yyyy",
    "ru-RU": "dd.MM.yyyy",
    "rw-RW": "M/d/yyyy",
    "sa-IN": "dd-MM-yyyy",
    "sah-RU": "MM.dd.yyyy",
    "se-FI": "d.M.yyyy",
    "se-NO": "dd.MM.yyyy",
    "se-SE": "yyyy-MM-dd",
    "si-LK": "yyyy-MM-dd",
    "sk-SK": "d. M. yyyy",
    "sl-SI": "d.M.yyyy",
    "sma-NO": "dd.MM.yyyy",
    "sma-SE": "yyyy-MM-dd",
    "smj-NO": "dd.MM.yyyy",
    "smj-SE": "yyyy-MM-dd",
    "smn-FI": "d.M.yyyy",
    "sms-FI": "d.M.yyyy",
    "sq-AL": "yyyy-MM-dd",
    "sr-Cyrl-BA": "d.M.yyyy",
    "sr-Cyrl-CS": "d.M.yyyy",
    "sr-Cyrl-ME": "d.M.yyyy",
    "sr-Cyrl-RS": "d.M.yyyy",
    "sr-Latn-BA": "d.M.yyyy",
    "sr-Latn-CS": "d.M.yyyy",
    "sr-Latn-ME": "d.M.yyyy",
    "sr-Latn-RS": "d.M.yyyy",
    "sv-FI": "d.M.yyyy",
    "sv-SE": "yyyy-MM-dd",
    "sw-KE": "M/d/yyyy",
    "syr-SY": "dd/MM/yyyy",
    "ta-IN": "dd-MM-yyyy",
    "te-IN": "dd-MM-yy",
    "tg-Cyrl-TJ": "dd.MM.yy",
    "th-TH": "d/M/yyyy",
    "tk-TM": "dd.MM.yy",
    "tn-ZA": "yyyy/MM/dd",
    "tr-TR": "dd.MM.yyyy",
    "tt-RU": "dd.MM.yyyy",
    "tzm-Latn-DZ": "dd-MM-yyyy",
    "ug-CN": "yyyy-M-d",
    "uk-UA": "dd.MM.yyyy",
    "ur-PK": "dd/MM/yyyy",
    "uz-Cyrl-UZ": "dd.MM.yyyy",
    "uz-Latn-UZ": "dd/MM yyyy",
    "vi-VN": "dd/MM/yyyy",
    "wo-SN": "dd/MM/yyyy",
    "xh-ZA": "yyyy/MM/dd",
    "yo-NG": "d/M/yyyy",
    "zh-CN": "yyyy/M/d",
    "zh-HK": "d/M/yyyy",
    "zh-MO": "d/M/yyyy",
    "zh-SG": "d/M/yyyy",
    "zh-TW": "yyyy/M/d",
    "zu-ZA": "yyyy/MM/dd",
};*/

    constructor() {
    }

    getDateFormat(language: string) {
        const sample = (window as any).Intl ? new Intl.DateTimeFormat(language, {
            numberingSystem: 'latn',
            calendar: 'gregory'
        } as any).format(new Date(1970, 11, 31)) : '';

        let mm = 0,
            mi = sample.indexOf(12 as any);
        let dd = 1,
            di = sample.indexOf(31 as any);
        let yy = 2,
            yi = sample.indexOf(1970 as any);

        // IE 10 or earlier, iOS 9 or earlier, non-Latin numbering system
        // or non-Gregorian calendar; fall back to mm/dd/yyyy
        if (yi >= 0 && mi >= 0 && di >= 0) {
            mm = (mi > yi) as any + (mi > di) as any;
            dd = (di > yi) as any + (di > mi) as any;
            yy = (yi > mi) as any + (yi > di) as any;
        }

        let r: string[] = [];
        r[yy] = 'yyyy';
        r[mm] = 'MM';
        r[dd] = 'dd';

        return r.join(sample.match(/[-.]/) || '/' as any);
    }

    getSampleDate () {
        const hour = 13, minute = 15;
        const sample = Intl.DateTimeFormat !== null ? new Intl.DateTimeFormat(navigator.language, {
            numberingSystem: 'latn',
            calendar: 'gregory',
            day: 'numeric',
            month: 'numeric',
            year: 'numeric',
            hour: '2-digit',
            minute: '2-digit'
        } as any).format(new Date(1970, 11, 31, hour, minute)) : '';
        return sample;
    }

    getLocaleTimeString(low = false) {
        low = low || false;
        const sample = this.getSampleDate();

        if (sample.slice(-2).toUpperCase() === 'PM') {
            return low ? 'hh:mm a' : 'hh:mm A';
        } else {
            return 'HH:mm';
        }
    }

    getLocaleTimeStringWithTT(low = false) {
        low = low || false;
        const sample = this.getSampleDate();

        if (sample.slice(-2).toUpperCase() === 'PM') {
            return low ? 'hh:mm tt' : 'hh:mm tt';
        } else {
            return 'HH:mm';
        }
    }

    getLocaleTimeCalendar () {
        const sample = this.getSampleDate();

        if (sample.slice(-2).toUpperCase() === 'PM') {
            return 'hh:mm a';
        } else {
            return 'HH:mm';
        }
    }

    getLocaleTimePlaceholder() {
        const sample = this.getSampleDate();

        if (sample.slice(-2).toUpperCase() === 'PM') {
            return "__:__ __";
        } else {
            return "__:__";
        }
    }

    getLocaleTimeMask () {
        const sample = this.getSampleDate();

        if (sample.slice(-2).toUpperCase() === 'PM') {
            return "99:99 [**]";
        } else {
            return "99:99";
        }
    }

    getTimeRegex () {
        const sample = this.getSampleDate();

        if (sample.slice(-2).toUpperCase() === 'PM') {
            return /^(0?\d|1[012]):[0-5]\d\s[APap][mM]$/;
        } else {
            return /^(0[0-9]|1[0-9]|2[0-3]):[0-5][0-9]$/;
        }
    }



    public formatDate(date: string) {
        if (!this.isValidDate(date)) {
            return date;
        }

        const localeDate = new Date(date).toLocaleDateString(window.navigator.language);
        return localeDate;
    }

    public formatTime(enTime: string) {
        const date = new Date('1970/11/31 ' + enTime);
        const localeTime = date.toLocaleTimeString(window.navigator.language, { hour: '2-digit', minute: '2-digit' });
        return localeTime || enTime;
    }

    isValidDate(d: string) {
        const timestamp = Date.parse(d);
        return isNaN(timestamp) === false;
    }



    getCalendarHeaderFormat(): string {
        let calendarHeaderFormat;
        const m = this.userDateFormat.match(/\W?[y]{2,4}\W?/);
        if (m && m[0]) {
            calendarHeaderFormat = this.userDateFormat.replace(m[0], '') + ' ddd';
        }

        return calendarHeaderFormat || "M/d ddd";
    }

    public userTimePlaceholder = this.getLocaleTimePlaceholder();
    public userTimeMask = this.getLocaleTimeMask();
    public userTimeFormat = this.getLocaleTimeString();
    public userTimeFormatTT = this.getLocaleTimeStringWithTT();
    public userTimeFormatLow = this.getLocaleTimeString(true);
    public userTimeFormatCalendar = this.getLocaleTimeCalendar();
    public timeRegex = this.getTimeRegex();
    public userDateFormat = this.dateFormats[navigator.language] || this.getDateFormat(navigator.language) || 'MM/dd/yyyy';
    public isDayFirst = this.userDateFormat.indexOf('d') < this.userDateFormat.indexOf('M');
    public userFullDateTimeFormat = 'EEE ' + this.userDateFormat + ' ' + this.userTimeFormatCalendar;
    public calendarHeaderFormat = this.getCalendarHeaderFormat();
    public is24Hour = this.getLocaleTimeCalendar() === 'HH:mm';
    public userDateTimeFormat = this.userDateFormat + ' ' + this.userTimeFormatCalendar;
    public userDateTimeFormatTT = this.userDateFormat + ' ' + this.userTimeFormatTT;
}