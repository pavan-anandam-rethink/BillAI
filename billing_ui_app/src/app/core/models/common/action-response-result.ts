export interface ActionResponseResult<T = any> {
    success: boolean;
    error: string | null;
    data: T;
}