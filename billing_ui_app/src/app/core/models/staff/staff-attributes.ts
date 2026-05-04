import { BasicOption } from '../common';
import { StaffDetails } from './staff-details';


export interface StaffAttributes {
    ageGroups: BasicOption[];
    experienceTypes: BasicOption[];
    genders: BasicOption[];
    languages: BasicOption[];
    staffDetails: StaffDetails;
}