import { CommonModule } from "@angular/common";
import { NgModule } from "@angular/core";
import { ContainedBtnComponent } from "./contained-btn";

@NgModule({
    imports: [
        CommonModule,
       
    ],
    declarations: [ContainedBtnComponent],
    exports: [
        ContainedBtnComponent
    ],
})
export class ContainedButtonModule { }