export interface MetaData {
    [key: string]: any;
}

export interface ClaimFilterOptionModel {
    id: number;
    name: string;
    typeId?: number;
    checked: boolean;
    staffMemberId ?: number;
}

export interface ClaimFilterRangeOption {
    valueFrom: number | undefined;
    valueTo: number | undefined;
}

export interface ClientHistoryUserInfo extends ClaimFilterOptionModel {
    clientId: number;
}

export interface ClaimEdiFilesModel {
    ClaimId?: number,
    ClaimSubmissionId?: number,
    PaymentId?: number,
    FileType?: string,
    AccountInfoId?: number,
    MemberId?: number,
    BatchId?: string | null
}


export interface GuarantorName {
    fullName?: string;
    firstName?: string;
    lastName?: string;
}

export interface ClientGuarantorInfo {
    id: number;
    userId: number;
    userType: string;
    name: GuarantorName;
    address: string;
    email: string;
    phoneNumber?: string;
    relationToClient?: string;
    relationshipToInsured?: string;
    timezoneId?: number;
    isPrimaryContact?: boolean;
    isGuarantor?: boolean;
    genderId?: number;
    maritalStatusId?: number;
    dateOfBirth?: string;
    medicalRecordNumber?: string;
    insurancePolicyNumber?: string;
    accountId?: number;
    hasSystemLogin?: boolean;
    memberId?: string;
    identifiers?: any[];
    metaData?: MetaData;
}
