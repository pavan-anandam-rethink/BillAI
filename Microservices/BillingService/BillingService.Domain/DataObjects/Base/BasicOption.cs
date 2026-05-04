using Rethink.Services.Common.Utils;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Diagnostics.CodeAnalysis;

namespace BillingService.Domain.DataObjects.Base
{
    [ExcludeFromCodeCoverage]
    public class BasicOption : BaseNameOption
    {
        public bool IsUserEntry { get; set; }
        public bool IsSelected { get; set; }
        public bool isIEP { get; set; }
        public bool IsArchived { get; set; }
        public bool isMastered { get; set; }

        public static List<BasicOption> Map(SqlDataReader reader, AdoHelper sqlHelper)
        {
            List<BasicOption> result = new List<BasicOption>();

            while (reader.Read())
            {
                result.Add(new BasicOption()
                {
                    Id = sqlHelper.ReadNullableValue<int>(reader, "Id").GetValueOrDefault(),
                    Name = sqlHelper.ReadString(reader, "Name")
                });
            }

            return result;
        }
    }
}