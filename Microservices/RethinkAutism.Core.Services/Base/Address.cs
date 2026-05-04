using Rethink.Services.Common.Models;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace RethinkAutism.Core.Services.Base
{
    public class Address
    {
        public Address() { }

        public Address(ClientAddress entity, Dictionary<int, string> states = null, Dictionary<int, string> countries = null)
        {
            if (entity == null)
                return;
            Street1 = entity.street1;
            Street2 = entity.street2;
            StateId = entity.stateId;
            CountryId = entity.countryId;
            Zip = entity.zip;
            Id = entity.Id;
            City = entity.city;
            Town = entity.town;
            if (states != null && StateId.HasValue && states.ContainsKey(StateId.Value))
                State = states[StateId.Value];

            // ContryLU is blank in some cases.         
            if (countries != null && CountryId.HasValue && countries.ContainsKey(CountryId.Value))
                Country = countries[CountryId.Value];
            else
                Country = entity.countryId != null ? entity.countryId.Name : string.Empty;
        }

        public Address(string addr)
        {
            this.ParseString(addr);
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

        /// <summary>
        /// Parse a basic unstructured address string
        /// format: street address, city, state zip
        /// - cannot handle address line 2 separated by comma, should be one line, ex. 123 Broadway Ave Suite 100
        /// - zip code can be provided with space or comma, ex. NY 10001 or NY, 10001
        /// </summary>
        /// <param name="input"></param>
        /// <returns>success</returns>
        public bool ParseString(string input)
        {
            var parts = input.Split(',');
            var success = true;

            //street address
            if (parts.Length >= 1)
                this.Street1 = parts[0].Trim();
            else
                success = false;

            //city
            if (parts.Length >= 2)
                this.City = parts[1].Trim();
            else
                success = false;

            //state
            if (parts.Length >= 3)
            {
                var temp = parts[2].Trim().Split(' ');
                if (temp.Length > 1 && temp.Last().Length >= 5 &&
                    int.TryParse(temp.Last().Substring(0, 5), out _))
                {
                    this.State = string.Join(" ", temp.Take(temp.Length - 1));
                    this.Zip = temp.Last().Trim();
                } 
                else 
                    this.State = parts[2].Trim();
            }
            else
                success = false;

            //zip
            if (parts.Length >= 4)
                this.Zip = parts[0].Trim();
            else if (string.IsNullOrEmpty(this.Zip))
                success = false;

            return success;
        }

        public AddressEntity GetEntity()
        {
            return new AddressEntity
            {
                Street1 = !string.IsNullOrEmpty(Street1) ? Street1 : "",
                Street2 = Street2,
                City = !string.IsNullOrEmpty(City) ? City : "",
                StateId = StateId == 0 ? null : StateId,
                CountryId = CountryId == 0 ? null : CountryId,
                Town = !string.IsNullOrEmpty(Town) ? Town : "",
                Zip = !string.IsNullOrEmpty(Zip) ? Zip : "",
                Id = Id
            };
        }
    }
}
