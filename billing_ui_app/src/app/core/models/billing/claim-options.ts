import { BasicOption } from '@core/models/common';


export class ClaimOptions {
    public clients: BasicOption[] = [];
    public locations: BasicOption[] = [];
    public members: BasicOption[] = [];
    public locationCodes: BasicOption[] = [];
    public claimIds: number[] = [];
    public unitTypes: BasicOption[] = [];
    public renderingProviders: BasicOption[] = [];
    public referringProviders: BasicOption[] = [];
    public serviceFacilities: BasicOption[] = [];
    public billingProviders: BasicOption[] = [];
}