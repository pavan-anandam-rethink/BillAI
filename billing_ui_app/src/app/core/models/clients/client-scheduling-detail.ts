import { Availability } from './availability';

export interface ClientSchedulingDetail {
  languages: Language[];
  selectedLanguages: number[];
  genders: Gender[];
  selectedGenders: number[];
  hasAggressiveBehavior: boolean;
  isGenderChecked: boolean
  aggressiveBehaviorDisplay: string;
  gendersDisplay: string;
  languagesDisplay: string;
  selectedGenderName: string;
  availabilities: Availability[];
}

export interface Language {
  id: number
  name: string;
}

export interface Gender {
    id: number
    name: string;
}