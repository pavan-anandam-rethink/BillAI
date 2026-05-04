export interface StaffSignature {
    id: number,
    staffMemberId: number,
    staffSignatureId: string,
    staffSignature: string | null,
    dateCreated: Date
}
