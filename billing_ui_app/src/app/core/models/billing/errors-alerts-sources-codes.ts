export class ClaimErrorsSources {
    errorsSources: string[] = [];
}

export class ClaimErrorsCodes {
    errorsCodes: ClaimErrorCode[];
}

export class ClaimErrorCode {
    name: string;
    checked: boolean;
}