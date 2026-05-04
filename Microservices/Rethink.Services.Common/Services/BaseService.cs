
using AutoMapper;
using Rethink.Services.Common.Entities.Base;
using Rethink.Services.Common.Enums.BH;
using Rethink.Services.Common.Enums.Billing;
using Rethink.Services.Common.Extensions;
using Rethink.Services.Common.Handlers;
using Rethink.Services.Common.Models.Claim;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Rethink.Services.Common.Services
{
    [ExcludeFromCodeCoverage]
    public abstract class BaseService
    {
        public IMapper Mapper { get; set; }

        public const string prAdjustmentGroupCode = "PR";

        public DateTime UtcDateTime => DateTime.UtcNow;
        public DateTime EstDateTime => GetEasternDateTime(null);

        public static DateTime GetEasternDateTime(DateTime? dateTimeEntry)
        {
            return DateTimeExt.GetEasternDateTime(dateTimeEntry);
        }

        public static int GetEasternDateTimeUtcOffset(DateTime? dateTimeEntry)
        {
            return DateTimeExt.GetEasternDateTimeUtcOffsetHours(dateTimeEntry);
        }

        public void MarkCreated<T>(T entity, int memberId)
            where T : class, IAuditedEntity
        {
            entity.CreatedBy = memberId;
            entity.DateCreated = EstDateTime;
            entity.ModifiedBy = memberId;
            entity.DateLastModified = (DateTime?)EstDateTime;
        }

        public void MarkUpdated<T>(T entity, int memberId)
            where T : class, IAuditedEntity
        {
            entity.ModifiedBy = memberId;
            entity.DateLastModified = EstDateTime;
        }

        public void SoftDelete<T>(T entity, int memberId)
            where T : class, IAuditedEntity
        {
            entity.ModifiedBy = memberId;
            entity.DateLastModified = EstDateTime;
            entity.DateDeleted = EstDateTime;
            entity.DeletedBy = memberId;
        }

        public void Restore<T>(T entity, int memberId)
            where T : class, IAuditedEntity
        {
            if (entity.DateDeleted != null)
            {
                entity.DateDeleted = null;
                entity.DateLastModified = EstDateTime;
                entity.ModifiedBy = memberId;
            }
        }

        protected void CopyValues<T>(T target, T source)
        {
            Type t = typeof(T);

            var properties = t.GetProperties().Where(prop => prop.CanRead && prop.CanWrite);

            foreach (var prop in properties)
            {
                var value = prop.GetValue(source, null);
                if (value != null)
                    prop.SetValue(target, value, null);
            }
        }

        public static int FindClaimTransactionTypeId(PaymentTypes claimPaymentType)
        {
            ClaimTransactionType transactionType;
            switch (claimPaymentType)
            {
                case PaymentTypes.InsurancePayment:
                    transactionType = ClaimTransactionType.insurancePayment;
                    break;
                case PaymentTypes.ClientPayment:
                    transactionType = ClaimTransactionType.patientPayment;
                    break;
                case PaymentTypes.OtherPayment:
                    transactionType = ClaimTransactionType.otherPayment;
                    break;
                default:
                    transactionType = ClaimTransactionType.insurancePayment;//Default
                    break;
            }
            return (int)transactionType;
        }

        public static int FindPaymentTypeId(ClaimTransactionType claimPaymentType)
        {
            PaymentTypes paymentType;
            switch (claimPaymentType)
            {
                case ClaimTransactionType.insurancePayment:
                    paymentType = PaymentTypes.InsurancePayment;
                    break;
                case ClaimTransactionType.patientPayment:
                    paymentType = PaymentTypes.ClientPayment;
                    break;
                case ClaimTransactionType.otherPayment:
                    paymentType = PaymentTypes.OtherPayment;
                    break;
                case ClaimTransactionType.eraReceived:
                    paymentType = PaymentTypes.ERAReceived;
                    break;
                default:
                    paymentType = PaymentTypes.InsurancePayment;//Default
                    break;
            }
            return (int)paymentType;
        }

        public static bool IsAdjustmentTypePR(ClaimTransactionType adjustmentType)
        {
            return (adjustmentType == ClaimTransactionType.patientResponsibility);
        }
        public static decimal CalculateOverallAdjustment(List<Tuple<bool?, decimal?>> adjustmentAmountList)
        {
            decimal? positiveAdjustmentAmount = 0;
            decimal? negativeAdjustmentAmount = 0;

            foreach (var adjustment in adjustmentAmountList)
            {
                positiveAdjustmentAmount += adjustment.Item1 == true ? adjustment.Item2 : 0;
                negativeAdjustmentAmount += adjustment.Item1 != true ? adjustment.Item2 : 0;
            }

            return (positiveAdjustmentAmount - negativeAdjustmentAmount) ?? 0;
        }

        public static ClaimTransactionModel PrepareClaimTransaction(int transactionTypeId, ClaimTransactionType transactionType)
        {
            return new ClaimTransactionModel
            {
                TransactionTypeId = transactionTypeId,
                TransactionType = (int)transactionType,
            };
        }

        public AppointmentBillingStatus PrepareAppointmentBillingStatus(int appointmentId, RethinkBillingStatus billingStatus) => new()
        {
            AppointmentId = appointmentId,
            BillingStatus = billingStatus,
            ModifiedDate = EstDateTime
        };

        public DateTime ConvertToCompleteDate(DateTime? date)
        {
            var fromDate = (DateTime)date;
            return new DateTime(fromDate.Year, fromDate.Month, fromDate.Day, 0, 0, 0);
        }
        
        public string GetEnumDescription(PatientInvoiceStatus status)
        {
            var type = typeof(PatientInvoiceStatus);
            var memInfo = type.GetMember(status.ToString());
            var attributes = memInfo[0].GetCustomAttributes(typeof(DescriptionAttribute), false);
            return attributes.Length > 0 ? ((DescriptionAttribute)attributes[0]).Description : status.ToString();
        }
    }
}
