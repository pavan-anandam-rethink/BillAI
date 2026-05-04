export interface RevSpringPayloadRequestModel {
  AccountInfoId: number;
  MemberId: number;
  ClientId: number;
  AmountDue: number;
  UserEmail: string;
  UserLastName: string;
  ReferenceNo: string | null;
}
