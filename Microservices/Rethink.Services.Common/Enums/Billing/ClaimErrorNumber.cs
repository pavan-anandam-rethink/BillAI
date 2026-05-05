namespace Rethink.Services.Common.Enums.Billing
{
    public enum ClaimErrorNumber
    {
        Unknown = 0,
        // Account              ACC    1000

        // Appointment          APT    1100
        AppointmentNoLinksFound = 1101, // No linked appointments were found

        // Authorization        ATH    1200
        AuthorizationMissing = 1201, // Authorization was not set in Claim
        AuthorizationNotFound = 1202, // Could not find Authorization
        AuthorizationNumber = 1203, // Authorization Number-Required
        AuthorizationInactive = 1204, // Authorization-Inactive
        AuthorizationModified = 1210, // Authorization-Modified

        // Billing Code         BC     1300

        // Billing Information  BI     1400
        BillingInformationPlaceOfServiceMissing = 1401,
        BillingInformationPlaceOfServiceInvalid = 1402,

        // Billing Provider     BP     1500
        BillingProviderMissing = 1501,
        BillingProviderNpiMissing = 1502,
        BillingProviderNpiInvalid = 1503,
        BillingProviderFederalTaxIdMissing = 1504,
        BillingProviderFederalTaxIdInvalid = 1505,
        BillingProviderAddressMissingOrInvalid = 1506,
        BillingProviderCityMissingOrInvalid = 1507,
        BillingProviderStateMissingOrInvalid = 1508,
        BillingProviderZipMissingOrInvalid = 1509,
        BillingProviderChanged = 1510,
        BillingProviderTaxonomyMissing = 1511,


        // Clearinghouse        CH     1600
        ClearinghouseProviderIdentifierMissing = 1601,
        ClearinghouseIdentifierMissing = 1602,
        ClearinghouseSubmitterNameMissing = 1603,

        ClearinghouseSubmitterContactInformationName = 3105,
        ClearinghouseSubmitterOrganizationName = 3106,
        ClearinghouseReceiverOrganizationName = 3107,

        // Confirmations        CNF    1700
        ConfirmationsBenefitAssignmentIndicatorMissing = 1701,
        ConfirmationsReleaseOfInformationMissing = 1702,
        // Date Of Service      DOS    1800

        // Diagnosis Code       DC     1900
        DiagnosisCodeMissing = 1901,
        DiagnosisCodeInactive = 1902,
        DiagnosisCodeInvalid = 1903,

        // Facility             FAC    2000
        FacilityMissing = 2001,
        FacilityNotFound = 2002,
        FacilityChanged = 2003,

        // Funder               FND    2100
        FunderMissing = 2101,
        FunderNotFound = 2102,
        FunderNotInactive = 2103,
        FunderInactivePolicy = 2106,
        FunderInformationUpdated = 2107,

        // Insurance            INS    2200
        InsuranceContactMissing = 2201, // InsuranceContactId was not set in Funder Mapping
        InsuranceContactNotFound = 2202, // Insurance contact was not found
        InsuranceContactMissingDOB = 2203, // Insurance contact does not specify a DOB for the insured
        InsuranceContactMissingGender = 2204, // Insurance contact does not specify a gender for the insured

        // Location             LOC    2300

        // Referring Provider   RFP    2400
        ReferringProviderMissing = 2401,
        ReferringProviderNotFound = 2402,
        ReferringProviderInactive = 2403,
        ReferringProviderChanged = 2404,
        ReferringProviderNpiMissing = 2405,
        ReferringProviderNpiInvalid = 2406,

        // Rendering Provider   RNP    2500
        RenderingProviderMissing = 2501,
        RenderingProviderNotFound = 2502,
        RenderingProviderInactive = 2503,
        RenderingProviderChanged = 2504,
        RenderingProviderNpiMissing = 2505,
        RenderingProviderNpiInvalid = 2506,

        // Service Line         SVC    2600
        ServiceLineNoChargeEntries = 2601, // No charge entries were found
        ServiceLineBillingCodeMissing = 2602,
        ServiceLineBillingCodeQty = 2603,
        ServiceLineBillingCodeAmount = 2604,
        ServiceLineBillingDuplicate = 2605,


        ChildProfileAddressMissingOrInvalid = 2701,
        ChildProfileCityMissingOrInvalid = 2702,
        ChildProfileStateMissingOrInvalid = 2703,
        ChildProfileZipMissingOrInvalid = 2704,

        AccountBillingAddressMissingOrInvalid = 2801,
        AccountBillingCityMissingOrInvalid = 2802,
        AccountBillingStateMissingOrInvalid = 2803,
        AccountBillingZipMissingOrInvalid = 2804,

        ServiceLocationAddressMissingOrInvalid = 2901,
        ServiceLocationCityMissingOrInvalid = 2902,
        ServiceLocationStateMissingOrInvalid = 2903,
        ServiceLocationZipMissingOrInvalid = 2904,

        SubscriberAddressMissingOrInvalid = 3001,
        SubscriberCityMissingOrInvalid = 3002,
        SubscriberStateMissingOrInvalid = 3003,
        SubscriberZipMissingOrInvalid = 3004,

        EraClearinghouseRejected = 3101,
        EraFunderAcceptedWithErrors = 3102,
        EraFunderRejected = 3103,
        EraFunderDenied = 3104,

        ClearingHouseDetailsMissing = 3201,
        ClearingHouseTitleMissing = 3202,
        ClearingHouseURLLinkMissing = 3203,
        ClearingHouseUserNameMissing = 3204,
        ClearingHousePasswordMissing = 3205,
        InsurancePolicyNumberSize = 3208,

        //Note validation in case of submission reason 7 or 8

        NoteMissing = 3206,
        OriginalClaimMissing = 3207,
        StediProviderEnrollment = 3209,

        // Clearing House Upload Errors  CH   3210-3212
        ClearingHouseAuthenticationFailure = 3210,  // Authentication failure
        ClearingHouseConnectionIssue = 3211,        // Connection issue
        ClearingHouseUploadFailed = 3212            // Upload failed
    }
}