export class TransactionDetail {
    constructor(public fieldName: string,
                public oldValue: string,
                public newValue: string)
    {
    }
}

export class Transaction
{
    public id: number;
    public oldValues: any | null;
    public newValues: any | null;
    public transactionOn: Date;
    public transactionBy: number;
    public action: string;
    public mode: string;
    public typeId: number;
    public referenceId: number;
    public referenceTypeId: number;
    public description: string;
    public transactionDetails: TransactionDetail[];

    constructor()
    {
        this.id = 0;
        this.oldValues = null;
        this.newValues = null;
        this.transactionOn = new Date();
        this.transactionBy = 0;
        this.action = "";
        this.mode = "";
        this.typeId = 0;
        this.referenceId = 0;
        this.referenceTypeId = 0;
        this.description = "";
        this.transactionDetails = [];
    }
}