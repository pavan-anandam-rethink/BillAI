export interface Payment {
    id: number;
    date: Date;
    cptCode: string;
    reasonCode: string;
    paymentMethod: string;
    amount: number;
    reference: string;
    postedBy: string;
}
