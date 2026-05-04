import { FormGroup, FormControl, FormArray } from '@angular/forms';

export const makeAllControlsTouched = (form: FormGroup) => {
  Object.keys(form.controls).forEach(field => {
    const control = form.get(field);
    if (control instanceof FormControl) {
      control.markAsTouched({ onlySelf: true });
    } else if (control instanceof FormGroup) {
      makeAllControlsTouched(control);
    } else if (control instanceof FormArray) {
      makeFormArrayTouched(control.controls)
    }
  });
}


export const makeFormArrayTouched = (controls: any) => {
  Object.keys(controls).forEach(control => {
    makeAllControlsTouched(controls[control]);
  });
}