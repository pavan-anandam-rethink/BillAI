import { Component, EventEmitter, Input, OnDestroy, OnInit, Output, SimpleChanges } from "@angular/core";
import { Subject, takeUntil } from "rxjs";
import { ClaimService } from "@core/services/billing";
import { ClaimFilterOptionModel } from "@core/models/billing/claim-filter-option-model";

import { AccountMemberService } from "@core/services/account/account-member.service";
import { ClaimListingTab } from "@core/enums/billing/claim-listing-tab";


@Component({
    selector: 'claim-filters-status-popup',
    templateUrl: './claim-filters-status-popup.component.html',
    styleUrls: ['./claim-filters-status-popup.component.css'],
})

export class ClaimFiltersStatusPopupComponent implements OnDestroy, OnInit {
    @Output() statusClicked = new EventEmitter<void>();
    @Input() selectedStatuses: ClaimFilterOptionModel[] = [];
    @Output() selectedStatusesChange = new EventEmitter<ClaimFilterOptionModel[]>();
    @Input() tab: ClaimListingTab | null;
    private unsubscribeAll$ = new Subject();
    @Output() showVoidedChange = new EventEmitter<boolean>();
    totalCount: number;
    statuses: ClaimFilterOptionModel[] = [];
    isLoading: boolean;
    @Input() isClosedFilterReset = false;
    searchTimeout: any;
    @Input() isClosedFilterSet = false;
    @Output() isClosedFilterSetChange = new EventEmitter<boolean>();
    @Input() userTouchedStatuses = false;
    @Output() userTouchedStatusesChange = new EventEmitter<boolean>();
    @Output() statusesLoaded = new EventEmitter<number[]>();

    private reqId = 0;

    constructor(private claimsService: ClaimService, private accountService: AccountMemberService) {

    }


    search() {
        if (this.searchTimeout) clearTimeout(this.searchTimeout);

        this.searchTimeout = setTimeout(() => {
            this.isLoading = true;
            const myId = ++this.reqId;
            this.statuses = [];
            this.statusesLoaded.emit([]);
            this.claimsService.getClaimTabStatuses({
                Tab: this.tab,
                SearchValue: '',
                AccountInfoId: this.accountService.memberDetails.accountInfoId
            })
                .pipe(takeUntil(this.unsubscribeAll$))
                .subscribe(x => {

                    if (myId !== this.reqId) return;

                    // merge without duplicates by id; preserve existing checked flags
                    const selectedIds = new Set((this.selectedStatuses || []).map(s => s.id));
                    const fetched = (x || []).map(s => ({ ...s, checked: selectedIds.has(s.id) }));
                    const selectedById = new Map((this.selectedStatuses || []).map(s => [s.id, s]));
                    this.statuses = fetched.map(s => selectedById.get(s.id) ? { ...s, ...selectedById.get(s.id)! } : s);

                    this.isLoading = false;
                    this.statusesLoaded.emit(this.statuses.map(s => s.id));
                    // Auto-select "Closed" ONCE on Closed tab, unless user has already chosen Closed/Void-Closed
                    if (this.tab === ClaimListingTab.Closed
                        && !this.userTouchedStatuses                // <-- don't override user's choice
                        && (!this.isClosedFilterSet || this.isClosedFilterReset)
                        && !this.userHasClosedOrVoidClosedSelected()) {

                        const preferred =
                            this.statuses.find(s => (s.name || '').toLowerCase() === 'closed'); // default = Closed

                        if (preferred) {
                            // check it in both arrays (local list + selected list)
                            preferred.checked = true;

                            const exists = (this.selectedStatuses || []).some(s => s.id === preferred.id);
                            this.selectedStatuses = exists
                                ? this.selectedStatuses.map(s => s.id === preferred.id ? { ...s, checked: true } : s)
                                : [...(this.selectedStatuses || []), { ...preferred }];

                            // Update flags + emit to parent
                            this.isClosedFilterSet = true;
                            this.isClosedFilterSetChange.emit(true);
                            this.isClosedFilterReset = false;

                            this.selectedStatusesChange.emit([...this.selectedStatuses]);
                            this.showVoidedChange.emit(this.isVoidedSelectedLocal());
                            this.statusClicked.emit(); // let middle notify parent if it wants
                        }
                    }
                });
        }, 300); // shorter debounce to reduce race windows
    }

    onStatusClicked(status: ClaimFilterOptionModel) {
        const name = (status.name || '').toLowerCase();
        const isClosedish = name === 'closed' || name === 'void-closed';

        if (status.checked) {
            // uncheck
            status.checked = false;
            this.selectedStatuses = (this.selectedStatuses || []).filter(s => s.id !== status.id);
        } else {
            // check
            status.checked = true;
            const exists = (this.selectedStatuses || []).some(s => s.id === status.id);
            this.selectedStatuses = exists
                ? this.selectedStatuses.map(s => s.id === status.id ? { ...s, checked: true } : s)
                : [...(this.selectedStatuses || []), { ...status }];
        }

        if (!this.userTouchedStatuses) {
            this.userTouchedStatuses = true;
            this.userTouchedStatusesChange.emit(true);
        }

        // Recompute the durable flag based on current selection
        const nowHasClosedish = (this.selectedStatuses || []).some(s => {
            const n = (s.name || '').toLowerCase();
            return (n === 'closed' || n === 'void-closed') && !!s.checked;
        });

        this.isClosedFilterSet = nowHasClosedish;
        this.isClosedFilterSetChange.emit(this.isClosedFilterSet);

        // Emit up so middle is in sync immediately

        this.showVoidedChange.emit(this.isVoidedSelectedLocal());
        this.selectedStatusesChange.emit([...this.selectedStatuses]);
        this.statusClicked.emit();

    }

    private isVoidedSelectedLocal(): boolean {
        return (this.selectedStatuses || []).some(s => (s.name || '').toLowerCase().includes('void') && !!s.checked);
    }

    ngOnDestroy(): void {
        if (this.searchTimeout)
            clearTimeout(this.searchTimeout)

        this.unsubscribeAll$.next(null);
        this.unsubscribeAll$.complete();
    }

    ngOnInit(): void {
        this.statuses = []
        this.search();
        // ensure parent sees correct voided state even before first click
        this.showVoidedChange.emit(this.isVoidedSelectedLocal());
    }

    private userHasClosedOrVoidClosedSelected(): boolean {
        return (this.selectedStatuses || []).some(s => {
            const n = (s.name || '').toLowerCase();
            return (n === 'closed' || n === 'void-closed') && !!s.checked;
        });
    }


    ngOnChanges(changes: SimpleChanges): void {
        if (changes['tab'] && !changes['tab'].firstChange) {
            if (this.searchTimeout) { clearTimeout(this.searchTimeout); this.searchTimeout = null; }
            // clear instantly so badge doesn't show stale count
            this.statuses = [];
            this.isLoading = true;
            this.search();
        }
    }

}