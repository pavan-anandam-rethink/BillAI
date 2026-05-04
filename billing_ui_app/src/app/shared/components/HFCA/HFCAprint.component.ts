import { Component, OnDestroy, OnInit } from '@angular/core';
import { Locale } from '@app/locale';
import { DialogRef } from '@progress/kendo-angular-dialog';
import { combineLatest, Subject } from 'rxjs';
import { takeUntil } from 'rxjs/operators';
import { FormBuilder, FormControl } from '@angular/forms';
import * as $ from 'jquery';

import { ClaimService } from '@core/services/billing';
import { RenderTextMode } from 'ng2-pdf-viewer';
const html2pdf = require('html2pdf.js')

export interface ClaimToPrint {
    claimId: number;
    cmsPages: number;
}

@Component({
    selector: 'HFCAprint',
    templateUrl: './HFCAprint.component.html',
    styleUrls: ['./HFCAprint.component.css']
})
export class HFCAprintComponent implements OnInit, OnDestroy {
    private readonly unsubscribe = new Subject();
    claimsToPrint: ClaimToPrint[] = [];
    claimIds: number[] = [];
    maxChargesPerPage: number = 6;
    cross: string = "X";
    no: string = "NO";
    yes: string = "YES";
    underscore: string = "_";
    showBackgroud = true;
    myForms = {};

    public inputList: Input[] = [];

    pdfPath = window.location.origin;
    pdfSrcCache: any;
    pdfImgCache: any;
    pdfImg2Cache: any;
    cache: any;
    printDisabled: boolean;
    renderTextMode: RenderTextMode = RenderTextMode.ENHANCED;

    get myFromsArr() {
        if (!this.cache) {
            this.cache = Object.values(this.myForms);
        }
        return this.cache;
    }

    get pdfSrc() {
        if (!this.pdfSrcCache) {
            this.pdfSrcCache = "/assets/pdf/CMS1500.pdf";
            // this.pdfSrcCache = require("./CMS1500.pdf");
        }

        return this.pdfSrcCache;
    }

    get pdfImg() {
        if (!this.pdfImgCache) {
            this.pdfImgCache = "/assets/img/CMS1500-1.png";
        //    this.pdfImgCache = require("./CMS1500-1.png");
        }

        return this.pdfImgCache;
    }

    get pdfImg2() {
        if (!this.pdfImg2Cache) {
            this.pdfImg2Cache = "/assets/img/CMS1500-2.png";
            // this.pdfImg2Cache = require("./CMS1500-2.png");
        }

        return this.pdfImg2Cache;
    }

    constructor(
        public locale: Locale,
        private dialog: DialogRef,
        private _fb: FormBuilder,
        private claimService: ClaimService

    ) {
    }

    cancel() {
        this.dialog.close(false);
    }
    new_print()
    {
        setTimeout(() => {
            this.print();
        }, 10);
    }



    async print() {
      const opt = {
        margin: 0,
        filename: 'HFCA.pdf',
        image: { type: 'jpeg', quality: 1.0 },
        html2canvas: { dpi: 900, scale: 9, useCORS: true },
        jsPDF: { unit: 'mm', format: 'a4', orientation: 'portrait', compress: false },
      };

      const target = document.getElementById('out-container')!.cloneNode(true);
      const inputs = $(target).find('input[type="text"]') as any;

      for (let i = 0; i < inputs.length; i++) {
        const val = inputs[i].value;
        $(inputs[i]).replaceWith(`<br/><br/><span style="font-size:10px; font-weight:200;">${val}</span>`);
      }

      await html2pdf()
        .set(opt)
        .from(target)
        .toPdf()
        .get('pdf')
        .then(pdfObj => {
          const pdfUrl = pdfObj.output('bloburl');
          const iframe = document.createElement('iframe');
          iframe.style.display = 'none';
          document.body.appendChild(iframe);
          iframe.src = pdfUrl;
          iframe.onload = () => iframe.contentWindow?.print();
        });
        setTimeout(() => {
        }, 1000);
    }

    savePDF(): void {
        const opt = {
            margin: 0,
            filename: 'HFCA' + ".pdf",
            image: { type: 'jpeg', quality: 0.98 },
            html2canvas: { dpi: 192, scale: 6 },
            jsPDF: { unit: 'mm', format: 'a4', orientation: 'portrait' },
        };
        const target = document.getElementById('out-container')!.cloneNode(true);
        const n = $(target).find('input[type="text"]') as any;
        for (let i = 0; i < n.length; i++){
            let val = n[i].value;
            $(n[i]).replaceWith("<br/><br/><span style='font-size: 10px; font-weight: 200'>" + val + "</span>");
        };
 
        //TODO:https://www.npmjs.com/package/pdfjs-dist
        //https://snyk.io/advisor/npm-package/pdfjs-dist
        html2pdf().from(target).set(opt).save();
        setTimeout(() => {
        }, 5000);
    }

    ngOnDestroy(): void {
        // this.unsubscribe.next();
        this.unsubscribe.complete();
    }

    info: any;

    ngOnInit(): void {
        this.claimsToPrint.each(c => {
            this.claimIds.push(c.claimId);
            for (let i = 1; i <= c.cmsPages; i++)
            {
                this.myForms['' + c.claimId + i] = this._fb.group({}); 
            }
        }
        );
        // console.log(this.myForms);
        // this.myFromsArr = Object.values(this.myForms);
    }

    isEmptyOrNullOrUndefinedCheck(str:string|null|undefined):boolean{
        return str===null||str===undefined||str==='';
    }

    fillPrinForm(info: any[]) {
        info.forEach(i => {
            // console.log(i);
            // console.log(this.claimIds);
            let noOfPages = Math.ceil(i.claimChargeEntries.length/this.maxChargesPerPage);
            for (let j = 0; j < noOfPages; j++)
            {
                const data = {};
                //const isIndividualAndGroup = i.serviceLineBillingProviderOption == BillingProviderOption.IndividualAndGroup;
                //const isIndividual = i.serviceLineBillingProviderOption == BillingProviderOption.IndividualOnly;
                data["insurance_name"] = i.funderName || '';
                data["insurance_address"] = i.funderAddress || '';
                var funderState=i.funderState!=null?', ' + i.funderState:"";
                var funderZip=i.funderZip!=null?', ' + i.funderZip:"";
                const insuranceCityStateZip = (i.funderCity + funderState + funderZip).replace(/, $/, "");
                if (!i.funderAddress2){
                    data["insurance_address2"] = insuranceCityStateZip || '';
                    data["insurance_city_state_zip"] = '';
                }
                else{
                    data["insurance_address2"] = i.funderAddress2 || '';
                    data["insurance_city_state_zip"] = insuranceCityStateZip || '';
                }
                data["insurance_type" + this.underscore + this.getInsuranceType(i.insuredCoverageTypeId)] =  this.cross;
                data["insurance_id"] = i.insuredNumber || '';
                data["pt_name"] = i.patientName || '';

                if (i.patientDOB) {
                    const patientDOB = new Date(i.patientDOB || '');
                    data["birth_mm"] = ("0" + (patientDOB.getMonth() + 1)).slice(-2);
                    data["birth_dd"] = ("0" + patientDOB.getDate()).slice(-2);
                    data["birth_yy"] = ("" + patientDOB.getFullYear()).slice(-2);
                }
                if (i.patientSex !== undefined)
                    data["sex" + this.underscore + (this.getGender(i.patientSex) ? "M" : "F")] = this.cross;
                data["ins_name"] = i.insuredName || '';
                data["pt_street"] = i.patientAddress.substring(0,29) || '';
                data["rel_to_ins" + this.underscore + this.getPatientRelationShipToInsured(i.patientRelationShipToInsured)] = this.cross; //this.getPatientRelationShipToInsured(i.patientRelationShipToInsured) || '';
                data["ins_street"] = i.insuredAddress.substring(0,29) || '';
                data["pt_city"] = i.patientCity.substring(0,23) || '';
                data["pt_state"] = i.patientState.substring(0,4) || '';
                data["ins_city"] = i.insuredCity.substring(0,23) || '';
                data["ins_state"] = i.insuredState.substring(0,4) || '';
                data["pt_zip"] = i.patientZip.substring(0,12) || '';
                //: phone code
                let m = i.patientMobile.match(/\((.*)\)(.*)/);
                data["pt_AreaCode"] = m && m[1] || '';
                data["pt_phone"] = m && m[2].trim() || '';
                data["NUCC USE"] = '';
                data["ins_zip"] = i.insuredZip.substring(0,12) || '';
                //TODO: phone code
                m = i.insuredMobile.match(/\((.*)\)(.*)/);
                data["ins_phone area"] = m && m[1] || '';
                data["ins_phone"] = m && m[2].trim() || '';

                data["NUCC USE"] = '';
                data["other_ins_name"] = i.secondaryInsuredName || '';
                data["ins_policy"] = i.insuredPolicyGroupNumber || '';
                data["other_ins_policy"] = i.secondaryInsuredNumber || '';
                data["employment" + this.underscore + this.no] = 'X';
                if (i.insuredSex !== undefined)
                    data["ins_sex" + this.underscore + (this.getGender(i.insuredSex) ? "MALE" : "FEMALE")] = this.cross;

                if (i.insuredDOB) {
                    const insuredDOB = new Date(i.insuredDOB || '');
                    data["ins_dob_mm"] = ("0" + (insuredDOB.getMonth() + 1)).slice(-2);
                    data["ins_dob_dd"] = ("0" + insuredDOB.getDate()).slice(-2);
                    data["ins_dob_yy"] = ("" + insuredDOB.getFullYear()).slice(-2);
                }
                data["40"] = '';
                data["pt_auto_accident" + this.underscore + this.no] = 'X';
                data["other_accident" + this.underscore + this.no] = 'X';
                data["accident_place"] = '';
                data["57"] = '';
                data["58"] = '';
                data["41"] = '';
                data["ins_plan_name"] = i.insurancePlanName || '';
                data["other_ins_plan_name"] = i.secondaryInsurancePlanName || '';
                data["50"] = '';
                const releaseOfInformationConfirmationType = i.releaseOfInformationConfirmationType == 'Y' ? 'Signature On File' : 'No Signature On File';
                data["pt_signature"] = releaseOfInformationConfirmationType;
                if (i.patientFunderSignatureDate) {
                    const patientFunderSignatureDate = new Date(i.patientFunderSignatureDate);
                    data["pt_date"] = patientFunderSignatureDate.toLocaleDateString("en-US"); //patientFunderSignatureDate.toLocaleDateString("en-US", { year: 'numeric', month: 'long', day: 'numeric' });
                }
                const authorizedPaymentConfirmationType = i.authorizedPaymentConfirmationType == 'Y' ? 'Signature On File' : 'No Signature On File';
                const authoriseReleaseOfInformationType = i.authoriseReleaseOfInfo == 2 ? 'No Signature On File' : 'Signature On File';
                data["ins_signature"] = authorizedPaymentConfirmationType;
                data["cur_ill_mm"] = '';
                data["cur_ill_dd"] = '';
                data["cur_ill_yy"] = '';
                data["73"] = '';
                data["74"] = '';
                data["sim_ill_mm"] = '';
                data["sim_ill_dd"] = '';
                data["sim_ill_yy"] = '';
                data["work_mm_from"] = '';
                data["work_dd_from"] = '';
                data["work_yy_from"] = '';
                data["work_mm_end"] = '';
                data["work_dd_end"] = '';
                data["work_yy_end"] = '';
                data["physician number 17a1"] = '';
                data["physician number 17a"] = '';
                data["85"] = '';
                data["ref_physician"] = i.referringProviderName || '';
                data["id_physician"] = i.referringProviderNPI || '';
                data["hosp_mm_from"] = '';
                data["hosp_dd_from"] = '';
                data["hosp_yy_from"] = '';
                data["hosp_mm_end"] = '';
                data["hosp_dd_end"] = '';
                data["hosp_yy_end"] = '';
                data["96"] = '';
                data["ins_benefit_plan" + this.underscore + (i.isAnotherPlan ? this.yes : this.no)] = this.cross;
                data["lab" + this.underscore + this.no] = 'X';
                var charges=0.00;
                var chargesintegerPart=Math.floor(charges);
                var chargesdecimalPart =(charges-chargesintegerPart)*100;
                var ifchargesDecimalPartisSingleDigit=!(chargesdecimalPart>=10&&chargesdecimalPart<=99)?'0'+chargesdecimalPart:chargesdecimalPart;
                let chargesvalueLen = chargesintegerPart?.toString()?.length;
                let chargesnewIntegerPartValue =chargesintegerPart?.toString();
                for (let index = chargesvalueLen; index < 12; index++) {
                    chargesnewIntegerPartValue+='  ';
                }
                data["charge"] = chargesnewIntegerPartValue || '';
                data["chargeb"]= ifchargesDecimalPartisSingleDigit || '';
                data["99icd"] = '0';
                //Only one diagnosis code as of now
                data["diagnosis1"] = i.patientDiagnosis[0] || '';
                data["diagnosis2"] = i.patientDiagnosis[1] || '';
                data["diagnosis3"] = i.patientDiagnosis[2] || '';
                data["diagnosis4"] = i.patientDiagnosis[3] || '';
                data["medicaid_resub"] = '';
                data["original_ref"] = '';
                data["diagnosis5"] = i.patientDiagnosis[4] || '';
                data["diagnosis6"] = i.patientDiagnosis[5] || '';
                data["diagnosis7"] = i.patientDiagnosis[6] || '';
                data["diagnosis8"] = i.patientDiagnosis[7] || '';
                data["diagnosis9"] = i.patientDiagnosis[8] || '';
                data["diagnosis10"] = i.patientDiagnosis[9] || '';
                data["diagnosis11"] = i.patientDiagnosis[10] || '';
                data["diagnosis12"] = i.patientDiagnosis[11] || '';
                data["prior_auth"] = i.authorizationNumber || '';
                
                let chargeEntries1=i.claimChargeEntries[j*this.maxChargesPerPage];
                if (chargeEntries1){
                    data["Suppl"] = '';
                    data["emg1"] = '';
                    data["local1a"] = '';
                    if (i.authorizationStartDate && chargeEntries1) {
                        const startDate = new Date(chargeEntries1.dateOfService || '');
                        data["sv1_mm_from"] = ("0" + (startDate.getMonth() + 1)).slice(-2);
                        data["sv1_dd_from"] = ("0" + startDate.getDate()).slice(-2);
                        data["sv1_yy_from"] = ("" + startDate.getFullYear()).slice(-2);
                    }
                    if (i.authorizationEndDate && chargeEntries1) {
                        const endDate = new Date(chargeEntries1.dateOfService || '');
                        data["sv1_mm_end"] = ("0" + (endDate.getMonth() + 1)).slice(-2);
                        data["sv1_dd_end"] = ("0" + endDate.getDate()).slice(-2);
                        data["sv1_yy_end"] = ("" + endDate.getFullYear()).slice(-2);
                    }
                    data["place1"] = i.claimChargeEntries[j*this.maxChargesPerPage] && i.claimChargeEntries[j*this.maxChargesPerPage].claim && i.claimChargeEntries[j*this.maxChargesPerPage].claim.locationCode.code || '';
                    data["type1"] = '';
                    data["cpt1"] = i.claimChargeEntries[j*this.maxChargesPerPage] && i.claimChargeEntries[j*this.maxChargesPerPage].billingCode || '';
                    data["mod1"] = i.claimChargeEntries[j*this.maxChargesPerPage] && i.claimChargeEntries[j*this.maxChargesPerPage].modifier1 || '';
                    data["mod1a"] = i.claimChargeEntries[j*this.maxChargesPerPage] && i.claimChargeEntries[j*this.maxChargesPerPage].modifier2 || '';
                    data["mod1b"] = i.claimChargeEntries[j*this.maxChargesPerPage] && i.claimChargeEntries[j*this.maxChargesPerPage].modifier3 || '';
                    data["mod1c"] = i.claimChargeEntries[j*this.maxChargesPerPage] && i.claimChargeEntries[j*this.maxChargesPerPage].modifier4 || '';
                    // data["diag1"] = i.claimChargeEntries[j*this.maxChargesPerPage] && i.claimChargeEntries[j*this.maxChargesPerPage].diagnosisCode || '';
                    data["diag1"] = i.claimChargeEntries[j*this.maxChargesPerPage] && 'A' || '';
                    var chargeEntry = this.addDecimals(i.claimChargeEntries[j * this.maxChargesPerPage] && i.claimChargeEntries[j * this.maxChargesPerPage].charges);
                    var chargeEntryintegerPart = Math.floor(chargeEntry);
                    var chargeEntrydecimalPart = Math.round((chargeEntry - chargeEntryintegerPart)*100);
                    var ifchargeEntryDecimalPartisSingleDigit = !(chargeEntrydecimalPart >= 10 && chargeEntrydecimalPart <= 99) ? '0' + chargeEntrydecimalPart : chargeEntrydecimalPart;
                    data["ch1"] = chargeEntryintegerPart || '';
                    data["ch1b"] = chargeEntrydecimalPart != 0 ? chargeEntrydecimalPart : ifchargeEntryDecimalPartisSingleDigit || '';
                    data["135"] = '';
                    data["day1"] = i.claimChargeEntries[j*this.maxChargesPerPage] && i.claimChargeEntries[j*this.maxChargesPerPage].units || '';
                    data["epsdt1"] = '';
                    //data["local1"] = isIndividualAndGroup || isIndividual ? i.claimChargeEntries[j*this.maxChargesPerPage] && i.claimChargeEntries[j*this.maxChargesPerPage].claim && i.claimChargeEntries[j*this.maxChargesPerPage].claim.renderingProviderNPI || '' : i.claimChargeEntries[j*this.maxChargesPerPage] && i.claimChargeEntries[j*this.maxChargesPerPage].claim && i.claimChargeEntries[j*this.maxChargesPerPage].claim.renderingStaffMemberId || '';
                    data["local1"] = i.claimChargeEntries[j * this.maxChargesPerPage] && i.claimChargeEntries[j * this.maxChargesPerPage].claim && i.claimChargeEntries[j * this.maxChargesPerPage].claim.renderingProviderNPI || '';
                }
                
                let chargeEntries2=i.claimChargeEntries[(j*this.maxChargesPerPage)+1];
                if (chargeEntries2){
                    data["Suppla"] = '';
                    data["emg2"] = '';
                    data["local2a"] = '';
                    if (i.authorizationStartDate && chargeEntries2) {
                        const startDate = new Date(chargeEntries2.dateOfService || '');
                        data["sv2_mm_from"] = ("0" + (startDate.getMonth() + 1)).slice(-2);
                        data["sv2_dd_from"] = ("0" + startDate.getDate()).slice(-2);
                        data["sv2_yy_from"] = ("" + startDate.getFullYear()).slice(-2);
                    }
                    if (i.authorizationEndDate && chargeEntries2) {
                        const endDate = new Date(chargeEntries2.dateOfService || '');
                        data["sv2_mm_end"] = ("0" + (endDate.getMonth() + 1)).slice(-2);
                        data["sv2_dd_end"] = ("0" + endDate.getDate()).slice(-2);
                        data["sv2_yy_end"] = ("" + endDate.getFullYear()).slice(-2);
                    }
                    data["place2"] = i.claimChargeEntries[(j*this.maxChargesPerPage)+1] && i.claimChargeEntries[(j*this.maxChargesPerPage)+1].claim && i.claimChargeEntries[(j*this.maxChargesPerPage)+1].claim.locationCode.code || '';
                    data["type2"] = '';
                    data["cpt2"] = i.claimChargeEntries[(j*this.maxChargesPerPage)+1] && i.claimChargeEntries[(j*this.maxChargesPerPage)+1].billingCode || '';
                    data["mod2"] = i.claimChargeEntries[(j*this.maxChargesPerPage)+1] && i.claimChargeEntries[(j*this.maxChargesPerPage)+1].modifier1 || '';
                    data["mod2a"] = i.claimChargeEntries[(j*this.maxChargesPerPage)+1] && i.claimChargeEntries[(j*this.maxChargesPerPage)+1].modifier2 || '';
                    data["mod2b"] = i.claimChargeEntries[(j*this.maxChargesPerPage)+1] && i.claimChargeEntries[(j*this.maxChargesPerPage)+1].modifier3 || '';
                    data["mod2c"] = i.claimChargeEntries[(j*this.maxChargesPerPage)+1] && i.claimChargeEntries[(j*this.maxChargesPerPage)+1].modifier4 || '';
                    // data["diag2"] = i.claimChargeEntries[(j*this.maxChargesPerPage)+1] && i.claimChargeEntries[(j*this.maxChargesPerPage)+1].diagnosisCode || '';
                    data["diag2"] = i.claimChargeEntries[(j*this.maxChargesPerPage)+1] && 'A' || '';
                    var chargeEntry1 = this.addDecimals(i.claimChargeEntries[(j * this.maxChargesPerPage) + 1] && i.claimChargeEntries[(j * this.maxChargesPerPage) + 1].charges);
                    var chargeEntryintegerPart1 = Math.floor(chargeEntry1);
                    var chargeEntrydecimalPart1 = Math.round((chargeEntry1 - chargeEntryintegerPart1) * 100);
                    var ifchargeEntryDecimalPartisSingleDigit1 = !(chargeEntrydecimalPart1 >= 10 && chargeEntrydecimalPart1 <= 99) ? '0' + chargeEntrydecimalPart1 : chargeEntrydecimalPart1;
                    data["ch2"] = chargeEntryintegerPart1 || '';
                    data["ch2b"] = chargeEntrydecimalPart1 != 0 ? chargeEntrydecimalPart1 : ifchargeEntryDecimalPartisSingleDigit1 || '';
                    data["157"] = '';
                    data["day2"] = i.claimChargeEntries[(j*this.maxChargesPerPage)+1] && i.claimChargeEntries[(j*this.maxChargesPerPage)+1].units || '';
                    data["plan2"] = '';
                    //data["local2"] = isIndividualAndGroup ? i.claimChargeEntries[(j*this.maxChargesPerPage)+1] && i.claimChargeEntries[(j*this.maxChargesPerPage)+1].claim && i.claimChargeEntries[(j*this.maxChargesPerPage)+1].claim.renderingProviderNPI || '' : i.claimChargeEntries[(j*this.maxChargesPerPage)+1] && i.claimChargeEntries[(j*this.maxChargesPerPage)+1].claim && i.claimChargeEntries[(j*this.maxChargesPerPage)+1].claim.renderingStaffMemberId || '';
                    data["local2"] = i.claimChargeEntries[(j * this.maxChargesPerPage) + 1] && i.claimChargeEntries[(j * this.maxChargesPerPage) + 1].claim && i.claimChargeEntries[(j * this.maxChargesPerPage) + 1].claim.renderingProviderNPI || '';
                }

                let chargeEntries3=i.claimChargeEntries[(j*this.maxChargesPerPage)+2];
                if (chargeEntries3){
                    data["Supplb"] = '';
                    data["emg3"] = '';
                    data["local3a"] = '';
                    if (i.authorizationStartDate && chargeEntries3) {
                        const startDate = new Date(chargeEntries3.dateOfService || '');
                        data["sv3_mm_from"] = ("0" + (startDate.getMonth() + 1)).slice(-2);
                        data["sv3_dd_from"] = ("0" + startDate.getDate()).slice(-2);
                        data["sv3_yy_from"] = ("" + startDate.getFullYear()).slice(-2);
                    }
                    if (i.authorizationEndDate && chargeEntries3) {
                        const endDate = new Date(chargeEntries3.dateOfService || '');
                        data["sv3_mm_end"] = ("0" + (endDate.getMonth() + 1)).slice(-2);
                        data["sv3_dd_end"] = ("0" + endDate.getDate()).slice(-2);
                        data["sv3_yy_end"] = ("" + endDate.getFullYear()).slice(-2);
                    }
                    data["place3"] = i.claimChargeEntries[(j*this.maxChargesPerPage)+2] && i.claimChargeEntries[(j*this.maxChargesPerPage)+2].claim && i.claimChargeEntries[(j*this.maxChargesPerPage)+2].claim.locationCode.code || '';
                    data["type3"] = '';
                    data["cpt3"] = i.claimChargeEntries[(j*this.maxChargesPerPage)+2] && i.claimChargeEntries[(j*this.maxChargesPerPage)+2].billingCode || '';
                    data["mod3"] = i.claimChargeEntries[(j*this.maxChargesPerPage)+2] && i.claimChargeEntries[(j*this.maxChargesPerPage)+2].modifier1 || '';
                    data["mod3a"] = i.claimChargeEntries[(j*this.maxChargesPerPage)+2] && i.claimChargeEntries[(j*this.maxChargesPerPage)+2].modifier2 || '';
                    data["mod3b"] = i.claimChargeEntries[(j*this.maxChargesPerPage)+2] && i.claimChargeEntries[(j*this.maxChargesPerPage)+2].modifier3 || '';
                    data["mod3c"] = i.claimChargeEntries[(j*this.maxChargesPerPage)+2] && i.claimChargeEntries[(j*this.maxChargesPerPage)+2].modifier4 || '';
                    // data["diag3"] = i.claimChargeEntries[(j*this.maxChargesPerPage)+2] && i.claimChargeEntries[(j*this.maxChargesPerPage)+2].diagnosisCode || '';
                    data["diag3"] = i.claimChargeEntries[(j*this.maxChargesPerPage)+2] && 'A' || '';
                    var chargeEntry2 = this.addDecimals(i.claimChargeEntries[(j * this.maxChargesPerPage) + 2] && i.claimChargeEntries[(j * this.maxChargesPerPage) + 2].charges);
                    var chargeEntryintegerPart2 = Math.floor(chargeEntry2);
                    var chargeEntrydecimalPart2 = Math.round((chargeEntry2 - chargeEntryintegerPart2) * 100);
                    var ifchargeEntryDecimalPartisSingleDigit2 = !(chargeEntrydecimalPart2 >= 10 && chargeEntrydecimalPart2 <= 99) ? '0' + chargeEntrydecimalPart2 : chargeEntrydecimalPart2;
                    data["ch3"] = chargeEntryintegerPart2 || '';
                    data["ch3b"] = chargeEntrydecimalPart2 != 0 ? chargeEntrydecimalPart2 : ifchargeEntryDecimalPartisSingleDigit2 || '';
                    data["179"] = '';
                    data["day3"] = i.claimChargeEntries[(j*this.maxChargesPerPage)+2] && i.claimChargeEntries[(j*this.maxChargesPerPage)+2].units || '';
                    data["plan3"] = '';
                    //data["local3"] = isIndividualAndGroup ? i.claimChargeEntries[(j*this.maxChargesPerPage)+2] && i.claimChargeEntries[(j*this.maxChargesPerPage)+2].claim && i.claimChargeEntries[(j*this.maxChargesPerPage)+2].claim.renderingProviderNPI || '' : i.claimChargeEntries[(j*this.maxChargesPerPage)+2] && i.claimChargeEntries[(j*this.maxChargesPerPage)+2].claim && i.claimChargeEntries[(j*this.maxChargesPerPage)+2].claim.renderingStaffMemberId || '';
                    data["local3"] = i.claimChargeEntries[(j * this.maxChargesPerPage) + 2] && i.claimChargeEntries[(j * this.maxChargesPerPage) + 2].claim && i.claimChargeEntries[(j * this.maxChargesPerPage) + 2].claim.renderingProviderNPI || '';
                }

                let chargeEntries4=i.claimChargeEntries[(j*this.maxChargesPerPage)+3];
                if (chargeEntries4){
                    data["Supplc"] = '';
                    data["emg4"] = '';
                    data["local4a"] = '';
                    if (i.authorizationStartDate && chargeEntries4) {
                        const startDate = new Date(chargeEntries4.dateOfService || '');
                        data["sv4_mm_from"] = ("0" + (startDate.getMonth() + 1)).slice(-2);
                        data["sv4_dd_from"] = ("0" + startDate.getDate()).slice(-2);
                        data["sv4_yy_from"] = ("" + startDate.getFullYear()).slice(-2);
                    }
                    if (i.authorizationEndDate && chargeEntries4) {
                        const endDate = new Date(chargeEntries4.dateOfService || '');
                        data["sv4_mm_end"] = ("0" + (endDate.getMonth() + 1)).slice(-2);
                        data["sv4_dd_end"] = ("0" + endDate.getDate()).slice(-2);
                        data["sv4_yy_end"] = ("" + endDate.getFullYear()).slice(-2);
                    }
                    data["place4"] = i.claimChargeEntries[(j*this.maxChargesPerPage)+3] && i.claimChargeEntries[(j*this.maxChargesPerPage)+3].claim && i.claimChargeEntries[(j*this.maxChargesPerPage)+3].claim.locationCode.code || '';
                    data["type4"] = '';
                    data["cpt4"] = i.claimChargeEntries[(j*this.maxChargesPerPage)+3] && i.claimChargeEntries[(j*this.maxChargesPerPage)+3].billingCode || '';
                    data["mod4"] = i.claimChargeEntries[(j*this.maxChargesPerPage)+3] && i.claimChargeEntries[(j*this.maxChargesPerPage)+3].modifier1 || '';
                    data["mod4a"] = i.claimChargeEntries[(j*this.maxChargesPerPage)+3] && i.claimChargeEntries[(j*this.maxChargesPerPage)+3].modifier2 || '';
                    data["mod4b"] = i.claimChargeEntries[(j*this.maxChargesPerPage)+3] && i.claimChargeEntries[(j*this.maxChargesPerPage)+3].modifier3 || '';
                    data["mod4c"] = i.claimChargeEntries[(j*this.maxChargesPerPage)+3] && i.claimChargeEntries[(j*this.maxChargesPerPage)+3].modifier4 || '';
                    // data["diag4"] = i.claimChargeEntries[(j*this.maxChargesPerPage)+3] && i.claimChargeEntries[(j*this.maxChargesPerPage)+3].diagnosisCode || '';
                    data["diag4"] = i.claimChargeEntries[(j*this.maxChargesPerPage)+3] && 'A' || '';
                    var chargeEntry3 = this.addDecimals(i.claimChargeEntries[(j * this.maxChargesPerPage) + 3] && i.claimChargeEntries[(j * this.maxChargesPerPage) + 3].charges);
                    var chargeEntryintegerPart3 = Math.floor(chargeEntry3);
                    var chargeEntrydecimalPart3 = Math.round((chargeEntry3 - chargeEntryintegerPart3) * 100);
                    var ifchargeEntryDecimalPartisSingleDigit3 = !(chargeEntrydecimalPart3 >= 10 && chargeEntrydecimalPart3 <= 99) ? '0' + chargeEntrydecimalPart3 : chargeEntrydecimalPart3;
                    data["ch4"] = chargeEntryintegerPart3 || '';
                    data["ch4b"] = chargeEntrydecimalPart3 != 0 ? chargeEntrydecimalPart3 : ifchargeEntryDecimalPartisSingleDigit3 || '';
                    data["201"] = '';
                    data["day4"] = i.claimChargeEntries[(j*this.maxChargesPerPage)+3] && i.claimChargeEntries[(j*this.maxChargesPerPage)+3].units || '';
                    data["plan4"] = '';
                    //data["local4"] = isIndividualAndGroup ? i.claimChargeEntries[(j*this.maxChargesPerPage)+3] && i.claimChargeEntries[(j*this.maxChargesPerPage)+3].claim && i.claimChargeEntries[(j*this.maxChargesPerPage)+3].claim.renderingProviderNPI || '' : i.claimChargeEntries[(j*this.maxChargesPerPage)+3] && i.claimChargeEntries[(j*this.maxChargesPerPage)+3].claim && i.claimChargeEntries[(j*this.maxChargesPerPage)+3].claim.renderingStaffMemberId || '';
                    data["local4"] = i.claimChargeEntries[(j * this.maxChargesPerPage) + 3] && i.claimChargeEntries[(j * this.maxChargesPerPage) + 3].claim && i.claimChargeEntries[(j * this.maxChargesPerPage) + 3].claim.renderingProviderNPI || '';
                }
                
                let chargeEntries5=i.claimChargeEntries[(j*this.maxChargesPerPage)+4]
                if (chargeEntries5){
                    data["Suppld"] = '';
                    data["emg5"] = '';
                    data["local5a"] = '';
                    if (i.authorizationStartDate && chargeEntries5) {
                        const startDate = new Date(chargeEntries5.dateOfService || '');
                        data["sv5_mm_from"] = ("0" + (startDate.getMonth() + 1)).slice(-2);
                        data["sv5_dd_from"] = ("0" + startDate.getDate()).slice(-2);
                        data["sv5_yy_from"] = ("" + startDate.getFullYear()).slice(-2);
                    }
                    if (i.authorizationEndDate && chargeEntries5) {
                        const endDate = new Date(chargeEntries5.dateOfService || '');
                        data["sv5_mm_end"] = ("0" + (endDate.getMonth() + 1)).slice(-2);
                        data["sv5_dd_end"] = ("0" + endDate.getDate()).slice(-2);
                        data["sv5_yy_end"] = ("" + endDate.getFullYear()).slice(-2);
                    }
                    data["place5"] = i.claimChargeEntries[(j*this.maxChargesPerPage)+4] && i.claimChargeEntries[(j*this.maxChargesPerPage)+4].claim && i.claimChargeEntries[(j*this.maxChargesPerPage)+4].claim.locationCode.code || '';
                    data["type5"] = '';
                    data["cpt5"] = i.claimChargeEntries[(j*this.maxChargesPerPage)+4] && i.claimChargeEntries[(j*this.maxChargesPerPage)+4].billingCode || '';
                    data["mod5"] = i.claimChargeEntries[(j*this.maxChargesPerPage)+4] && i.claimChargeEntries[(j*this.maxChargesPerPage)+4].modifier1 || '';
                    data["mod5a"] = i.claimChargeEntries[(j*this.maxChargesPerPage)+4] && i.claimChargeEntries[(j*this.maxChargesPerPage)+4].modifier2 || '';
                    data["mod5b"] = i.claimChargeEntries[(j*this.maxChargesPerPage)+4] && i.claimChargeEntries[(j*this.maxChargesPerPage)+4].modifier3 || '';
                    data["mod5c"] = i.claimChargeEntries[(j*this.maxChargesPerPage)+4] && i.claimChargeEntries[(j*this.maxChargesPerPage)+4].modifier4 || '';
                    // data["diag5"] = i.claimChargeEntries[(j*this.maxChargesPerPage)+4] && i.claimChargeEntries[(j*this.maxChargesPerPage)+4].diagnosisCode || '';
                    data["diag5"] = i.claimChargeEntries[(j*this.maxChargesPerPage)+4] && 'A' || '';
                    var chargeEntry4 = this.addDecimals(i.claimChargeEntries[(j * this.maxChargesPerPage) + 4] && i.claimChargeEntries[(j * this.maxChargesPerPage) + 4].charges);
                    var chargeEntryintegerPart4 = Math.floor(chargeEntry4);
                    var chargeEntrydecimalPart4 = Math.round((chargeEntry4 - chargeEntryintegerPart4) * 100);
                    var ifchargeEntryDecimalPartisSingleDigit4 = !(chargeEntrydecimalPart4 >= 10 && chargeEntrydecimalPart4 <= 99) ? '0' + chargeEntrydecimalPart4 : chargeEntrydecimalPart4;
                    data["ch5"] = chargeEntryintegerPart4 || '';
                    data["ch5b"] = chargeEntrydecimalPart4 != 0 ? chargeEntrydecimalPart4 : ifchargeEntryDecimalPartisSingleDigit4 || '';
                    data["223"] = '';
                    data["day5"] = i.claimChargeEntries[(j*this.maxChargesPerPage)+4] && i.claimChargeEntries[(j*this.maxChargesPerPage)+4].units || '';
                    data["plan5"] = '';
                    //data["local5"] = isIndividualAndGroup ? i.claimChargeEntries[(j*this.maxChargesPerPage)+4] && i.claimChargeEntries[(j*this.maxChargesPerPage)+4].claim && i.claimChargeEntries[(j*this.maxChargesPerPage)+4].claim.renderingProviderNPI || '' : i.claimChargeEntries[(j*this.maxChargesPerPage)+4] && i.claimChargeEntries[(j*this.maxChargesPerPage)+4].claim && i.claimChargeEntries[(j*this.maxChargesPerPage)+4].claim.renderingStaffMemberId || '';
                    data["local5"] = i.claimChargeEntries[(j*this.maxChargesPerPage)+4] && i.claimChargeEntries[(j*this.maxChargesPerPage)+4].claim && i.claimChargeEntries[(j*this.maxChargesPerPage)+4].claim.renderingProviderNPI || '';
                }
                
                let chargeEntries6=i.claimChargeEntries[(j*this.maxChargesPerPage)+5];
                if (chargeEntries6){
                    data["Supple"] = '';
                    data["emg6"] = '';
                    data["local6a"] = '';
                    if (i.authorizationStartDate && chargeEntries6) {
                        const startDate = new Date(chargeEntries6.dateOfService || '');
                        data["sv6_mm_from"] = ("0" + (startDate.getMonth() + 1)).slice(-2);
                        data["sv6_dd_from"] = ("0" + startDate.getDate()).slice(-2);
                        data["sv6_yy_from"] = ("" + startDate.getFullYear()).slice(-2);
                    }
                    if (i.authorizationEndDate && chargeEntries6) {
                        const endDate = new Date(chargeEntries6.dateOfService || '');
                        data["sv6_mm_end"] = ("0" + (endDate.getMonth() + 1)).slice(-2);
                        data["sv6_dd_end"] = ("0" + endDate.getDate()).slice(-2);
                        data["sv6_yy_end"] = ("" + endDate.getFullYear()).slice(-2);
                    }
                    data["place6"] = i.claimChargeEntries[(j*this.maxChargesPerPage)+5] && i.claimChargeEntries[(j*this.maxChargesPerPage)+5].claim && i.claimChargeEntries[(j*this.maxChargesPerPage)+5].claim.locationCode.code || '';
                    data["type6"] = '';
                    data["cpt6"] = i.claimChargeEntries[(j*this.maxChargesPerPage)+5] && i.claimChargeEntries[(j*this.maxChargesPerPage)+5].billingCode || '';
                    data["mod6"] = i.claimChargeEntries[(j*this.maxChargesPerPage)+5] && i.claimChargeEntries[(j*this.maxChargesPerPage)+5].modifier1 || '';
                    data["mod6a"] = i.claimChargeEntries[(j*this.maxChargesPerPage)+5] && i.claimChargeEntries[(j*this.maxChargesPerPage)+5].modifier2 || '';
                    data["mod6b"] = i.claimChargeEntries[(j*this.maxChargesPerPage)+5] && i.claimChargeEntries[(j*this.maxChargesPerPage)+5].modifier3 || '';
                    data["mod5c"] = i.claimChargeEntries[(j*this.maxChargesPerPage)+5] && i.claimChargeEntries[(j*this.maxChargesPerPage)+5].modifier4 || '';
                    // data["diag6"] = i.claimChargeEntries[(j*this.maxChargesPerPage)+5] && i.claimChargeEntries[(j*this.maxChargesPerPage)+5].diagnosisCode || '';
                    data["diag6"] = i.claimChargeEntries[(j*this.maxChargesPerPage)+5] && 'A' || '';
                    var chargeEntry5 = this.addDecimals(i.claimChargeEntries[(j * this.maxChargesPerPage) + 5] && i.claimChargeEntries[(j * this.maxChargesPerPage) + 5].charges);
                    var chargeEntryintegerPart5 = Math.floor(chargeEntry5);
                    var chargeEntrydecimalPart5 = Math.round((chargeEntry5 - chargeEntryintegerPart5) * 100);
                    var ifchargeEntryDecimalPartisSingleDigit5 = !(chargeEntrydecimalPart5 >= 10 && chargeEntrydecimalPart5 <= 99) ? '0' + chargeEntrydecimalPart5 : chargeEntrydecimalPart5;
                    data["ch6"] = chargeEntryintegerPart5 || '';
                    data["ch6b"] = chargeEntrydecimalPart5 != 0 ? chargeEntrydecimalPart5 : ifchargeEntryDecimalPartisSingleDigit5 || '';
                    data["245"] = '';
                    data["day6"] = i.claimChargeEntries[(j*this.maxChargesPerPage)+5] && i.claimChargeEntries[(j*this.maxChargesPerPage)+5].units || '';
                    data["plan6"] = '';
                    //data["local6"] = isIndividualAndGroup ? i.claimChargeEntries[(j*this.maxChargesPerPage)+5] && i.claimChargeEntries[(j*this.maxChargesPerPage)+5].claim && i.claimChargeEntries[(j*this.maxChargesPerPage)+5].claim.renderingProviderNPI || '' : i.claimChargeEntries[(j*this.maxChargesPerPage)+5] && i.claimChargeEntries[(j*this.maxChargesPerPage)+5].claim && i.claimChargeEntries[(j*this.maxChargesPerPage)+5].claim.renderingStaffMemberId || '';
                    data["local6"] = i.claimChargeEntries[(j * this.maxChargesPerPage) + 5] && i.claimChargeEntries[(j * this.maxChargesPerPage) + 5].claim && i.claimChargeEntries[(j * this.maxChargesPerPage) + 5].claim.renderingProviderNPI || '';
                }
                
                data["276"] = '';
                data["tax_id"] = i.federalTaxId || '';
                data["pt_account"] = i.medicalRecordNumber || '';
                data["ssn"] = '';
                data["assignment" + this.underscore + this.yes] = 'X';
                var totalChargesWithDecimal = this.addDecimals(i.totalCharge)
                var integerPart = Math.floor(totalChargesWithDecimal);
                var decimalPart = Math.round((totalChargesWithDecimal - integerPart) * 100);
                var ifDecimalPartisSingleDigit = !(decimalPart >= 10 && decimalPart <= 99) ? '0' + decimalPart : decimalPart;
                let newIntegerPartValue = integerPart?.toString();

                data["t_charge"] = newIntegerPartValue || '';
                data["t_chargeb"] = decimalPart != 0 ? decimalPart : ifDecimalPartisSingleDigit || '';

                var amountTotalChargesWithDecimal = i.paid
                var amountIntegerPart = Math.floor(amountTotalChargesWithDecimal);
                var amountDecimalPart = Math.round((amountTotalChargesWithDecimal - amountIntegerPart) * 100);
                var ifAmountDecimalPartisSingleDigit = !(amountDecimalPart >= 10 && amountDecimalPart <= 99) ? '0' + amountDecimalPart : amountDecimalPart;
                let amountNewIntegerPartValue = amountIntegerPart?.toString();

                data["amt_paid"] = amountNewIntegerPartValue || '';
                data["amt_paidb"] = amountDecimalPart != 0 ? amountDecimalPart : ifAmountDecimalPartisSingleDigit || '';
                m = i.providerPhoneNumber.match(/\((.*)\)(.*)/);
                data["doc_phone area"] = m && m[1] || '';
                data["doc_phone"] = m && m[2].trim() || '';
                data["fac_name"] = i.serviceLocation || '';
                data["doc_name"] = i.providerName || '';
                data["fac_street"] = i.serviceAddress1 || '';
                data["fac_street2"] = i.serviceAddress2 ||'';
                data["doc_street"] = i.providerAddress1 || '';
                data["doc_street2"] = i.providerAddress2||'';
                data["physician_signature"] = 'Signature On File' || '';
                data["fac_location"] = i.serviceCity + ' ' + i.serviceState + ' ' + i.serviceZip || '';
                data["doc_location"] = i.providerCity + ' ' + i.providerState + ' ' + i.providerZip + (i.providerZipExt ? '-' + i.providerZipExt : '') || '';
                data["physician_date"] = new Date().toLocaleDateString("en-US"); //new Date().toLocaleDateString("en-US", { year: 'numeric', month: 'long', day: 'numeric' });
                data["pin1"] = i.serviceLocationNPI || '';
                data["grp1"] = i.serviceLocationTaxId || '';
                data["pin"] = i.providerLocationNPI ? i.providerLocationNPI : i.accountNPI || '';
                data["grp"] = i.providerTaxonomyCode || '';
                data["Clear Form"] = '';
                data["plan1"] = '';
                data["epsdt2"] = '';
                data["epsdt3"] = '';
                data["epsdt4"] = '';
                data["epsdt6"] = '';
                data["epsdt5"] = '';
                data['id'] =  '' + i.id + (j + 1);
                this.myForms['' + i.id + (j + 1)].patchValue(data);
            }
        });
    }

    addDecimals(charge: number | null): any {
        if (!charge) return null;
        return charge.toFixed(2);
    }

    getGender(sex: number | null): any {
        if (!sex) return null;

        switch (sex) {
            case 1: return true;
            case 2: return false;
            default: return null;
        }
    }



    getPatientRelationShipToInsured(patientRelationShipToInsured: number | null): any {
        if (!patientRelationShipToInsured) return null;

        switch (patientRelationShipToInsured) {
            case 1: return "S";
            case 2: return "C";
            case 3: return "M";
            case 4: return "O";
            default: return null;
        }
    }

    getInsuranceType(insuredCoverageTypeId: number | null) {
        if (!insuredCoverageTypeId) return null;

        switch (insuredCoverageTypeId) {
            case 1: return "Medicare";
            case 2: return "Medicaid";
            case 3: return "Tricare";
            case 4: return "Champva";
            case 5: return "Group";
            case 6: return "Feca";
            case 7: return "Other";
            default: return null;
        }
    }

    readonly dpiRatio = 96 / 72;
    loadCompleted = false;
    pdfWidth = 816;
    pdfHeight = 1056;
    get style() {
        return {
            'height': this.pdfHeight + 'px',
            'width': this.pdfWidth + 'px',
            'visibility': this.loadCompleted && this.showBackgroud ? 'visible' : 'hidden'
        }
    };
    loadComplete(pdf: any): void {
        this.loadCompleted = true;
        let counter=1;
        for (let i = 1; i <= 1/*pdf.numPages*/; i++) {
            let currentPage: any = null;
            combineLatest(
                pdf.getPage(i).then((p: any) => {
                    currentPage = p;
                    // console.log(p.getAnnotations(i));
                    return p.getAnnotations(i);
                }).then((annotations: EPDFAnnotationData[]) => {
                    const w = document.querySelector('.pdfViewer .page');
                    this.pdfWidth = w ? w.clientWidth : 812;
                    annotations
                        .filter(a => a.subtype === 'Widget')
                        .forEach((a: EPDFAnnotationData) => {
                            const fieldRect = currentPage.getViewport({ scale: this.dpiRatio })
                                .convertToViewportRectangle(a.rect);
                                if(a.fieldName != 'fac_street' && a.fieldName != 'fac_name' && a.fieldName != "doc_street" && a.fieldName != 'doc_name' ){
                                this.addInput(a, fieldRect);
                            }
                            if(a.fieldName == 'ch'+counter)
                            {
                                let b: EPDFAnnotationData= JSON.parse(JSON.stringify(a))
                                b.fieldName = a.fieldName+'b';
                                b.id = a.id+'b';
                                b.rect[0] += 45;
                                let newRect = currentPage.getViewport({ scale: this.dpiRatio })
                                .convertToViewportRectangle(b.rect);
                                this.addInput(b, newRect);
                                counter+=1;
                            }
                            if (a.fieldName == 'charge') {
                                let b: EPDFAnnotationData = JSON.parse(JSON.stringify(a))
                                b.fieldName = a.fieldName + 'b';
                                b.id = a.id + 'b';
                                b.rect[0] += 53;
                                let newRect = currentPage.getViewport({ scale: this.dpiRatio })
                                    .convertToViewportRectangle(b.rect);
                                this.addInput(b, newRect);
                            }
                            if (a.fieldName == 't_charge') {
                                let b: EPDFAnnotationData = JSON.parse(JSON.stringify(a))
                                b.fieldName = a.fieldName + 'b';
                                b.id = a.id + 'b';
                                b.rect[0] += 53;
                                let newRect = currentPage.getViewport({ scale: this.dpiRatio })
                                    .convertToViewportRectangle(b.rect);
                                this.addInput(b, newRect);
                            }

                            if (a.fieldName == 'amt_paid') {
                                let b: EPDFAnnotationData = JSON.parse(JSON.stringify(a))
                                b.fieldName = a.fieldName + 'b';
                                b.id = a.id + 'b';
                                b.rect[0] += 44;
                                let newRect = currentPage.getViewport({ scale: this.dpiRatio })
                                    .convertToViewportRectangle(b.rect);
                                this.addInput(b, newRect);
                            }
                            if(a.fieldName == 'fac_street'){
                                let b: EPDFAnnotationData = JSON.parse(JSON.stringify(a))
                                b.rect[3] += 6;
                                let newRect = currentPage.getViewport({ scale: this.dpiRatio })
                                    .convertToViewportRectangle(b.rect);
                                this.addInput(b, newRect);

                                let c: EPDFAnnotationData = JSON.parse(JSON.stringify(a))
                                c.fieldName = a.fieldName + '2';
                                c.id = a.id + '2';
                                c.rect[3]-=3;
                                let newRect1 = currentPage.getViewport({ scale: this.dpiRatio })
                                    .convertToViewportRectangle(c.rect);
                                this.addInput(c, newRect1);
                            }
                            if(a.fieldName == 'fac_name'){
                                let b: EPDFAnnotationData = JSON.parse(JSON.stringify(a))
                                b.rect[3] += 5;
                                let newRect = currentPage.getViewport({ scale: this.dpiRatio })
                                    .convertToViewportRectangle(b.rect);
                                this.addInput(b, newRect);
                            }
                            if(a.fieldName == 'doc_street')
                            {
                                let b: EPDFAnnotationData = JSON.parse(JSON.stringify(a))
                                b.rect[3] += 6;
                                let newRect = currentPage.getViewport({ scale: this.dpiRatio })
                                    .convertToViewportRectangle(b.rect);
                                this.addInput(b, newRect);

                                let c: EPDFAnnotationData = JSON.parse(JSON.stringify(a))
                                c.fieldName = a.fieldName + '2';
                                c.id = a.id + '2';
                                c.rect[3]-=3;
                                let newRect1 = currentPage.getViewport({ scale: this.dpiRatio })
                                    .convertToViewportRectangle(c.rect);
                                this.addInput(c, newRect1);
                            }
                            if(a.fieldName == 'doc_name'){
                                let b: EPDFAnnotationData = JSON.parse(JSON.stringify(a))
                                b.rect[3] += 5;
                                let newRect = currentPage.getViewport({ scale: this.dpiRatio })
                                    .convertToViewportRectangle(b.rect);
                                this.addInput(b, newRect);
                            }
                        });
                    return;
                }),
                this.claimService.getHFCAClaimInfo(this.claimIds)
                ).pipe(takeUntil(this.unsubscribe)).subscribe(([_, info]: [any, any]) => {
                    this.fillPrinForm(info);                    
                }
            );
        }
    }

    private addInput(annotation: EPDFAnnotationData, rect: any = null): void {
        const control = this.createInput(annotation, rect);
        if (control)
            this.claimsToPrint.forEach(c => {
                for (let i = 1; i <= c.cmsPages; i++)
                {
                    if (annotation.fieldType === 'Tx') {
                        this.myForms['' + c.claimId + i].addControl(annotation.fieldName, new FormControl(annotation.fieldValue || ''));
                    }
                    else if (annotation.fieldType === 'Btn') {
                        this.myForms['' + c.claimId + i].addControl(annotation.fieldName + '_' + annotation.exportValue, new FormControl(''));
                    }
                }
            }
            );
    }

    private createInput(annotation: EPDFAnnotationData, rect: number[] | null = null) {
        if (annotation.fieldName === 'Clear Form') return null;

        const formControl = annotation.fieldName;

        const input = new Input();
        input.id = annotation.id;
        // input.title = annotation.fieldName;
        input.type = 'text';
        input.value = '';

        // if (annotation.fieldType === 'Tx') {
            input.name = annotation.fieldName;
        // }

        if (annotation.fieldType === 'Btn') {
            input.name += '_' + annotation.exportValue;
        }

        if (rect) {
            input.top = rect[1] - (rect[1] - rect[3]);
            input.left = rect[0];
            input.height = (rect[1] - rect[3]);
            input.width = (rect[2] - rect[0]);

            // if (annotation.fieldType === 'Btn') {
            //     input.left -= 5;
            // }
        }

        this.inputList.push(input);

        // if (this.inputList.filter((i: any) => i.name === annotation.fieldName).length === 1)
            return formControl;
        // else
        //     return null;
    }

    public getInputPosition(input: Input): any {
        return {
            top: `${input.top}px`,
            left: `${input.left}px`,
            height: `${input.height}px`,
            width: `${input.width}px`,
        };
    }
}

interface EPDFAnnotationData {
    rect: any;
    subtype: string;
    id: any;
    exportValue: string;
    fieldName: string;
    fieldType: string;
    fieldValue: string;
}

export class Input {
    name: string;
    type: string;
    top: number;
    left: number;
    width: number;
    height: number;
    value: any;
    checked: boolean;
    id: any;
    title: any;
}
