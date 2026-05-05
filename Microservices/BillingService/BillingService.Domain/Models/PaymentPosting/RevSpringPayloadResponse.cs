using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BillingService.Domain.Models.PaymentPosting
{
    public class RevSpringPayloadResponse
    {
        public RevSpringPayload Payload { get; set; }
    }
    public class RevSpringPayload
    {
        public string ConsumerNumber { get; set; }
        public string ExternalUsername { get; set; }
        public string UserEmail { get; set; }
        public string UserLastName { get; set; }
        public string RoleName { get; set; }
        public string AccessLevel { get; set; }
        public string PatientFirstName { get; set; } = string.Empty;
        public string PatientLastName { get; set; } = string.Empty;
        public string OrgSiteName { get; set; }
        public string PatientId { get; set; }

        public int AccountId { get; set; }
        public int MemberId { get; set; }
        public string ReferenceNo { get; set; }
        public RevSpringDataContext DataContext { get; set; }
    }
    public class RevSpringConsumer
    {
        public string ConsumerNumber { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }

        public string Address1 { get; set; } = string.Empty;
        public string Address2 { get; set; } = string.Empty;
        public string Country { get; set; }
        public string City { get; set; }
        public string State { get; set; }
        public string Zip { get; set; }

        public string Phone { get; set; }
        public string Email { get; set; }

        public string? DateOfBirth { get; set; }

        public decimal AmountDue { get; set; }
    }

    public class RevSpringDataContext
    {
        public RevSpringConsumer Consumer { get; set; }
    }




}
