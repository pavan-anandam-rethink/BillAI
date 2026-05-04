
using EdiFabric.Core.Annotations.Edi;
using EdiFabric.Core.Annotations.Validation;
using EdiFabric.Core.Model.Edi.X12;
using EdiFabric.Templates.Hipaa5010;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace BillingService.Domain.Services.Billing.EDI
{
    /// <summary>
    /// Loop for Service Facility Location Name
    /// </summary>
    [Serializable()]
    [DataContract()]
    [Group(typeof(NM1_ServiceFacilityLocation), "2310C")]
    public class Rethink_Loop_2310C_837P : Loop_2310C_837P
    {
        /// <summary>
        /// Additional Location Information
        /// </summary>
        [DataMember]
        [Pos(6)]
        //public virtual List<C040_ReferenceIdentifier> REF_AdditionalLocationInformation { get; set; }
        public virtual List<REF_AdditionalLocationIdentification> REF_AdditionalLocationInformation { get; set; }
    }

    /// <summary>
    /// Other Payer Service Facility Location Secondary Identification
    /// </summary>
    [Serializable()]
    [DataContract()]
    [Segment("REF", typeof(X12_ID_128_25))]
    public class REF_AdditionalLocationIdentification : REF, I_REF<C040_ReferenceIdentifier>
    {

        /// <summary>
        /// Reference Identification Qualifier
        /// </summary>
        [DataMember]
        [Required]
        [DataElement("128", typeof(X12_ID_128_25))]
        [Pos(1)]
        public override string ReferenceIdentificationQualifier_01 { get; set; }
        /// <summary>
        /// Reference Identification
        /// </summary>
        [DataMember]
        [RequiredAny(3)]
        [Required]
        [StringLength(1, 50)]
        [DataElement("127", typeof(X12_AN))]
        [Pos(2)]
        public override string ReferenceIdentificationREF_02 { get; set; }

        public C040_ReferenceIdentifier ReferenceIdentifier_04 { get; set; }
    }



}
