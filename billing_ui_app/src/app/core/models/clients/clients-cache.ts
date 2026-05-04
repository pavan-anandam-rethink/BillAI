class BaseClientsCache {
    data: any = undefined;
    protected clientId = 0;

    protected invalid(clientId: number): boolean {
        return this.data === undefined || this.clientId !== clientId;
    }

    clear(): void {
        this.data = undefined;
    }
}

export class AuthGridCache extends BaseClientsCache{
    private includeInactive: boolean | undefined = undefined;

    override invalid(clientId: number, includeInactive?: boolean): boolean {
        const isInvalid = super.invalid(clientId) || this.includeInactive !== includeInactive;
        if (isInvalid) {
            this.clientId = clientId;
            this.includeInactive = includeInactive;
        }

        return isInvalid;
    }

    getLastIncludeInactive() {
        return this.includeInactive || false;
    }

    override clear(): void {
        super.clear();
        this.includeInactive = undefined;
    }
}

export class AuthEditInfoCache extends BaseClientsCache {
    override invalid(clientId: number): boolean {
        const isInvalid = super.invalid(clientId);
        if (isInvalid) {
            this.clientId = clientId;
        }

        return isInvalid;
    }
}
