using BillingService.Domain.Interfaces.Billing;
using BillingService.Domain.Models.Patients;
using BillingService.Domain.Models.PaymentPosting;
using Rethink.Services.Common.Interfaces;
using Rethink.Services.Common.Utils;
using System.Linq;
using System.Threading.Tasks;

namespace BillingService.Domain.Services.Billing
{
    public class ChildProfileService : IChildProfileService
    {
        private readonly IRethinkMasterDataMicroServices _rethinkServices;

        public ChildProfileService(IRethinkMasterDataMicroServices rethinkServices)
        {
            _rethinkServices = rethinkServices;
        }

        public async Task<IQueryable<PatientsDropdownModel>> GetAccountPatinetsByNameAsync(PersonSearchModel personSearchModel)
        {
            var patient = await _rethinkServices.GetChildProfile(personSearchModel.AccountInfoId);

            var patients = patient.AsQueryable().Where(x => x.Name.ToLower().Contains(personSearchModel.PersonName.ToLower()));

            var patientList = patients.Select(cp => new PatientsDropdownModel
            {
                Id = cp.Id,
                PatientName = cp.Name
            })
                .OrderBy(x => x.PatientName);

            var test = patientList.ToList();

            return patientList;

        }
    }
}
