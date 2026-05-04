using Rethink.Services.Common.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rethink.Services.Domain.Mapper
{
    public static class ClientUserModelExtensions
    {
        public static ChildProfileRethinkModel ToChildProfileRethinkModel(this ClientUserModel source)
        {
            if (source == null)
                return null;

            var name = source.name;

            return new ChildProfileRethinkModel
            {
                Id = source.id,
                FirstName = name.firstName,
                MiddleName = name.middleName,
                LastName = name.lastName,
                Name = string.Concat(
                    name.firstName, " ",
                    string.IsNullOrEmpty(name.middleName) ? "" : name.middleName + " ",
                    name.lastName)
            };
        }
    }

}
