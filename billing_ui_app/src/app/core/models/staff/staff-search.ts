import { BasicOption } from '@core/models/common';
import { CustomFieldTypesEnum } from '../common/custom-fields';


export interface StaffSearch {
    funders: BasicOption[];
    facilities: BasicOption[];
    serviceLines: BasicOption[];
    staffStatus: BasicOption[];
    customFieldValues?: CustomFieldValue[];
}

export interface CustomFieldValue {
    id: number;
    type: number;
    values: string[];
    label: string;
}
