export interface Adjustment {
    id: number;
    amount: number;
    isPositive:boolean;
    groupCode: string;
    reasonCode: string;
    description: string;
    paymentIdentifier: string;
    postDate: Date;
    paymentId:number;
    reasonCodeKey:string;
}