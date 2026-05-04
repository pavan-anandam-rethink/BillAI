import { Component, ComponentFactoryResolver, ComponentRef, OnDestroy, Type, ViewChild, EventEmitter } from "@angular/core";
import { InsertionDirective } from './insertion.directive';

@Component({
    selector: 'sidebar',
    templateUrl: './sidebar.html',
    styleUrls: ['./sidebar.css']
})
export class SidebarComponent implements OnDestroy {
    isOpened = false;
    title = 'hello!';
    options: SidebarOptions = { position: 'right-sidebar', popupWidthClass: "md" };
    componentRef: ComponentRef<any>;
    childComponentType: Type<any>;
    closeIconEnabled = true;
    IsSidebarFixed = false;
    public onClose = new EventEmitter();

    @ViewChild(InsertionDirective, {static: false}) insertionPoint: InsertionDirective

    constructor(
        private componentFactoryResolver: ComponentFactoryResolver
    ) { }


    ngOnDestroy(): void {
        if (this.componentRef) {
            this.componentRef.destroy();
        }
    }

    loadChildComponent(componentType: Type<any>): void {
        const componentFactory = this.componentFactoryResolver.resolveComponentFactory(componentType);
        const viewContainerRef = this.insertionPoint.viewContainerRef;
        viewContainerRef.clear();
        this.componentRef = viewContainerRef.createComponent(componentFactory);
    }

    onSidebarClicked(evt: MouseEvent): void {
        evt.stopPropagation()
    }

    close(): void {
        this.isOpened = false;
        this.onClose.emit(true);
        document.body.classList.remove('sidebar-open');
    }

    open(closeIconEnabled = true, width, IsSidebarFixed = false): void {
        this.isOpened = true;
        this.options.popupWidthClass = width;
        this.closeIconEnabled = closeIconEnabled;
        this.IsSidebarFixed = IsSidebarFixed;

        document.body.classList.add('sidebar-open');
    }
}

export interface SidebarOptions {
    position: string;
    popupWidthClass: string;
}