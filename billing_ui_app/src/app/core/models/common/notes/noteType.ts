import { MemberType } from "../../company-account";

export interface NoteType
{
    id: number;
    companyAccountId: number;
    description: string;
    longDescription: string;
    isActive: boolean;
    memberType: MemberType;
}
