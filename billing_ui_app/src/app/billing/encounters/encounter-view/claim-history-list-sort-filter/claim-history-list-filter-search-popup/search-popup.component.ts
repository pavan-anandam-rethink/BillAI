import {Component, ElementRef, EventEmitter, HostListener, Input, OnDestroy, OnInit, Output, ViewChild, OnChanges, SimpleChanges} from '@angular/core';
import { Subject, Observable } from "rxjs";
import { ClaimHistoryListSearchBase, ClaimHistorySearch, ClaimHistoryListSearch } from '@core/models/billing/claim-history-list-search';
import { HistoryFilterModels } from '../../encounter-transaction/encounter-transaction.component';

@Component({
    selector: 'search-popup',
    templateUrl: './search-popup.component.html',
    styleUrls: ['./search-popup.component.css'],
})

export class SearchPopupComponent implements OnInit, OnChanges, OnDestroy {
    @Input() anchor: ElementRef;
    @Input() field: string;
    @Input() selectedItems: ClaimHistoryListSearch[] = [];
    @Input() historyFilterModels: HistoryFilterModels;
    @Output() anchorViewportLeave = new EventEmitter();
    @Output() searchItemClickedEmmiter = new EventEmitter<object>();
    @Output() onFilterLeave = new EventEmitter<boolean>();
    @ViewChild('popup', { read: ElementRef }) public popup: ElementRef;

    @HostListener('document:click', ['$event'])
    public documentClick(event: any): void {
        if (!this.contains(event.target)) {
            this.anchorViewportLeave.emit();
            this.onFilterLeave.emit(false);
        }
    }
    
    private unsubscribe = new Subject<void>();    
    totalCount: number = 0;    
    isLoading: boolean = false;
    private delayedCall: any;

    searchRequest: ClaimHistoryListSearchBase = new ClaimHistoryListSearchBase(10000);
    searchResult: Observable<ClaimHistorySearch>;

    userList: ClaimHistoryListSearch[] = [];
    actionList: ClaimHistoryListSearch[] = [];
    modeList: ClaimHistoryListSearch[] = [];
    searchList: ClaimHistoryListSearch[] = [];

    constructor() {
        
    }

    private contains(target: any): boolean {
        return this.anchor.nativeElement.contains(target) ||
            (this.popup ? this.popup.nativeElement.contains(target) : false);
    }

    loadSearchList(selectedVal: ClaimHistoryListSearch[], enteredVal: any): void { 
        if (selectedVal.length === 0 && enteredVal !== '') {
            this.isLoading = true;
            if (this.field === 'user') {
                this.searchList = this.userList.filter(x => (x.name).includes(enteredVal));
            } else if (this.field === 'action') {
                this.searchList = this.actionList.filter(x => (x.name).includes(enteredVal));
            } else if (this.field === 'mode') {
                this.searchList = this.modeList.filter(x => (x.name).includes(enteredVal));
            }        
        } else if (selectedVal.length === 0) {
            this.isLoading = true;
            if (this.field === 'user') {
                this.searchList = this.userList;
            } else if (this.field === 'action') {
                this.searchList = this.actionList;
            } else if (this.field === 'mode') {
                this.searchList = this.modeList;
            }
        } else if (selectedVal.length > 0) {
            this.isLoading = true;
            if (this.field === 'user') {
                for (var i = 0; i < this.userList.length; i++) {
                    for (var j = 0; j < selectedVal.length; j++) {
                        if (this.userList[i].name === selectedVal[j].name) {
                            this.userList.splice(i, 1, selectedVal[j]);
                            break;
                        }
                    }
                }
                this.searchList = this.userList;
            } else if (this.field === 'action') {
                for (var i = 0; i < this.actionList.length; i++) {
                    for (var j = 0; j < selectedVal.length; j++) {
                        if (this.actionList[i].name === selectedVal[j].name) {
                            this.actionList.splice(i, 1, selectedVal[j]);
                            break;
                        }
                    }
                }
                this.searchList = this.actionList;
            } else if (this.field === 'mode') {
                for (var i = 0; i < this.modeList.length; i++) {
                    for (var j = 0; j < selectedVal.length; j++) {
                        if (this.modeList[i].name === selectedVal[j].name) {
                            this.modeList.splice(i, 1, selectedVal[j]);
                            break;
                        }
                    }
                }
                this.searchList = this.modeList;
            }
        }
        this.totalCount = this.searchList.length;
        this.isLoading = false;
    }

    searchValueChanged(event: any): void {
        let newVal = event.target.value;

        this.delayedCall = window.setTimeout(() => {
            if (newVal == this.searchRequest.name) {
                this.loadSearchList([], this.searchRequest.name);
            }
        }, 1000);

        this.searchRequest.name = newVal;
    }

    searchItemClicked(searchItem: ClaimHistoryListSearch, field: string) {
        let searchObject = {
            searchItem: searchItem,
            field: field
        }
        this.searchItemClickedEmmiter.emit(searchObject);
    }
    
    ngOnDestroy() {
        this.unsubscribe.next();
        this.unsubscribe.complete();
    }
    
    ngOnInit() {        
        this.loadSearchList(this.selectedItems, '');        
    }

    ngOnChanges(changes: SimpleChanges) {
        if (changes.historyFilterModels && changes.historyFilterModels.currentValue != undefined) {
            if (this.field == "user") {
                const users = new Set(this.historyFilterModels.userList);
                this.userList = Array.from(users).map(x => ({
                    name: x,
                    checked: false
                }));
            } else if (this.field === 'action') {
                const actions = new Set(this.historyFilterModels.actionList);
                this.actionList = Array.from(actions).map(x => ({
                    name: x,
                    checked: false
                }));
            } else if (this.field === 'mode') {
                const modes = new Set(this.historyFilterModels.modeList);
                this.modeList = Array.from(modes).map(x => ({
                    name: x,
                    checked: false
                }));
            }
        }
    }
}