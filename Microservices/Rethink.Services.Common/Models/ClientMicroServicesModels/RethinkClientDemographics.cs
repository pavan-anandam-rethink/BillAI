using System;

namespace Rethink.Services.Common.Models.Clients
{
    public class RethinkClientDemographics
    {
        public int Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string MiddleName { get; set; }
        public int GenderId { get; set; }
        public string Gender { get; set; }
        public DateTime DOB { get; set; }
        public string Address { get; set; }
        public string Address2 { get; set; }
        public string City { get; set; }
        public string Town { get; set; }
        public int? StateId { get; set; }
        public string StateName { get; set; }
        public int? CountryId { get; set; }
        public string CountryName { get; set; }
        public string ZipCode { get; set; }
        public string UCI { get; set; }
        public string ReasonForReferral { get; set; }
        public int FacilityId { get; set; }
        public string PhotoLocation { get; set; }
        public string Location { get; set; }
        public int StatusId { get; set; }
        public string Status { get; set; }
        public string Notes { get; set; }
        public bool ShowClinical { get; set; }
        public bool ShowScheduling { get; set; }
        public bool ShowBilling { get; set; }
        public int ServiceIntensityTypeId { get; set; }
        public string ServiceIntensityType { get; set; }
        public bool CanChangeServiceIntensityType { get; set; }
        public string ClientId { get; set; }
        public DateTime? HipaaNopAgreementDate { get; set; }
        public bool? HipaaAgreementCompleted { get; set; }
        public int? ClientOrder { get; set; }
        public DateTime? DateDeleted { get; set; }

        public string DOBString { get; set; }
        public string HipaaNopAgreementDateString { get; set; }
        public int? KareoPatientId { get; set; }

        public string EnrollmentNumber { get; set; }
        public bool? ConsentToAccess { get; set; }

        public DateTime? ConsentToAccessDate { get; set; }

        public string ConsentToAccessString { get; set; }

        //public List<ClientCustomFieldData> CustomFields { get; set; } = new List<ClientCustomFieldData>();

        public static string CalculateAge(DateTime DOB)
        {
            DateTime today = DateTime.Today;

            int months = today.Month - DOB.Month;
            int years = today.Year - DOB.Year;

            if (today.Day < DOB.Day)
            {
                months--;
            }

            if (months < 0)
            {
                years--;
                months += 12;
            }

            int days = (today - DOB.AddMonths(years * 12 + months)).Days;

            return string.Format("{0}y {1}m {2}d",
                                 years,
                                 months,
                                 days);

        }

        //public PropagatingAppointmentData PropagatingData { get; set; }

        public string FullName => FirstName + " " + LastName;

        public string Age => CalculateAge(DOB);
    }
}
