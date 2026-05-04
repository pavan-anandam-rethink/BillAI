export interface RevSpringPayloadResponse {
  payload: {
    consumerNumber: string;
    externalUsername: string | null;
    userEmail: string | null;
    userLastName: string | null;
    roleName: string | null;
    accessLevel: string | null;
    accountId: number;
    memberId: number;
    referenceNo : string | null;
    OrgSiteName:string | null;
    dataContext: {
      consumer: {
        consumerNumber: string;
        firstName: string | null;
        lastName: string | null;
        address1: string | null;
        address2: string | null;
        country: string | null;
        city: string | null;
        state: string | null;
        zip: string | null;
        phone: string | null;
        email: string | null;
        dateOfBirth: string | null;
        amountDue: number | null;
      };
    };
  };
}
