import { AccountMemberSubscription } from "./account-member-subscription";
import { AccountPermissions } from '@core/enums/account'

export interface AccountMemberSettings {
    subscription: AccountMemberSubscription;
    isEmployeeBenefit: boolean;
    isEducation: boolean;
    isHealthcare: boolean;
    permissionsList: AccountPermissions[];
    isAcceptTermsRequired: boolean;
    isAdmin: boolean;
    userName: string;
    isInternational: boolean;
    isParentUser: boolean;
    isDPH: boolean;
    requireAppointmentLocation: boolean;
    isHhax: boolean;
    fullUserName: string;
    role: string;
    id: number;
    isNextGen: boolean;
    allowToEnterLocation: boolean;
    sessionNoteDraftSaveTime: number;
}