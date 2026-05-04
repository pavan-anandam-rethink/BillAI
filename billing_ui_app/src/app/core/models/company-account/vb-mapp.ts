import { Country, State, } from '../common'

export class VbMappSettings {
  public countries: Country[];
  public countryId: number;
  public remainingLicenses: number;
  public states: State[];
  public totalLicenses: number;
}

export class VbMappModel {
  public countryId: number;
  public name: string;
  public firstName: string;
  public lastName: string;
  public cardType: string;
  public cardNum: string;
  public cardDate: string;
  public cardCode: string;
  public email: string;
  public address1: string;
  public address2: string;
  public apt: string;
  public city: string;
  public stateId: number;
  public zip: string;
  public qty: number;
}
