using AutoMapper;
using BillingService.Domain.Interfaces.Billing;
using BillingService.Domain.Models;
using Rethink.Services.Common.Interfaces;
using Rethink.Services.Common.Services;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace BillingService.Domain.Services.Billing
{
    public class MemberAccountService : BaseService, IMemberAccountService
    {
        private readonly IRethinkMasterDataMicroServices _rethinkServices;
        private readonly IMapper _mapper;

        public MemberAccountService(
            IRethinkMasterDataMicroServices rethinkServices,
            IMapper mapper
            )
        {

            _rethinkServices = rethinkServices;
            _mapper = mapper;
        }

        public async Task<List<MemberItem>> GetMembersByAccountInfoId(int accountInfoId)
        {
            var members = await _rethinkServices.GetMemberListAsync(accountInfoId);
            var memberList = members.data.Select(x => new MemberItem
            {
                Id = x.id,
                UserName = x.userName,
                Email = x.email,
                AccountInfoId = x.accountId,
                Title = x.userName,
                FirstName = x.firstName,
                LastName = x.lastName
            }).ToList();

            return memberList;
        }

        //public async task<int> getmembertimezoneoffsetminutes(int memberid)
        //{
        //    var fromtimezone = timezoneinfo.findsystemtimezonebyid("eastern standard time");

        //    var membertimezoneinfo = await getmembertimezoneinfo(memberid);

        //    var membertimezoneutcoffset = membertimezoneinfo.getutcoffset(datetime.now);
        //    var fromtimezoneutcoffset = fromtimezone.getutcoffset(datetime.now);

        //    if (fromtimezone.id == "eastern standard time" && !membertimezoneinfo.supportsdaylightsavingtime)
        //    {
        //        fromtimezoneutcoffset = timespan.fromhours(-4);
        //    }

        //    timespan difftimezone = membertimezoneutcoffset - fromtimezoneutcoffset;

        //    return (int)difftimezone.totalminutes;
        //}

        //public int getmembertimezoneoffsetminutes(timezoneinfo membertimezone, datetime date)
        //{
        //    var fromtimezone = timezoneinfo.findsystemtimezonebyid("eastern standard time");
        //    var membertimezoneutcoffset = membertimezone.getutcoffset(date);
        //    var fromtimezoneutcoffset = fromtimezone.getutcoffset(date);

        //    timespan difftimezone = membertimezoneutcoffset - fromtimezoneutcoffset;

        //    return (int)difftimezone.totalminutes;
        //}

        //public async task<timezoneinfo> getmembertimezoneinfo(int memberid)
        //{
        //    var account = await getaccountbymemberid(memberid);
        //    var staffmember = await _staffrepository.query().include(x => x.timezone)
        //        .where(staff => staff.memberid == memberid && staff.timezoneid != null).firstordefaultasync();

        //    var accounttimezone = await _timezonerepository.query().firstordefaultasync(t => t.id == account.timezoneid);

        //    var timezone = staffmember != null ? staffmember.timezone : accounttimezone != null ? accounttimezone : null;

        //    return timezoneinfo.findsystemtimezonebyid(timezone != null ? timezone.simplename : "eastern standard time");
        //}

        //private async task<accountinfoentity> getaccountbymemberid(int memberid)
        //{
        //    //var member =  await _memberrepository.query().where(x => (x.id == memberid)).singleordefaultasync();
        //    //var accountinfo = await _accountinforepository.query().singleordefaultasync(v => (v.id == member.accountinfoid));
        //    var accountinfo = await _accountinforepository.query().singleordefaultasync(v => v.members.any(k => k.id == memberid));

        //    if (accountinfo == null)
        //        throw new exception("account information not found.");
        //    return accountinfo;
        //}
    }
}
