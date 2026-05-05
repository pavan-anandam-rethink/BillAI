using BillingService.Domain.Interfaces.Payment;
using BillingService.Domain.Models;
using BillingService.Domain.Models.PaymentPosting;
using Microsoft.EntityFrameworkCore;
using Rethink.Services.Common.Entities.Billing.Payment;
using Rethink.Services.Common.Infrastructure.Context.Billing;
using Rethink.Services.Common.Infrastructure.Repository;
using Rethink.Services.Common.Interfaces;
using Rethink.Services.Common.Services;
using Rethink.Services.Common.Utils;
using Rethink.Services.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace BillingService.Domain.Services.Payment
{
    public class PaymentAttachmentService : BaseService, IPaymentAttachmentService
    {
        private readonly IBlobProcessingService _blobProcessingService;
        private readonly IRepository<BillingDbContext, PaymentAttachmentEntity> _paymentAttachmentRepository;
        private readonly IRethinkMasterDataMicroServices _rethinkServices;

        public PaymentAttachmentService(IBlobProcessingService blobProcessingService,
            IRepository<BillingDbContext, PaymentAttachmentEntity> paymentAttachmentRepository,
            IRethinkMasterDataMicroServices rethinkServices)
        {
            _blobProcessingService = blobProcessingService;
            _paymentAttachmentRepository = paymentAttachmentRepository;
            _rethinkServices = rethinkServices;
        }

        public async Task<int> UploadFile(PaymentUploadModelWithUserInfo model)
        {
            var guid = Guid.NewGuid().ToString();
            var filePath = $"paymentattachment/{guid}";

            var existingAttachment = await _paymentAttachmentRepository.Query().Where(x => x.PaymentId == model.PaymentId && x.FileName == model.FileName && x.DateDeleted == null).ToListAsync();
            if (!existingAttachment.Any())
            {

                var paymentAttachmentEntity = new PaymentAttachmentEntity
                {
                    CreatedBy = model.MemberId,
                    FileName = model.FileName,
                    FilePath = filePath,
                    FileSize = model.Data.Length,
                    PaymentId = model.PaymentId,
                    FileMimeType = model.FileMimeType,
                    BlobFileName = guid
                };

                MarkCreated(paymentAttachmentEntity, model.MemberId);

                var payment = await _paymentAttachmentRepository.AddAndGetAsync(paymentAttachmentEntity);

                await _blobProcessingService.UploadIntoContainerAsync("paymentattachment", guid, new MemoryStream(model.Data));

                return payment.Id;
            }
            return 0;
        }

        public async Task DeleteUpload(IdWithUserInfo model)
        {
            var attachment = await _paymentAttachmentRepository.GetByIdAsync(model.Id);

            if (attachment.CreatedBy != model.MemberId)
            {
                throw new UnauthorizedAccessException("User does not own this attachment");
            }

            var splittedPath = attachment.FilePath.Split('/');

            await _blobProcessingService.DeleteBlobFromContainerAsync(splittedPath[0], splittedPath[1]);

            SoftDelete(attachment, model.MemberId);

            await _paymentAttachmentRepository.CommitAsync();
        }

        public async Task DeleteUploads(DeleteAttachmentsModelWithUserInfo model)
        {
            var attachments = await (await _paymentAttachmentRepository
                    .GetAllAsync(x => model.Ids.Contains(x.Id)))
                .ToListAsync();

            foreach (var attachment in attachments)
            {
                if (attachment.CreatedBy != model.MemberId)
                {
                    continue;
                    // UnauthorizedAccessException("User does not own this attachment");
                }

                var splittedPath = attachment.FilePath.Split('/');
                await _blobProcessingService.DownloadBlobFromContainerAsync(splittedPath[0], splittedPath[1]);


                SoftDelete(attachment, model.MemberId);
            }

            await _paymentAttachmentRepository.CommitAsync();
        }

        public async Task<PaymentAttachmentReturnModel> GetUpload(int id)
        {
            var attachment = await _paymentAttachmentRepository.GetByIdAsync(id);

            var splittedPath = attachment.FilePath.Split('/');
            var memoryStream =
                await _blobProcessingService.DownloadBlobFromContainerAsync(splittedPath[0], splittedPath[1]);
            memoryStream.Seek(0, SeekOrigin.Begin);

            var attachmentReturnModel = new PaymentAttachmentReturnModel
            {
                MemoryStream = memoryStream,
                Filename = attachment.FileName
            };

            return attachmentReturnModel;
        }

        public async Task<AttachmentsResponseModel> GetPaymentAttachmentsAsync(
            GetByIdSortFilterWithUserInfo model)
        {
            var query = (await _paymentAttachmentRepository.GetAllAsync())
                .Where(x => x.PaymentId == model.Id && x.CreatedBy == model.MemberId && x.DateDeleted == null);

            var totalCount = await query.CountAsync();

            var memberUser = await _rethinkServices.GetMemberAsync(model.AccountInfoId, model.MemberId);
            var memberUsername = memberUser.userName;

            //because we always sent a single member id so we load only 1 member and sorting by the same username makes no sense
            if (model.SortingModels.Any(s => s.Field == "createdBy"))
            {
                var sortingModel = model.SortingModels.FirstOrDefault(x => x.Field == "createdBy");
                model.SortingModels.Remove(sortingModel);
            }

            var filteredQuery =  query
                .Select(x => new AttachmentViewModel()
                {
                    Filename = x.FileName,
                    Id = x.Id,
                    DateCreated = x.DateCreated
                })
                .OrderBy(model.SortingModels)
                .Skip(model.Skip);

            if (model.Take > 0)
            {
                filteredQuery = filteredQuery.Take(model.Take);
            }

            var result = await filteredQuery.ToListAsync();

            result.ForEach(x => x.CreatedBy = memberUsername);

            return new AttachmentsResponseModel
            {
                Data = result,
                TotalCount = totalCount
            };
        }

        public async Task RenameAttachmentAsync(RenameAttachmentModelWithUserInfo model)
        {

            var attachment = await _paymentAttachmentRepository.GetByIdAsync(model.AttachmentId);

            if (attachment.CreatedBy != model.MemberId)
            {
                throw new UnauthorizedAccessException("User does not own this attachment");
            }

            attachment.FileName = model.FileName;
            MarkUpdated(attachment, model.MemberId);
            await _paymentAttachmentRepository.CommitAsync();
        }
    }
}