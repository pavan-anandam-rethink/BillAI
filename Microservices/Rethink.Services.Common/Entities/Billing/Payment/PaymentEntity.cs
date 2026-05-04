using Rethink.Services.Common.Entities.Base;
using Rethink.Services.Common.Enums.Billing;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace Rethink.Services.Common.Entities.Billing.Payment
{
    public partial class PaymentEntity : BasePersistEntity, IAuditedEntity
    {
        public PaymentEntity()
        {
            PaymentClaims = new HashSet<PaymentClaimEntity>();
        }

        public int? AccountInfoId { get; set; }
        [Column("hcPaymentEraUploadId")]
        public int? PaymentEraUploadId { get; set; }
        [Column("ReceivedDate", TypeName = "datetime")]
        public DateTime? ReceivedDate { get; set; }
        public string InterchangeControlNumber { get; set; }
        public bool RequestsAck { get; set; }
        public bool IsTestData { get; set; }
        public string TransactionHandlingCode { get; set; }
        public decimal PaymentAmount { get; set; }
        public decimal PaymentAmountOrig { get; set; }
        public string CreditOrDebit { get; set; }
        public string EraPaymentMethod { get; set; }
        public string PaymentIdentifier { get; set; }
        [Column("hcPaymentMethodId")]
        public int PaymentMethodId { get; set; }
        //[NotMapped]
        //public PaymentMethods PaymentMethod { get; set; }
        [Column("hcPaymentTypeId")]
        public int PaymentTypeId { get; set; }
        public DateTime? PaymentDate { get; set; }
        public DateTime? DepositDate { get; set; }
        public DateTime? PostDate { get; set; }
        public PaymentStatus Status { get; set; }
        public bool HasAcknowledgedErrors { get; set; }
        public bool IsManualPayment { get; set; }
        public bool IsManualReconciled { get; set; }
        public string ReferenceNumber { get; set; }
        [Column("hcFunderId")]
        public int? HcFunderId { get; set; }
        public string FunderTaxID { get; set; }
        public string FunderName { get; set; }
        public string FunderID { get; set; }
        public string FunderContactName { get; set; }
        public string FunderContactType { get; set; }
        public string FunderContactInfo { get; set; }
        public string FunderBankRoutingQualifier { get; set; }
        public string FunderBankRouting { get; set; }
        public string FunderBankAccount { get; set; }

        public string Payee { get; set; }
        public string PayeeIdType { get; set; }
        public string PayeeId { get; set; }
        public string PayeeBankRoutingQualifier { get; set; }
        public string PayeeBankRouting { get; set; }
        public string PayeeBankAccountQualifier { get; set; }
        public string PayeeBankAccount { get; set; }
        public string PayeeTaxId { get; set; }
        public string PayeeAddress1 { get; set; }
        public string PayeeAddress2 { get; set; }
        public string PayeeAddressCity { get; set; }
        public string PayeeAddressState { get; set; }
        public string PayeeAddressZip { get; set; }
        public string PayeeAddressCountry { get; set; }

        public int TransactionNumber { get; set; }
        public int TransactionCount { get; set; }
        public string EraDocumentEdi { get; set; }
        public string TransactionXml { get; set; }

        public int CreatedBy { get; set; }
        public int? ModifiedBy { get; set; }
        public int? DeletedBy { get; set; }
        public DateTime DateCreated { get; set; }
        public DateTime? DateLastModified { get; set; }
        public DateTime? DateDeleted { get; set; }

        public virtual PaymentEraUploadEntity PaymentEraUpload { get; set; }
        public virtual ICollection<PaymentClaimEntity> PaymentClaims { get; set; }
        public virtual ICollection<PaymentErrorEntity> PaymentErrors { get; set; }
        public virtual ICollection<PaymentNoteEntity> Notes { get; set; }
        public virtual PaymentMethodEntity PaymentMethodEntity { get; set; }
        public virtual PaymentTypeEntity PaymentTypeEntity { get; set; }
        public virtual ICollection<UnAllocatedPaymentEntity> UnallocatedPayments { get; set; }

        //public virtual ICollection<UpdatePaymentServiceLineAmountsModelWithUserInfo> modelWithUserInfo { get; set; }

        // not in DB
        public bool IsErrorPayment { get; set; }
    }
}