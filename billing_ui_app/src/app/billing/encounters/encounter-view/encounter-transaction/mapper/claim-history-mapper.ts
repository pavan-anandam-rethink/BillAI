import { groupBy, GroupResult } from '@progress/kendo-data-query';
import { ClaimHistory } from "@core/models/billing/claim-history";
import { ClaimStatus } from '@core/enums/billing/claim-status';
import { ClaimHistoryField } from '@core/enums/billing/claim-history-field';
import { ClaimAction } from '@core/enums/billing/claim-action';
import { ClaimActionMode } from '@core/enums/billing/claim-action-mode';

export interface ClaimHistoryActionModel {
    id: number;
    name: string;
    description: string;
}

export interface HistoryViewModel {
    changeDate: Date;
    changeBy: string;
    mode: string;
    description: string | null;
    action: string;
    rethinkUser: string | null;
    status: string | undefined;
    details: HistoryDetails[] | null;
    claimVersionHistoryId: number | null;
}

export interface HistoryDetails {
    fieldName: string | undefined;
    oldValue: string | undefined;
    newValue: string | undefined;
}

const newValuePlaceholder = '@newValue';
const oldValuePlaceholder = '@oldValue';
const claimIdentifierPlaceholder = 'Claim#';
const defaultIdPlaceholder = '#';
const appointmentPlaceholder = 'Appointment#';
const chargeEntryPlaceholder = 'Charge Entry#';

export class ClaimHistoryMapper {
    private readonly historyActions: ClaimHistoryActionModel[]
    private readonly claimIdentifier: string;

    constructor(claimIdentifier: string, historyActions: ClaimHistoryActionModel[]) {
        this.historyActions = historyActions || [];
        this.claimIdentifier = claimIdentifier;
    }

public map(serverModel: ClaimHistory[] | undefined): HistoryViewModel[] {
    const result: HistoryViewModel[] = [];

    if (serverModel) {
        const sortedServerModel = [...serverModel].sort((a, b) =>
            new Date(b.changeDate).getTime() - new Date(a.changeDate).getTime()
        );

        sortedServerModel.forEach((item) => {
            const mappedData: HistoryViewModel = {
                changeDate: new Date(item.changeDate),
                changeBy: item.changeBy,
                rethinkUser: item.rethinkUser ?? null,
                mode: ClaimActionMode[item.mode],
                description: this.mapActionDescription(item),
                action: ClaimAction[item.actionId],
                status: item.status && ClaimStatus[item.status],
                details: this.mapHistoryDetails([item]),
                claimVersionHistoryId: item.claimVersionHistoryId,
            };

            result.push(mappedData);
        });
    }

    return result;
}




    private mapHistoryDetails(serverModel: ClaimHistory[] | null): HistoryDetails[] | null {
        return serverModel && serverModel.any((x) => !!x.fieldId) ? serverModel.map<HistoryDetails>((x) => ({
            fieldName: x.fieldId && ClaimHistoryField[x.fieldId],
            oldValue: x.oldValue,
            newValue: x.newValue,
        })) :
        null;
    }

    private mapActionDescription(historyItem: ClaimHistory) {
        const historyAction = this.historyActions.find((x) => x.id == historyItem.historyActionId);
        const historyActionDescription = historyAction && historyAction.description;
        const actionDescription = this.mapActionPlaceholders(historyActionDescription!, historyItem.oldValue, historyItem.newValue);

        return actionDescription;
    }

    private mapActionPlaceholders(template: string, oldValue: string | undefined, newValue: string | undefined) {
        const hasClaimIdentifier = template.includes(claimIdentifierPlaceholder);
        if (hasClaimIdentifier) {
            const claimIdentifierReplacment = `${claimIdentifierPlaceholder}${this.claimIdentifier}`

            template = this.replace(template, claimIdentifierPlaceholder, claimIdentifierReplacment);
        }

        const hasDefaultIdentifier = !hasClaimIdentifier && template.includes(defaultIdPlaceholder);
        if (hasDefaultIdentifier) {
            // this is done to show generic description in history tab. this can be update later.
            if (template.includes(appointmentPlaceholder) || template.includes(chargeEntryPlaceholder)) {
                template = this.replace(template, defaultIdPlaceholder, '');
            } else {
                template = this.replace(template, defaultIdPlaceholder, `${defaultIdPlaceholder}${newValue}`);
            }
        }

        if (newValue) template = this.replace(template, newValuePlaceholder, newValue);
        if (oldValue) template = this.replace(template, oldValuePlaceholder, oldValue);

        return template;
    }

    private replace(text: string, search: string, byValue: string): string {
		return text && text.split(search).join(byValue || '');
	}
}