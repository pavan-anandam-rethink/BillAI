export interface ClaimHistorySearch {
    name: ClaimHistoryListSearch[];
    totalCount: number;
}

export interface ClaimHistoryListSearch {
    name: string;
    checked: boolean;
}

export class ClaimHistoryListSearchBase {
    name = "";
    skip = 0;
    take = 10;

    constructor(takeNumber: number = 10) {
        this.take = takeNumber;
    }
}