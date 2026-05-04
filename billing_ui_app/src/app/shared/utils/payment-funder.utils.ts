

export function filterPaymentMethodByFunderType(methodValue: string, paymentFunderTypeId: number, isEdit: boolean = false): boolean {
    const paymentMethodTypesInsuranceCreate = ['Credit Card', 'ACH', 'Check', 'ERA'];
    const paymentMethodTypesInsuranceEdit = ['Credit Card', 'ACH', 'Check'];
    const paymentMethodTypesPatient = ['Credit Card', 'Cash', 'Check', 'FSA/HSA'];
    const paymentMethodTypesOther = ['Credit Card', 'ACH', 'Check'];

    switch (paymentFunderTypeId) {
        case 1: // Insurance
            return (isEdit ? paymentMethodTypesInsuranceEdit : paymentMethodTypesInsuranceCreate).includes(methodValue);
        case 3: // Patient
            return paymentMethodTypesPatient.includes(methodValue);
        case 4: // Other
            return paymentMethodTypesOther.includes(methodValue);
        default: return true;
    }
}