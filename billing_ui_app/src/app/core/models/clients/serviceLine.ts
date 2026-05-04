export interface ServiceLine {
    id: number;
    name: string;
    isSelected?: boolean;
    responsibilitySequenceType?: string;
    bypassPrimary?: boolean;
    order?: number;
    usedByAuth?: boolean;
    usedByClaim?: boolean;
    disabled?: boolean;
}

export interface FunderServiceLine extends ServiceLine {
    isDph?: boolean;
    isDphOnClient?: boolean;
    isAutismCoveredBenefit?: boolean | null;
}