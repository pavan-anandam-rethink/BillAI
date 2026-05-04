import { FormGroup } from "@angular/forms";

export class Helper {
    static ConvertDate(datestring: any): Date {
        const date: any = new Date(datestring.substr(0, datestring.length - 3) * 1000);
        return new Date(date + date.getTimezoneOffset() * 60000);
    }

    // remove after upgrade to Angular8+ as soon as there is native markAllAsTouched
    static markAllAsTouched(fg: FormGroup) {
        if (!fg) {
            return;
        }

        fg.markAsTouched();

        if (!fg.controls) {
            return;
        }

        Object.keys(fg.controls).forEach(controlName => {
            const control: any = fg.controls[controlName];
            Helper.markAllAsTouched(control);
        })
    }

    static shiftDateToUTC(date: Date | undefined) {
        let eDate = date;
        if (eDate) {
            const offset = eDate.getTimezoneOffset() || 0;
            eDate = new Date(eDate.getTime() - offset * 60 * 1000);
        }

        return eDate;
    }
}
