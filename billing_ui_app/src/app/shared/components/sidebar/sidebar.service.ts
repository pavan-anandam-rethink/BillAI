import { ApplicationRef, ComponentFactoryResolver, ComponentRef, Injectable, Injector, EmbeddedViewRef, OnDestroy, Type, EventEmitter, AfterViewInit } from '@angular/core'
import { Observable, Subject, of } from 'rxjs';
import { SidebarComponent, SidebarOptions } from './sidebar.component'
import { delay, map } from 'rxjs/operators';

@Injectable(/*{ providedIn: 'root' }*/)
export class SidebarService implements OnDestroy, AfterViewInit {
    rightSidebarComponentRef: ComponentRef<SidebarComponent>;
    leftSidebarComponentRef: ComponentRef<SidebarComponent>;
    updateEmitter = new EventEmitter();
    private adjustmentChangedSubject = new Subject<number>();
    adjustmentChanged$ = this.adjustmentChangedSubject.asObservable();
    private _adjustmentChanged = new Subject<number>();       // patientId

    constructor(
        private componentFactoryResolver: ComponentFactoryResolver,
        private appRef: ApplicationRef,
        private injector: Injector
    ) {
        this.rightSidebarComponentRef = this.init(this.rightSidebarComponentRef, { position: 'right-sidebar', popupWidthClass: 'md' });
        
        this.leftSidebarComponentRef = this.init(this.leftSidebarComponentRef, { position: 'left-sidebar', popupWidthClass: 'md' });

        this.rightSidebarComponentRef.changeDetectorRef.detectChanges();
        this.leftSidebarComponentRef.changeDetectorRef.detectChanges();
    }
    ngAfterViewInit(): void {

    }

    private init(ref: ComponentRef<SidebarComponent>, opt: SidebarOptions): ComponentRef<SidebarComponent> {
        if (!ref) {
            ref = this.appendSidebarComponentToBody(ref);
            ref.instance.options = opt;
        }

        return ref;
    }

    emitUpdate(): void {
        this.updateEmitter.emit();
    }
    
    getUpdateEmitter(): EventEmitter<any> {
        return this.updateEmitter;
    }
    ngOnDestroy(): void {
        this.removeDialogComponentFromBody();

    }

    dataChanged(id: number): void {
        this.adjustmentChangedSubject.next(id);
    }

    
    _adjustmentChanged$ = this._adjustmentChanged.asObservable();
    
    emitAdjustmentChanged(patientId: number) {
        this._adjustmentChanged.next(patientId);
    }

    appendSidebarComponentToBody(ref: ComponentRef<SidebarComponent>): ComponentRef<SidebarComponent> {
        const componentFactory = this.componentFactoryResolver.resolveComponentFactory(SidebarComponent);
        ref = componentFactory.create(this.injector);
        this.appRef.attachView(ref.hostView);

        const domElem = (ref.hostView as EmbeddedViewRef<any>).rootNodes[0] as HTMLElement;
        document.body.appendChild(domElem);
        return ref;
    }

    private removeDialogComponentFromBody(): void {
        if (this.rightSidebarComponentRef) {
            this.rightSidebarComponentRef && this.appRef.detachView(this.rightSidebarComponentRef.hostView);
            this.rightSidebarComponentRef.destroy();
        }
        if (this.leftSidebarComponentRef) {
            this.leftSidebarComponentRef && this.appRef.detachView(this.leftSidebarComponentRef.hostView);
            this.leftSidebarComponentRef.destroy();
        }
    }

    public openRight(componentType: Type<any>, closeIconEnabled = true, width, IsSidebarFixed = false): Observable<ComponentRef<any>> {
        return this.open(this.rightSidebarComponentRef, componentType, closeIconEnabled, width, IsSidebarFixed);
    }

    public openLeft(componentType: Type<any>, closeIconEnabled = true): Observable<ComponentRef<any>> {
        return this.open(this.leftSidebarComponentRef, componentType, closeIconEnabled, "md");
    }
    private open(ref: ComponentRef<SidebarComponent>, componentType: Type<any>, closeIconEnabled: boolean, width: string, IsSidebarFixed = false): Observable<ComponentRef<any>> {
        return of(<ComponentRef<any>>{})
            .pipe(map(x => {
                ref.instance.close();
                return x;
            }))
            .pipe(delay(ref.instance.isOpened ? 500 : 0))
            .pipe(map(x => {
                ref.instance.loadChildComponent(componentType);
                ref.instance.open(closeIconEnabled, width, IsSidebarFixed);
                return ref.instance.componentRef || x;
            }));
    }

    public closeAll(): void {
        this.rightSidebarComponentRef.instance.close();
        this.leftSidebarComponentRef.instance.close();
    }
}