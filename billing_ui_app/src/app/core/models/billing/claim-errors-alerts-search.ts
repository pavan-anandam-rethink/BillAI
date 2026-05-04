export interface ClaimErrorsAlertsSearch {
    name: ClaimErrorsAlertsListSearch[];
    totalCount: number;
}

export interface ClaimErrorsAlertsListSearch {
    name: string;
    checked: boolean;
}

export class ClaimErrorsAlertsListSearchBase {
    name = "";
    skip = 0;
    take = 10;

    constructor(takeNumber: number = 10) {
        this.take = takeNumber;
    }
}