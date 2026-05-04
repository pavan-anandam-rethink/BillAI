export interface AssigneeModel {
    memberId: number;
    name: string;
}

export interface AssigneeRequestModel {
    claimIds: number[];
    assigneeId: number;
    memberId: number;
}