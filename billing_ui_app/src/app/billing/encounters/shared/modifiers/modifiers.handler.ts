import { AbstractControl, FormGroup, ValidationErrors, Validators } from "@angular/forms";

export class ModifiersHandler {

    subscribeModifiers(m1: AbstractControl, m2: AbstractControl, m3: AbstractControl, m4: AbstractControl, callback?: () => void) {
        m1.valueChanges.subscribe(x => this.validateM1(x, m1, m2, m3, m4, callback));
        m2.valueChanges.subscribe(x => this.validateM2(x, m1, m2, m3, m4, callback));
        m3.valueChanges.subscribe(x => this.validateM3(x, m1, m2, m3, m4, callback));
        m4.valueChanges.subscribe(x => this.validateM4(x, m1, m2, m3, m4, callback));
    }

    validateM1(x: string, m1: AbstractControl, m2: AbstractControl, m3: AbstractControl, m4: AbstractControl, callback?: () => void) {
        if ((!x || m1.invalid)) {
            this.removeRequire(m1);
        } else {
            m2.enable({ emitEvent: false });
            this.addRequire(m1);
        }
        if(!x && !m2.value) {
            m2.disable({ emitEvent: false });
        }

        callback && callback();
    }

    validateM2(x: string, m1: AbstractControl, m2: AbstractControl, m3: AbstractControl, m4: AbstractControl, callback?: () => void) {
        if ((!x || m2.invalid)) {
            this.removeRequire(m2);
        } else {
            m3.enable({ emitEvent: false });
            this.addRequire(m2);
        }

        if (!x && !m1.value&&!m3.value) {
            m2.disable({ emitEvent: false });
            this.removeRequire(m1);
            m1.updateValueAndValidity({ emitEvent: false });
        }
        if(!x && !m3.value) {
            m3.disable({ emitEvent: false });
        }

        callback && callback();
    }

    validateM3(x: string, m1: AbstractControl, m2: AbstractControl, m3: AbstractControl, m4: AbstractControl, callback?: () => void ) {
        if ((!x || m3.invalid)) {
            this.removeRequire(m3);
        } else {
            m4.enable({ emitEvent: false });
            this.addRequire(m3);
        }

        if (!x && !m2.value&&!m4.value) {
            m3.disable({ emitEvent: false });
            this.removeRequire(m2);
            m2.updateValueAndValidity({ emitEvent: false });
        }
        if(!x && !m4.value) {
            m4.disable({ emitEvent: false });
        }

        callback && callback();
    }

    validateM4(x: string, m1: AbstractControl, m2: AbstractControl, m3: AbstractControl, m4: AbstractControl, callback?: () => void) {
        if (!x && !m3.value) {
            m3.updateValueAndValidity({ emitEvent: false });
        } else {
            this.addRequire(m4);
        }

        if (!x && m4.invalid) {
            this.removeRequire(m4);
        }

        if(!x && !m3.value) {
            m4.disable({ emitEvent: false });
        }

        callback && callback();
    }

    addRequire(c: AbstractControl) {
        c.setValidators([Validators.required, Validators.minLength(2)]);
        c.updateValueAndValidity({ emitEvent: false });
    }

    removeRequire(c: AbstractControl, callback?: () => void) {
        c.clearValidators();
        c.setValidators(Validators.minLength(2));
        c.updateValueAndValidity({ emitEvent: false });

        if (callback) callback();
    }
 
    modifiersValidator(formGroup: FormGroup): ValidationErrors | null {
        const m1 = formGroup.controls["modifier1"];
        const m2 = formGroup.controls["modifier2"];
        const m3 = formGroup.controls["modifier3"];
        const m4 = formGroup.controls["modifier4"];
        let notUnique = false;
        const mArr = [m1, m2, m3, m4];
        const arr: string[] = [];

        mArr.forEach(m => {
            if (m.value)
                if (arr.indexOf(m.value) < 0) {
                    arr.push(m.value);
                    m.errors && (m.errors as any).notUnique && m.setErrors(null);
                    m.updateValueAndValidity({ onlySelf: true, emitEvent: false });
                } else {
                    notUnique = true;
                    m.setErrors({ 'notUnique': true });
                }
        });

        if (!m4.value) {
            if (m3.invalid) m4.disable({ onlySelf: true, emitEvent: false });
            if (!m3.value) {
                if (m2.invalid) m3.disable({ onlySelf: true, emitEvent: false });
            }
        }

        if (notUnique) return { 'notUnique': true };

        return null;
    }
}
