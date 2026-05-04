IF NOT EXISTS (SELECT 1 FROM dbo.ExternalCodes WHERE code = '4' AND codeTypeId = 508)
    INSERT INTO dbo.ExternalCodes (code, description, codeTypeId, dateCreated, dateLastModified, dateDeleted) 
    VALUES ('4', 'This is a subsequent request for information from the original request', 508, GETDATE(), GETDATE(), NULL);

IF NOT EXISTS (SELECT 1 FROM dbo.ExternalCodes WHERE code = '5' AND codeTypeId = 508)
    INSERT INTO dbo.ExternalCodes (code, description, codeTypeId, dateCreated, dateLastModified, dateDeleted) 
    VALUES ('5', 'This is a final request for information', 508, GETDATE(), GETDATE(), NULL);

IF NOT EXISTS (SELECT 1 FROM dbo.ExternalCodes WHERE code = '7' AND codeTypeId = 508)
    INSERT INTO dbo.ExternalCodes (code, description, codeTypeId, dateCreated, dateLastModified, dateDeleted) 
    VALUES ('7', 'Claim may be reconsidered at a future date', 508, GETDATE(), GETDATE(), NULL);

IF NOT EXISTS (SELECT 1 FROM dbo.ExternalCodes WHERE code = '8' AND codeTypeId = 508)
    INSERT INTO dbo.ExternalCodes (code, description, codeTypeId, dateCreated, dateLastModified, dateDeleted) 
    VALUES ('8', 'No payment due to contract/plan provisions', 508, GETDATE(), GETDATE(), NULL);

IF NOT EXISTS (SELECT 1 FROM dbo.ExternalCodes WHERE code = '9' AND codeTypeId = 508)
    INSERT INTO dbo.ExternalCodes (code, description, codeTypeId, dateCreated, dateLastModified, dateDeleted) 
    VALUES ('9', 'No payment will be made for this claim', 508, GETDATE(), GETDATE(), NULL);

IF NOT EXISTS (SELECT 1 FROM dbo.ExternalCodes WHERE code = '10' AND codeTypeId = 508)
    INSERT INTO dbo.ExternalCodes (code, description, codeTypeId, dateCreated, dateLastModified, dateDeleted) 
    VALUES ('10', 'All originally submitted procedure codes have been combined', 508, GETDATE(), GETDATE(), NULL);

IF NOT EXISTS (SELECT 1 FROM dbo.ExternalCodes WHERE code = '11' AND codeTypeId = 508)
    INSERT INTO dbo.ExternalCodes (code, description, codeTypeId, dateCreated, dateLastModified, dateDeleted) 
    VALUES ('11', 'Some originally submitted procedure codes have been combined', 508, GETDATE(), GETDATE(), NULL);

IF NOT EXISTS (SELECT 1 FROM dbo.ExternalCodes WHERE code = '13' AND codeTypeId = 508)
    INSERT INTO dbo.ExternalCodes (code, description, codeTypeId, dateCreated, dateLastModified, dateDeleted) 
    VALUES ('13', 'All originally submitted procedure codes have been modified', 508, GETDATE(), GETDATE(), NULL);

IF NOT EXISTS (SELECT 1 FROM dbo.ExternalCodes WHERE code = '14' AND codeTypeId = 508)
    INSERT INTO dbo.ExternalCodes (code, description, codeTypeId, dateCreated, dateLastModified, dateDeleted) 
    VALUES ('14', 'Some all originally submitted procedure codes have been modified', 508, GETDATE(), GETDATE(), NULL);

IF NOT EXISTS (SELECT 1 FROM dbo.ExternalCodes WHERE code = '22' AND codeTypeId = 508)
    INSERT INTO dbo.ExternalCodes (code, description, codeTypeId, dateCreated, dateLastModified, dateDeleted) 
    VALUES ('22', '... before entering the adjudication system', 508, GETDATE(), GETDATE(), NULL);

IF NOT EXISTS (SELECT 1 FROM dbo.ExternalCodes WHERE code = '28' AND codeTypeId = 508)
    INSERT INTO dbo.ExternalCodes (code, description, codeTypeId, dateCreated, dateLastModified, dateDeleted) 
    VALUES ('28', 'Claim submitted to wrong payer', 508, GETDATE(), GETDATE(), NULL);

IF NOT EXISTS (SELECT 1 FROM dbo.ExternalCodes WHERE code = '36' AND codeTypeId = 508)
    INSERT INTO dbo.ExternalCodes (code, description, codeTypeId, dateCreated, dateLastModified, dateDeleted) 
    VALUES ('36', 'Predetermination is on file, awaiting completion of services', 508, GETDATE(), GETDATE(), NULL);

IF NOT EXISTS (SELECT 1 FROM dbo.ExternalCodes WHERE code = '43' AND codeTypeId = 508)
    INSERT INTO dbo.ExternalCodes (code, description, codeTypeId, dateCreated, dateLastModified, dateDeleted) 
    VALUES ('43', 'Charges pending provider audit', 508, GETDATE(), GETDATE(), NULL);

IF NOT EXISTS (SELECT 1 FROM dbo.ExternalCodes WHERE code = '48' AND codeTypeId = 508)
    INSERT INTO dbo.ExternalCodes (code, description, codeTypeId, dateCreated, dateLastModified, dateDeleted) 
    VALUES ('48', 'Referral/authorization', 508, GETDATE(), GETDATE(), NULL);

IF NOT EXISTS (SELECT 1 FROM dbo.ExternalCodes WHERE code = '58' AND codeTypeId = 508)
    INSERT INTO dbo.ExternalCodes (code, description, codeTypeId, dateCreated, dateLastModified, dateDeleted) 
    VALUES ('58', 'Pending COBRA information requested', 508, GETDATE(), GETDATE(), NULL);

IF NOT EXISTS (SELECT 1 FROM dbo.ExternalCodes WHERE code = '62' AND codeTypeId = 508)
    INSERT INTO dbo.ExternalCodes (code, description, codeTypeId, dateCreated, dateLastModified, dateDeleted) 
    VALUES ('62', 'Eligibility for extended benefits', 508, GETDATE(), GETDATE(), NULL);

IF NOT EXISTS (SELECT 1 FROM dbo.ExternalCodes WHERE code = '63' AND codeTypeId = 508)
    INSERT INTO dbo.ExternalCodes (code, description, codeTypeId, dateCreated, dateLastModified, dateDeleted) 
    VALUES ('63', 'Re-pricing information', 508, GETDATE(), GETDATE(), NULL);

IF NOT EXISTS (SELECT 1 FROM dbo.ExternalCodes WHERE code = '67' AND codeTypeId = 508)
    INSERT INTO dbo.ExternalCodes (code, description, codeTypeId, dateCreated, dateLastModified, dateDeleted) 
    VALUES ('67', 'Payment made in full', 508, GETDATE(), GETDATE(), NULL);

IF NOT EXISTS (SELECT 1 FROM dbo.ExternalCodes WHERE code = '68' AND codeTypeId = 508)
    INSERT INTO dbo.ExternalCodes (code, description, codeTypeId, dateCreated, dateLastModified, dateDeleted) 
    VALUES ('68', 'Partial payment made for this claim', 508, GETDATE(), GETDATE(), NULL);

IF NOT EXISTS (SELECT 1 FROM dbo.ExternalCodes WHERE code = '69' AND codeTypeId = 508)
    INSERT INTO dbo.ExternalCodes (code, description, codeTypeId, dateCreated, dateLastModified, dateDeleted) 
    VALUES ('69', 'Payment reflects plan provisions', 508, GETDATE(), GETDATE(), NULL);

IF NOT EXISTS (SELECT 1 FROM dbo.ExternalCodes WHERE code = '70' AND codeTypeId = 508)
    INSERT INTO dbo.ExternalCodes (code, description, codeTypeId, dateCreated, dateLastModified, dateDeleted) 
    VALUES ('70', 'Payment reflects contract provisions', 508, GETDATE(), GETDATE(), NULL);

IF NOT EXISTS (SELECT 1 FROM dbo.ExternalCodes WHERE code = '71' AND codeTypeId = 508)
    INSERT INTO dbo.ExternalCodes (code, description, codeTypeId, dateCreated, dateLastModified, dateDeleted) 
    VALUES ('71', 'Periodic installment released', 508, GETDATE(), GETDATE(), NULL);

IF NOT EXISTS (SELECT 1 FROM dbo.ExternalCodes WHERE code = '74' AND codeTypeId = 508)
    INSERT INTO dbo.ExternalCodes (code, description, codeTypeId, dateCreated, dateLastModified, dateDeleted) 
    VALUES ('74', 'Duplicate of an existing claim/line, awaiting processing', 508, GETDATE(), GETDATE(), NULL);

IF NOT EXISTS (SELECT 1 FROM dbo.ExternalCodes WHERE code = '75' AND codeTypeId = 508)
    INSERT INTO dbo.ExternalCodes (code, description, codeTypeId, dateCreated, dateLastModified, dateDeleted) 
    VALUES ('75', 'Contract/plan does not cover pre-existing conditions', 508, GETDATE(), GETDATE(), NULL);

IF NOT EXISTS (SELECT 1 FROM dbo.ExternalCodes WHERE code = '76' AND codeTypeId = 508)
    INSERT INTO dbo.ExternalCodes (code, description, codeTypeId, dateCreated, dateLastModified, dateDeleted) 
    VALUES ('76', 'No coverage for newborns', 508, GETDATE(), GETDATE(), NULL);

IF NOT EXISTS (SELECT 1 FROM dbo.ExternalCodes WHERE code = '77' AND codeTypeId = 508)
    INSERT INTO dbo.ExternalCodes (code, description, codeTypeId, dateCreated, dateLastModified, dateDeleted) 
    VALUES ('77', 'Service not authorized', 508, GETDATE(), GETDATE(), NULL);

IF NOT EXISTS (SELECT 1 FROM dbo.ExternalCodes WHERE code = '79' AND codeTypeId = 508)
    INSERT INTO dbo.ExternalCodes (code, description, codeTypeId, dateCreated, dateLastModified, dateDeleted) 
    VALUES ('79', 'Diagnosis and patient gender mismatch', 508, GETDATE(), GETDATE(), NULL);

IF NOT EXISTS (SELECT 1 FROM dbo.ExternalCodes WHERE code = '80' AND codeTypeId = 508)
    INSERT INTO dbo.ExternalCodes (code, description, codeTypeId, dateCreated, dateLastModified, dateDeleted) 
    VALUES ('80', 'Denied: Entity not found', 508, GETDATE(), GETDATE(), NULL);

IF NOT EXISTS (SELECT 1 FROM dbo.ExternalCodes WHERE code = '82' AND codeTypeId = 508)
    INSERT INTO dbo.ExternalCodes (code, description, codeTypeId, dateCreated, dateLastModified, dateDeleted) 
    VALUES ('82', 'Entity not eligible for benefits for submitted dates of service', 508, GETDATE(), GETDATE(), NULL);

IF NOT EXISTS (SELECT 1 FROM dbo.ExternalCodes WHERE code = '87' AND codeTypeId = 508)
    INSERT INTO dbo.ExternalCodes (code, description, codeTypeId, dateCreated, dateLastModified, dateDeleted) 
    VALUES ('87', 'Requested additional information not received', 508, GETDATE(), GETDATE(), NULL);

IF NOT EXISTS (SELECT 1 FROM dbo.ExternalCodes WHERE code = '108' AND codeTypeId = 508)
    INSERT INTO dbo.ExternalCodes (code, description, codeTypeId, dateCreated, dateLastModified, dateDeleted) 
    VALUES ('108', 'Coverage has been canceled for this entity', 508, GETDATE(), GETDATE(), NULL);

IF NOT EXISTS (SELECT 1 FROM dbo.ExternalCodes WHERE code = '112' AND codeTypeId = 508)
    INSERT INTO dbo.ExternalCodes (code, description, codeTypeId, dateCreated, dateLastModified, dateDeleted) 
    VALUES ('112', 'Policyholder processes their own claims', 508, GETDATE(), GETDATE(), NULL);

IF NOT EXISTS (SELECT 1 FROM dbo.ExternalCodes WHERE code = '113' AND codeTypeId = 508)
    INSERT INTO dbo.ExternalCodes (code, description, codeTypeId, dateCreated, dateLastModified, dateDeleted) 
    VALUES ('113', 'Cannot process individual insurance policy claims', 508, GETDATE(), GETDATE(), NULL);

IF NOT EXISTS (SELECT 1 FROM dbo.ExternalCodes WHERE code = '115' AND codeTypeId = 508)
    INSERT INTO dbo.ExternalCodes (code, description, codeTypeId, dateCreated, dateLastModified, dateDeleted) 
    VALUES ('115', 'Cannot process HMO claims', 508, GETDATE(), GETDATE(), NULL);

IF NOT EXISTS (SELECT 1 FROM dbo.ExternalCodes WHERE code = '118' AND codeTypeId = 508)
    INSERT INTO dbo.ExternalCodes (code, description, codeTypeId, dateCreated, dateLastModified, dateDeleted) 
    VALUES ('118', 'TPO rejected claim/line because payer name is missing', 508, GETDATE(), GETDATE(), NULL);

IF NOT EXISTS (SELECT 1 FROM dbo.ExternalCodes WHERE code = '119' AND codeTypeId = 508)
    INSERT INTO dbo.ExternalCodes (code, description, codeTypeId, dateCreated, dateLastModified, dateDeleted) 
    VALUES ('119', 'TPO rejected claim/line because certification information is missing', 508, GETDATE(), GETDATE(), NULL);

IF NOT EXISTS (SELECT 1 FROM dbo.ExternalCodes WHERE code = '120' AND codeTypeId = 508)
    INSERT INTO dbo.ExternalCodes (code, description, codeTypeId, dateCreated, dateLastModified, dateDeleted) 
    VALUES ('120', 'TPO rejected claim/line because claim does not contain enough information', 508, GETDATE(), GETDATE(), NULL);

IF NOT EXISTS (SELECT 1 FROM dbo.ExternalCodes WHERE code = '122' AND codeTypeId = 508)
    INSERT INTO dbo.ExternalCodes (code, description, codeTypeId, dateCreated, dateLastModified, dateDeleted) 
    VALUES ('122', 'Missing/invalid data prevents payer from processing claim', 508, GETDATE(), GETDATE(), NULL);

IF NOT EXISTS (SELECT 1 FROM dbo.ExternalCodes WHERE code = '151' AND codeTypeId = 508)
    INSERT INTO dbo.ExternalCodes (code, description, codeTypeId, dateCreated, dateLastModified, dateDeleted) 
    VALUES ('151', 'Processor Control Number', 508, GETDATE(), GETDATE(), NULL);

IF NOT EXISTS (SELECT 1 FROM dbo.ExternalCodes WHERE code = '169' AND codeTypeId = 508)
    INSERT INTO dbo.ExternalCodes (code, description, codeTypeId, dateCreated, dateLastModified, dateDeleted) 
    VALUES ('169', 'Entity''s employer id', 508, GETDATE(), GETDATE(), NULL);

IF NOT EXISTS (SELECT 1 FROM dbo.ExternalCodes WHERE code = '220' AND codeTypeId = 508)
    INSERT INTO dbo.ExternalCodes (code, description, codeTypeId, dateCreated, dateLastModified, dateDeleted) 
    VALUES ('220', 'Drug product id number', 508, GETDATE(), GETDATE(), NULL);

IF NOT EXISTS (SELECT 1 FROM dbo.ExternalCodes WHERE code = '221' AND codeTypeId = 508)
    INSERT INTO dbo.ExternalCodes (code, description, codeTypeId, dateCreated, dateLastModified, dateDeleted) 
    VALUES ('221', 'Drug days supply and dosage', 508, GETDATE(), GETDATE(), NULL);

IF NOT EXISTS (SELECT 1 FROM dbo.ExternalCodes WHERE code = '248' AND codeTypeId = 508)
    INSERT INTO dbo.ExternalCodes (code, description, codeTypeId, dateCreated, dateLastModified, dateDeleted) 
    VALUES ('248', 'Accident date, state, description and cause', 508, GETDATE(), GETDATE(), NULL);

IF NOT EXISTS (SELECT 1 FROM dbo.ExternalCodes WHERE code = '253' AND codeTypeId = 508)
    INSERT INTO dbo.ExternalCodes (code, description, codeTypeId, dateCreated, dateLastModified, dateDeleted) 
    VALUES ('253', 'Procedure/revenue code for service(s) rendered', 508, GETDATE(), GETDATE(), NULL);

IF NOT EXISTS (SELECT 1 FROM dbo.ExternalCodes WHERE code = '278' AND codeTypeId = 508)
    INSERT INTO dbo.ExternalCodes (code, description, codeTypeId, dateCreated, dateLastModified, dateDeleted) 
    VALUES ('278', 'Signed claim form', 508, GETDATE(), GETDATE(), NULL);

IF NOT EXISTS (SELECT 1 FROM dbo.ExternalCodes WHERE code = '280' AND codeTypeId = 508)
    INSERT INTO dbo.ExternalCodes (code, description, codeTypeId, dateCreated, dateLastModified, dateDeleted) 
    VALUES ('280', 'Itemized claim by provider', 508, GETDATE(), GETDATE(), NULL);

IF NOT EXISTS (SELECT 1 FROM dbo.ExternalCodes WHERE code = '285' AND codeTypeId = 508)
    INSERT INTO dbo.ExternalCodes (code, description, codeTypeId, dateCreated, dateLastModified, dateDeleted) 
    VALUES ('285', 'Vouchers/explanation of benefits (EOB)', 508, GETDATE(), GETDATE(), NULL);

IF NOT EXISTS (SELECT 1 FROM dbo.ExternalCodes WHERE code = '289' AND codeTypeId = 508)
    INSERT INTO dbo.ExternalCodes (code, description, codeTypeId, dateCreated, dateLastModified, dateDeleted) 
    VALUES ('289', 'Reason for late discharge', 508, GETDATE(), GETDATE(), NULL);

IF NOT EXISTS (SELECT 1 FROM dbo.ExternalCodes WHERE code = '302' AND codeTypeId = 508)
    INSERT INTO dbo.ExternalCodes (code, description, codeTypeId, dateCreated, dateLastModified, dateDeleted) 
    VALUES ('302', 'Refer to codes 300 for lab notes and 311 for pathology notes', 508, GETDATE(), GETDATE(), NULL);

IF NOT EXISTS (SELECT 1 FROM dbo.ExternalCodes WHERE code = '303' AND codeTypeId = 508)
    INSERT INTO dbo.ExternalCodes (code, description, codeTypeId, dateCreated, dateLastModified, dateDeleted) 
    VALUES ('303', 'Physical therapy notes', 508, GETDATE(), GETDATE(), NULL);

IF NOT EXISTS (SELECT 1 FROM dbo.ExternalCodes WHERE code = '304' AND codeTypeId = 508)
    INSERT INTO dbo.ExternalCodes (code, description, codeTypeId, dateCreated, dateLastModified, dateDeleted) 
    VALUES ('304', 'Reports for service', 508, GETDATE(), GETDATE(), NULL);

IF NOT EXISTS (SELECT 1 FROM dbo.ExternalCodes WHERE code = '309' AND codeTypeId = 508)
    INSERT INTO dbo.ExternalCodes (code, description, codeTypeId, dateCreated, dateLastModified, dateDeleted) 
    VALUES ('309', 'Code was duplicate of code 299', 508, GETDATE(), GETDATE(), NULL);

IF NOT EXISTS (SELECT 1 FROM dbo.ExternalCodes WHERE code = '317' AND codeTypeId = 508)
    INSERT INTO dbo.ExternalCodes (code, description, codeTypeId, dateCreated, dateLastModified, dateDeleted) 
    VALUES ('317', 'Patient''s medical records', 508, GETDATE(), GETDATE(), NULL);

IF NOT EXISTS (SELECT 1 FROM dbo.ExternalCodes WHERE code = '321' AND codeTypeId = 508)
    INSERT INTO dbo.ExternalCodes (code, description, codeTypeId, dateCreated, dateLastModified, dateDeleted) 
    VALUES ('321', 'Radiographs or models', 508, GETDATE(), GETDATE(), NULL);

IF NOT EXISTS (SELECT 1 FROM dbo.ExternalCodes WHERE code = '328' AND codeTypeId = 508)
    INSERT INTO dbo.ExternalCodes (code, description, codeTypeId, dateCreated, dateLastModified, dateDeleted) 
    VALUES ('328', 'Speech therapy notes', 508, GETDATE(), GETDATE(), NULL);

IF NOT EXISTS (SELECT 1 FROM dbo.ExternalCodes WHERE code = '332' AND codeTypeId = 508)
    INSERT INTO dbo.ExternalCodes (code, description, codeTypeId, dateCreated, dateLastModified, dateDeleted) 
    VALUES ('332', 'Authorization/certification (include period covered)', 508, GETDATE(), GETDATE(), NULL);

IF NOT EXISTS (SELECT 1 FROM dbo.ExternalCodes WHERE code = '338' AND codeTypeId = 508)
    INSERT INTO dbo.ExternalCodes (code, description, codeTypeId, dateCreated, dateLastModified, dateDeleted) 
    VALUES ('338', 'Home health certification', 508, GETDATE(), GETDATE(), NULL);

IF NOT EXISTS (SELECT 1 FROM dbo.ExternalCodes WHERE code = '347' AND codeTypeId = 508)
    INSERT INTO dbo.ExternalCodes (code, description, codeTypeId, dateCreated, dateLastModified, dateDeleted) 
    VALUES ('347', 'Refer to code 345 for treatment plan and code 282 for prescription', 508, GETDATE(), GETDATE(), NULL);

IF NOT EXISTS (SELECT 1 FROM dbo.ExternalCodes WHERE code = '348' AND codeTypeId = 508)
    INSERT INTO dbo.ExternalCodes (code, description, codeTypeId, dateCreated, dateLastModified, dateDeleted) 
    VALUES ('348', 'Chiropractic treatment plan', 508, GETDATE(), GETDATE(), NULL);

IF NOT EXISTS (SELECT 1 FROM dbo.ExternalCodes WHERE code = '349' AND codeTypeId = 508)
    INSERT INTO dbo.ExternalCodes (code, description, codeTypeId, dateCreated, dateLastModified, dateDeleted) 
    VALUES ('349', 'Psychiatric treatment plan', 508, GETDATE(), GETDATE(), NULL);

IF NOT EXISTS (SELECT 1 FROM dbo.ExternalCodes WHERE code = '350' AND codeTypeId = 508)
    INSERT INTO dbo.ExternalCodes (code, description, codeTypeId, dateCreated, dateLastModified, dateDeleted) 
    VALUES ('350', 'Speech pathology treatment plan', 508, GETDATE(), GETDATE(), NULL);

IF NOT EXISTS (SELECT 1 FROM dbo.ExternalCodes WHERE code = '351' AND codeTypeId = 508)
    INSERT INTO dbo.ExternalCodes (code, description, codeTypeId, dateCreated, dateLastModified, dateDeleted) 
    VALUES ('351', 'Physical/occupational therapy treatment plan.', 508, GETDATE(), GETDATE(), NULL);

IF NOT EXISTS (SELECT 1 FROM dbo.ExternalCodes WHERE code = '355' AND codeTypeId = 508)
    INSERT INTO dbo.ExternalCodes (code, description, codeTypeId, dateCreated, dateLastModified, dateDeleted) 
    VALUES ('355', 'Has claim been paid?', 508, GETDATE(), GETDATE(), NULL);

IF NOT EXISTS (SELECT 1 FROM dbo.ExternalCodes WHERE code = '356' AND codeTypeId = 508)
    INSERT INTO dbo.ExternalCodes (code, description, codeTypeId, dateCreated, dateLastModified, dateDeleted) 
    VALUES ('356', 'Was blood furnished?', 508, GETDATE(), GETDATE(), NULL);

IF NOT EXISTS (SELECT 1 FROM dbo.ExternalCodes WHERE code = '357' AND codeTypeId = 508)
    INSERT INTO dbo.ExternalCodes (code, description, codeTypeId, dateCreated, dateLastModified, dateDeleted) 
    VALUES ('357', 'Has or will blood be replaced?', 508, GETDATE(), GETDATE(), NULL);

IF NOT EXISTS (SELECT 1 FROM dbo.ExternalCodes WHERE code = '358' AND codeTypeId = 508)
    INSERT INTO dbo.ExternalCodes (code, description, codeTypeId, dateCreated, dateLastModified, dateDeleted) 
    VALUES ('358', 'Does provider accept assignment of benefits?', 508, GETDATE(), GETDATE(), NULL);

IF NOT EXISTS (SELECT 1 FROM dbo.ExternalCodes WHERE code = '359' AND codeTypeId = 508)
    INSERT INTO dbo.ExternalCodes (code, description, codeTypeId, dateCreated, dateLastModified, dateDeleted) 
    VALUES ('359', 'Is there a release of information signature on file?', 508, GETDATE(), GETDATE(), NULL);

IF NOT EXISTS (SELECT 1 FROM dbo.ExternalCodes WHERE code = '361' AND codeTypeId = 508)
    INSERT INTO dbo.ExternalCodes (code, description, codeTypeId, dateCreated, dateLastModified, dateDeleted) 
    VALUES ('361', 'Is there other insurance?', 508, GETDATE(), GETDATE(), NULL);

IF NOT EXISTS (SELECT 1 FROM dbo.ExternalCodes WHERE code = '362' AND codeTypeId = 508)
    INSERT INTO dbo.ExternalCodes (code, description, codeTypeId, dateCreated, dateLastModified, dateDeleted) 
    VALUES ('362', 'Is the dental patient covered by medical insurance?', 508, GETDATE(), GETDATE(), NULL);

IF NOT EXISTS (SELECT 1 FROM dbo.ExternalCodes WHERE code = '367' AND codeTypeId = 508)
    INSERT INTO dbo.ExternalCodes (code, description, codeTypeId, dateCreated, dateLastModified, dateDeleted) 
    VALUES ('367', 'Is service performed for a recurring condition or new condition?', 508, GETDATE(), GETDATE(), NULL);

IF NOT EXISTS (SELECT 1 FROM dbo.ExternalCodes WHERE code = '368' AND codeTypeId = 508)
    INSERT INTO dbo.ExternalCodes (code, description, codeTypeId, dateCreated, dateLastModified, dateDeleted) 
    VALUES ('368', 'Is medical doctor (MD) or doctor of osteopath (DO) on staff of this facility?', 508, GETDATE(), GETDATE(), NULL);

IF NOT EXISTS (SELECT 1 FROM dbo.ExternalCodes WHERE code = '369' AND codeTypeId = 508)
    INSERT INTO dbo.ExternalCodes (code, description, codeTypeId, dateCreated, dateLastModified, dateDeleted) 
    VALUES ('369', 'Does patient condition preclude use of ordinary bed?', 508, GETDATE(), GETDATE(), NULL);

IF NOT EXISTS (SELECT 1 FROM dbo.ExternalCodes WHERE code = '370' AND codeTypeId = 508)
    INSERT INTO dbo.ExternalCodes (code, description, codeTypeId, dateCreated, dateLastModified, dateDeleted) 
    VALUES ('370', 'Can patient operate controls of bed?', 508, GETDATE(), GETDATE(), NULL);

IF NOT EXISTS (SELECT 1 FROM dbo.ExternalCodes WHERE code = '371' AND codeTypeId = 508)
    INSERT INTO dbo.ExternalCodes (code, description, codeTypeId, dateCreated, dateLastModified, dateDeleted) 
    VALUES ('371', 'Is patient confined to room?', 508, GETDATE(), GETDATE(), NULL);

IF NOT EXISTS (SELECT 1 FROM dbo.ExternalCodes WHERE code = '372' AND codeTypeId = 508)
    INSERT INTO dbo.ExternalCodes (code, description, codeTypeId, dateCreated, dateLastModified, dateDeleted) 
    VALUES ('372', 'Is patient confined to bed?', 508, GETDATE(), GETDATE(), NULL);

IF NOT EXISTS (SELECT 1 FROM dbo.ExternalCodes WHERE code = '373' AND codeTypeId = 508)
    INSERT INTO dbo.ExternalCodes (code, description, codeTypeId, dateCreated, dateLastModified, dateDeleted) 
    VALUES ('373', 'Is patient an insulin diabetic?', 508, GETDATE(), GETDATE(), NULL);

IF NOT EXISTS (SELECT 1 FROM dbo.ExternalCodes WHERE code = '376' AND codeTypeId = 508)
    INSERT INTO dbo.ExternalCodes (code, description, codeTypeId, dateCreated, dateLastModified, dateDeleted) 
    VALUES ('376', 'Was charge for ambulance for a round-trip?', 508, GETDATE(), GETDATE(), NULL);

IF NOT EXISTS (SELECT 1 FROM dbo.ExternalCodes WHERE code = '377' AND codeTypeId = 508)
    INSERT INTO dbo.ExternalCodes (code, description, codeTypeId, dateCreated, dateLastModified, dateDeleted) 
    VALUES ('377', 'Was durable medical equipment purchased new or used?', 508, GETDATE(), GETDATE(), NULL);

IF NOT EXISTS (SELECT 1 FROM dbo.ExternalCodes WHERE code = '378' AND codeTypeId = 508)
    INSERT INTO dbo.ExternalCodes (code, description, codeTypeId, dateCreated, dateLastModified, dateDeleted) 
    VALUES ('378', 'Is pacemaker temporary or permanent?', 508, GETDATE(), GETDATE(), NULL);

IF NOT EXISTS (SELECT 1 FROM dbo.ExternalCodes WHERE code = '379' AND codeTypeId = 508)
    INSERT INTO dbo.ExternalCodes (code, description, codeTypeId, dateCreated, dateLastModified, dateDeleted) 
    VALUES ('379', 'Were services performed supervised by a physician?', 508, GETDATE(), GETDATE(), NULL);

IF NOT EXISTS (SELECT 1 FROM dbo.ExternalCodes WHERE code = '381' AND codeTypeId = 508)
    INSERT INTO dbo.ExternalCodes (code, description, codeTypeId, dateCreated, dateLastModified, dateDeleted) 
    VALUES ('381', 'Is drug generic?', 508, GETDATE(), GETDATE(), NULL);

IF NOT EXISTS (SELECT 1 FROM dbo.ExternalCodes WHERE code = '392' AND codeTypeId = 508)
    INSERT INTO dbo.ExternalCodes (code, description, codeTypeId, dateCreated, dateLastModified, dateDeleted) 
    VALUES ('392', 'Date(s) of blood transfusion(s)', 508, GETDATE(), GETDATE(), NULL);

IF NOT EXISTS (SELECT 1 FROM dbo.ExternalCodes WHERE code = '393' AND codeTypeId = 508)
    INSERT INTO dbo.ExternalCodes (code, description, codeTypeId, dateCreated, dateLastModified, dateDeleted) 
    VALUES ('393', 'Date of previous pacemaker check', 508, GETDATE(), GETDATE(), NULL);

IF NOT EXISTS (SELECT 1 FROM dbo.ExternalCodes WHERE code = '399' AND codeTypeId = 508)
    INSERT INTO dbo.ExternalCodes (code, description, codeTypeId, dateCreated, dateLastModified, dateDeleted) 
    VALUES ('399', 'Report of prior testing related to this service, including dates', 508, GETDATE(), GETDATE(), NULL);

IF NOT EXISTS (SELECT 1 FROM dbo.ExternalCodes WHERE code = '404' AND codeTypeId = 508)
    INSERT INTO dbo.ExternalCodes (code, description, codeTypeId, dateCreated, dateLastModified, dateDeleted) 
    VALUES ('404', 'Specific findings, complaints, or symptoms necessitating service', 508, GETDATE(), GETDATE(), NULL);

IF NOT EXISTS (SELECT 1 FROM dbo.ExternalCodes WHERE code = '405' AND codeTypeId = 508)
    INSERT INTO dbo.ExternalCodes (code, description, codeTypeId, dateCreated, dateLastModified, dateDeleted) 
    VALUES ('405', 'Summary of services', 508, GETDATE(), GETDATE(), NULL);

IF NOT EXISTS (SELECT 1 FROM dbo.ExternalCodes WHERE code = '410' AND codeTypeId = 508)
    INSERT INTO dbo.ExternalCodes (code, description, codeTypeId, dateCreated, dateLastModified, dateDeleted) 
    VALUES ('410', 'Explain differences between treatment plan and patient''s condition', 508, GETDATE(), GETDATE(), NULL);

IF NOT EXISTS (SELECT 1 FROM dbo.ExternalCodes WHERE code = '411' AND codeTypeId = 508)
    INSERT INTO dbo.ExternalCodes (code, description, codeTypeId, dateCreated, dateLastModified, dateDeleted) 
    VALUES ('411', 'Medical necessity for non-routine service(s)', 508, GETDATE(), GETDATE(), NULL);

IF NOT EXISTS (SELECT 1 FROM dbo.ExternalCodes WHERE code = '412' AND codeTypeId = 508)
    INSERT INTO dbo.ExternalCodes (code, description, codeTypeId, dateCreated, dateLastModified, dateDeleted) 
    VALUES ('412', 'Medical records to substantiate decision of non-coverage', 508, GETDATE(), GETDATE(), NULL);

IF NOT EXISTS (SELECT 1 FROM dbo.ExternalCodes WHERE code = '413' AND codeTypeId = 508)
    INSERT INTO dbo.ExternalCodes (code, description, codeTypeId, dateCreated, dateLastModified, dateDeleted) 
    VALUES ('413', 'Explain/justify differences between treatment plan and services rendered', 508, GETDATE(), GETDATE(), NULL);

IF NOT EXISTS (SELECT 1 FROM dbo.ExternalCodes WHERE code = '415' AND codeTypeId = 508)
    INSERT INTO dbo.ExternalCodes (code, description, codeTypeId, dateCreated, dateLastModified, dateDeleted) 
    VALUES ('415', 'Justify services outside composite rate', 508, GETDATE(), GETDATE(), NULL);

IF NOT EXISTS (SELECT 1 FROM dbo.ExternalCodes WHERE code = '416' AND codeTypeId = 508)
    INSERT INTO dbo.ExternalCodes (code, description, codeTypeId, dateCreated, dateLastModified, dateDeleted) 
    VALUES ('416', 'Verification of patient''s ability to retain and use information', 508, GETDATE(), GETDATE(), NULL);

IF NOT EXISTS (SELECT 1 FROM dbo.ExternalCodes WHERE code = '418' AND codeTypeId = 508)
    INSERT INTO dbo.ExternalCodes (code, description, codeTypeId, dateCreated, dateLastModified, dateDeleted) 
    VALUES ('418', 'Indicating why medications cannot be taken orally', 508, GETDATE(), GETDATE(), NULL);

IF NOT EXISTS (SELECT 1 FROM dbo.ExternalCodes WHERE code = '421' AND codeTypeId = 508)
    INSERT INTO dbo.ExternalCodes (code, description, codeTypeId, dateCreated, dateLastModified, dateDeleted) 
    VALUES ('421', 'Medical review attachment/information for service(s)', 508, GETDATE(), GETDATE(), NULL);

IF NOT EXISTS (SELECT 1 FROM dbo.ExternalCodes WHERE code = '422' AND codeTypeId = 508)
    INSERT INTO dbo.ExternalCodes (code, description, codeTypeId, dateCreated, dateLastModified, dateDeleted) 
    VALUES ('422', 'Homebound status', 508, GETDATE(), GETDATE(), NULL);

IF NOT EXISTS (SELECT 1 FROM dbo.ExternalCodes WHERE code = '423' AND codeTypeId = 508)
    INSERT INTO dbo.ExternalCodes (code, description, codeTypeId, dateCreated, dateLastModified, dateDeleted) 
    VALUES ('423', 'Prognosis', 508, GETDATE(), GETDATE(), NULL);

IF NOT EXISTS (SELECT 1 FROM dbo.ExternalCodes WHERE code = '424' AND codeTypeId = 508)
    INSERT INTO dbo.ExternalCodes (code, description, codeTypeId, dateCreated, dateLastModified, dateDeleted) 
    VALUES ('424', 'Statement of non-coverage including itemized bill', 508, GETDATE(), GETDATE(), NULL);

IF NOT EXISTS (SELECT 1 FROM dbo.ExternalCodes WHERE code = '425' AND codeTypeId = 508)
    INSERT INTO dbo.ExternalCodes (code, description, codeTypeId, dateCreated, dateLastModified, dateDeleted) 
    VALUES ('425', 'Itemize non-covered services', 508, GETDATE(), GETDATE(), NULL);

IF NOT EXISTS (SELECT 1 FROM dbo.ExternalCodes WHERE code = '426' AND codeTypeId = 508)
    INSERT INTO dbo.ExternalCodes (code, description, codeTypeId, dateCreated, dateLastModified, dateDeleted) 
    VALUES ('426', 'All current diagnoses', 508, GETDATE(), GETDATE(), NULL);

IF NOT EXISTS (SELECT 1 FROM dbo.ExternalCodes WHERE code = '427' AND codeTypeId = 508)
    INSERT INTO dbo.ExternalCodes (code, description, codeTypeId, dateCreated, dateLastModified, dateDeleted) 
    VALUES ('427', 'Emergency care provided during transport', 508, GETDATE(), GETDATE(), NULL);

IF NOT EXISTS (SELECT 1 FROM dbo.ExternalCodes WHERE code = '429' AND codeTypeId = 508)
    INSERT INTO dbo.ExternalCodes (code, description, codeTypeId, dateCreated, dateLastModified, dateDeleted) 
    VALUES ('429', 'Loaded miles and charges for transport to nearest facility with appropriate services', 508, GETDATE(), GETDATE(), NULL);

IF NOT EXISTS (SELECT 1 FROM dbo.ExternalCodes WHERE code = '436' AND codeTypeId = 508)
    INSERT INTO dbo.ExternalCodes (code, description, codeTypeId, dateCreated, dateLastModified, dateDeleted) 
    VALUES ('436', 'Short term goals', 508, GETDATE(), GETDATE(), NULL);

IF NOT EXISTS (SELECT 1 FROM dbo.ExternalCodes WHERE code = '437' AND codeTypeId = 508)
    INSERT INTO dbo.ExternalCodes (code, description, codeTypeId, dateCreated, dateLastModified, dateDeleted) 
    VALUES ('437', 'Long term goals', 508, GETDATE(), GETDATE(), NULL);

IF NOT EXISTS (SELECT 1 FROM dbo.ExternalCodes WHERE code = '438' AND codeTypeId = 508)
    INSERT INTO dbo.ExternalCodes (code, description, codeTypeId, dateCreated, dateLastModified, dateDeleted) 
    VALUES ('438', 'Number of patients attending session', 508, GETDATE(), GETDATE(), NULL);

IF NOT EXISTS (SELECT 1 FROM dbo.ExternalCodes WHERE code = '439' AND codeTypeId = 508)
    INSERT INTO dbo.ExternalCodes (code, description, codeTypeId, dateCreated, dateLastModified, dateDeleted) 
    VALUES ('439', 'Size, depth, amount, and type of drainage wounds', 508, GETDATE(), GETDATE(), NULL);

IF NOT EXISTS (SELECT 1 FROM dbo.ExternalCodes WHERE code = '440' AND codeTypeId = 508)
    INSERT INTO dbo.ExternalCodes (code, description, codeTypeId, dateCreated, dateLastModified, dateDeleted) 
    VALUES ('440', 'Why non-skilled caregiver has not been taught procedure', 508, GETDATE(), GETDATE(), NULL);

IF NOT EXISTS (SELECT 1 FROM dbo.ExternalCodes WHERE code = '444' AND codeTypeId = 508)
    INSERT INTO dbo.ExternalCodes (code, description, codeTypeId, dateCreated, dateLastModified, dateDeleted) 
    VALUES ('444', 'Method used to obtain test sample', 508, GETDATE(), GETDATE(), NULL);

IF NOT EXISTS (SELECT 1 FROM dbo.ExternalCodes WHERE code = '445' AND codeTypeId = 508)
    INSERT INTO dbo.ExternalCodes (code, description, codeTypeId, dateCreated, dateLastModified, dateDeleted) 
    VALUES ('445', 'Explain why hearing loss not correctable by hearing aid', 508, GETDATE(), GETDATE(), NULL);

IF NOT EXISTS (SELECT 1 FROM dbo.ExternalCodes WHERE code = '446' AND codeTypeId = 508)
    INSERT INTO dbo.ExternalCodes (code, description, codeTypeId, dateCreated, dateLastModified, dateDeleted) 
    VALUES ('446', 'Documentation from prior claim(s) related to service(s)', 508, GETDATE(), GETDATE(), NULL);

IF NOT EXISTS (SELECT 1 FROM dbo.ExternalCodes WHERE code = '447' AND codeTypeId = 508)
    INSERT INTO dbo.ExternalCodes (code, description, codeTypeId, dateCreated, dateLastModified, dateDeleted) 
    VALUES ('447', 'Plan of teaching', 508, GETDATE(), GETDATE(), NULL);

IF NOT EXISTS (SELECT 1 FROM dbo.ExternalCodes WHERE code = '448' AND codeTypeId = 508)
    INSERT INTO dbo.ExternalCodes (code, description, codeTypeId, dateCreated, dateLastModified, dateDeleted) 
    VALUES ('448', 'Invalid billing combination. See STC12 for details', 508, GETDATE(), GETDATE(), NULL);

IF NOT EXISTS (SELECT 1 FROM dbo.ExternalCodes WHERE code = '461' AND codeTypeId = 508)
    INSERT INTO dbo.ExternalCodes (code, description, codeTypeId, dateCreated, dateLastModified, dateDeleted) 
    VALUES ('461', 'NUBC Occurrence Code(s) and Date(s)', 508, GETDATE(), GETDATE(), NULL);

IF NOT EXISTS (SELECT 1 FROM dbo.ExternalCodes WHERE code = '462' AND codeTypeId = 508)
    INSERT INTO dbo.ExternalCodes (code, description, codeTypeId, dateCreated, dateLastModified, dateDeleted) 
    VALUES ('462', 'NUBC Occurrence Span Code(s) and Date(s)', 508, GETDATE(), GETDATE(), NULL);

IF NOT EXISTS (SELECT 1 FROM dbo.ExternalCodes WHERE code = '463' AND codeTypeId = 508)
    INSERT INTO dbo.ExternalCodes (code, description, codeTypeId, dateCreated, dateLastModified, dateDeleted) 
    VALUES ('463', 'NUBC Value Code(s) and/or Amount(s)', 508, GETDATE(), GETDATE(), NULL);

IF NOT EXISTS (SELECT 1 FROM dbo.ExternalCodes WHERE code = '482' AND codeTypeId = 508)
    INSERT INTO dbo.ExternalCodes (code, description, codeTypeId, dateCreated, dateLastModified, dateDeleted) 
    VALUES ('482', 'Date Error, Century Missing', 508, GETDATE(), GETDATE(), NULL);

IF NOT EXISTS (SELECT 1 FROM dbo.ExternalCodes WHERE code = '570' AND codeTypeId = 508)
    INSERT INTO dbo.ExternalCodes (code, description, codeTypeId, dateCreated, dateLastModified, dateDeleted) 
    VALUES ('570', 'Free Form Message Text', 508, GETDATE(), GETDATE(), NULL);

IF NOT EXISTS (SELECT 1 FROM dbo.ExternalCodes WHERE code = '641' AND codeTypeId = 508)
    INSERT INTO dbo.ExternalCodes (code, description, codeTypeId, dateCreated, dateLastModified, dateDeleted) 
    VALUES ('641', 'Service Adjudication or Payment Date', 508, GETDATE(), GETDATE(), NULL);

IF NOT EXISTS (SELECT 1 FROM dbo.ExternalCodes WHERE code = '797' AND codeTypeId = 508)
    INSERT INTO dbo.ExternalCodes (code, description, codeTypeId, dateCreated, dateLastModified, dateDeleted) 
    VALUES ('797', 'Entity''s TRICARE provider id', 508, GETDATE(), GETDATE(), NULL);

IF NOT EXISTS (SELECT 1 FROM dbo.ExternalCodes WHERE code = '798' AND codeTypeId = 508)
    INSERT INTO dbo.ExternalCodes (code, description, codeTypeId, dateCreated, dateLastModified, dateDeleted) 
    VALUES ('798', 'Claim predetermination/estimation could not be completed in real time. Claim requires manual review upon submission. Do not resubmit', 508, GETDATE(), GETDATE(), NULL);

IF NOT EXISTS (SELECT 1 FROM dbo.ExternalCodes WHERE code = '799' AND codeTypeId = 508)
    INSERT INTO dbo.ExternalCodes (code, description, codeTypeId, dateCreated, dateLastModified, dateDeleted) 
    VALUES ('799', 'Resubmit a replacement claim, not a new claim', 508, GETDATE(), GETDATE(), NULL);

IF NOT EXISTS (SELECT 1 FROM dbo.ExternalCodes WHERE code = '800' AND codeTypeId = 508)
    INSERT INTO dbo.ExternalCodes (code, description, codeTypeId, dateCreated, dateLastModified, dateDeleted) 
    VALUES ('800', 'Entity''s required reporting has been forwarded to the jurisdiction', 508, GETDATE(), GETDATE(), NULL);

IF NOT EXISTS (SELECT 1 FROM dbo.ExternalCodes WHERE code = '801' AND codeTypeId = 508)
    INSERT INTO dbo.ExternalCodes (code, description, codeTypeId, dateCreated, dateLastModified, dateDeleted) 
    VALUES ('801', 'Entity''s required reporting was accepted by the jurisdiction', 508, GETDATE(), GETDATE(), NULL);

IF NOT EXISTS (SELECT 1 FROM dbo.ExternalCodes WHERE code = '802' AND codeTypeId = 508)
    INSERT INTO dbo.ExternalCodes (code, description, codeTypeId, dateCreated, dateLastModified, dateDeleted) 
    VALUES ('802', 'Entity''s required reporting was rejected by the jurisdiction', 508, GETDATE(), GETDATE(), NULL);

IF NOT EXISTS (SELECT 1 FROM dbo.ExternalCodes WHERE code = '803' AND codeTypeId = 508)
    INSERT INTO dbo.ExternalCodes (code, description, codeTypeId, dateCreated, dateLastModified, dateDeleted) 
    VALUES ('803', 'Provider reporting has been rejected due to non-compliance with the jurisdiction''s mandated registration', 508, GETDATE(), GETDATE(), NULL);

IF NOT EXISTS (SELECT 1 FROM dbo.ExternalCodes WHERE code = '804' AND codeTypeId = 508)
    INSERT INTO dbo.ExternalCodes (code, description, codeTypeId, dateCreated, dateLastModified, dateDeleted) 
    VALUES ('804', 'Exceeds inquiry limit for batch', 508, GETDATE(), GETDATE(), NULL);

IF NOT EXISTS (SELECT 1 FROM dbo.ExternalCodes WHERE code = '805' AND codeTypeId = 508)
    INSERT INTO dbo.ExternalCodes (code, description, codeTypeId, dateCreated, dateLastModified, dateDeleted) 
    VALUES ('805', 'Mammography Certification Number', 508, GETDATE(), GETDATE(), NULL);

IF NOT EXISTS (SELECT 1 FROM dbo.ExternalCodes WHERE code = '806' AND codeTypeId = 508)
    INSERT INTO dbo.ExternalCodes (code, description, codeTypeId, dateCreated, dateLastModified, dateDeleted) 
    VALUES ('806', 'Residential county does not match the county of the service location', 508, GETDATE(), GETDATE(), NULL);

IF NOT EXISTS (SELECT 1 FROM dbo.ExternalCodes WHERE code = '807' AND codeTypeId = 508)
    INSERT INTO dbo.ExternalCodes (code, description, codeTypeId, dateCreated, dateLastModified, dateDeleted) 
    VALUES ('807', 'Health Risk Assessment', 508, GETDATE(), GETDATE(), NULL);

IF NOT EXISTS (SELECT 1 FROM dbo.ExternalCodes WHERE code = '808' AND codeTypeId = 508)
    INSERT INTO dbo.ExternalCodes (code, description, codeTypeId, dateCreated, dateLastModified, dateDeleted) 
    VALUES ('808', 'Manifestation diagnosis code cannot be billed as a Principal Diagnosis', 508, GETDATE(), GETDATE(), NULL);