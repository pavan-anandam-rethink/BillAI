using AutoMapper;
using BillingService.Domain.Interfaces.Billing;
using BillingService.Domain.Models.PaymentPosting;
using Microsoft.EntityFrameworkCore;
using Rethink.Services.Common.Entities.Billing.Payment;
using Rethink.Services.Common.Infrastructure.Context.Billing;
using Rethink.Services.Common.Infrastructure.Repository;
using System.Threading.Tasks;

namespace BillingService.Domain.Services.Payment
{
    public class PaymentMethodService : IPaymentMethodService
    {
        private readonly IRepository<BillingDbContext, PaymentMethodEntity> _paymentMethodRepository;
        private readonly IMapper _mapper;

        public PaymentMethodService(IRepository<BillingDbContext, PaymentMethodEntity> paymentMethodRepository, IMapper mapper)
        {
            _paymentMethodRepository = paymentMethodRepository;
            _mapper = mapper;
        }

        public async Task<PaymentMethodModel> GetPaymentMethodByName(string methodName)
        {
            var model = new PaymentMethodModel();
            var entity = await _paymentMethodRepository.Query().FirstOrDefaultAsync(x => x.Name == methodName);
            if (entity != null)
            {
                model = _mapper.Map<PaymentMethodModel>(entity);
            }

            return model;
        }

        public async Task<PaymentMethodModel> GetPaymentMethodById(int methodId)
        {
            var model = new PaymentMethodModel();
            var entity = await _paymentMethodRepository.Query().FirstOrDefaultAsync(x => x.Id == methodId);
            if (entity != null)
            {
                model = _mapper.Map<PaymentMethodModel>(entity);
            }

            return model;
        }
    }
}