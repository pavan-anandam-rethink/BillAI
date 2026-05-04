import { ServiceLine } from ".";

export interface CreateDiagnosisCode {
	id?: number;
	clientId: number;
	diagnosisCodeId: number;
	existingDiagnosisCodeId?: number;
	diagnosisLUCode?: string;
	diagnosisLUDescription?: string;
	serviceLinesList: ServiceLine[];
	startDate: Date;
	endDate?: Date;
	status?: string;
	editableDescription?: boolean;
	isCustom?: boolean;
	serviceLineName?: string;
	isExist?: boolean;
	hasAuthorization?: boolean;
}