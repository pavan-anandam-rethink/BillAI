import { UserInfo } from "./get-claim-by-identifier";

export interface PaymentPostingFunderSearch {
    funders: PaymentPostingListFunderSearch[];
    totalCount: number;
}

export interface PaymentPostingListFunderSearch{
    id: string;
    funderName: string;
    checked: boolean;
}

export class PaymentPostingListFunderSearchBase extends UserInfo {
    funderName = "";
    skip = 0;
    take = 10;
    constructor(takeNumber: number = 10) {
        super();
        this.take = takeNumber;
    }
}