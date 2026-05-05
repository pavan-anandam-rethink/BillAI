using AutoMapper;
using BillingService.Domain.DataObjects.Base;
using BillingService.Domain.DataObjects.Billing;
using BillingService.Domain.Interfaces.Billing;
using BillingService.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Rethink.Services.Common.Entities.Billing.Claim;
using Rethink.Services.Common.Entities.Billing.Payment;
using Rethink.Services.Common.Infrastructure.Context.Billing;
using Rethink.Services.Common.Infrastructure.Repository;
using Rethink.Services.Common.Interfaces;
using Rethink.Services.Common.Services;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BillingService.Domain.Services.Billing
{
    public class ChargePaymentService : BaseService, IChargePaymentService
    {
        private readonly IRepository<BillingDbContext, ClaimChargeEntryEntity> _chargeEntryRepository;
        private readonly IRepository<BillingDbContext, ClaimEntity> _claimRepository;
        private readonly IRepository<BillingDbContext, PaymentMethodEntity> _paymentMethodRepository;
        private readonly IRepository<BillingDbContext, ChargePaymentEntity> _chargePaymentRepository;
        private readonly IMapper _mapper;
        private readonly IRethinkMasterDataMicroServices _rethinkServices;

        public ChargePaymentService(IRepository<BillingDbContext, ClaimChargeEntryEntity> chargeEntryRepository,
            IRepository<BillingDbContext, ClaimEntity> claimRepository,
            IRepository<BillingDbContext, PaymentMethodEntity> paymentMethodRepository,
            IRepository<BillingDbContext, ChargePaymentEntity> chargePaymentRepository,
            IMapper mapper,
            IRethinkMasterDataMicroServices rethinkServices)
        {
            _chargeEntryRepository = chargeEntryRepository;
            _claimRepository = claimRepository;
            _mapper = mapper;
            _paymentMethodRepository = paymentMethodRepository;
            _chargePaymentRepository = chargePaymentRepository;
            _rethinkServices = rethinkServices;
        }

        public async Task<PaymentOptions> GetPaymentOptions(int claimId, int accountInfoId)
        {
            var result = new PaymentOptions
            {
                Charges = new List<BaseNameOption>(),
                Reasons = (await _rethinkServices.GetReasonCodes()).Select(rc => new BaseNameOption { Id = rc.id, Name = rc.name }).ToList(),
                PaymentMethods = (await _paymentMethodRepository.GetAllAsync()).Select(pm => new BaseNameOption { Id = pm.Id, Name = pm.Name }).ToList()
            };

            var claim = await _claimRepository.Query()
                .FirstOrDefaultAsync(e => e.Id == claimId && e.AccountInfoId == accountInfoId && e.DateDeleted == null);

            if (claim != null)
            {
                result.Charges = await _chargeEntryRepository.Query()
                    .Where(x => x.ClaimId == claimId && x.DateDeleted == null)
                    .Select(x => new BaseNameOption { Id = x.Id, Name = string.Format("{0} - {1:MM/dd/yyyy}", x.BillingCode, x.DateOfService) })
                    .ToListAsync();
            }

            return result;
        }

        public async Task<decimal> GetRemainingAmount(int chargeId, int accountInfoId)
        {
            var charge = await _chargeEntryRepository.Query()
                .FirstOrDefaultAsync(ce => ce.Id == chargeId && ce.Claim.AccountInfoId == accountInfoId && ce.DateDeleted == null);

            if (charge != null)
            {
                var payments = await _chargePaymentRepository
                    .Query()
                    .Where(cp => cp.ChargeId == chargeId && cp.DateDeleted == null)
                    .ToListAsync();
                var totalPaid = payments.Sum(p => p.Amount);

                return charge.Charges - totalPaid;
            }

            return 0;
        }

        public async Task<List<ChargePaymentItem>> GetForClaim(int claimId, int accountInfoId)
        {
            List<ChargePaymentEntity> result = new List<ChargePaymentEntity>();

            ClaimEntity claim = await _claimRepository.Query()
                .FirstOrDefaultAsync(e => e.Id == claimId && e.AccountInfoId == accountInfoId && e.DateDeleted == null);

            if (claim != null)
            {
                var e = await _chargePaymentRepository.Query()
                    .Include(x => x.ChargeEntry)
                    .ThenInclude(x => x.Claim)
                    .Include(x => x.PaymentMethod)
                    .Include(x => x.ReasonCode)
                    .Include(x => x.CreatedMember)
                    .ToListAsync();
                result = e.Where(x => x.ChargeEntry.Claim.Id == claimId && x.DateDeleted == null).ToList();
            }

            return _mapper.Map<List<ChargePaymentItem>>(result);
        }

        public async Task<ChargePaymentItem> Save(ChargePaymentItem item, int memberId)
        {
            var paymentEntity = new ChargePaymentEntity();
            var charge = await _chargeEntryRepository.Query().FirstOrDefaultAsync(e => e.Id == item.ChargeEntryId);
            if (charge != null)
            {
                item.UpdateEntity(paymentEntity);

                paymentEntity.CreatedBy = memberId;
                paymentEntity.DateCreated = EstDateTime;
                paymentEntity.ModifiedBy = memberId;
                paymentEntity.DateLastModified = EstDateTime;

                _chargePaymentRepository.Add(paymentEntity);
                await _chargeEntryRepository.CommitAsync();
            }

            return _mapper.Map<ChargePaymentItem>(paymentEntity);
        }

        public async Task<ChargePaymentItem> Delete(ChargePaymentItem item, int memberId)
        {
            ChargePaymentEntity updatedEntity = new ChargePaymentEntity();
            ClaimChargeEntryEntity chargeEntry = await _chargeEntryRepository.Query()
                .Include(c => c.ChargePayments)
                .FirstOrDefaultAsync(c => c.Id == item.ChargeEntryId);

            if (chargeEntry != null)
            {
                var chargePayment = chargeEntry.ChargePayments.FirstOrDefault(x => x.Id == item.Id);

                if (chargePayment != null && chargePayment.DateDeleted == null)
                {
                    chargePayment.ModifiedBy = memberId;
                    chargePayment.DateLastModified = EstDateTime;
                    chargePayment.DateDeleted = EstDateTime;

                    _chargePaymentRepository.Update(chargePayment);
                    await _chargePaymentRepository.CommitAsync();

                    updatedEntity = chargePayment;
                }
            }

            return _mapper.Map<ChargePaymentItem>(updatedEntity);
        }

        public async Task AddChargePaymentEntitesAsync(IEnumerable<ChargePaymentEntity> entites)
        {
            await _chargePaymentRepository.AddRangeAsync(entites);
        }
    }
}
