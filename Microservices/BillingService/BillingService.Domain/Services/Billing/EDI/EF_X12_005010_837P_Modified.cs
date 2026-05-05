
using EdiFabric.Core.Annotations.Edi;
using EdiFabric.Core.Annotations.Validation;
using EdiFabric.Templates.Hipaa5010;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace BillingService.Domain.Services.Billing.EDI
{

    public class Rethink_Loop_2300_837P : Loop_2300_837P
    {
        /// <summary>
        /// Loop for REFERRING PROVIDER NAME
        /// </summary>
        [DataMember]
        [ListCount(10)]
        [Pos(15)]
        public virtual List<Loop_2310A_837P> Loop2310A { get; set; }
        /// <summary>
        /// Loop for  RENDERING PROVIDER NAME
        /// </summary>
        [DataMember]
        [ListCount(10)]
        [Pos(16)]
        public virtual List<Loop_2310B_837P> Loop2310B { get; set; }
        /// <summary>
        /// Loop for SERVICE FACILITY LOCATION NAME
        /// </summary>
        [DataMember]
        [ListCount(10)]
        [Pos(17)]
        public virtual List<Rethink_Loop_2310C_837P> Loop2310C { get; set; }


        // ----------------------------------------------
        // redefine existing loops to change their index
        // ----------------------------------------------
        /// /// <summary>
        /// Loop for Other Subscriber Information
        /// </summary>
        [DataMember]
        [ListCount(10)]
        [Pos(18)]
        public new virtual List<Loop_2320_837P> Loop2320 { get; set; }


        /// <summary>
        /// Loop for Service Line Number
        /// </summary>
        [DataMember]
        [Required]
        [ListCount(50)]
        [Pos(19)]
        public new virtual List<Loop_2400_837P> Loop2400 { get; set; }

    }
}
