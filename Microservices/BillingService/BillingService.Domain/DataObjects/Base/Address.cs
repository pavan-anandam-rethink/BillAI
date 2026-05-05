using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace BillingService.Domain.DataObjects.Base
{
    public class Address
    {
        public Address() { }

        public Address(AddressEntity entity, Dictionary<int, string> states = null, Dictionary<int, string> countries = null)
        {
            if (entity == null)
                return;
            Street1 = entity.Street1;
            Street2 = entity.Street2;
            StateId = entity.StateId;
            CountryId = entity.CountryId;
            Zip = entity.Zip;
            Id = entity.Id;
            City = entity.City;
            Town = entity.Town;
            if (states != null && StateId.HasValue && states.ContainsKey(StateId.Value))
                State = states[StateId.Value];
            else
                State = entity?.StateLU?.Abbreviation?? string.Empty;

            // ContryLU is blank in some cases.         
            if (countries != null && CountryId.HasValue && countries.ContainsKey(CountryId.Value))
                Country = countries[CountryId.Value];
            else
                Country = entity?.CountryLU?.Name ?? string.Empty;
        }

        public int Id { get; set; }
        [Required(ErrorMessage = "Please include all address information")]
        public string Street1 { get; set; }
        public string Street2 { get; set; }
        [Required(ErrorMessage = "Please include all address information")]
        public string City { get; set; }
        [Required(ErrorMessage = "Please include all address information")]
        public int? StateId { get; set; }
        [Required(ErrorMessage = "Invalid Zipcode")]
        public string Zip { get; set; }
        public int? CountryId { get; set; }
        public string Town { get; set; }
        public string State { get; set; }
        public string Country { get; set; }

        public AddressEntity GetEntity()
        {
            return new AddressEntity
            {
                // Street1 = !string.IsNullOrEmpty(Street1) ? Street1 : "",
                // Street2 = Street2,
                // City = !string.IsNullOrEmpty(City) ? City : "",
                // StateId = StateId == 0 ? null : StateId,
                // CountryId = CountryId == 0 ? null : CountryId,
                // Town = !string.IsNullOrEmpty(Town) ? Town : "",
                // Zip = !string.IsNullOrEmpty(Zip) ? Zip : "",
                // Id = Id
            };
        }

        public override string ToString()
        {
            return $"{Street1} {Street2} {City}, {State} {Zip}";
        }
    }
}
