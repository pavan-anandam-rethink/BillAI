using Rethink.Services.Common.Entities.Billing.Payment;
using System;

namespace BillingService.Domain.Models
{
    public class ChargePaymentItem
    {
        public int Id { get; set; }

        public int ChargeEntryId { get; set; }

        public DateTime Date { get; set; }

        public decimal Amount { get; set; }

        public string CPTCode { get; set; }

        public int ReasonCodeId { get; set; }

        public string ReasonCode { get; set; }

        public int PaymentMethodId { get; set; }

        public string PaymentMethod { get; set; }

        public string Reference { get; set; }

        public string PostedBy { get; set; }

        public void UpdateEntity(ChargePaymentEntity entity)
        {
            entity.ChargeId = ChargeEntryId;
            entity.Amount = Amount;
            entity.ReasonCodeId = ReasonCodeId;
            entity.PaymentMethodId = PaymentMethodId;
            entity.Reference = Reference;
        }
    }
}