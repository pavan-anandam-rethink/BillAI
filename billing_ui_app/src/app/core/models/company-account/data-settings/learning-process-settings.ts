import { MasteryCriteria } from './mastery-criteria';

export class LearningProcessSettings {
  action: string;
  collectionType: number;
  goalLevelLabel: number;
  id: number;
  isAutoSavePhaseChangeLine: boolean;
  showTargetNotes: boolean;
  masteryCriterias: MasteryCriteria[];
  prompts: Prompts[];
  targetLevelLabel: number;
  trackObjectives: boolean;
  isSkillAcquisitionAutomastery: boolean;
  isBehaviorReductionAutomastery: boolean;
}

export class Prompts {
  code: string;
  id?: number;
  isCorrect?: boolean;
  name: string;
  position: number;
}
