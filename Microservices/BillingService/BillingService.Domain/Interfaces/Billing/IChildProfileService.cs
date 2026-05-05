using BillingService.Domain.Models.Patients;
using BillingService.Domain.Models.PaymentPosting;
using System.Linq;
using System.Threading.Tasks;

namespace BillingService.Domain.Interfaces.Billing
{
    public interface IChildProfileService
    {
        Task<IQueryable<PatientsDropdownModel>> GetAccountPatinetsByNameAsync(PersonSearchModel personSearchModel);
        //Task<ChildProfileEntity> GetChildProfileById(int childProfileId);
    }
}
