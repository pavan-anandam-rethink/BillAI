import { Injectable } from "@angular/core";
import { FormControl } from "@angular/forms";

@Injectable({
    providedIn: 'root'
})
export class ClientsFunderService {
	updateServiceLinesAvailability(control: FormControl, funder: any, formServiceLineTypes: any[], formUsedTypes: any[], startDate?: Date, endDate?: Date, uncheckBypass = false): void {
        funder.serviceLines.forEach((serviceLine: any) => {
            if (uncheckBypass) {
                serviceLine.bypassPrimary = false;
            }

            if (serviceLine.selectionError) {
                delete(serviceLine.selectionError);
            }
            
            let usedTypes = formUsedTypes.filter(ut => ut.id == serviceLine.id);
            if(usedTypes.length > 0){
                const serviceLineTypes = formServiceLineTypes.filter(slt => slt.serviceLineId == serviceLine.id);

                serviceLineTypes.forEach((slType) => {
                    let filteredTypes = usedTypes;
                    if (!!startDate) {
                        filteredTypes = filteredTypes.filter(ft => !ft.endDate || new Date(ft.endDate) >= startDate);
                    }

                    if (!!endDate) {
                        filteredTypes = filteredTypes.filter(ft => !ft.startDate || new Date(ft.startDate) <= endDate)
                    }

                    const result = filteredTypes.find(x => x.type == slType.optionName);

                    if (result) {
                        const selectedValue = control.value.find((x: any) => x.id === result.id && (result.type == x.responsibilitySequenceType
                            || result.type === x.responsibilitySequenceType.optionName));
                        if (selectedValue) {
                            serviceLine.selectionError = `Funder ${result.funderName} is covering
                            this service line with the same sequence you are trying
                             to set`;
                        }

                        slType.usedByFunder = result.funderName;
                        slType.usedType = result.type;
                    } else if (slType.usedByFunder) {
                        if (serviceLine.selectionError) {
                            delete(serviceLine.selectionError);
                        }
                        delete(slType.usedByFunder);
                        delete(slType.usedType);
                    }
                });
            }
        });
    }

    //check that SL is DPH on settings level
    DPHServiceLine(serviceLine: any, funderServiceLines: any): boolean {
        const selectedServiceLines = funderServiceLines;
        const isDPHServiceLineSelected = selectedServiceLines && selectedServiceLines.first((x: any) => x.name === serviceLine.name && serviceLine.isDph) != null;
        if (isDPHServiceLineSelected) {
            return true;
        }
        return false;
    }
}