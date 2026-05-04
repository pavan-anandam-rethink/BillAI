export enum ClaimSubmissionStatus {
    Unknown = 0,
    ClearingHousePending = 1,
    ClearingHouseError = 2,
    ClearinghouseProcessing = 3,
    ClearinghouseRejected = 4,
    FunderRejected = 5,
    FunderReceived = 6,
    FunderAccepted = 7,
    FunderSubmitted = 8,
    FunderPending = 9,
    FunderDenied = 10,
    FunderProcessed = 11,
    Abandoned = 99
}