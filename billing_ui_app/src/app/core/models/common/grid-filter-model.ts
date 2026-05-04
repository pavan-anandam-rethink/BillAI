export class GridFilterModel {
    constructor(public propertyName: string = '', public  operatorName: string = '', public value: string = '') {
    }
}

export class InsurancePaymentGridFilterModel {
    public ClientIds: string = '';
    public ClaimIdentifier: string = '';
    public PaidAmountFrom: number | undefined;
    public PaidAmountTo: number | undefined;
    public BalanceAmountFrom: number | undefined;
    public BalanceAmountTo: number | undefined;
    public ShowPaid: boolean | undefined;

    constructor(init?: Partial<InsurancePaymentGridFilterModel>) {
        Object.assign(this, init);
    }
}