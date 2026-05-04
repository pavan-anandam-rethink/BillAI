using BillingService.Domain.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BillingService.Domain.Interfaces.Billing
{
    public interface IMemberAccountService
    {

        //Task<int> GetMemberTimeZoneOffSetMinutes(int memberId);

        //int GetMemberTimeZoneOffSetMinutes(TimeZoneInfo memberTimezone, DateTime date);

        //Task<TimeZoneInfo> GetMemberTimezoneInfo(int memberId);

        //AccountInfoEntity GetAccountInfoById(int accountInfoId);
        //MemberItem GetMemberItemByName(string userName);
        //MemberItem GetMemberItemById(int memberId);
        //MemberItem GetStaffMembersByMemberId(int memberId);
        //AccountInfoItem GetAccountInfo(int accountInfoId);
        //SubscriptionItem GetAccountSubscription(int accountInfoId);
        //List<MemberItem> GetMembersByIds(List<int> memberIds);

        //double GetMemberTimeZoneOffSet(int memberId);
        //int GetMemberTimeZoneOffSetMinutes(int memberId);

        //List<PermissionItem> GetEducationMemberPermissions(int memberId, int accountId);
        //List<PermissionItem> GetHealthCareMemberPermissions(int memberId, int accountId);
        Task<List<MemberItem>> GetMembersByAccountInfoId(int accountInfoId);
        //string GetMemberTimeZoneName(int memberId);
        //MemberItem GetMemberByStaffId(int staffId);
        //Task<List<int>> GetAvailableClientIdsByStaffPernissions(int memberId, int accountInfoId, bool accessAllLocations, bool accessUserLocation, bool accessAssigned);
    }
}
