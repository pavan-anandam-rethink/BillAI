using System;
using System.Diagnostics.CodeAnalysis;

namespace BillingService.Domain.DataObjects.Base
{
    [ExcludeFromCodeCoverage]
    public class BaseItem : BasicOption
    {
        public string Description { get; set; }

        public virtual bool DeactivationNotAllowed { get; set; }
        public virtual bool CanDelete { get; set; }
        public virtual DateTime DateLastModified { get; set; }
    }
}