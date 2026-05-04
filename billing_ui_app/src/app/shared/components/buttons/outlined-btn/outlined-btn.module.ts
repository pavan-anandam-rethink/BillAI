import { CommonModule } from "@angular/common";
import { NgModule } from "@angular/core";
import { OutlinedBtnComponent } from "./outlined-btn";

@NgModule({
    imports: [
        CommonModule,

    ],
    declarations: [OutlinedBtnComponent],
    exports: [
        OutlinedBtnComponent
    ],
})
export class OutlinedButtonModule { }