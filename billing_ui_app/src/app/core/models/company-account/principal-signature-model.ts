export interface PrincipalSignatureModel {
  id: number;
  accountInfoId: number;
  principalName: string;
  principalSignatureId: string;
  dateCreated?: string | Date;
  principalSignature: string | null;
}