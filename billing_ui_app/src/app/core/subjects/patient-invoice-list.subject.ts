import { BehaviorSubject, Observable, of, Subject } from 'rxjs';
import { catchError, map, switchMap } from 'rxjs/operators';
import { BaseBehaviorSubject } from './base.behavior.subject';
import { PatientInvoiceDetails , PatientInvoiceHeader, PatientInvoiceHeaderSearch } from '@core/models/billing/patient-invoice';
import { PatientInvoiceService } from '@core/services/billing/patient-invoice.service';


export class PatientInvoiceListSubject extends BaseBehaviorSubject<PatientInvoiceHeader> {
    private loadData$ = new Subject<PatientInvoiceHeaderSearch>();
    private appendData$ = new Subject<PatientInvoiceHeaderSearch>();
    private buffer: PatientInvoiceHeader[] = [];
    private isVirtualScrollMode = false;
    private allUniqueClientIds = new Set<number>();
    public totalCount = 0;
    public InvoiceDetails : PatientInvoiceDetails[] | [];

    constructor(private patientInvoiceService: PatientInvoiceService) {
        super();
        this.loadData$.pipe(
            switchMap((params: PatientInvoiceHeaderSearch) => this.patientInvoiceService.getPatientInvoiceDetails(params, true).pipe(
                map(result => ({ result, params })),
                catchError(err => {
                    console.error(err);
                    return [];
                })
            ))
        ).subscribe(({ result, params }) => {
            if (result && result.data) {
                const aggregatedData = result.data.reduce((acc, item) => {
                    // Check if the id already exists in the accumulator
                    const existingItem = acc.find(i => i.clientId === item.clientId);
                    if (existingItem) {
                        // If it exists, sum the values
                        existingItem.id = item.clientId;
                        existingItem.charges += item.charges;
                        existingItem.patientAmount += item.patientAmount;
                        existingItem.insuranceAmount += item.insuranceAmount;
                        existingItem.patientBalance += item.patientBalance;
                        existingItem.adjustment_Non_Patient_responsibility += item.adjustment_Non_Patient_responsibility;
                        existingItem.adjustment_Patient_responsibility += item.adjustment_Patient_responsibility;
                    } else {
                        // Otherwise, push a new item into the accumulator
                        var id = item.id;
                        item.id = item.clientId;
                        acc.push({ ...item });
                        item.id = id;
                    }
                    return acc;
                }, [] as PatientInvoiceHeader[]);

                if (this.isVirtualScrollMode) {
                    // Track unique client IDs from raw data for accurate total
                    if (result.data) {
                        result.data.forEach(item => this.allUniqueClientIds.add(item.clientId));
                    }
                    // Use unique client count as total, not raw detail count
                    this.totalCount = this.allUniqueClientIds.size;
                    
                    // Enforce batch size after aggregation
                    const requestedTake = params.take || aggregatedData.length;
                    const slicedData = aggregatedData.slice(0, requestedTake);
                    this.buffer = slicedData;
                    this.data = this.buffer;
                } else {
                    this.data = aggregatedData;
                    // In non-virtual mode, use aggregated data length as total
                    this.totalCount = this.data.length;
                }
            }

            if (result.data) {
                this.InvoiceDetails = result.data;
            }
            this.sync();
        });

        // Handle append operations for virtual scroll
        this.appendData$.pipe(
            switchMap((params: PatientInvoiceHeaderSearch) => 
                this.patientInvoiceService.getPatientInvoiceDetails(params, false).pipe(
                    map(result => ({ result, params })),
                    catchError(err => {
                        console.error('Error loading data for virtual scroll:', err);
                        return of({ result: { data: [], totalCount: this.totalCount } as any, params });
                    })
                )
            )
        ).subscribe(({ result, params }) => {
            if (result && result.data && result.data.length > 0 && this.isVirtualScrollMode) {
                const aggregatedChunk = result.data.reduce((acc, item) => {
                    const existingItem = acc.find(i => i.clientId === item.clientId);
                    if (existingItem) {
                        existingItem.id = item.clientId;
                        existingItem.charges += item.charges;
                        existingItem.patientAmount += item.patientAmount;
                        existingItem.insuranceAmount += item.insuranceAmount;
                        existingItem.patientBalance += item.patientBalance;
                        existingItem.adjustment_Non_Patient_responsibility += item.adjustment_Non_Patient_responsibility;
                        existingItem.adjustment_Patient_responsibility += item.adjustment_Patient_responsibility;
                    } else {
                        var id = item.id;
                        item.id = item.clientId;
                        acc.push({ ...item });
                        item.id = id;
                    }
                    return acc;
                }, [] as PatientInvoiceHeader[]);

                // Enforce batch size after aggregation
                const requestedTake = params.take || aggregatedChunk.length;
                const chunk = aggregatedChunk.slice(0, requestedTake);
                this.buffer = this.buffer.concat(chunk);
                this.data = this.buffer;

                // Merge invoice details (only for the sliced chunk)
                if (result.data && chunk.length > 0) {
                    // Match detail records to the sliced aggregated chunk
                    const chunkClientIds = new Set(chunk.map(c => c.id));
                    const matchingDetails = result.data.filter(d => chunkClientIds.has(d.clientId));
                    this.InvoiceDetails = [...(this.InvoiceDetails || []), ...matchingDetails];
                    // Track unique client IDs
                    result.data.forEach(item => this.allUniqueClientIds.add(item.clientId));
                }
            }
            // Update total with unique client count
            this.totalCount = this.allUniqueClientIds.size;
            this.sync();
        });
    }

    getAll(params: PatientInvoiceHeaderSearch, virtualScroll = false) {
        this.isVirtualScrollMode = virtualScroll;
        this.buffer = [];
        if (virtualScroll) {
            this.allUniqueClientIds.clear();
        }
        this.loadData$.next(params);
    }

    append(params: PatientInvoiceHeaderSearch) {
        this.appendData$.next(params);
    }

    prependBatch(params: PatientInvoiceHeaderSearch): Observable<void> {
        return this.patientInvoiceService.getPatientInvoiceDetails(params, false).pipe(
            map(result => {
                if (result && result.data && result.data.length > 0 && this.isVirtualScrollMode) {
                    const aggregatedChunk = result.data.reduce((acc, item) => {
                        const existingItem = acc.find(i => i.clientId === item.clientId);
                        if (existingItem) {
                            existingItem.id = item.clientId;
                            existingItem.charges += item.charges;
                            existingItem.patientAmount += item.patientAmount;
                            existingItem.insuranceAmount += item.insuranceAmount;
                            existingItem.patientBalance += item.patientBalance;
                            existingItem.adjustment_Non_Patient_responsibility += item.adjustment_Non_Patient_responsibility;
                            existingItem.adjustment_Patient_responsibility += item.adjustment_Patient_responsibility;
                        } else {
                            var id = item.id;
                            item.id = item.clientId;
                            acc.push({ ...item });
                            item.id = id;
                        }
                        return acc;
                    }, [] as PatientInvoiceHeader[]);

                    // Enforce batch size after aggregation
                    const requestedTake = params.take || aggregatedChunk.length;
                    const chunk = aggregatedChunk.slice(0, requestedTake);
                    this.buffer = chunk.concat(this.buffer);
                    this.data = this.buffer;

                    // Prepend invoice details (only for the sliced chunk)
                    if (result.data && chunk.length > 0) {
                        // Match detail records to the sliced aggregated chunk
                        const chunkClientIds = new Set(chunk.map(c => c.id));
                        const matchingDetails = result.data.filter(d => chunkClientIds.has(d.clientId));
                        this.InvoiceDetails = [...matchingDetails, ...(this.InvoiceDetails || [])];
                        // Track unique client IDs
                        result.data.forEach(item => this.allUniqueClientIds.add(item.clientId));
                    }
                }
                // Update total with unique client count
                this.totalCount = this.allUniqueClientIds.size;
                this.sync();
            }),
            catchError(err => {
                console.error('Error prepending batch:', err);
                return of(void 0);
            })
        );
    }

    removeFromTop(count: number): void {
        if (count <= 0 || count >= this.buffer.length) return;
        this.buffer = this.buffer.slice(count);
        this.data = this.buffer;
        this.sync();
    }

    removeFromBottom(count: number): void {
        if (count <= 0 || count >= this.buffer.length) return;
        this.buffer = this.buffer.slice(0, this.buffer.length - count);
        this.data = this.buffer;
        this.sync();
    }

    clearBuffer() {
        this.buffer = [];
        this.isVirtualScrollMode = false;
        this.InvoiceDetails = [];
        this.allUniqueClientIds.clear();
    }

    getInvoiceDetails(): PatientInvoiceDetails[] | [] {
        return this.InvoiceDetails;
    }

    getCount(): number {
        return this.totalCount
    }

    getDataLength(): number {
        return this.data.length;
    }

}