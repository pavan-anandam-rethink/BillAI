export enum PatientInvoiceStatus {
  ReadyToInvoice = 1,
  InvoiceSent = 2,
  PartiallyPaid = 3,
  FullyPaid = 4
}

export const PatientInvoiceStatusLabel: Record<number, string> = {
  1: "Ready To Invoice",
  2: "Invoice Sent",
  3: "Partially Paid",
  4: "Fully Paid"
};