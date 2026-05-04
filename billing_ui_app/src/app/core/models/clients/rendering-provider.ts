import { ProviderBaseModel } from "./provider-base-model";

export interface ClientRenderingProvider extends ProviderBaseModel {
    name: string;
    staffMemberId: number;
}