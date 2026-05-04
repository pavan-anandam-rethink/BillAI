export enum AssignType {
    UnAssigned = 0,
    Assigned = 1,
    Substitute = 2
}

export class ClientAssigment {
    id: number;
    name: string;
    firstName: string;
    lastName: string;
    title: string;
    assignType: AssignType;
    facilities: number[];
    serviceLines: number[];
    memberId: number;
}

export class StaffAssigment {
    id: number;
    name: string;
    firstName: string;
    lastName: string;
    title: string;
    assignType: AssignType;
    locations: string[];
    serviceLines: string[];
    memberId: number;
}
