export interface RequestResult {
    success: boolean;
    message?: string;
    hasError? : boolean;
    errorMessage? : string;
}
