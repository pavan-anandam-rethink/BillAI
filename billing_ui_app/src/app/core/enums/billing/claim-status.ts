export enum ClaimStatus {
    PendingReview = 1,
    Void = 2,
    Billed = 3,
    Pending = 4,
    Denied = 5,
    Closed = 6,
    ReadyToBill = 7,
    RejectedClearinghouse = 8,
    RejectedFunder = 9,
    Rebill = 10, 
    BillNextFunder = 11,
    Paid = 12,
}