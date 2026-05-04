import { SortDescriptor } from "@progress/kendo-data-query";
import { IdWithUserInfo } from "./get-claim-by-identifier";

export class AccountsReceivablesRequestModel{
    payerOrFunder:number[]=[];
    closingDate:Date;
    accountInfoId:number;
    skip: number;
    take: number;
    sortingModels: SortDescriptor[] = [];
}

export class paymentsAdjustmentsRequestModel
{
    funderId:number[] | null=[];
    RangeType:number;
    startDate:Date;
    endDate:Date;
    accountInfoId:number;
    sortingModels: SortDescriptor[] = [];
    skip: number;
    take: number;
}

export class claimFollowUpRequestModel {
  funderIds: number[] = [];
  FollowUpType: number;
  startDate: Date;
  endDate: Date;
  accountInfoId: number;
  sortingModels: SortDescriptor[] = [];
  skip: number;
  take: number;
}

export class AccountsReceivablesResponseModel
{
    accountsReceivables:AccountsReceivablesResponse[];
    totalCount:number;
}
export class AccountsReceivablesResponse
{
    id:number;
    funderName:string;
    clientId:number;
    clientFirstName:string;
    clientLastName:string;
    claimFrom:Date;
    claimThrough:Date;
    claimStatus:string;
    billedDate:Date;
    billedAmount:number;
    adjustments:number;
    adjustedClaimAmount:number;
    paymentsReceived:number;
    netReceivable:number;
    oneToThirty:number;
    thirtyOneToSixty:number;
    sixtyOneToNinty:number;
    nintyOneToOneHundredTwenty:number;
    moreThanOneHundredTwenty:number;
}
export class PaymentsAdjustmentsResponseModel
{
    paymentsAdjustments:PaymentsAdjustmentsResponse[];
    totalCount:number;
}
export class PaymentsAdjustmentsResponse
{
    id:number;
    funderName:string;
    clientId:number;
    clientFirstName:string;
    clientLastName:string;
    claimFrom:Date;
    claimThrough:Date;
    claimStatus:string;
    billedDate:Date;
    transactionType:string;
    reasonCode:string;
    remarkCode:number;
    transactionDate:Date;
    paymentOrAdjustmentDate:Date;
    eftOrCheckNumber:string;
    payment:number;
    adjustment:number;
}

export class ClaimFollowUpResponseModel {
  id: number;
  claimId: string;
  claimIdValue: number;
  memberId: number;
  clientFirst: string;
  clientLast: string;
  funderName: string;
  renderingProvider: string;
  placeOfService: string;
  dateOfService: string;
  claimFrom: string; 
  claimThrough: string; 
  authorization?: string;
  expectedAmount?: number;
  billedAmount?: number;
  paymentAmount?: number;
  adjustmentAmount?: number;
  balance?: number;
  billedDate?: string; // ISO date string or null
  claimStatus: string;
  note?: string;
  noteCreatedBy?: number;
  noteCreatedDate?: string; 
  followUpDate?: string; 
  followUpStatus?: string;
  dateCreated?: string; 
  dateModified?: string; 
  dateDeleted?: string; 
  noteCreatedByName?: string;
  totalCount: number;
}

export class FunderDetails
{
    funderId:number;
    funderName:string;
}

export class UnbilledAppointmentsRequestModel {
    payerOrFunder:number[]=[];
    clients:number[]=[];
    startDate:Date;
    endDate:Date;
    staff: number[]=[];
    location: number[]=[];
    placeOfService: number[]=[];
    skip: number;
    take: number;
    sortingModels: SortDescriptor[] = [];
    accountInfoId: number;
    memberId: number;
}

export class UnbilledAppointmentsResponseModel {
    appointmentModels: UnbilledAppointmentsResponse[];
    totalCount: number;
}

export class UnbilledAppointmentsResponse {
    id:number;
    clientId: number;
    clientName: string;
    funderId: number;   
    funderName: string;
    dateOfService: Date;
    startTime: string;
    endTime: string;
    staffName: string;
    serviceName: string;
    billingCode: string;
    locationName: string;
    placeOfService: string;
}


export class UnprocessedAppointmentsResponseModel
{
  id: number;
  clientId: number;
  clientName: string;
  funderId: number;
  funderName: string;
  dateOfService: Date;
  startTime: string;
  endTime: string;
  staffName: string;
  serviceName: string;
  billingCode: string;
  locationName: string;
  placeOfService: string;
  appointmentErrorMessage: string;
}



//Charge Request and Response Model 
export class AccountsReceivablesChargeLevelRequestModel{
    payerOrFunder:number[]=[];
    closingDate:Date;
    accountInfoId:number;
    skip: number;
    take: number;
    sortingModels: SortDescriptor[] = [];
}

export class AccountsReceivablesChargeLevelResponseModel
{
    accountsReceivables:AccountsReceivablesResponse[];
    totalCount:number;
}
export class AccountsReceivablesChargeLevelResponse
{
     id:number;
     funderName:string;
     clientId:number;
     clientFirstName:string;
     clientLastName:string;
     appointmentId: string; 
     billingProvider: string;
     renderingProvider: string;
     billingCode: number;
     dateOfService: Date;
     billedDate: Date;
     ageInDays: number; 
     expectedAmount: number;
     allowedAmount: number;
     billedAmount: number;
     adjustments: number;
     adjustedChargeAmount: number;
     patientPayments: number; 
     paymentsReceived: number; 
     netReceivable:number;
     oneToThirty:number;
     thirtyOneToSixty:number;
     sixtyOneToNinty:number;
     nintyOneToOneHundredTwenty:number;
     moreThanOneHundredTwenty:number;
}
export class FinancialSummaryRequestModel {
    
    startDate: Date;
    endDate: Date;
    dataRangeOption: string;
    location: number[]=[];
    funder: number[]=[];
    skip: number;
    take: number;
    
}
