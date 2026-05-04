import { Injectable } from '@angular/core';
import { HttpService } from '../http.service';
import { Observable } from 'rxjs/internal/Observable';

declare var google: any;

enum FrequencyTypes {
    Daily = 1,
    Weekly = 2,
    Monthly = 3,
    Total = 4
}

@Injectable({
    providedIn: 'root'
})
export class FormBuilderService {
    constructor(private http: HttpService) {

    }

    //$builder.forms['default'] = [];

    isSessionNoteDraft: boolean = false;

    // heper methods
    validDate(testdate: any) {
        /// <summary>
        /// Helper method that validate the Date
        /// </summary>
        /// <param name="testdate" type="Date">The date to validate</param>
        /// <returns type="bool">Returns if date is valid or not.</returns>
        var date_regex = /^(0[1-9]|1[0-2])\/(0[1-9]|1\d|2\d|3[01])\/(19|20)\d{2}$/;
        return date_regex.test(testdate);
    };

    validField(validationType: any, value: any) {
        var input_regex = null;
        switch (validationType) {
            case "[email]":
                input_regex = /^([a-zA-Z0-9_\-\.]+)@([a-zA-Z0-9_\-\.]+)\.([a-zA-Z]{2,5})$/;
                break;
            case "[number]":
                input_regex = /^-*[0-9,\.]+$/;
                break;
            default:
        }

        if (input_regex != null) {
            if (!input_regex.test(value))
                return false;
        }
        return true;
    }



    getInput(elementTypeId: number, inputList: any) {
        return inputList.where((item: any) => item.id == elementTypeId);
    };

    metadata: any = {};
    templateControls = [];
    formResponse: any;
    formId: number;
    hideLoader = false;
    isSaveNewSessionNoteResponse = false;

    isInternational: boolean;

    buildFromAPI(url: string) {
        return this.http.get(url).subscribe((result: any) => {
            var data = result.data;
            if (data.childProfileId == 0) {
                return;
            }

            this.metadata.id = data.id;
            this.metadata.name = data.name;
            this.metadata.accountInfoId = data.accountInfoId;
            this.metadata.funderId = data.funders.length == 0 ? [] : data.funders[0].id;
            this.templateControls = data.formElements;

            this.clearTemplate();

            var defaultValue = {};


            // add all form elements to the default control
            this.templateControls.each((cntrl: any) => {
                var defaultVal = cntrl.isArray ? cntrl.defaultValueArray : cntrl.component == "progressReport" ? cntrl.defaultValueInput.split("<body")[1].split(">").slice(1).join(">").split("</body>")[0] : cntrl.defaultValueInput;
                var defaultCheckboxVal = [];

                if ((cntrl.component == "checkbox" || cntrl.component == "select") && defaultVal != null && defaultVal.length > 0) {
                    if (cntrl.component == "checkbox") {
                        for (var i = 0; i < cntrl.options.length; i++) {
                            if (cntrl.options[i].Id == defaultVal[0].Id) {
                                defaultCheckboxVal.push(true);
                            } else {
                                var isExisted = false;
                                // multiful checkbox
                                for (var j = 0; j < defaultVal.length; j++) {
                                    if (defaultVal[j].Id == cntrl.options[i].Id) {
                                        defaultCheckboxVal.push(true);
                                        isExisted = true;
                                        break;
                                    }
                                }
                                if (!isExisted)
                                    defaultCheckboxVal.push(false);
                            }
                        }
                        defaultVal = defaultCheckboxVal;
                    } else if (cntrl.component == "select") {
                        for (var i = 0; i < cntrl.options.length; i++) {
                            if (cntrl.options[i].Id == defaultVal) {
                                defaultVal = cntrl.options[i];
                            }
                        }
                    }
                }
                //var rptControl = cntrl.id;
                //window[rptControl] = $builder.insertFormObject('default', cntrl.index, cntrl);
                defaultValue[cntrl.id] = defaultVal;
            });
            //hasResponses = data.hasResponses;

            this.formResponse = data.formResponse;
            return defaultValue;
        });
    }

    buildTemplate(template: any, client: any, appointment: any, hideLoader: boolean, displayDate: any) {

        var clientId = (client) ? "&childProfileId=" + client : "";
        var appointmentId = (appointment) ? "&appointmentId=" + appointment : "";
        var displayDateValue = displayDate ? "&displayDate=" + displayDate : "";

        var funderId = template.Funders.length == 0 ? null : template.Funders[0].Id;

        var url = "/HealthCare/FunderForm/GetFormInfoAsync?formId=" + template.id + "&hcFunderId=" + funderId + "&name=" + template.name + clientId + appointmentId + displayDateValue;

        this.formId = template.id;
        hideLoader = hideLoader == true;

        return this.buildFromAPI(url);
    }

    buildSessionNoteDraft(draft: any, appointment: any) {
        this.hideLoader = true;
        var url = "/HealthCare/FunderForm/GetFormInfoAsync?formId=" + draft.FormId + "&childProfileId=" + appointment.ClientId + "&serviceLineId=" + appointment.ServiceId + "&formResponseId=" + draft.Id + "&hcFunderId=" + appointment.FunderId + "&appointmentId=" + appointment.AppointmentId;

        this.formId = draft.FormId;

        return this.buildFromAPI(url);
    }

    files: any[] = [];
    ////Set on form builder object directive's scope.
    //RTA.HealthCare = RTA.HealthCare || {};
    //RTA.HealthCare.onFileSelect = function ($files, formName) {
    //    window[formName] = $files;
    //    if (files[formName] == undefined) {
    //        files[formName] = [];
    //    }
    //    files[formName].push($files[$files.length - 1]);
    //    /*
    //     $scope.File = $files[0];
    //     console.log($scope.File);
    //     */
    //};

    filesToUpload: any[] = [];
    uploadGraph(clientId: any, isDraft: boolean) {
        //var data = { childProfileId: clientId };
        //this.files.each((i: number, val: any) => {
        //    this.files[i].each((item: any) => {
        //        this.filesToUpload.push(item);
        //    });


        //});
        //if (this.filesToUpload.length == 0 && isDraft != true) {
        //    alert("No graph file added");
        //    return ;
        //}

        //$upload.upload({
        //    url: '/HealthCare/FunderForm/UploadGraphAsync',
        //    method: 'POST',
        //    data: data,
        //    file: this.filesToUpload
        //})
        //    .success((data, status, headers, config) {
        //        defer.resolve();
        //    });

        //return defer.promise;
    }


    saveTemplate(inputList: any, clientId: number, serviceId: number, isDraft: boolean, responseId: number, appointmentData: any, fileName: string, isUserSave: boolean, currentGeoLocation: any) {
        //this.isSessionNoteDraft = isDraft;

        //if (this.templateControls.length == 0) {
        //    alert("Build the template before saving.");
        //}


        //if (isDraft !== undefined && isDraft != true && (responseId == null || responseId == 0)) {
        //    this.formResponse = null;
        //    this.isSaveNewSessionNoteResponse = true;
        //}

        //if (isUserSave == true) {
        //    this.isSaveNewSessionNoteResponse = true;
        //}

        //if (this.formResponse === undefined || this.formResponse == null) {
        //    this.formResponse = {
        //        id: 0,
        //        formId: this.formId,
        //        name: "new name",
        //        childProfileId: 0,
        //        serviceLineId: 0,
        //    };
        //}

        //this.formResponse.childProfileId = clientId;
        //this.formResponse.serviceLineId = serviceId;

        //var returnResponses: any[] = [];
        //var validClientReportInputs = true;
        //var validClientInputs = true;
        //var validClientSignature = true;
        //var datesToValidate: any[] = [];
        //var isProgressReport = false;
        //this.templateControls.each((cntrl: any) => {
        //    var input = cntrl.isNoInput ? {} : this.getInput(cntrl.id, inputList)[0];
        //    var validationType = cntrl.validation;
        //    //if(cntrl.isMultipleInput || cntrl.hasOptionValues) {
        //    cntrl.fieldElements.each((index: any, fe: any) => {

        //        var val: any = "";
        //        var inputVal = input.value !== "";
        //        var inpp = (input.value !== "undefined" && input.value !== null);
        //        if ((input.value !== "undefined" && input.value !== null) && input.value !== "") {

        //            if (typeof(input.value) != "string") {

        //                if (cntrl.isMultipleInput) {
        //                    val = typeof(input.value) === "object" ? input.value.Id : input.value[index] != undefined ? input.value[index].Name : null;
        //                }
        //                else if (cntrl.hasOptionValues) {
        //                    val = input.value.Id;
        //                }
        //                else if (typeof(input.value) == "boolean") {
        //                    val = input.value;
        //                }
        //            }
        //            else {
        //                val = input.value; //string
        //            }
        //        }
        //        else {
        //            val = null; //noInput
        //        }

        //        if (cntrl.label == "Report Name") {
        //            this.formResponse.name = val;
        //        }

        //        var responseCount = fe.Responses.length == 0 ? 1 : fe.Responses.length;
        //        for (var i = 0; i < responseCount; i++) {

        //            var finalVal: any = "";
        //            var dateLastModified = null;

        //            if ((input.value !== "undefined" && input.value !== "") || cntrl.isNoInput) { // a value has been set or we're dealing with control with no input
        //                finalVal = val; // take the value..
        //            }
        //            else if (fe.Responses.length > 0 && cntrl.component != "behaviorFunctions") { // a value has not been set but there is an existing response
        //                finalVal = fe.Responses[i].value;
        //            }
        //            if (input.value == "") {
        //                finalVal = "";
        //            }

        //            if (cntrl.isProgressReport == true && isDraft != true) {
        //                isProgressReport = true;
        //                if (finalVal == null || finalVal == "") {
        //                    validClientReportInputs = false;
        //                }
        //                else {
        //                    datesToValidate.push(finalVal);
        //                }
        //            }
        //            if (cntrl.required && (finalVal === null || finalVal === "") && isDraft != true && cntrl.component !== "digitalSignature") {
        //                if (cntrl.component.toLowerCase().indexOf('address') !== -1 || cntrl.component.toLowerCase().indexOf('contact') !== -1) {
        //                    if (this.isInternational) {
        //                        if (fe.name != "State") {
        //                            validClientReportInputs = false;
        //                        }
        //                    }
        //                    else {
        //                        if (fe.name != "Country" && fe.name != "Town" && fe.name != "State / Town") {
        //                            validClientReportInputs = false;
        //                        }
        //                    }
        //                }
        //                else {
        //                    validClientReportInputs = false;
        //                }
        //            }

        //            if (cntrl.component == "staffSignature" && cntrl.required == true && isDraft != true) {
        //                // account for first entry when value is 0 or when re-entring form with no signature found
        //                if (finalVal != true && (cntrl.defaultValueInput == "0" || cntrl.defaultValueInput == "No signature found")) {
        //                    validClientReportInputs = false;
        //                }
        //            }

        //            if (cntrl.component === "digitalSignature") {
        //                //dateLastModified = input.value[0].DateLastModified;
        //            }

        //            if (cntrl.component === "digitalSignature" && !cntrl.isParentSignature) {
        //                if (cntrl.required && (finalVal === null || finalVal === '')) validClientReportInputs = false;
        //            }

        //            if (cntrl.component === "digitalSignature" && cntrl.isParentSignature) {
        //                if (cntrl.required && (finalVal === null || finalVal === '')) validClientReportInputs = false;
        //                var count = 0;
        //                input.value.forEach((item: any) => {
        //                    if (item.Name) count++;
        //                });
        //                if (count > 0 && count < 3) {
        //                    validClientSignature = false;
        //                }
        //                if (fe.name === 'SignatureId' && finalVal && validClientSignature && this.isSaveNewSessionNoteResponse) {
        //                    if (currentGeoLocation) {
        //                        appointmentData.ParentLatitude = currentGeoLocation.latitude;
        //                        appointmentData.ParentLongitude = currentGeoLocation.longitude;
        //                        appointmentData.ParentVerifiedAddress = currentGeoLocation.address;
        //                    }
        //                }
        //            }

        //            if (validationType != null && !(cntrl.required == false && (finalVal == null || finalVal == "")) && isDraft != true) {
        //                if (!this.validField(validationType, finalVal)) {
        //                    validClientInputs = false;
        //                }
        //            }


        //            var response = {
        //                id: fe.Responses.length == 0 || (isDraft == true && responseId == null) || (responseId == null || responseId == 0) ? 0 : fe.Responses[i].id,
        //                hcFormResponseId: (isDraft == true) ? responseId : this.formResponse.id,
        //                hcElementTypeId: fe.hcElementTypeId,
        //                hcFormFieldId: fe.hcFormFieldId,
        //                value: finalVal,
        //                DateLastModified: dateLastModified
        //            };
        //            returnResponses.push(response);
        //        }

        //    });
        //});

        //// Validation of dates only for client progress report
        //if (datesToValidate.length > 0 && isDraft != true) {

        //    if (datesToValidate.length != 2) {
        //        validClientReportInputs = false;
        //        //return;
        //    }
        //    if (!this.validDate(datesToValidate[0]) || !this.validDate(datesToValidate[1])) {
        //        validClientReportInputs = false;
        //        //$scope.dateErrorFormat = 2;
        //        //return;
        //    }

        //    var d1 = Date.parse(datesToValidate[0]);
        //    var d2 = Date.parse(datesToValidate[1]);

        //    if (d1 > d2) {
        //        validClientReportInputs = false;
        //    }
        //}

        //if (this.formResponse.name == null) {
        //    this.formResponse.name = "";
        //}

        //if (!validClientSignature && this.isSaveNewSessionNoteResponse) {
        //    alert("Parent signature cannot be partial. You need to fill all 3 signature boxes.");
        //    return ;
        //}

        //if ((validClientReportInputs == false || validClientInputs == false) && isDraft != true) {
        //    $validator.validate($rootScope, 'default');
        //    alert("Please input required field or valid input data.");
        //    return defer.promise;
        //}

        //var hideLoader = (isDraft == true);
        //var formResponseData = { ...this.formResponse };
        //if (isDraft == true) {
        //    formResponseData.id = responseId;
        //}


        //this.http.post("/HealthCare/FunderForm/SaveResponseAsync", {
        //    responses: returnResponses,
        //    formResponse: formResponseData,
        //    formId: this.formId != null ? this.formId : this.formResponse.formId,
        //    containProgressReport: isProgressReport,
        //    isSaveNewSessionNote: this.isSaveNewSessionNoteResponse,
        //    isSessionNoteDraft: isDraft,
        //    appointment: appointmentData,
        //    fileName: fileName,
        //}).subscribe((result: any) => {
        //    this.isSessionNoteDraft = false;
        //    var data = result.data;
        //    var responses = data.Responses;

        //    if (responses === undefined) {
        //        redirectToLogout();
        //    }

        //    result = {
        //        sessionNoteResponseId: responses[0].hcFormResponseId,
        //        sessionNoteFormId: this.formResponse.formId,
        //        SavedParentSignature: data.SavedParentSignature,
        //        responses: responses,
        //        AppointmentWithChanges: data.AppointmentWithChanges
        //    };

        //    $timeout(function () {
        //        uploadGraph(clientId, false).then(function (graphData) {

        //            defer.resolve(result);
        //        }).catch(function (error) {
        //            defer.resolve(result);
        //        });
        //    }, 300);
        //});

        //this.isSaveNewSessionNoteResponse = false;

        //return defer.promise;
    }

    getSessionNoteElements(sessionNoteResponseId: number, clientId: number, appointmentId: number, hideLoader: boolean) {
        return this.http.post("/HealthCare/FunderForm/GetFormResponsesAsync", { responseId: sessionNoteResponseId, cId: clientId, appointmentId: appointmentId }).subscribe((result: any) => {
            var data = result.data;

            var fieldElements = data.fieldElementResponses;

            return {
                Elementes: fieldElements,
                EnteredBy: data.EnteredBy,
                EnteredOn: data.EnteredOn,
                containsDailySummarySheet: data.containsDailySummarySheet,
                dailyDataReports: data.DailyDataReports
            };
        });
    }

    buildSessionNoteResponse(appointment: any, isDraft: boolean) {
        this.hideLoader = isDraft == true;
        var sessionNoteFormId = isDraft == true ? appointment.SessionNoteDraftFormId : appointment.SessionNoteFormId;
        var sessionNoteResponseId = isDraft == true ? appointment.SessionNoteDraftResponseId : appointment.SessionNoteResponseId;
        if (isDraft == true && appointment.SessionNoteDraftResponseId != null && appointment.SessionNoteDraftResponseId > 0 && appointment.SessionNoteDraftResponseId == appointment.SessionNoteResponseId) {
            sessionNoteResponseId = null;
        }

        var url = "/HealthCare/FunderForm/GetFormInfoAsync?formId=" + sessionNoteFormId + "&childProfileId=" + appointment.ClientId + "&serviceLineId=" + appointment.ServiceId + "&formResponseId=" + sessionNoteResponseId + "&hcFunderId=" + appointment.FunderId + "&appointmentId=" + appointment.AppointmentId;

        this.formId = sessionNoteFormId;

        return this.buildFromAPI(url);
    }

    updateSessionNoteElements(appointment: any, isDraft: boolean) {
        this.hideLoader = isDraft == true;
        var sessionNoteFormId = isDraft == true ? appointment.SessionNoteDraftFormId : appointment.SessionNoteFormId;
        var sessionNoteResponseId = isDraft == true ? appointment.SessionNoteDraftResponseId : appointment.SessionNoteResponseId;
        if (isDraft == true && appointment.SessionNoteDraftResponseId != null && appointment.SessionNoteDraftResponseId > 0 && appointment.SessionNoteDraftResponseId == appointment.SessionNoteResponseId) {
            sessionNoteResponseId = null;
        }

        var url = "/HealthCare/FunderForm/GetFormInfoAsync?formId=" + sessionNoteFormId + "&childProfileId=" + appointment.ClientId + "&serviceLineId=" + appointment.ServiceId + "&formResponseId=" + sessionNoteResponseId + "&hcFunderId=" + appointment.FunderId + "&appointmentId=" + appointment.AppointmentId;

        return this.http.get(url).subscribe((result: any) => {
            this.templateControls = result.data.formElements;
            return result.data;
        });
    }

    buildBehaviorPlanResponse(formId: number, formResponseId: number, childProfileId: number) {
        var url = "/HealthCare/FunderForm/GetFormInfoAsync?formId=" + formId + "&childProfileId=" + childProfileId + "&formResponseId=" + formResponseId;

        return this.buildFromAPI(url);
    }

    deleteSessionNoteResponse(sessionNoteResponseId: number) {
        this.http.post("/HealthCare/FunderForm/DeleteReport", { id: sessionNoteResponseId }).subscribe((result: any) => {
            if (!result.data.success)
                alert("There was a problem deleting session note respone.");
        });
    }

    getFunderFormViewHtml() {
        //var clone = $("#divFunderFormView").clone();
        //clone.find(".signatureCanvas").remove();
        //clone.find(".notPrintable").remove();

        //// for details with plus sign need to replace with encoded html to print
        //clone.find(".dailyDataSummaryDetail").text((index: any, text: any) => {
        //    return text.replace(/\+/g, '&#43;');
        //});

        //return clone.html();
    }

    printSessionNoteResponse(sessionNoteResponseId: any) {
        ///// <summary>
        ///// Print the note response that is displayed.
        ///// </summary>
        ///// <param name="sessionNoteResponseId" type="int">Takes the session note response id.</param>
        //var styleSheet = "<link rel=\"stylesheet\" type=\"text/css\" href=\"" + GetDomainUrl() + "/styles/Healthcare/Shared/Common.css\" /><br/>";
        //// adding printing margins
        //styleSheet += "<style type='text/css'>body,head { margin: 0mm 15mm 15mm 15mm; background: none; } .FloatContainer:after { content:\"\\a\\a\"; white-space: pre; } .FloatContainer:before { content:\"\\a\\a\"; white-space: pre; } div.FloatContainer {overflow: hidden;} textarea { display: none; } .responseValue { clear:both; }</style>";

        //var html = styleSheet + this.getFunderFormViewHtml();

        //var printForm = document.createElement("form");
        //printForm.id = "printForm";

        //var element1 = document.createElement("input");
        //element1.type = 'hidden';
        //var element2 = document.createElement("input");
        //element2.type = 'hidden';
        //var element3 = document.createElement("input");
        //element3.type = 'hidden';

        //printForm.method = "POST";
        //printForm.action = "/HealthCare/FunderForm/ExportFunderFormReport";

        //element1.name = "pdfHtmlEncoded";
        //element1.value = escape(html);
        //printForm.appendChild(element1);
        //element2.name = "formResponseId";
        //element2.value = escape(sessionNoteResponseId);
        //printForm.appendChild(element2);

        //element3.name = "savetoFileCabinet";
        //element3.value = escape('false');
        //printForm.appendChild(element3);

        //document.body.appendChild(printForm);

        //printForm.submit();

        //$("#printForm").remove();
    }

    clearTemplate = function () {
        //$builder.forms['default'].length = 0;
    }

    saveSessionNoteToFileCabinet(sessionNoteResponseId: any, clientId: any, saveFileName: string, appointmentId: any, displayDate: any, actualStartTime: any, actualEndTime: any): Observable<any> {
        return this.http.post("/core/api/scheduling/FunderForm/ExportSessionNoteAsync", {
            formResponseId: sessionNoteResponseId,
            childProfileId: clientId,
            saveFileName: saveFileName,
            referenceId: appointmentId,
            displayDate: displayDate,
            actualStartTime: actualStartTime,
            actualEndTime: actualEndTime
        });
    };

    getFormResponse(formId: number, formResponseId: number, childProfileId: number) {
        var url = "/HealthCare/FunderForm/GetFormInfoAsync?formId=" + formId + "&childProfileId=" + childProfileId + "&formResponseId=" + formResponseId;

        return this.http.get(url).subscribe((result: any) => {
            var data = result.data;
            this.metadata.id = data.id;
            this.metadata.name = data.name;
            this.metadata.accountInfoId = data.accountInfoId;
            this.metadata.funderId = data.funders.length == 0 ? [] : data.funders[0].id;

            this.formResponse = data.formResponse;
            return this.formResponse;
        });
    }

    checkSessionNoteDraft() {
        return this.isSessionNoteDraft;
    }

    getDailySummaryData(childProfileId: number, startDate: Date, memberId: number | null, includeUnlinked: boolean, appointmentId: number, includeAll: boolean): Observable<any[]> {
        return this.http.get("/core/api/scheduling/FunderForm/GetDailyDataReportAsync?childProfileId=" + childProfileId +
            "&dataDate=" + startDate.toLocaleDateString() + "&memberId=" + memberId + "&includeUnlinked=" + includeUnlinked + "&appointmentId=" + appointmentId + "&includeAll=" + includeAll);
    }
}