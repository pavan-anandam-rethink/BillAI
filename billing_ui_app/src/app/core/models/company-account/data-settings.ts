import { LearningProcessSettings } from './data-settings/learning-process-settings'
import { MasteryCriteria } from './data-settings/mastery-criteria';
import { Defaults } from './data-settings/defaults';

export class DataSettings {
  defaults: Defaults;
  learningProcessSettings: LearningProcessSettings;
  opportunityMasteryCriteria: MasteryCriteria;
  taskAnalysisMasteryCriteria: MasteryCriteria;
}