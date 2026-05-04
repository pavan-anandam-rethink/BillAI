import { SortDescriptor } from "@progress/kendo-data-query/dist/npm/sort-descriptor";

export interface ClientHistoryGrid{
    clientHistoryResponse: ClientHistory[];
    total: number;
}
export class ClientHistory  {
    id: number;
    clientName: string;
    clientId: number;
    dateOfBirth: Date;       
    billed: number;
    insurancePaid: number;
    patientPaid: number;
    remainingClaimBalance: number;
    address: string;
    age: number;
    gender: string;
    location?: string;
    primaryFunder?: string;
    secondaryFunder?: string;

    constructor(clientId: number) {
        this.id = clientId;
    }

} 
export class ClientRecordFilterModel {
    locationId: number [] =[];
    clientId: number [] =[];
    dateOfBirth?: Date | null;
    funderId?: number [] =[];
}
export class ClientHistoryRequest {
    accountInfoId: number;
    memberId: number;
    skip?: number; // default 0
    take?: number; // default 20
    sortingModels: SortDescriptor[] = [];
}

export class ClientHistoryRequestModel {
    ClientRecordFilterModel: ClientRecordFilterModel = new ClientRecordFilterModel();
    ClientHistoryRequest: ClientHistoryRequest = new ClientHistoryRequest();

}


export interface ClientHistoryChargeDetailsResponse{
    chargeDetails: ClientHistoryChargeDetails[];
    total: number;
}

export class ClientHistoryChargeDetails {
  id : number;
  dateOfService: Date;                
  placeOfService: string;            
  renderingProvider: string;         
  authorizationNumber: string;       
  modifiers: string[];               
  diagnosis: string;                 
  primaryFunder: string;             
  primaryClaimId: string;            
  claimStatus: string;                
  hours: number;                     
  units: number;                     
  perUnitCharge: number;             
  billedAmount: number;               
  insurancePayments: number;          
  adjustments: number;                
  patientResponsibilityAdjustments: number; 
  claimBalance: number;               
  invoiceNumber: string;              
  invoiceStatus: string;             
  patientResponsibility: number;     
  patientPayments: number;          
  patientBalance: number;            
}

export interface ClientChargeFilterModel {
    fromDate: Date;
    throughDate: Date;
    placeOfService?: number [] ;
    renderingProvider?: number [] ;
    authorizationNumber?: number [] ;
    primaryFunder?: number [] ;
    // skip?: number;   // for paging
    // take?: 20; 
}

export interface ClientHistoryChargeDetailsRequest {
    take?: number;
    skip?: number;
    clientId: number;
    sortingModels: SortDescriptor[];
}

export class ClientHistoryChargeDetailsRequestModel {

    ClientHistoryChargeFilterModel: ClientChargeFilterModel = {} as ClientChargeFilterModel;
    ClientHistoryChargeDetailsRequest: ClientHistoryChargeDetailsRequest = {} as ClientHistoryChargeDetailsRequest;
}


// Client Invoice History
export interface ClientInvoiceHistoryRequest {
    clientId: number;
    skip?: number; // default 0
    take?: number; // default 20
    sortingModels: SortDescriptor[];
}

export interface ClientInvoiceHistoryRequestFilterModel {
  accountInfoId: number | undefined;
  status: number[];
  patientResponsibilityFrom: number | undefined;
  patientResponsibilityTo: number | undefined;
  dateOfServiceFrom: Date | undefined;
  dateOfServiceTo: Date | undefined;
  invoiceDateFrom: Date | undefined;
  invoiceDateTo: Date | undefined;
  invoiceDueDateFrom: Date | undefined;
  invoiceDueDateTo: Date | undefined;
  patientBalanceFrom: number | undefined;
  patientBalanceTo: number | undefined;
}

export class ClientInvoiceHistoryRequestModel {
  InvoiceHistoryRequest: ClientInvoiceHistoryRequest = {} as ClientInvoiceHistoryRequest;
  InvoiceHistoryRequestFilterModel: ClientInvoiceHistoryRequestFilterModel = {} as ClientInvoiceHistoryRequestFilterModel;
}

export interface ClientInvoiceHistoryResponse {
    data: ClientInvoiceHistoryDetails[];
    totalCount: number;
}

export class ClientInvoiceHistoryDetails {
  id!: number;
  clientId!: number;
  billingCode!: string;
  dateOfService!: string;
  billedAmount!: number;
  adjustments!: number;
  adjustmentsPR!: number;
  insurancePayments!: number;
  patientPayments!: number;
  patientBalance!: number;
  invoiceNumber!: string;
  invoiceDate!: string;
  paymentDue!: string;
  status!: string;
  placeOfService!: string | null;
  renderingProvider!: string | null;
}
