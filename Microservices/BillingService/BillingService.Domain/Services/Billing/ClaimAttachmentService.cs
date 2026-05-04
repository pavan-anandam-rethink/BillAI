using AutoMapper;
using BillingService.Domain.Interfaces.Billing;
using BillingService.Domain.Interfaces.Files;
using BillingService.Domain.Models;
using BillingService.Domain.Models.Claims;
using BillingService.Domain.Models.PaymentPosting;
using Microsoft.EntityFrameworkCore;
using Rethink.Services.Common.Entities.Billing.Claim;
using Rethink.Services.Common.Infrastructure.Context.Billing;
using Rethink.Services.Common.Infrastructure.Repository;
using Rethink.Services.Common.Interfaces;
using Rethink.Services.Common.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace BillingService.Domain.Services.Billing
{
   public class ClaimAttachmentService : BaseService, IClaimAttachmentService
    {
        private readonly IRepository<BillingDbContext, ClaimAttachmentEntity> _claimAttachmentRepository;
        private readonly IRepository<BillingDbContext, ClaimEntity> _claimRepository;

        private readonly IFileManagerService _fileManager;
        private readonly IFileService _fileService;
        private readonly IMapper _mapper;
        private readonly IRethinkMasterDataMicroServices _rethinkMasterDataMicroServices;

        public ClaimAttachmentService(
            IRepository<BillingDbContext, ClaimAttachmentEntity> claimAttachmentRepository,
            IRepository<BillingDbContext, ClaimEntity> claimRepository,
            IMapper mapper, IFileManagerService fileManager, IFileService fileService,
            IRethinkMasterDataMicroServices rethinkMasterDataMicroServices
                )
        {
            _rethinkMasterDataMicroServices = rethinkMasterDataMicroServices;
            _claimAttachmentRepository = claimAttachmentRepository;
            _claimRepository = claimRepository;
            _mapper = mapper;
            _fileManager = fileManager;
            _fileService = fileService;
        }

        public async Task<AttachmentsResponseModel> GetForClaimAsync(IdWithUserInfo model)
        {
            var query = (await _claimAttachmentRepository.GetAllAsync())
                .Where(x => x.ClaimId == model.Id && x.CreatedBy == model.MemberId && x.DateDeleted == null);

            var totalCount = await query.CountAsync();
            var member = await _rethinkMasterDataMicroServices.GetMemberAsync(model.AccountInfoId, model.MemberId);

            var result = await query
                .Select(x => new AttachmentViewModel()
                {
                    Filename = x.FileName,
                    Id = x.Id,
                    DateCreated = x.DateCreated,
                    CreatedBy = member.userName
                })
                .ToListAsync();

            return new AttachmentsResponseModel
            {
                Data = result,
                TotalCount = totalCount
            };
        }

        public async Task<ClaimAttachmentItem> Get(int id)
        {
            ClaimAttachmentEntity result =
                await _claimAttachmentRepository.Query().FirstOrDefaultAsync(a => a.Id == id);

            return _mapper.Map<ClaimAttachmentItem>(result);
        }

        public async Task<List<ClaimAttachmentItem>> Save(List<ClaimAttachmentItem> items, int claimId, int memberId,
            int accountInfoId)
        {
            List<ClaimAttachmentEntity> updatedEntities = new List<ClaimAttachmentEntity>();
            ClaimEntity claim = await _claimRepository.Query()
                .FirstOrDefaultAsync(e => e.Id == claimId && e.AccountInfoId == accountInfoId);

            if (claim != null)
            {
                foreach (var item in items)
                {
                    ClaimAttachmentEntity claimAttachmentEntity =
                        await _claimAttachmentRepository.Query().FirstOrDefaultAsync(i => i.Id == item.Id);
                    if (claimAttachmentEntity == null)
                    {
                        claimAttachmentEntity = new ClaimAttachmentEntity();
                        claimAttachmentEntity.ClaimId = claimId;

                        item.UpdateEntity(claimAttachmentEntity);

                        claimAttachmentEntity.CreatedBy = memberId;
                        claimAttachmentEntity.DateCreated = EstDateTime;
                        claimAttachmentEntity.ModifiedBy = memberId;
                        claimAttachmentEntity.DateLastModified = EstDateTime;

                        _claimAttachmentRepository.Add(claimAttachmentEntity);
                    }
                    else
                    {
                        item.UpdateEntity(claimAttachmentEntity);
                        claimAttachmentEntity.ModifiedBy = memberId;
                        claimAttachmentEntity.DateLastModified = EstDateTime;
                        _claimAttachmentRepository.Update(claimAttachmentEntity);
                    }

                    updatedEntities.Add(claimAttachmentEntity);
                }

                await _claimAttachmentRepository.CommitAsync();
            }


            return _mapper.Map<List<ClaimAttachmentItem>>(updatedEntities);
        }

        public async Task<ClaimAttachmentItem> Delete(ClaimAttachmentItem item, int memberId, int accountInfoId)
        {
            ClaimAttachmentEntity claimAttachmentEntity =
                await _claimAttachmentRepository.Query().FirstOrDefaultAsync(e => e.Id == item.Id);

            ClaimEntity claim = await _claimRepository.Query().FirstOrDefaultAsync(e =>
                e.Id == claimAttachmentEntity.ClaimId && e.AccountInfoId == accountInfoId);

            if (claim != null)
            {
                if (claimAttachmentEntity != null && claimAttachmentEntity.DateDeleted == null)
                {
                    item.UpdateEntity(claimAttachmentEntity);
                    claimAttachmentEntity.DateDeleted = EstDateTime;
                    claimAttachmentEntity.DateLastModified = EstDateTime;
                    claimAttachmentEntity.ModifiedBy = memberId;

                    _claimAttachmentRepository.Update(claimAttachmentEntity);
                    await _claimAttachmentRepository.CommitAsync();
                }
            }

            return _mapper.Map<ClaimAttachmentItem>(claimAttachmentEntity);
        }

        public async Task<int> UploadFileAsync(ClaimUploadModelWithUserInfo model)
        {
            var claim = await _claimRepository.Query().FirstOrDefaultAsync(e =>
                e.Id == model.ClaimId && e.AccountInfoId == model.AccountInfoId);

            if (claim == null)
                throw new ArgumentException(nameof(model.ClaimId));

            if (model.Data == null || model.Data.Length <= 0)
                throw new ArgumentException();

            var query = await _claimAttachmentRepository.Query().Where(x => x.FileName == model.FileName && x.ClaimId == claim.Id && x.DateDeleted == null).FirstOrDefaultAsync();

            if (query == null)
            {
                var fileName = Path.GetFileNameWithoutExtension(model.FileName);
                var fileExtension = Path.GetExtension(model.FileName);

                var filePath = _fileService.PrepareFolderForEncounterAttachmentFile(model.AccountInfoId,
                    fileName + fileExtension, "Encounter", true);

                await _fileManager.UploadFileAsync(filePath, model.FileName, new MemoryStream(model.Data));

                var fullPath = filePath + model.FileName;
                //var fullPathLink = await _fileManager.GetFileUrl(fullPath, 30);

                var claimAttachmentEntity = new ClaimAttachmentEntity
                {
                    ClaimId = claim.Id,
                    FileName = model.FileName,
                    FileSize = model.Data.Length,
                    FilePath = fullPath,
                    FileMimeType = model.FileMimeType
                };

                MarkCreated(claimAttachmentEntity, model.MemberId);

                var attachment = await _claimAttachmentRepository.AddAndGetAsync(claimAttachmentEntity);
                return attachment.Id;
            }
            return 0;
        }
        //public async Task<byte[]> DownloadAttachmentFile(int claimId, int encounterAttachmentId)
        //{
        //    ClaimEntity encounter = await _claimRepository.Query().FirstOrDefaultAsync(e => e.Id == claimId);

        //    EncounterAttachmentItem encounterAttachment = await Get(encounterAttachmentId);
        //    //if (encounter != null && encounterAttachment != null)
        //    //{
        //    //    _fileManager
        //    //}


        //    return null;
        //}

        public async Task RenameAttachmentAsync(RenameAttachmentModelWithUserInfo model)
        {
            var attachment = await _claimAttachmentRepository.GetByIdAsync(model.AttachmentId);

            if (attachment == null)
            {
                throw new NullReferenceException("Attachment with such id does not exist");
            }

            if (attachment.CreatedBy != model.MemberId)
            {
                throw new UnauthorizedAccessException("User does not own this attachment");
            }

            attachment.FileName = model.FileName;
            MarkUpdated(attachment, model.MemberId);
            await _claimAttachmentRepository.CommitAsync();
        }

        public async Task DeleteUpload(IdWithUserInfo model)
        {
            var attachment = await _claimAttachmentRepository.GetByIdAsync(model.Id);

            if (attachment == null)
            {
                throw new NullReferenceException("Attachment with such id does not exist");
            }

            if (attachment.CreatedBy != model.MemberId)
            {
                throw new UnauthorizedAccessException("User does not own this attachment");
            }

            SoftDelete(attachment, model.MemberId);

            await _claimAttachmentRepository.CommitAsync();
        }

        public async Task<string> GetUploadAsync(IdWithUserInfo model)
        {
            var attachment = await _claimAttachmentRepository.GetByIdAsync(model.Id);

            if (attachment == null)
            {
                throw new NullReferenceException("Attachment with such id does not exist");
            }

            if (attachment.CreatedBy != model.MemberId)
            {
                throw new UnauthorizedAccessException("User does not own this attachment");
            }

            var fileLink = await _fileManager.GetFileUrl(attachment.FilePath);

            return fileLink;
        }
    }
}