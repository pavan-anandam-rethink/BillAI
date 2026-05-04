import { ComponentFixture, TestBed } from "@angular/core/testing";
import { By } from "@angular/platform-browser";
import { BrowserAnimationsModule } from "@angular/platform-browser/animations";
import { KendoModule } from "@app/plugins/kendo.module";
import { TitleTooltip } from "./title-tooltip.component";

describe("EncounterListComponent", () => {
    let fixture: ComponentFixture<TitleTooltip>;
    let component: TitleTooltip;

    beforeEach(async () => {
        await TestBed.configureTestingModule({
            declarations: [TitleTooltip],
            imports: [KendoModule, BrowserAnimationsModule]
        }).compileComponents();

        fixture = TestBed.createComponent(TitleTooltip);
        component = fixture.componentInstance;
        fixture.detectChanges();
    });

    it("renders without errors", () => {
        expect(component).toBeTruthy();
    });

    it("renders title", () => {
        component.title="Test";
        component.show = true;
        fixture.detectChanges()

        expect(fixture.debugElement.query(By.css("span")).nativeElement.innerHTML).toBe("Test");
    })
});