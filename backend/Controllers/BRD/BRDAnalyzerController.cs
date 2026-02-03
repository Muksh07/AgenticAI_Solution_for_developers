using Azure.Core;
using backend.Models;
using backend.Service;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient.DataClassification;
using Microsoft.Identity.Client;
using Microsoft.VisualBasic;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.Eventing.Reader;
using System.IO;
using System.Linq;
using System.Reflection.Emit;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Runtime.Intrinsics.X86;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;
using static System.Net.Mime.MediaTypeNames;
using static System.Runtime.InteropServices.JavaScript.JSType;


namespace backend.Controllers.BRD
{
    [ApiController]
    [Route("api/[controller]")]
    public class BRDAnalyzerController : ControllerBase
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<BRDAnalyzerController> _logger;
        private readonly Functionality _functionality;
        // private readonly DabaseFunctionality _DabaseFunctionality;
        public BRDAnalyzerController(HttpClient httpClient, ILogger<BRDAnalyzerController> logger, Functionality functionality)
        {
            _httpClient = httpClient;
            _logger = logger;
            _functionality = functionality;
            //  _DabaseFunctionality = DabaseFunctionality;

        }

        [HttpPost("log")]
        public IActionResult LogFrontendMessage([FromBody] LogMessageDto log)
        {
            _logger.LogInformation("Frontend Log: {Message}", log.Message);
            return Ok(new { message = "Log received" });
        }


        [HttpPost("analyse")]
        public async Task<IActionResult> AnalyzeBRD([FromBody] BRDRequestModel request)
        {
            _logger.LogInformation("AnalyzeBRD called with task: {Task}", request.task);

            if (string.IsNullOrEmpty(request.brdContent) || string.IsNullOrEmpty(request.task))
            {
                _logger.LogWarning("AnalyzeBRD missing brdContent or task.");
                return BadRequest("Please provide Business Requirement and Task to perform");
            }

            try
            {
                string strRequirements = string.Empty;
                string strtask = request.task.Trim().ToLower();
                string strTraceIDsDetail = string.Empty;
                string result = string.Empty;
                string resultStructure = string.Empty;
                string solutionOverview = string.Empty;
                string strHierarchy = string.Empty;
                if (request.stepno == 0)
                {
                    if (strtask.StartsWith("udo:")) //Suggest the Content in the prompt specified format
                    {
                        strtask = _functionality.GetPromptTask(0, 0);
                        result = await _functionality.AnalyzeBRD(request.brdContent, strtask, 0);
                        if (result != null)
                        {
                            if (!string.IsNullOrEmpty(result))
                            {
                                result = _functionality.GetHeaderFormatedforUDO(result);
                            }
                        }
                    }
                    //---------------------------------
                    else
                    {
                        result = await _functionality.AnalyzeBRD(request.brdContent, strtask, 0);
                    }
                    //---------------------------
                }
                else if (request.stepno == 1)
                {

                    string strtaskOptimize = string.Empty;
                    string requirementTraceIDs = string.Empty;
                    string BusinessObjectives = string.Empty;
                    string BusinessRequirements = string.Empty;
                    string FunctionalRequirements = string.Empty;
                    string TechnicalRequirements = string.Empty;
                    string DataRequirements = string.Empty;
                    string ScopeofWork = string.Empty;
                    string AssumptionsandConstraints = string.Empty;
                    string NonFunctionalRequirements = string.Empty;
                    string SecurityAccessControl = string.Empty;
                    string ProjectDeliverables = string.Empty;
                    string Milestones = string.Empty;
                    string SuccessCriteria = string.Empty;
                    string OtherKeyPoints = string.Empty;

                    if (!strtask.StartsWith("prompt:") && !strtask.StartsWith("udo:"))
                    {

                        strtaskOptimize = _functionality.GetPromptTask(0, 4);

                        BusinessObjectives = _functionality.GetSectionData(request.brdContent, "Business Objectives");
                        if (BusinessObjectives != null)
                        {
                            if (BusinessObjectives.Trim() == string.Empty)
                            {
                                strtask = _functionality.GetPromptTask(0, 1, "Business Objectives");
                                BusinessObjectives = await _functionality.AnalyzeBRD(request.brdContent, strtask, 0);
                            }
                            else
                            {
                                BusinessObjectives = await _functionality.AnalyzeBRD(BusinessObjectives, strtaskOptimize, 0);
                            }
                        }
                        else
                        {
                            strtask = _functionality.GetPromptTask(0, 1, "Business Objectives");
                            BusinessObjectives = await _functionality.AnalyzeBRD(request.brdContent, strtask, 0);
                        }
                        if (BusinessObjectives.ToLower().Trim().StartsWith("error") || string.IsNullOrEmpty(BusinessObjectives.Trim()))
                        {
                            throw new Exception("API response is null or empty");
                        }
                        BusinessRequirements = _functionality.GetSectionData(request.brdContent, "Business Requirements");

                        if (BusinessRequirements != null)
                        {
                            if (BusinessRequirements.Trim() == string.Empty)
                            {
                                strtask = _functionality.GetPromptTask(0, 1, "Business Requirements");
                                BusinessRequirements = await _functionality.AnalyzeBRD(request.brdContent, strtask, 0);
                            }
                            else
                            {
                                BusinessRequirements = await _functionality.AnalyzeBRD(BusinessRequirements, strtaskOptimize, 0);
                            }
                        }
                        else
                        {
                            strtask = _functionality.GetPromptTask(0, 1, "Business Requirements");
                            BusinessRequirements = await _functionality.AnalyzeBRD(request.brdContent, strtask, 0);
                        }
                        if (BusinessRequirements.ToLower().Trim().StartsWith("error") || string.IsNullOrEmpty(BusinessRequirements.Trim()))
                        {
                            throw new Exception("API response is null or empty");
                        }
                        FunctionalRequirements = _functionality.GetSectionData(request.brdContent, "Functional Requirements");
                        if (FunctionalRequirements != null)
                        {
                            if (FunctionalRequirements.Trim() == string.Empty)
                            {
                                strtask = _functionality.GetPromptTask(0, 1, "Functional Requirements");
                                FunctionalRequirements = await _functionality.AnalyzeBRD(request.brdContent, strtask, 0);
                            }
                            else
                            {
                                FunctionalRequirements = await _functionality.AnalyzeBRD(FunctionalRequirements, strtaskOptimize, 0);
                            }
                        }
                        else
                        {
                            strtask = _functionality.GetPromptTask(0, 1, "Functional Requirements");
                            FunctionalRequirements = await _functionality.AnalyzeBRD(request.brdContent, strtask, 0);
                        }
                        if (FunctionalRequirements.ToLower().Trim().StartsWith("error") || string.IsNullOrEmpty(FunctionalRequirements.Trim()))
                        {
                            throw new Exception("API response is null or empty");
                        }
                        TechnicalRequirements = _functionality.GetSectionData(request.brdContent, "Technical Requirements");
                        if (TechnicalRequirements != null)
                        {

                            if (TechnicalRequirements.Trim() == string.Empty)
                            {
                                strtask = _functionality.GetPromptTask(0, 1, "Technical Requirements");
                                TechnicalRequirements = await _functionality.AnalyzeBRD(request.brdContent, strtask, 0);
                            }
                            else
                            {
                                TechnicalRequirements = await _functionality.AnalyzeBRD(TechnicalRequirements, strtaskOptimize, 0);
                            }
                        }
                        else
                        {
                            strtask = _functionality.GetPromptTask(0, 1, "Technical Requirements");
                            TechnicalRequirements = await _functionality.AnalyzeBRD(request.brdContent, strtask, 0);
                        }
                        if (TechnicalRequirements.ToLower().Trim().StartsWith("error") || string.IsNullOrEmpty(TechnicalRequirements.Trim()))
                        {
                            throw new Exception("API response is null or empty");
                        }
                        DataRequirements = _functionality.GetSectionData(request.brdContent, "Data Requirements");
                        if (DataRequirements != null)
                        {
                            if (DataRequirements.Trim() == string.Empty)
                            {
                                strtask = _functionality.GetPromptTask(0, 1, "Data Requirements");
                                DataRequirements = await _functionality.AnalyzeBRD(request.brdContent, strtask, 0);
                            }
                            else
                            {
                                DataRequirements = await _functionality.AnalyzeBRD(DataRequirements, strtaskOptimize, 0);
                            }
                        }
                        else
                        {
                            strtask = _functionality.GetPromptTask(0, 1, "Data Requirements");
                            DataRequirements = await _functionality.AnalyzeBRD(request.brdContent, strtask, 0);
                        }
                        if (DataRequirements.ToLower().Trim().StartsWith("error") || string.IsNullOrEmpty(DataRequirements.Trim()))
                        {
                            throw new Exception("API response is null or empty");
                        }
                        ScopeofWork = _functionality.GetSectionData(request.brdContent, "Scope of Work");
                        if (ScopeofWork != null)
                        {
                            if (ScopeofWork.Trim() == string.Empty)
                            {
                                strtask = _functionality.GetPromptTask(0, 1, "Scope of Work");
                                ScopeofWork = await _functionality.AnalyzeBRD(request.brdContent, strtask, 0);
                            }
                            else
                            {
                                ScopeofWork = await _functionality.AnalyzeBRD(ScopeofWork, strtaskOptimize, 0);
                            }
                        }
                        else
                        {
                            strtask = _functionality.GetPromptTask(0, 1, "Scope of Work");
                            ScopeofWork = await _functionality.AnalyzeBRD(request.brdContent, strtask, 0);
                        }
                        if (ScopeofWork.ToLower().Trim().StartsWith("error") || string.IsNullOrEmpty(ScopeofWork.Trim()))
                        {
                            throw new Exception("API response is null or empty");
                        }
                        AssumptionsandConstraints = _functionality.GetSectionData(request.brdContent, "Assumptions and Constraints");
                        if (AssumptionsandConstraints != null)
                        {
                            if (AssumptionsandConstraints.Trim() == string.Empty)
                            {
                                strtask = _functionality.GetPromptTask(0, 1, "Assumptions and Constraints");
                                AssumptionsandConstraints = await _functionality.AnalyzeBRD(request.brdContent, strtask, 0);
                            }
                            else
                            {
                                AssumptionsandConstraints = await _functionality.AnalyzeBRD(AssumptionsandConstraints, strtaskOptimize, 0);
                            }
                        }
                        else
                        {
                            strtask = _functionality.GetPromptTask(0, 1, "Assumptions and Constraints");
                            AssumptionsandConstraints = await _functionality.AnalyzeBRD(request.brdContent, strtask, 0);
                        }
                        if (AssumptionsandConstraints.ToLower().Trim().StartsWith("error") || string.IsNullOrEmpty(AssumptionsandConstraints.Trim()))
                        {
                            throw new Exception("API response is null or empty");
                        }
                        NonFunctionalRequirements = _functionality.GetSectionData(request.brdContent, "Non-Functional Requirements");
                        if (NonFunctionalRequirements != null)
                        {
                            if (NonFunctionalRequirements.Trim() == string.Empty)
                            {
                                strtask = _functionality.GetPromptTask(0, 1, "Non-Functional Requirements");
                                NonFunctionalRequirements = await _functionality.AnalyzeBRD(request.brdContent, strtask, 0);
                            }
                            else
                            {
                                NonFunctionalRequirements = await _functionality.AnalyzeBRD(NonFunctionalRequirements, strtaskOptimize, 0);
                            }
                        }
                        else
                        {
                            strtask = _functionality.GetPromptTask(0, 1, "Non-Functional Requirements");
                            NonFunctionalRequirements = await _functionality.AnalyzeBRD(request.brdContent, strtask, 0);
                        }
                        if (NonFunctionalRequirements.ToLower().Trim().StartsWith("error") || string.IsNullOrEmpty(NonFunctionalRequirements.Trim()))
                        {
                            throw new Exception("API response is null or empty");
                        }
                        SecurityAccessControl = _functionality.GetSectionData(request.brdContent, "Security Access Control");
                        if (SecurityAccessControl != null)
                        {
                            if (SecurityAccessControl.Trim() == string.Empty)
                            {
                                strtask = _functionality.GetPromptTask(0, 1, "Security Access Control");
                                SecurityAccessControl = await _functionality.AnalyzeBRD(request.brdContent, strtask, 0);
                            }
                            else
                            {
                                SecurityAccessControl = await _functionality.AnalyzeBRD(SecurityAccessControl, strtaskOptimize, 0);
                            }
                        }
                        else
                        {
                            strtask = _functionality.GetPromptTask(0, 1, "Security Access Control");
                            SecurityAccessControl = await _functionality.AnalyzeBRD(request.brdContent, strtask, 0);
                        }
                        if (SecurityAccessControl.ToLower().Trim().StartsWith("error") || string.IsNullOrEmpty(SecurityAccessControl.Trim()))
                        {
                            throw new Exception("API response is null or empty");
                        }
                        ProjectDeliverables = _functionality.GetSectionData(request.brdContent, "Project Deliverables");
                        if (ProjectDeliverables != null)
                        {

                            if (ProjectDeliverables.Trim() == string.Empty)
                            {
                                strtask = _functionality.GetPromptTask(0, 1, "Project Deliverables");
                                ProjectDeliverables = await _functionality.AnalyzeBRD(request.brdContent, strtask, 0);
                            }
                            else
                            {
                                ProjectDeliverables = await _functionality.AnalyzeBRD(ProjectDeliverables, strtaskOptimize, 0);
                            }
                        }
                        else
                        {
                            strtask = _functionality.GetPromptTask(0, 1, "Project Deliverables");
                            ProjectDeliverables = await _functionality.AnalyzeBRD(request.brdContent, strtask, 0);
                        }
                        if (ProjectDeliverables.ToLower().Trim().StartsWith("error") || string.IsNullOrEmpty(ProjectDeliverables.Trim()))
                        {
                            ProjectDeliverables = string.Empty;
                        }
                        Milestones = _functionality.GetSectionData(request.brdContent, "Milestones");
                        if (Milestones != null)
                        {
                            if (Milestones.Trim() == string.Empty)
                            {
                                strtask = _functionality.GetPromptTask(0, 1, "Milestones");
                                Milestones = await _functionality.AnalyzeBRD(request.brdContent, strtask, 0);
                            }
                            else
                            {
                                Milestones = await _functionality.AnalyzeBRD(Milestones, strtaskOptimize, 0);
                            }
                        }
                        else
                        {
                            strtask = _functionality.GetPromptTask(0, 1, "Milestones");
                            Milestones = await _functionality.AnalyzeBRD(request.brdContent, strtask, 0);
                        }
                        if (Milestones.ToLower().Trim().StartsWith("error") || string.IsNullOrEmpty(Milestones.Trim()))
                        {
                            Milestones = string.Empty;
                        }
                        SuccessCriteria = _functionality.GetSectionData(request.brdContent, "Success Criteria");
                        if (SuccessCriteria != null)
                        {
                            if (SuccessCriteria.Trim() == string.Empty)
                            {
                                strtask = _functionality.GetPromptTask(0, 1, "Success Criteria");
                                SuccessCriteria = await _functionality.AnalyzeBRD(request.brdContent, strtask, 0);
                            }
                            else
                            {
                                SuccessCriteria = await _functionality.AnalyzeBRD(SuccessCriteria, strtaskOptimize, 0);
                            }
                        }
                        else
                        {
                            strtask = _functionality.GetPromptTask(0, 1, "Success Criteria");
                            SuccessCriteria = await _functionality.AnalyzeBRD(request.brdContent, strtask, 0);
                        }
                        if (SuccessCriteria.ToLower().Trim().StartsWith("error") || string.IsNullOrEmpty(SuccessCriteria.Trim()))
                        {
                            SuccessCriteria = string.Empty;
                        }
                        OtherKeyPoints = _functionality.GetSectionData(request.brdContent, "Other Key Points");
                        if (OtherKeyPoints != null)
                        {
                            if (OtherKeyPoints.Trim() == string.Empty)
                            {
                                strtask = _functionality.GetPromptTask(0, 1, "Other Key Points");
                                OtherKeyPoints = await _functionality.AnalyzeBRD(request.brdContent, strtask, 0);
                            }
                            else
                            {
                                OtherKeyPoints = await _functionality.AnalyzeBRD(OtherKeyPoints, strtaskOptimize, 0);
                            }
                        }
                        else
                        {
                            strtask = _functionality.GetPromptTask(0, 1, "Other Key Points");
                            OtherKeyPoints = await _functionality.AnalyzeBRD(request.brdContent, strtask, 0);
                        }
                        if (OtherKeyPoints.ToLower().Trim().StartsWith("error") || string.IsNullOrEmpty(OtherKeyPoints.Trim()))
                        {
                            OtherKeyPoints = string.Empty;
                        }

                        result = "Business Objectives: " + "\r\n" + BusinessObjectives + "\r\n" +
                            "Business Requirements: " + "\r\n" + BusinessRequirements + "\r\n" +
                            "Functional Requirements:" + "\r\n" + FunctionalRequirements + "\r\n" +
                            "Technical Requirements:" + "\r\n" + TechnicalRequirements + "\r\n" +
                            "Data Requirements:" + "\r\n" + DataRequirements + "\r\n" +
                            "Scope of Work" + "\r\n" + ScopeofWork + "\r\n" +
                            "Assumptions and Constraints" + "\r\n" + AssumptionsandConstraints + "\r\n" +
                            "Non-Functional Requirements" + "\r\n" + NonFunctionalRequirements + "\r\n" +
                            "Security Access Control:" + "\r\n" + SecurityAccessControl + "\r\n" +
                            "Project Deliverables:" + "\r\n" + ProjectDeliverables + "\r\n" +
                            "Milestones:" + "\r\n" + Milestones + "\r\n" +
                            "Success Criteria:" + "\r\n" + SuccessCriteria + "\r\n" +
                            "Other Key Points" + "\r\n" + OtherKeyPoints;

                        result = result.Replace("BadRequest", "").Replace("Bad Request", "");

                        result += "\r\n" + "greenfield/brownfield:" + "green" + "\r\n";

                        strRequirements = "requirements:start\r\n" + result + "\r\nrequirements:end\r\n";

                        strtask = _functionality.GetPromptTask(1, 7);
                        strHierarchy = await _functionality.AnalyzeBRD(request.brdContent, strtask, 1);

                        if (strHierarchy.ToLower().Trim().StartsWith("error") || string.IsNullOrEmpty(strHierarchy.Trim()))
                        {
                            throw new Exception("API response is null or empty");
                        }
                        //--------------------------------
                        strtask = _functionality.GetPromptTask(0, 10);
                        string pointWiseHeirarachy = await _functionality.AnalyzeBRD(strHierarchy, strtask, 1);

                        strtask = _functionality.GetPromptTask(0, 6);
                        string userflow = await _functionality.AnalyzeBRD(result, strtask, 0);

                        bool booldbobject = userflow.Contains("dbobject:yes");

                        string[] arrayModules = pointWiseHeirarachy.Split("ModSubName:", StringSplitOptions.RemoveEmptyEntries);
                        //string[] arrayLayers = userflow.Split(new string[] { "userdataflow:\n", "userdataflow:", "Layer Name:" }, StringSplitOptions.RemoveEmptyEntries);

                        string strLayerKeyPoints = "";
                        string strFinalLayerKeyPoints = "";
                        Dictionary<string, string> dictReqKeyPoints = new Dictionary<string, string>();
                        int reqCount = 0;
                        string strPrvModuleDetails = "";
                        string strdbbobjectdetail = "";
                        foreach (string module in arrayModules)
                        {
                            strLayerKeyPoints = "";
                            strFinalLayerKeyPoints = "";
                            strtask = _functionality.GetPromptTask(0, 11, module.Trim());
                            strLayerKeyPoints = await _functionality.AnalyzeBRD(result, strtask, 0);

                            strtask = _functionality.GetPromptTask(0, 12, module.Trim());
                            strFinalLayerKeyPoints = await _functionality.AnalyzeBRD("Requirements" + "\r\n" + strLayerKeyPoints + "\r\n" + userflow.Trim(), strtask, 0);

                            //dictReqKeyPoints.Add(module, strFinalLayerKeyPoints);
                            //reqCount++;

                            strtask = _functionality.GetPromptTask(0, 13);

                            if (strPrvModuleDetails != "")
                            {
                                strtask += "\r\n" + "- do not generate details for previously generated details for moduleandsubmodule:" + strPrvModuleDetails;
                            }

                            //requirementTraceIDs = "Req-" + reqCount.ToString() + "\r\n";
                            requirementTraceIDs = await _functionality.AnalyzeBRD("ModuleSubModule:" + module + "\r\n" + strFinalLayerKeyPoints, strtask, 1);
                            requirementTraceIDs = requirementTraceIDs.Replace("Implementation detail:", "Implementation detail:" + "\r\n" + strFinalLayerKeyPoints);
                            strPrvModuleDetails += module.Trim() + ",";

                            dictReqKeyPoints.Add(module.Trim(), requirementTraceIDs);

                            if (booldbobject)
                            {
                                strtask = _functionality.GetPromptTask(0, 14);
                                strdbbobjectdetail += "dbrequirements:" + module.Trim() + "\r\n" + await _functionality.AnalyzeBRD(requirementTraceIDs, strtask, 0) + "\r\n";

                            }

                        }

                        requirementTraceIDs = "";
                        reqCount = 0;
                        foreach (string strk in dictReqKeyPoints.Keys)
                        {
                            reqCount++;

                            requirementTraceIDs += "Req-" + reqCount.ToString() + "\r\n";
                            requirementTraceIDs += dictReqKeyPoints[strk].ToString() + "\r\n\r\n";
                        }
                        strtask = _functionality.GetPromptTask(0, 15, dictReqKeyPoints.Count.ToString());
                        string reqdbobjectdetail = await _functionality.AnalyzeBRD(strdbbobjectdetail, strtask, 1);

                        requirementTraceIDs += "\r\n" + reqdbobjectdetail;


                        //strtask = _functionality.GetPromptTask(0, 12);

                        //foreach (string strk in dictReqKeyPoints.Keys)
                        //{
                        //    requirementTraceIDs = await _functionality.AnalyzeBRD("ModuleSubModule:" + strk + "\r\n" + dictReqKeyPoints[strk], strtask, 1);
                        //}

                        //--------------------------------
                        // need to comment below two lines
                        // strtask = _functionality.GetPromptTask(0, 5);

                        //requirementTraceIDs = await _functionality.AnalyzeBRD(result + "\r\nModuleSubModuleHierarchy:\r\n" + strHierarchy, strtask, 1);


                        if (requirementTraceIDs.ToLower().Trim().StartsWith("error") || string.IsNullOrEmpty(requirementTraceIDs.Trim()))
                        {
                            throw new Exception("API response is null or empty");
                        }


                        strTraceIDsDetail = "\r\ntaggedrequirements:start" + "\r\n" + requirementTraceIDs + "\r\n" + "taggedrequirements:end\r\n";

                        strRequirements += strTraceIDsDetail;

                        result = strRequirements;

                    }
                }
                else if (request.stepno == 2)
                {


                    strRequirements = request.brdContent;


                    string[] taggedrequirements = strRequirements.Split("taggedrequirements:start", StringSplitOptions.RemoveEmptyEntries);


                    if (taggedrequirements.Length == 2)
                    {
                        string solutionOverviewDetails = string.Empty;
                        string solutionDetailsFunctional = string.Empty;
                        string solutionGuidance = string.Empty;


                        string strRequiremetnonly = string.Empty;
                        strRequiremetnonly = taggedrequirements[0].ToString();
                        strTraceIDsDetail = "taggedrequirements:start" + "\r\n" + taggedrequirements[1].ToString();

                        string strReq = taggedrequirements[1].ToString().Replace("taggedrequirements:end", "");

                        taggedrequirements = strReq.Split("Req-", StringSplitOptions.RemoveEmptyEntries);
                        //-----------------------------
                        //strtask = _functionality.GetPromptTask(1, 8);
                        //string solutionStepwise = await _functionality.AnalyzeBRD(strRequiremetnonly, strtask, 1);
                        //-----------------------------
                        // Retrive the solution overview from solidification prompt
                        strtask = _functionality.GetPromptTask(1, 1);
                        solutionOverview = await _functionality.AnalyzeBRD(strRequiremetnonly, strtask, 1);

                        string[] arraystrHierarchy = solutionOverview.ToLower().Split(new[] { "modulesubmodule hierarchy:", "greenfield/brownfield:" }, StringSplitOptions.None);

                        if (arraystrHierarchy.Length > 1)
                        {
                            strHierarchy = arraystrHierarchy[1].Trim();

                        }

                        if (strHierarchy.ToLower().Trim().StartsWith("error") || string.IsNullOrEmpty(strHierarchy.Trim()))
                        {
                            throw new Exception("API response is null or empty");
                        }

                        string[] arrayObj = null;


                        string strReqFunctional = string.Empty;
                        string strReqOthers = string.Empty;

                        foreach (string tag in taggedrequirements)
                        {
                            if (!string.IsNullOrEmpty(tag.Trim()))
                            {
                                if (tag.ToLower().Contains("type: functional"))
                                {
                                    strReqFunctional += "Req-" + tag + "\r\n";

                                }
                                else
                                {
                                    strReqOthers += "Req-" + tag + "\r\n";
                                }
                            }
                        }

                        if (!string.IsNullOrEmpty(strReqOthers) || !string.IsNullOrEmpty(strReqFunctional))
                        {
                            string solutionOverviewval = string.Empty;
                            if (!string.IsNullOrEmpty(strReqOthers))
                            {
                                strReqOthers = "\r\ntaggedrequirements:start" + "\r\n" + strReqOthers + "\r\n" + "taggedrequirements:end\r\n";
                                strtask = _functionality.GetPromptTask(1, 6);
                                solutionGuidance = await _functionality.AnalyzeBRD(strRequiremetnonly + "\r\n" + strReqOthers, strtask, 1);

                                if (solutionGuidance.ToLower().Trim().StartsWith("error") || string.IsNullOrEmpty(solutionGuidance.Trim()))
                                {
                                    solutionGuidance = string.Empty;
                                }
                                else
                                {
                                    arrayObj = solutionGuidance.Split("solution guidance:", StringSplitOptions.RemoveEmptyEntries);

                                    if (arrayObj.Length > 1)
                                    {
                                        solutionGuidance = "solution guidance:" + "\r\n" + arrayObj[arrayObj.Length - 1].ToString().Trim();

                                    }
                                    //solutionOverviewval = solutionGuidance + "\r\n";
                                }
                            }

                            if (!string.IsNullOrEmpty(strReqFunctional))
                            {
                                string functionalReq = string.Empty;
                                string prepSolStructure = string.Empty;
                                string strReqNo = string.Empty;
                                string strPatternReq = @"req-\d+(\.\d+)*";
                                Dictionary<string, string> dictSolStructure = new Dictionary<string, string>();
                                strtask = _functionality.GetPromptTask(0, 6);
                                //-------------------
                                string userflow = await _functionality.AnalyzeBRD(strRequiremetnonly, strtask, 0);
                                //-------------------
                                strtask = _functionality.GetPromptTask(1, 11);
                                string archDiagram = await _functionality.AnalyzeBRD(userflow, strtask, 1);

                                Dictionary<string, string> dictLayerwiseFiles = new Dictionary<string, string>();
                                string strFeedback = string.Empty;

                                foreach (string tag in taggedrequirements)
                                {
                                    if (!string.IsNullOrEmpty(tag.Trim()))
                                    {
                                        if (tag.ToLower().Contains("type: functional"))
                                        {
                                            functionalReq = "Req-" + tag.Trim() + "\r\n";

                                            MatchCollection matches = Regex.Matches(functionalReq, strPatternReq, RegexOptions.IgnoreCase);
                                            strReqNo = "";
                                            if ((matches.Count == 1))
                                            {
                                                strReqNo = matches[0].ToString();


                                                solutionOverviewval = solutionGuidance + "\r\n";
                                                solutionOverviewval += "\r\ntaggedrequirements:start" + "\r\n" + functionalReq + "\r\n" + "taggedrequirements:end\r\n";

                                                if (strHierarchy.Contains("ModuleSubModuleHierarchy") || strHierarchy.Contains("ModuleSubModule Hierarchy"))
                                                {
                                                    solutionOverviewval = "\r\n" + solutionOverviewval + "\r\n" + strHierarchy;
                                                }
                                                else
                                                {
                                                    solutionOverviewval = "\r\n" + solutionOverviewval + "\r\n" + "ModuleSubModuleHierarchy:" + "\r\n" + strHierarchy;
                                                }

                                                if (!userflow.ToLower().Trim().StartsWith("error") && !string.IsNullOrEmpty(userflow.Trim()))
                                                {
                                                    if (!userflow.StartsWith("userdataflow:"))
                                                    {
                                                        solutionOverviewval += "\r\n" + userflow;
                                                    }
                                                    else
                                                    {
                                                        solutionOverviewval += "\r\n" + "userdataflow:" + "\r\n" + userflow;
                                                    }
                                                }

                                                if (string.IsNullOrEmpty(strFeedback.Trim()))
                                                {
                                                    solutionOverviewval += "\r\n" + "instruction to avoid duplicate:\tavoid generating duplicates referring bellow previously generated information:\r\n";
                                                    solutionOverviewval += "no information for this time";
                                                }
                                                else
                                                {
                                                    solutionOverviewval += "\r\n" + "instruction to avoid duplicate:\tavoid generating duplicates referring bellow previously generated information:\r\n";
                                                    solutionOverviewval += strFeedback;
                                                }

                                                strtask = _functionality.GetPromptTask(0, 7);
                                                //------------------
                                                string resultStructureOnly = await _functionality.AnalyzeBRD(solutionOverviewval.Trim(), strtask, 0) + "\r\n";
                                                //------------------
                                                strtask = _functionality.GetPromptTask(0, 9);
                                                strFeedback += await _functionality.AnalyzeBRD(resultStructureOnly, strtask, 0) + "\r\n";

                                                if (!resultStructureOnly.ToLower().Trim().StartsWith("error") && !string.IsNullOrEmpty(resultStructureOnly.Trim()))
                                                {
                                                    if (!dictSolStructure.ContainsKey(strReqNo))
                                                    {
                                                        dictSolStructure.Add(strReqNo, resultStructureOnly);

                                                        string[] layerDetails = resultStructureOnly.ToLower().Split(new string[] { "projectname:", "projectname" }, StringSplitOptions.RemoveEmptyEntries);

                                                        //dictLayerwiseFiles

                                                        foreach (string layer in layerDetails)
                                                        {
                                                            string[] layerDetails1 = layer.ToLower().Split(new string[] { "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);
                                                            if (layerDetails1.Length > 1)
                                                            {
                                                                string strprojectname = layerDetails1[0].ToString().Trim();
                                                                string projectfiles = string.Join(Environment.NewLine, layerDetails1.Skip(1).ToArray());
                                                                bool canAdd = true;
                                                                foreach (string strlyr in dictLayerwiseFiles.Keys)
                                                                {
                                                                    if (_functionality.IsEqualTwoString(strlyr, strprojectname))
                                                                    {
                                                                        dictLayerwiseFiles[strlyr] += "\r\n" + projectfiles;
                                                                        canAdd = false;
                                                                        break;
                                                                    }
                                                                }
                                                                if (canAdd)
                                                                {
                                                                    dictLayerwiseFiles.Add(strprojectname, projectfiles);
                                                                }
                                                            }

                                                        }

                                                    }
                                                }

                                            }
                                        }
                                    }
                                }

                                string solutionName = string.Empty;
                                resultStructure = string.Empty;

                                string[] arraySolutionOverview = solutionOverview.ToLower().Split(new string[] { "application name:", "description:" }, StringSplitOptions.RemoveEmptyEntries);

                                if (arraySolutionOverview.Length > 1)
                                {
                                    solutionName = arraySolutionOverview[1].ToString().Trim().Replace(" ", "");
                                }

                                string rootFolder = solutionName;
                                resultStructure += "SolutionName:" + solutionName + "\r\n";
                                resultStructure += "RootFolder:" + rootFolder + "\r\n";
                                resultStructure += "Project:" + "\r\n\r\n";

                                foreach (string strPrj in dictLayerwiseFiles.Keys)
                                {
                                    resultStructure += "ProjectName:" + strPrj.Trim().Replace(" ", "") + "\r\n";
                                    resultStructure += "Paths:" + "\r\n";
                                    string strdetail = dictLayerwiseFiles[strPrj];
                                    //modulesubmodulename:
                                    string[] prjPath = strdetail.ToLower().Split(new string[] { "modulesubmodulename:" }, StringSplitOptions.RemoveEmptyEntries);
                                    string projectPath = string.Empty;

                                    foreach (string ppath in prjPath)
                                    {
                                        if (!string.IsNullOrEmpty(ppath.Trim()))
                                        {
                                            string[] arrayppath = ppath.ToLower().Split(new string[] { "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);
                                            if (arrayppath.Length > 1)
                                            {
                                                string strprojectPath = arrayppath[0].ToString().Trim().Replace(" ", "");
                                                string strprojectPath1 = arrayppath[1].ToString().Trim().Replace("parentmodule:", "").Replace(" ", "");

                                                if (!string.IsNullOrEmpty(strprojectPath1))
                                                {
                                                    projectPath = strPrj.Trim().Replace(" ", "") + "/" + strprojectPath1; // + "/" + strprojectPath;
                                                }
                                                else
                                                {
                                                    projectPath = strPrj.Trim().Replace(" ", "") + "/" + strprojectPath;

                                                }
                                                resultStructure += "ProjectPath:" + projectPath + "\r\n";
                                                string projectfiles = string.Join(Environment.NewLine, arrayppath.Skip(2).ToArray());
                                                resultStructure += projectfiles + "\r\n\r\n";
                                            }
                                        }
                                    }

                                }

                                //if(solutionOverview.Contains("ArchitectureDiagram:"))
                                //{
                                //    solutionOverview = solutionOverview.Replace("ArchitectureDiagram:", archDiagram);
                                //}

                                if (solutionOverview.Contains("ArchitectureDiagram:"))
                                {
                                    string[] arrayforArch = solutionOverview.Split(new string[] { "ArchitectureDiagram:", "Technology Stack:" }, StringSplitOptions.RemoveEmptyEntries);

                                    if (arrayforArch.Length == 3)
                                    {
                                        solutionOverview = arrayforArch[0] + "\r\n" + archDiagram + "\r\n" + "Technology Stack:" + "\r\n" + arrayforArch[2];
                                    }
                                }

                                if (resultStructure.StartsWith("```"))
                                {
                                    resultStructure = resultStructure.Replace("```yaml", "").Replace("```", "").Trim();
                                }
                                if (!string.IsNullOrEmpty(resultStructure))
                                {
                                    result = "\r\n \r\n" + solutionOverview + "\r\n\r\n Solution Structure: \r\n \r\n" + resultStructure + "\r\n" + "RequirementSummary:" + "\r\n" + strTraceIDsDetail;
                                    result = result.Trim();
                                }
                            }
                        }
                    }

                        if (result is not null)
                        {
                        // await _DabaseFunctionality.SaveOrUpdateProjectAsync(request.id, "AnalyzeBRD");
                        }

                }

                _logger.LogInformation("AnalyzeBRD successfully completed.");

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred in AnalyzeBRD.");
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpPost("solidify")]
        public async Task<ActionResult<SolidificationResponseModel>> SolidifyBRD([FromBody] SolidificationRequestModel request)
        {
            _logger.LogInformation("SolidifyBRD called.");

            if (string.IsNullOrEmpty(request.AnalysisResult))
            {
                _logger.LogWarning("SolidifyBRD missing AnalysisResult.");
                return BadRequest("Please provide Business Requirement and Task to perform");
            }

            try
            {
                string strtask = string.Empty;// _functionality.GetPromptTask(1, 1);

                string[] strSoliify = request.AnalysisResult.ToLower().Split(new string[] { "solution overview:", "solution structure:", "requirementsummary:" }, StringSplitOptions.RemoveEmptyEntries);
                string requirementsummary = string.Empty;
                string strsolutinooverview = string.Empty;
                string strsolutionstructure = string.Empty;
                var result = string.Empty;
                string resultStructure = string.Empty;
                string resultSolutionStrcuture = string.Empty;
                string resultFiles = string.Empty;
                string resultPrep = string.Empty;
                string InsightresultTecnical = string.Empty;
                string dbmsDatabasename = string.Empty;
                Dictionary<string,string > dictDBObj = new Dictionary<string,string>();
                
                if (strSoliify.Length == 3)
                {
                    strsolutinooverview = strSoliify[0].ToString();
                    strsolutionstructure = strSoliify[1].ToString().Replace("\"","");
                    requirementsummary = strSoliify[2].Trim().ToString();
                }
                if (!string.IsNullOrEmpty(strsolutinooverview) && !string.IsNullOrEmpty(strsolutionstructure) &&  !string.IsNullOrEmpty(requirementsummary))
                {
                    //---------------------------------

                    string pattern = @"req-\d+(\.\d+)*";
                    MatchCollection matches = Regex.Matches(requirementsummary, pattern, RegexOptions.IgnoreCase);

                    string strRequirementsummary = requirementsummary.Replace("taggedrequirements:start", "").Replace("taggedrequirements:end","").Trim();
                    string strReq = "";

                    //string.Join(Environment.NewLine, matches.ToArray());

                    HashSet<string> sectionReqs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    
                    foreach (Match match in matches)
                    {
                        sectionReqs.Add(match.ToString());
                        //if (!dictTraceMatrix.ContainsKey(match.Value))
                        //{
                        //    dictTraceMatrix.Add(match.Value, "");
                        //}
                    }
                    Dictionary<string, string> reqs = _functionality.GetTextSplitedbySections(strRequirementsummary, sectionReqs);
                                        
                    //---------------------------------

                    string[] strArrayYAML = strsolutionstructure.ToLower().Split(new string[] { "solutionname:" }, StringSplitOptions.RemoveEmptyEntries);

                    if (strArrayYAML.Length > 0)
                    {
                        resultStructure = strArrayYAML[strArrayYAML.Length - 1];
                        strtask = _functionality.GetPromptTask(1, 4);

                        if (!string.IsNullOrEmpty(resultStructure))
                        {
                            InsightresultTecnical = await _functionality.AnalyzeBRD(strsolutinooverview, _functionality.GetPromptTask(1, 3), 1);

                            //string[] strArrayDBlayer = InsightresultTecnical.ToLower().Split(new string[] { "dblayer:", "dbms name:" }, StringSplitOptions.RemoveEmptyEntries);
                            //string dblayer = "";
                            //if(strArrayDBlayer.Length==3)
                            //{
                            //    dblayer = strArrayDBlayer[1].ToString().Trim();
                            //}

                            InsightresultTecnical = _functionality.GetStringTrimStart(InsightresultTecnical);

                            HashSet<string> sectionHeaders = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                            {
                            "DBMS name:",
                            "Suggested database name:"
                            };
                            Dictionary<string, string> sections=_functionality.GetTextSplitedbySections(InsightresultTecnical, sectionHeaders);

                            dbmsDatabasename = ""; //await _functionality.AnalyzeBRD(InsightresultTecnical, _functionality.GetPromptTask(1, 9), 1);
                            
                            foreach(string strk in sections.Keys )
                            {
                                if(_functionality.IsEqualTwoString(strk,"DBMS name:"))
                                {
                                    dbmsDatabasename += "dbms:" + sections[strk]+"\r\n";
                                }
                                if (_functionality.IsEqualTwoString(strk, "Suggested database name:"))
                                {
                                    dbmsDatabasename += "database name:" + sections[strk] + "\r\n";
                                }
                                
                            }
                            resultStructure = "Solution Name:" + resultStructure;

                            string[] strProjects = resultStructure.Split(new string[] { "project:" }, StringSplitOptions.RemoveEmptyEntries);
                            string strdbscript = string.Empty;
                            string strdbscriptDstinct = string.Empty;


                            if (strProjects.Length == 2)
                            {
                                string strSolutionSection = strProjects[0].ToString().Replace("rootfolder", "Root Folder");
                                string strProjectSection = strProjects[1].ToString();

                                resultSolutionStrcuture = strSolutionSection + "\r\n";

                                string[] strProjectNames = strProjectSection.Split(new string[] { "- projectname:", "-projectname:", "projectname:" }, StringSplitOptions.RemoveEmptyEntries);

                                if (strProjectNames.Length > 0)
                                {
                                    string strProjectName = string.Empty;
                                    string strProjectPath = string.Empty;
                                    int cnt = 0;

                                    foreach (string str in strProjectNames)
                                    {
                                        if (str.Trim() == string.Empty)
                                        {
                                            continue;
                                        }
                                        string strPathData = _functionality.GetStringTrimStart(str);

                                        string[] strPaths = strPathData.Split(new string[] { "paths:" }, StringSplitOptions.RemoveEmptyEntries);

                                        if (strPaths.Length == 2)
                                        {
                                            strProjectName = "Project Name:" + strPaths[0].ToString().Replace("\"", "").Trim();

                                            resultSolutionStrcuture += strProjectName + "\r\n";

                                            string[] strProjectPaths = strPaths[1].Split(new string[] { "- projectpath:", "-projectpath:", "projectpath:" }, StringSplitOptions.RemoveEmptyEntries);

                                            foreach (string strprjpath in strProjectPaths)
                                            {

                                                if (strprjpath.Trim() == string.Empty)
                                                {
                                                    continue;
                                                }
                                                string[] strFiles = strprjpath.Split(new string[] { "files:" }, StringSplitOptions.RemoveEmptyEntries);
                                                if (strFiles.Length == 2)
                                                {

                                                    if (!strFiles[0].Trim().StartsWith(strPaths[0].Trim()) && !strFiles[0].Trim().StartsWith("/" + strPaths[0].Trim()))
                                                    {
                                                        string[] parts = { strPaths[0].ToString().Trim().Replace("\"", ""), strFiles[0].Trim().Replace("\"", "") };

                                                        strProjectPath = "\r\n" + "Project Path:" + string.Join("/", parts);
                                                    }
                                                    else
                                                    {
                                                        strProjectPath = "\r\n" + "Project Path:" + strFiles[0].Trim().Replace("\"", "") + "\r\n";
                                                    }

                                                    resultSolutionStrcuture += strProjectPath + "\r\n";

                                                    string[] strFileNames = strFiles[1].Split(new string[] { "- filename:", "-filename:", "filename:" }, StringSplitOptions.RemoveEmptyEntries);

                                                    string filename = string.Empty;
                                                    //string filemetadata = string.Empty;
                                                    string ImplementationDetails = string.Empty;

                                                    foreach (string strfname in strFileNames)
                                                    {
                                                        if (strfname.Trim() == string.Empty)
                                                        {
                                                            continue;
                                                        }

                                                        string[] strimpdetails = strfname.Split(new string[] { "implementationdetails:", "implementation details:", "implementation_details:" }, StringSplitOptions.RemoveEmptyEntries);
                                                        if (strimpdetails.Length == 2)
                                                        {
                                                            filename = "File Name:" + strimpdetails[0].ToString().Trim();
                                                            ImplementationDetails = strimpdetails[1].ToString().Trim().Replace("|", "");
                                                            //----------------------------
                                                             matches = Regex.Matches(strfname, pattern, RegexOptions.IgnoreCase);
                                                            string strRequirements = "Requirements:"+"\r\n";
                                                            Dictionary<string, string> dictPreventDuplicate = new Dictionary<string, string>(); ;
                                                            foreach (Match match in matches)
                                                            {
                                                                if (reqs.ContainsKey(match.ToString()) && !dictPreventDuplicate.ContainsKey(match.ToString()))
                                                                {
                                                                    strRequirements += match.ToString() +"\r\n" + reqs[match.ToString()] + "\r\n"; 
                                                                }
                                                                if(!dictPreventDuplicate.ContainsKey(match.ToString()))
                                                                {
                                                                    dictPreventDuplicate.Add(match.ToString(), ""); ;
                                                                }
                                                            }

                                                            //----------------------------

                                                            resultPrep = strSolutionSection + strProjectName + strProjectPath;
                                                            resultPrep += "\r\n" + filename + "\r\n implementation pseudocode:" + ImplementationDetails;

                                                            resultSolutionStrcuture += "\r\n" + filename;
                                                            //-------------------------
                                                            resultPrep = "\r\n" + InsightresultTecnical + "\r\n" + strRequirements + "\r\n" + resultPrep;
                                                            //-------------------------
                                                            resultFiles = await _functionality.AnalyzeBRD(resultPrep, strtask, 1);
                                                                                                                        
                                                            string[] strresultFiles = resultFiles.Split(new string[] { "Implementation Details:", "Implementation Details" }, StringSplitOptions.RemoveEmptyEntries);

                                                            if (strresultFiles.Length > 0)
                                                            {
                                                                //--------------------------------
                                                                resultSolutionStrcuture += "\r\n" + "Implementation Details:" + "\r\n" + strresultFiles[strresultFiles.Length - 1].ToString();// + "\r\n\tAdditional Details:\r\n" + ImplementationDetails;
                                                                //string strresultSolutionStrcuture = "\r\n" + "Implementation Details:" + "\r\n" + strresultFiles[strresultFiles.Length - 1].ToString();// + "\r\n\tAdditional Details:\r\n" + ImplementationDetails;
                                                                
                                                                /*
                                                                string strtask1 = _functionality.GetPromptTask(1, 10);
                                                                
                                                                string strdbscriptDstinctval = await _functionality.AnalyzeBRD(strRequirements, strtask1, 1); //+ "\r\n dbmsDatabasename:\r\n" + dbmsDatabasename
                                                                //--------------------------------
                                                                if (!string.IsNullOrEmpty(strdbscriptDstinctval.Trim()))
                                                                {
                                                                    //----------------------------
                                                                    HashSet<string> sectionDBObjectTitle = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                                                                    {
                                                                    "tables:",
                                                                    "views:",
                                                                    "stored procedures:",
                                                                    "functions:"
                                                                    };

                                                                    Dictionary<string, string> sectionDBObjects = _functionality.GetTextSplitedbySections(strdbscriptDstinctval.ToLower().Trim(), sectionDBObjectTitle);
                                                                    int looplCnt = 0;
                                                                    string keystr = "";
                                                                    string valstr = "";
                                                                    foreach(string strk in sectionDBObjects.Keys )
                                                                    {
                                                                        if(strk=="tables:")
                                                                        {
                                                                            Dictionary<string, string> sectionTables = _functionality.GetTextSplitedbySections(sectionDBObjects[strk].ToLower().Trim(), 
                                                                                new HashSet<string>() { "table name:", "table detail:" });
                                                                            
                                                                            if((sectionTables.Count % 2) == 0)
                                                                            {
                                                                                looplCnt = 0;
                                                                                keystr = "";
                                                                                valstr = "";
                                                                                foreach (string strkt in sectionTables.Keys)
                                                                                {
                                                                                    if(looplCnt==0)
                                                                                    {
                                                                                        keystr = strkt.Trim();
                                                                                    }
                                                                                    else if(looplCnt == 1)
                                                                                    {
                                                                                        valstr += strkt.Trim() + "\r\n" + sectionTables[strkt].Trim();
                                                                                    }
                                                                                    if (looplCnt == 1 && keystr !="")
                                                                                    {
                                                                                        if (!dictDBObj.ContainsKey(keystr))
                                                                                        {
                                                                                            dictDBObj.Add(keystr, valstr);
                                                                                        }
                                                                                        looplCnt = 0;
                                                                                        keystr = "";
                                                                                        valstr = "";
                                                                                    }
                                                                                    looplCnt++;
                                                                                }
                                                                            }


                                                                        }
                                                                        else if (strk == "views:")
                                                                        {
                                                                            Dictionary<string, string> sectionViews = _functionality.GetTextSplitedbySections(sectionDBObjects[strk].ToLower().Trim(),
                                                                                new HashSet<string>() { "view name:", "view detail:" });
                                                                            if ((sectionViews.Count % 2) == 0)
                                                                            {
                                                                                looplCnt = 0;
                                                                                keystr = "";
                                                                                valstr = "";
                                                                                foreach (string strkt in sectionViews.Keys)
                                                                                {
                                                                                    if (looplCnt == 0)
                                                                                    {
                                                                                        keystr = strkt.Trim();
                                                                                    }
                                                                                    else if (looplCnt == 1)
                                                                                    {
                                                                                        valstr += strkt.Trim() + "\r\n" + sectionViews[strkt].Trim();
                                                                                    }
                                                                                    if (looplCnt == 1 && keystr != "")
                                                                                    {
                                                                                        if (!dictDBObj.ContainsKey(keystr))
                                                                                        {
                                                                                            dictDBObj.Add(keystr, valstr);
                                                                                        }
                                                                                        looplCnt = 0; 
                                                                                        keystr = "";
                                                                                        valstr = "";
                                                                                    }
                                                                                    looplCnt++;
                                                                                }
                                                                            }
                                                                        }
                                                                        else if (strk == "stored procedures:")
                                                                        {
                                                                            Dictionary<string, string> sectionSps = _functionality.GetTextSplitedbySections(sectionDBObjects[strk].ToLower().Trim(),
                                                                                new HashSet<string>() { "sp name:", "sp detail:" });
                                                                            if ((sectionSps.Count % 2) == 0)
                                                                            {
                                                                                looplCnt = 0;
                                                                                keystr = "";
                                                                                valstr = "";
                                                                                foreach (string strkt in sectionSps.Keys)
                                                                                {
                                                                                    if (looplCnt == 0)
                                                                                    {
                                                                                        keystr = strkt.Trim();
                                                                                    }
                                                                                    else if (looplCnt == 1)
                                                                                    {
                                                                                        valstr += strkt.Trim() + "\r\n" + sectionSps[strkt].Trim();
                                                                                    }
                                                                                    if (looplCnt == 1 && keystr != "")
                                                                                    {
                                                                                        if (!dictDBObj.ContainsKey(keystr))
                                                                                        {
                                                                                            dictDBObj.Add(keystr, valstr);
                                                                                        }
                                                                                        looplCnt = 0;
                                                                                        keystr = "";
                                                                                        valstr = "";
                                                                                    }
                                                                                    looplCnt++;
                                                                                }
                                                                            }
                                                                        }
                                                                        else if (strk == "functions:")
                                                                        {
                                                                            Dictionary<string, string> sectionFunctions = _functionality.GetTextSplitedbySections(sectionDBObjects[strk].ToLower().Trim(),
                                                                                new HashSet<string>() { "function name:", "function detail:" });
                                                                            if ((sectionFunctions.Count % 2) == 0)
                                                                            {
                                                                                looplCnt = 0;
                                                                                keystr = "";
                                                                                valstr = "";
                                                                                foreach (string strkt in sectionFunctions.Keys)
                                                                                {
                                                                                    if (looplCnt == 0)
                                                                                    {
                                                                                        keystr = strkt.Trim();
                                                                                    }
                                                                                    else if (looplCnt == 1)
                                                                                    {
                                                                                        valstr += strkt.Trim() + "\r\n" + sectionFunctions[strkt].Trim();
                                                                                    }
                                                                                    if (looplCnt == 1 && keystr != "")
                                                                                    {
                                                                                        if (!dictDBObj.ContainsKey(keystr))
                                                                                        {
                                                                                            dictDBObj.Add(keystr, valstr);
                                                                                        }
                                                                                        looplCnt = 0;
                                                                                        keystr = "";
                                                                                        valstr = "";
                                                                                    }
                                                                                    looplCnt++;
                                                                                }
                                                                            }
                                                                        }
                                                                    }
                                                            */
                                                                   //----------------------------
                                                                   /*
                                                                    string[] arrayObj1 = strdbscriptDstinctval.Split(new string[] { "tables:", "views:", "stored procedures:", "functions:","tables", "views", "stored procedures", "functions" },StringSplitOptions.RemoveEmptyEntries );

                                                                    if (arrayObj1.Length > 0)
                                                                    {
                                                                        foreach (string strdbobj in arrayObj1)
                                                                        {
                                                                            if (strdbobj.Trim().Contains("table name") && strdbobj.Trim().Contains("table detail"))
                                                                            {
                                                                                string[] arrayObj2 = strdbobj.Trim().Split(new string[] { "table name:", "table detail:", "table name", "table detail" }, StringSplitOptions.RemoveEmptyEntries);
                                                                                if(arrayObj2.Length==2)
                                                                                {
                                                                                    if (!dictDBObj.ContainsKey("table name:" + arrayObj2[0].Trim()))
                                                                                    {
                                                                                        dictDBObj.Add("table name:" + arrayObj2[0].Trim(), arrayObj2[1].Trim());
                                                                                    }

                                                                                }
                                                                            }
                                                                            if (strdbobj.Trim().Contains("view name") && strdbobj.Trim().Contains("view detail"))
                                                                            {
                                                                                string[] arrayObj2 = strdbobj.Trim().Split(new string[] { "view name:", "view detail:", "view name", "view detail" }, StringSplitOptions.RemoveEmptyEntries);
                                                                                if (arrayObj2.Length == 2)
                                                                                {
                                                                                    if (!dictDBObj.ContainsKey("view name:" + arrayObj2[0].Trim()))
                                                                                    {
                                                                                        dictDBObj.Add("view name:" + arrayObj2[0].Trim(), arrayObj2[1].Trim());
                                                                                    }

                                                                                }
                                                                            }
                                                                            if (strdbobj.Trim().Contains("sp name") && strdbobj.Trim().Contains("sp detail"))
                                                                            {
                                                                                string[] arrayObj2 = strdbobj.Trim().Split(new string[] { "sp name:", "sp detail:", "sp name", "sp detail" }, StringSplitOptions.RemoveEmptyEntries);
                                                                                if (arrayObj2.Length == 2)
                                                                                {
                                                                                    if (!dictDBObj.ContainsKey("sp name:" + arrayObj2[0].Trim()))
                                                                                    {
                                                                                        dictDBObj.Add("sp name:" + arrayObj2[0].Trim(), arrayObj2[1].Trim());
                                                                                    }

                                                                                }
                                                                            }
                                                                            if (strdbobj.Trim().Contains("function name") && strdbobj.Trim().Contains("function detail"))
                                                                            {
                                                                                string[] arrayObj2 = strdbobj.Trim().Split(new string[] { "function name:", "function detail:", "function name", "function detail" }, StringSplitOptions.RemoveEmptyEntries);
                                                                                if (arrayObj2.Length == 2)
                                                                                {
                                                                                    if (!dictDBObj.ContainsKey("function name:" + arrayObj2[0].Trim()))
                                                                                    {
                                                                                        dictDBObj.Add("function name:" + arrayObj2[0].Trim(), arrayObj2[1].Trim());
                                                                                    }

                                                                                }
                                                                            }
                                                                        }
                                                                    }
                                                                    */
                                                                //}
                                                                

                                                            }
                                                            
                                                        }
                                                    }
                                                }
                                            }
                                        }

                                        cnt++;
                                    }


                                    resultPrep = "solution structure:" + "\r\n" + resultSolutionStrcuture;

                                    //--------------- DB object creations
                                    string strReqDB = requirementsummary.Replace("taggedrequirements:start", "").Replace("taggedrequirements:end", "");

                                    string[] dbRequirements = strReqDB.Split("req-", StringSplitOptions.RemoveEmptyEntries);

                                    foreach (string tag in dbRequirements)
                                    {
                                        if (!string.IsNullOrEmpty(tag.Trim()))
                                        {
                                            if (tag.ToLower().Contains("type: database requirement"))
                                            {
                                                string strtask1 = _functionality.GetPromptTask(1, 12);

                                                string strDBreq = "req-"+ tag.Trim();

                                                string strdbscriptDstinctval = await _functionality.AnalyzeBRD(strDBreq, strtask1, 1); //+ "\r\n dbmsDatabasename:\r\n" + dbmsDatabasename
                                                                                                                                              //--------------------------------
                                                if (!string.IsNullOrEmpty(strdbscriptDstinctval.Trim()))
                                                {
                                                    //----------------------------
                                                    HashSet<string> sectionDBObjectTitle = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                                                                    {
                                                                    "tables:",
                                                                    "views:",
                                                                    "stored procedures:",
                                                                    "functions:"
                                                                    };

                                                    Dictionary<string, string> sectionDBObjects = _functionality.GetTextSplitedbySections(strdbscriptDstinctval.ToLower().Trim(), sectionDBObjectTitle);
                                                    int looplCnt = 0;
                                                    string keystr = "";
                                                    string valstr = "";
                                                    foreach (string strk in sectionDBObjects.Keys)
                                                    {
                                                        if (strk == "tables:")
                                                        {
                                                            Dictionary<string, string> sectionTables = _functionality.GetTextSplitedbySections(sectionDBObjects[strk].ToLower().Trim(),
                                                                new HashSet<string>() { "table name:", "table detail:" });

                                                            if ((sectionTables.Count % 2) == 0)
                                                            {
                                                                looplCnt = 0;
                                                                keystr = "";
                                                                valstr = "";
                                                                foreach (string strkt in sectionTables.Keys)
                                                                {
                                                                    if (looplCnt == 0)
                                                                    {
                                                                        keystr = strkt.Trim();
                                                                    }
                                                                    else if (looplCnt == 1)
                                                                    {
                                                                        valstr += strkt.Trim() + "\r\n" + sectionTables[strkt].Trim();
                                                                    }
                                                                    if (looplCnt == 1 && keystr != "")
                                                                    {
                                                                        if (!dictDBObj.ContainsKey(keystr))
                                                                        {
                                                                            dictDBObj.Add(keystr, valstr);
                                                                        }
                                                                        looplCnt = 0;
                                                                        keystr = "";
                                                                        valstr = "";
                                                                    }
                                                                    looplCnt++;
                                                                }
                                                            }


                                                        }
                                                        else if (strk == "views:")
                                                        {
                                                            Dictionary<string, string> sectionViews = _functionality.GetTextSplitedbySections(sectionDBObjects[strk].ToLower().Trim(),
                                                                new HashSet<string>() { "view name:", "view detail:" });
                                                            if ((sectionViews.Count % 2) == 0)
                                                            {
                                                                looplCnt = 0;
                                                                keystr = "";
                                                                valstr = "";
                                                                foreach (string strkt in sectionViews.Keys)
                                                                {
                                                                    if (looplCnt == 0)
                                                                    {
                                                                        keystr = strkt.Trim();
                                                                    }
                                                                    else if (looplCnt == 1)
                                                                    {
                                                                        valstr += strkt.Trim() + "\r\n" + sectionViews[strkt].Trim();
                                                                    }
                                                                    if (looplCnt == 1 && keystr != "")
                                                                    {
                                                                        if (!dictDBObj.ContainsKey(keystr))
                                                                        {
                                                                            dictDBObj.Add(keystr, valstr);
                                                                        }
                                                                        looplCnt = 0;
                                                                        keystr = "";
                                                                        valstr = "";
                                                                    }
                                                                    looplCnt++;
                                                                }
                                                            }
                                                        }
                                                        else if (strk == "stored procedures:")
                                                        {
                                                            Dictionary<string, string> sectionSps = _functionality.GetTextSplitedbySections(sectionDBObjects[strk].ToLower().Trim(),
                                                                new HashSet<string>() { "sp name:", "sp detail:" });
                                                            if ((sectionSps.Count % 2) == 0)
                                                            {
                                                                looplCnt = 0;
                                                                keystr = "";
                                                                valstr = "";
                                                                foreach (string strkt in sectionSps.Keys)
                                                                {
                                                                    if (looplCnt == 0)
                                                                    {
                                                                        keystr = strkt.Trim();
                                                                    }
                                                                    else if (looplCnt == 1)
                                                                    {
                                                                        valstr += strkt.Trim() + "\r\n" + sectionSps[strkt].Trim();
                                                                    }
                                                                    if (looplCnt == 1 && keystr != "")
                                                                    {
                                                                        if (!dictDBObj.ContainsKey(keystr))
                                                                        {
                                                                            dictDBObj.Add(keystr, valstr);
                                                                        }
                                                                        looplCnt = 0;
                                                                        keystr = "";
                                                                        valstr = "";
                                                                    }
                                                                    looplCnt++;
                                                                }
                                                            }
                                                        }
                                                        else if (strk == "functions:")
                                                        {
                                                            Dictionary<string, string> sectionFunctions = _functionality.GetTextSplitedbySections(sectionDBObjects[strk].ToLower().Trim(),
                                                                new HashSet<string>() { "function name:", "function detail:" });
                                                            if ((sectionFunctions.Count % 2) == 0)
                                                            {
                                                                looplCnt = 0;
                                                                keystr = "";
                                                                valstr = "";
                                                                foreach (string strkt in sectionFunctions.Keys)
                                                                {
                                                                    if (looplCnt == 0)
                                                                    {
                                                                        keystr = strkt.Trim();
                                                                    }
                                                                    else if (looplCnt == 1)
                                                                    {
                                                                        valstr += strkt.Trim() + "\r\n" + sectionFunctions[strkt].Trim();
                                                                    }
                                                                    if (looplCnt == 1 && keystr != "")
                                                                    {
                                                                        if (!dictDBObj.ContainsKey(keystr))
                                                                        {
                                                                            dictDBObj.Add(keystr, valstr);
                                                                        }
                                                                        looplCnt = 0;
                                                                        keystr = "";
                                                                        valstr = "";
                                                                    }
                                                                    looplCnt++;
                                                                }
                                                            }
                                                        }
                                                    }
                                                }

                                            }

                                        }
                                    }



                                    // --------------- DB object


                                    string strtable = string.Empty;
                                    string strview = string.Empty;
                                    string strsp = string.Empty;
                                    string strfunction = string.Empty;

                                    foreach (string strobj in dictDBObj.Keys)
                                    {
                                        if(strobj.StartsWith("table name"))
                                        {
                                            strtable += strobj +"\r\n" + dictDBObj[strobj]+"\r\n";
                                        }
                                        else if (strobj.StartsWith("view name"))
                                        {
                                            strview += strobj + "\r\n" + dictDBObj[strobj] + "\r\n";
                                        }
                                        else if (strobj.StartsWith("sp name"))
                                        {
                                            strsp += strobj + "\r\n" + dictDBObj[strobj] + "\r\n";
                                        }
                                        else if (strobj.StartsWith("function name"))
                                        {
                                            strfunction += strobj + "\r\n" + dictDBObj[strobj] + "\r\n";
                                        }
                                    }

                                    strtable = "tables:" + "\r\n" + strtable;
                                    strview = "views:" + "\r\n" + strview;
                                    strsp = "stored procedures:" + "\r\n" + strsp;
                                    strfunction = "functions:" + "\r\n" + strfunction;

                                    strdbscriptDstinct = "database object"+"\r\n" + dbmsDatabasename +
                                        "\r\n" + strtable +
                                        "\r\n" + strview +
                                        "\r\n" + strsp +
                                        "\r\n" + strfunction;


                                    strdbscript = _functionality.GetStringTrimStart(strdbscriptDstinct);

                                    result = "solution overview:" + "\r\n" + strsolutinooverview + "\r\n" + resultPrep + "\r\n" + strdbscript + "\r\n" + "RequirementSummary:" + "\r\n" + requirementsummary;
                                }
                            }
                        }
                    }
                }


                _logger.LogInformation("SolidifyBRD completed successfully.");

                // if (result is not null)
                //     {
                //        await _DabaseFunctionality.SaveOrUpdateProjectAsync(request.id, "SolidifyBRD");
                //     }


                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred in SolidifyBRD.");
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }


        [HttpPost("BluePrinting")]
        public async Task<ActionResult<SolidificationResponseModel>> Blueprinting([FromBody] SolidificationResponseModel request)
        {
            _logger.LogInformation("Blueprinting called.");

            if (string.IsNullOrEmpty(request.SolidificationOutput))
            {
                _logger.LogWarning("Blueprinting missing SolidificationOutput.");
                return BadRequest("Please provide Business Requirement and Task to perform");
            }

            var tabs = request.SelectedTabs ?? new List<string>();
            try
            {


                


                _logger.LogInformation("Generating blueprint details...");

                string greenbrown = string.Empty;
                greenbrown = _functionality.GetGreenBrownField(request.SolidificationOutput.ToLower().Trim());

                var result = _functionality.GenerateBluePrintDetails("summary", request.SolidificationOutput.Trim().ToLower(), greenbrown); 
                Console.WriteLine("Result after getblueprinting details: ", result);

                string solutionoverview = string.Empty;
                string commonfuncitonalities = string.Empty;
                string strdatabaseobjects = string.Empty;
                string strsolutionstructure = string.Empty;

                if (result.ContainsKey("solutionOverview") && result.ContainsKey("solutionOverview"))
                {
                    if (result["solutionOverview"] != null)
                    {
                        solutionoverview = result["solutionOverview"].Trim();
                    }
                }
                if (result.ContainsKey("commonFunctionalities") && result.ContainsKey("commonFunctionalities"))
                {
                    if (result["commonFunctionalities"] != null)
                    {
                        commonfuncitonalities = result["commonFunctionalities"].Trim();
                    }
                }

                Dictionary<string, string> dictmetadatapart = new Dictionary<string, string>();

                Dictionary<string, string> dictmetadata = new Dictionary<string, string>();// databse object, unit, funtional, integration metadata

                strsolutionstructure = _functionality.GetStringTrimStart(result["projectStructure"].Trim().ToLower());

                string[] lines = strsolutionstructure.Split(new string[] { "database object" }, StringSplitOptions.RemoveEmptyEntries);


                 if (greenbrown == "green")
                    {
                    //    await _DabaseFunctionality.SaveOrUpdateProjectAsync(request.id, "green");
                    }
                    else if (greenbrown == "brown")
                    {
                    //    await _DabaseFunctionality.SaveOrUpdateProjectAsync(request.id, "brown");

                    }

                if (lines.Length == 2 && greenbrown == "green")
                {
                    strsolutionstructure = lines[0].Trim().ToLower();
                    strdatabaseobjects = lines[1].Trim().ToLower();

                    if (tabs.Contains("Database Scripts"))
                    {
                        _logger.LogInformation("Generating database scripts...");

                        result["databaseScripts"] = "database script" + "\r\n" + strdatabaseobjects;

                    }

                }

                if (tabs.Contains("Requirement Summary")&& result.ContainsKey("requirementSummary") && greenbrown == "green")
                {
                    if (result["requirementSummary"] != null)
                    {
                       string requirementSummary = result["requirementSummary"].Trim();

                        requirementSummary = requirementSummary.Replace("taggedrequirements:start", "").Replace("taggedrequirements:end", "").Trim();

                        result["requirementSummary"] = requirementSummary;
                    }
                    
                }

                Console.WriteLine("Result beforeGenerateTestcases: ", result);
                if (!string.IsNullOrEmpty(solutionoverview) && !string.IsNullOrEmpty(strsolutionstructure) && 
                    (tabs.Contains("Unit Testing") || tabs.Contains("Functional Testing") || tabs.Contains("Integration Testing")))
                {
                    var result2 = await _functionality.GenerateTestcases(solutionoverview, strsolutionstructure, commonfuncitonalities,
                     tabs.Contains("Unit Testing"), tabs.Contains("Functional Testing"), tabs.Contains("Integration Testing"), result);
                    result = result2;

                }

                if(greenbrown == "brown")
                {
                    result["solutionOverview"] = request.SolidificationOutput.Trim();
                }


                _logger.LogInformation("Blueprinting completed.");
                Console.WriteLine("Result before blueprinting end: ", result);
                if (result is not null)
                {
                //    await _DabaseFunctionality.SaveOrUpdateProjectAsync(request.id, "Blueprinting");
                }
                return Ok(result);


            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred in Blueprinting.");
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpPost("CodeSyn")]
        public async Task<IActionResult> CodeSynth([FromBody] CodeSynRequestModel requestModel)
        {
            _logger.LogInformation("CodeSynth called with Filename: {Filename}, Case: {Case}", requestModel.Filename, requestModel.i);

            try
            {
                string generatedCode = string.Empty;
                string strdocumenttype = requestModel.Filename;

                string greenbrown = string.Empty;
                greenbrown = _functionality.GetGreenBrownField(requestModel.SolutionOverview.ToLower().Trim());

                switch (requestModel.i)
                {
                    case 0:
                        if (greenbrown != "brown")
                        {
                            _logger.LogInformation("Generating business logic code...");
                            var prompt0 = _functionality.GetPromptText(requestModel.Filename, requestModel.FileContent, requestModel.DataFlow, requestModel.SolutionOverview);
                            generatedCode = await _functionality.AnalyzeBRD(prompt0, _functionality.GetPromptTask(4), 3);
                            if (generatedCode is not null)
                            {
                            // await _DabaseFunctionality.SaveOrUpdateProjectAsync(requestModel.id, "code");
                            }
                        }
                        break;

                    case 1:
                        if (greenbrown != "brown")
                        {
                            _logger.LogInformation("Generating database code...");
                            var prompt1 = _functionality.GetPromptText(requestModel.Filename, requestModel.FileContent, requestModel.DataFlow, requestModel.SolutionOverview);
                            generatedCode = await _functionality.AnalyzeBRD(prompt1, _functionality.GetPromptTask(6), 3);
                        }
                            break;

                    case 2:
                        _logger.LogInformation("Generating unit testing code...");
                        var prompt2 = _functionality.GetPromptText(requestModel.Filename, requestModel.FileContent, requestModel.DataFlow, requestModel.SolutionOverview);
                        generatedCode = await _functionality.AnalyzeBRD(prompt2, _functionality.GetPromptTask(7), 3);
                        if (generatedCode is not null)
                        {
                        //   await _DabaseFunctionality.SaveOrUpdateProjectAsync(requestModel.id, "Testing_Unit");

                        }
                        break;

                    case 3:
                        _logger.LogInformation("Describing code...");
                        generatedCode = await _functionality.AnalyzeBRD(requestModel.FileContent, _functionality.GetPromptTask(8), 3);
                        if (generatedCode is not null)
                        {
                        //    await _DabaseFunctionality.SaveOrUpdateProjectAsync(requestModel.id, "description");

                        }
                        break;

                    case 4:
                        _logger.LogInformation("Generating Functional testing code...");
                        var prompt3 = _functionality.GetPromptText(requestModel.Filename, requestModel.FileContent, requestModel.DataFlow, requestModel.SolutionOverview);
                        generatedCode = await _functionality.AnalyzeBRD(prompt3, _functionality.GetPromptTask(10), 3);
                        if (generatedCode is not null)
                        {
                        //    await _DabaseFunctionality.SaveOrUpdateProjectAsync(requestModel.id, "Testing_Functional");

                        }
                        break;
                    case 5:
                        _logger.LogInformation("Generating Integration testing code...");
                        var prompt4 = _functionality.GetPromptText(requestModel.Filename, requestModel.FileContent, requestModel.DataFlow, requestModel.SolutionOverview);
                        generatedCode = await _functionality.AnalyzeBRD(prompt4, _functionality.GetPromptTask(12), 3);
                        if (generatedCode is not null)
                        {
                        //    await _DabaseFunctionality.SaveOrUpdateProjectAsync(requestModel.id, "Testing_Integration");

                        }
                        break;

                    case 6:
                        _logger.LogInformation("Generating Documentation for " + strdocumenttype + "...");

                        var prompt5 = string.Empty;

                        if (requestModel.FileContent != null)
                        {
                            HashSet<string> sectionHeaders = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                            {
                            "solution overview:",
                            "solution structure:",
                            "requirementsummary:",
                            "document template:"
                            };

                            Dictionary<string, string> sections = _functionality.GetTextSplitedbySections(requestModel.FileContent.ToLower().Trim(), sectionHeaders);


                            //string[] strDocContents = requestModel.FileContent.ToLower().Trim().Split(new string[] { "solution overview:", "solution structure:", "requirementsummary:", "document template:" }, StringSplitOptions.RemoveEmptyEntries);
                            string solutionOverview = string.Empty;
                            string solutionStructure = string.Empty;
                            string documentTemplate = string.Empty;
                            string requirementsSummary = string.Empty;

                            if(sections.Count ==4)
                            {
                                solutionOverview = sections["solution overview:"];
                                solutionStructure = sections["solution structure:"];
                                documentTemplate = sections["document template:"];
                                requirementsSummary = sections["requirementsummary:"];
                            }
                            
                            if (!string.IsNullOrEmpty(solutionOverview) &&
                                !string.IsNullOrEmpty(solutionStructure) &&
                                !string.IsNullOrEmpty(requirementsSummary) &&
                                !string.IsNullOrEmpty(documentTemplate)
                                )
                            {

                                prompt5 = "solution overview:" + solutionOverview + "solution structure:" + solutionStructure;
                                bool isNeeded = _functionality.IsNeeded(strdocumenttype.ToLower().Trim());

                                if (strdocumenttype.ToLower().Trim() == "lld")
                                {
                                    if (isNeeded)
                                    {
                                        int loopcnt = 0;
                                        while (loopcnt < 2)
                                        {
                                            generatedCode = await _functionality.AnalyzeBRD(prompt5, _functionality.GetPromptTask(14, 1), 3);

                                            generatedCode = _functionality.GetStringTrimStart(generatedCode);

                                            string[] strArray = generatedCode.Split(new string[] { "use_case:", "user_stories:" }, StringSplitOptions.RemoveEmptyEntries);

                                            if (strArray.Length > 2)
                                            {
                                                string strusecase = strArray[1].Trim();
                                                if (strusecase.Length > 10)
                                                {
                                                    loopcnt = 10;
                                                    break; ;
                                                }
                                            }
                                            loopcnt++;
                                        }
                                        if (!string.IsNullOrEmpty(generatedCode) && !generatedCode.ToLower().StartsWith("error"))
                                        {
                                            generatedCode = await _functionality.GetSectionWiseContent(solutionOverview, solutionStructure, requirementsSummary, documentTemplate, strdocumenttype.ToLower(), generatedCode.ToLower());

                                        }
                                    }
                                    else
                                    {
                                        generatedCode = await _functionality.GetSectionWiseContent(solutionOverview, solutionStructure, requirementsSummary, documentTemplate, strdocumenttype.ToLower(), generatedCode.ToLower());


                                    }

                                    if (generatedCode is not null)
                                    {
                                        // await _DabaseFunctionality.SaveOrUpdateProjectAsync(requestModel.id, "Doc_LLD");

                                    }
                                }
                                else if (strdocumenttype.ToLower().Trim() == "user manual")
                                {
                                    if (isNeeded)
                                    {
                                        int loopcnt = 0;
                                        while (loopcnt < 2)
                                        {
                                            generatedCode = await _functionality.AnalyzeBRD(prompt5, _functionality.GetPromptTask(14, 2), 3);

                                            generatedCode = _functionality.GetStringTrimStart(generatedCode);

                                            string[] strArray = generatedCode.Split(new string[] { "use_case:", "user_stories:" }, StringSplitOptions.RemoveEmptyEntries);

                                            if (strArray.Length > 2)
                                            {
                                                string strusecase = strArray[1];
                                                if (strusecase.Length > 10)
                                                {
                                                    loopcnt = 10;
                                                    break;
                                                }
                                            }
                                            loopcnt++;
                                        }

                                        if (!string.IsNullOrEmpty(generatedCode) && !generatedCode.ToLower().StartsWith("error"))
                                        {
                                            generatedCode = await _functionality.GetSectionWiseContent(solutionOverview, solutionStructure, requirementsSummary, documentTemplate, strdocumenttype.ToLower(), generatedCode.ToLower());

                                        }
                                    }
                                    else
                                    {
                                        generatedCode = await _functionality.GetSectionWiseContent(solutionOverview, solutionStructure, requirementsSummary, documentTemplate, strdocumenttype.ToLower(), generatedCode.ToLower());


                                    }
                                    if (generatedCode is not null)
                                    {
                                        // await _DabaseFunctionality.SaveOrUpdateProjectAsync(requestModel.id, "Doc_UserManual");

                                    }
                                }
                                else if (strdocumenttype.ToLower().Trim() == "hld")
                                {
                                    if (isNeeded)
                                    {
                                        int loopcnt = 0;
                                        while (loopcnt < 2)
                                        {
                                            generatedCode = await _functionality.AnalyzeBRD(prompt5, _functionality.GetPromptTask(14, 1), 3);

                                            generatedCode = _functionality.GetStringTrimStart(generatedCode);

                                            string[] strArray = generatedCode.Split(new string[] { "use_case:", "user_stories:" }, StringSplitOptions.RemoveEmptyEntries);

                                            if (strArray.Length > 2)
                                            {
                                                string strusecase = strArray[1];
                                                if (strusecase.Length > 10)
                                                {
                                                    loopcnt = 10;
                                                    break;
                                                }
                                            }
                                            loopcnt++;
                                        }
                                        if (!string.IsNullOrEmpty(generatedCode) && !generatedCode.ToLower().StartsWith("error"))
                                        {
                                            generatedCode = await _functionality.GetSectionWiseContent(solutionOverview, solutionStructure, requirementsSummary, documentTemplate, strdocumenttype.ToLower(), generatedCode.ToLower());

                                        }
                                    }
                                    else
                                    {
                                        generatedCode = await _functionality.GetSectionWiseContent(solutionOverview, solutionStructure, requirementsSummary, documentTemplate, strdocumenttype.ToLower(), generatedCode.ToLower());

                                    }
                                    if (generatedCode is not null)
                                    {
                                    //    await _DabaseFunctionality.SaveOrUpdateProjectAsync(requestModel.id, "Doc_HLD");
                                    }

                                }

                            }
                        }
                        break;

                    case 7:  // Code review

                        string filename = requestModel.Filename;
                        string code = requestModel.code;
                        string UploadChecklist = requestModel.UploadChecklist;
                        string UploadBestPractice = requestModel.UploadBestPractice;
                        string EnterLanguageType = requestModel.EnterLanguageType;
                        Console.WriteLine(filename);
                        Console.WriteLine(code);

                        _logger.LogInformation("Generating code Review...");

                        if (!string.IsNullOrEmpty(code) && !string.IsNullOrEmpty(filename) && !string.IsNullOrEmpty(EnterLanguageType) &&
                            !string.IsNullOrEmpty(UploadChecklist) && !string.IsNullOrEmpty(UploadBestPractice))
                        {
                            generatedCode = await _functionality.GetCodeReview(filename, code, EnterLanguageType, UploadChecklist, UploadBestPractice);
                            if (generatedCode is not null)
                            {
                            //    await _DabaseFunctionality.SaveOrUpdateProjectAsync(requestModel.id, "codeReview");

                            }
                        }

                        break;

                    default:
                        _logger.LogWarning("Invalid case value: {Case}", requestModel.i);
                        return BadRequest("Invalid case value");
                }
                _logger.LogInformation("CodeSynth generated successfully.");
                if (generatedCode is not null)
                {
                //    await _DabaseFunctionality.SaveOrUpdateProjectAsync(requestModel.id, "CodeSynth");
 
                }
                return Ok(generatedCode);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred in CodeSynth.");
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }


        [HttpPost("reverse")]
        public async Task<IActionResult> reverseEngineering([FromBody] ReveseEngineeringRequestModel requestModel)
        {
            Dictionary<string, dynamic> result = new Dictionary<string, dynamic>();
            if (requestModel is not null)
            {
                bool isGettingGenerate = false;

                if (requestModel.FolderStructure is not null && requestModel.SolutionDescription is not null && !requestModel.reverseEngineeringAlreadyCalled)
                {

                    string SolutionDescription = requestModel.SolutionDescription;
                    //SolutionDescription=SolutionDescription.ToLower().Replace("root name:", "root folder:");
                    FileNode FolderStructure = requestModel.FolderStructure;
                    result["SolutionDescription"] = SolutionDescription;
                    result["FolderStructure"] = FolderStructure;
                    string UploadChecklist = requestModel.UploadChecklist;
                    string UploadBestPractice = requestModel.UploadBestPractice;
                    string EnterLanguageType = requestModel.EnterLanguageType;

                    result = await _functionality.ReverseEngMetadataFromFiles(result, FolderStructure, UploadChecklist, UploadBestPractice, EnterLanguageType);
                    isGettingGenerate = true;

                }

                if (requestModel.generateBRDWithTRD)
         
                {   
                    string solutionOverview = string.Empty ;
                    string requirementSummary = string.Empty ;
                    result["BRDwithTRD"] = ""; 

                    if (isGettingGenerate)
                    
                    {
                        solutionOverview = result["SolutionOverview"];
                        requirementSummary = result["requirementSummary"];
                    }
                    else
                    {
                        solutionOverview = requestModel.solutionOverview;
                        requirementSummary = requestModel.requirementSummary;
                    }
                    if (solutionOverview.Trim() != "" && requirementSummary.Trim() != "")
                    {
                        string inputForBRD = solutionOverview.Trim() + "\r\n" + "RequirementSummary:" + "\r\n" + requirementSummary.Trim();

                        string strtask = _functionality.GetPromptTask(18, 0);

                        string strBRDwithTRD = await _functionality.AnalyzeBRD(inputForBRD, strtask, 2);

                        result["BRDwithTRD"] = strBRDwithTRD;
                    }

                }
                return Ok(result);
            }

            return Ok("");
        }



        [HttpPost("getsummary")]
        public async Task<IActionResult> codeReview([FromBody] FileNode requestModel)
        {

            string Result = await _functionality.CodeReviewForFilesWithOverallSummary(requestModel);
            return Ok(Result);
        }


        [HttpPost("Traceability")]
        public async Task<IActionResult> Traceability([FromBody] TraceabilityRequest requestModel)
        {
            string result = string.Empty;
            string[] arrayLine = null;

            if (requestModel.requirementSummary != null)
            {
                string fieldType = requestModel.field;

                Dictionary<string, string> dictTrace = await _functionality.TraverseFolderStructureforTraceabilityMatrixBase(requestModel.FileNode, fieldType);

                Dictionary<string, string> dictTraceMatrix = new Dictionary<string, string>();
                               
                string pattern = @"req-\d+(\.\d+)*";
                MatchCollection matches = Regex.Matches(requestModel.requirementSummary, pattern,RegexOptions.IgnoreCase);

                foreach (Match match in matches)
                {
                    if (!dictTraceMatrix.ContainsKey(match.Value))
                    {
                        dictTraceMatrix.Add(match.Value, "");
                    }
                }

                // Requirement ID | code | unittest | functionaltest | integrationtest | hld | lld | user manual

                Dictionary<int, string> tmLine = new Dictionary<int, string>();

                tmLine.Add(1, ""); 
                tmLine.Add(2, "");
                tmLine.Add(3, "");
                tmLine.Add(4, "");
                tmLine.Add(5, "");
                tmLine.Add(6, "");
                tmLine.Add(7, "");
                tmLine.Add(8, "");
                result = string.Empty;

                int clm1 = "Requirement ID".Length; 
                int clm2 = "Code Files".Length;
                int clm3 = "Unit Test".Length;
                int clm4 = "Functional Test".Length;
                int clm5 = "Integration Test".Length;
                int clm6 = "HLD".Length;
                int clm7 = "LLD".Length;
                int clm8 = "User Manual".Length;


                foreach (string strtm in dictTraceMatrix.Keys)
                {

                    arrayLine = null;
                    tmLine[1] = "";
                    tmLine[2] = "";
                    tmLine[3] = "";
                    tmLine[4] = "";
                    tmLine[5] = "";
                    tmLine[6] = "";
                    tmLine[7] = "";
                    tmLine[8] = "";
                    foreach (string strtrace in dictTrace.Keys)
                    {
                        
                        if (strtrace == "code")
                        {
                            arrayLine = dictTrace[strtrace].Split(new string[] { "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);

                            foreach (string str in arrayLine)
                            {
                                string[] arrayLine1 = str.Split(new string[] { "filename:", "=" }, StringSplitOptions.RemoveEmptyEntries);

                                if (arrayLine1.Length == 2)
                                {
                                    if (arrayLine1[1].ToLower().Trim().Contains(strtm.ToLower().Trim() + ","))
                                    {
                                        tmLine[2] += arrayLine1[0].Trim() + ",";
                                    }
                                }

                            }
                        }
                        
                        if (strtrace == "unittest")
                        {
                            arrayLine = dictTrace[strtrace].Split(new string[] { "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);

                            foreach (string str in arrayLine)
                            {
                                string[] arrayLine1 = str.Split(new string[] { "filename:", "=" }, StringSplitOptions.RemoveEmptyEntries);

                                if (arrayLine1.Length == 2)
                                {
                                    if (arrayLine1[1].ToLower().Trim().Contains(strtm.ToLower().Trim() + ","))
                                    {
                                        tmLine[3] += arrayLine1[0].Trim() + ",";
                                    }
                                }

                            }
                        }
                        
                        if (strtrace == "functionaltest")
                        {
                            arrayLine = dictTrace[strtrace].Split(new string[] { "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);

                            foreach (string str in arrayLine)
                            {
                                string[] arrayLine1 = str.Split(new string[] { "filename:", "=" }, StringSplitOptions.RemoveEmptyEntries);

                                if (arrayLine1.Length == 2)
                                {
                                    if (arrayLine1[1].ToLower().Trim().Contains(strtm.ToLower().Trim() + ","))
                                    {
                                        tmLine[4] += arrayLine1[0].Trim() + ",";
                                    }
                                }

                            }
                        }
                        
                        if (strtrace == "integrationtest")
                        {
                            arrayLine = dictTrace[strtrace].Split(new string[] { "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);

                            foreach (string str in arrayLine)
                            {
                                string[] arrayLine1 = str.Split(new string[] { "filename:", "=" }, StringSplitOptions.RemoveEmptyEntries);

                                if (arrayLine1.Length == 2)
                                {
                                    if (arrayLine1[1].ToLower().Trim().Contains(strtm.ToLower().Trim() + ","))
                                    {
                                        tmLine[5] += arrayLine1[0].Trim() + ",";
                                    }
                                }

                            }
                        }
                        
                        if (strtrace == "hld")
                        {
                            arrayLine = dictTrace[strtrace].Split(new string[] { "\r","\n" }, StringSplitOptions.RemoveEmptyEntries);

                            foreach (string str in arrayLine)
                            {
                                string[] arrayLine1 = str.Split(new string[] { "=" }, StringSplitOptions.RemoveEmptyEntries);

                                if (arrayLine1.Length == 2)
                                {
                                    if (arrayLine1[0].ToLower().Trim()==strtm.ToLower().Trim())
                                    {
                                        tmLine[6] += arrayLine1[1].Trim() + ",";
                                    }
                                }

                            }
                        }
                        
                        if (strtrace == "lld")
                        {
                            arrayLine = dictTrace[strtrace].Split(new string[] { "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);

                            foreach (string str in arrayLine)
                            {
                                string[] arrayLine1 = str.Split(new string[] { "=" }, StringSplitOptions.RemoveEmptyEntries);

                                if (arrayLine1.Length == 2)
                                {
                                    if (arrayLine1[0].ToLower().Trim() == strtm.ToLower().Trim())
                                    {
                                        tmLine[7] += arrayLine1[1].Trim() + ",";
                                    }
                                }

                            }
                        }
                        
                        if (strtrace == "user manual")
                        {
                            arrayLine = dictTrace[strtrace].Split(new string[] { "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);

                            foreach (string str in arrayLine)
                            {
                                string[] arrayLine1 = str.Split(new string[] { "=" }, StringSplitOptions.RemoveEmptyEntries);

                                if (arrayLine1.Length == 2)
                                {
                                    if (arrayLine1[0].ToLower().Trim() == strtm.ToLower().Trim())
                                    {
                                        tmLine[8] += arrayLine1[1].Trim() + ",";
                                    }
                                }

                            }
                        }

                    }

                    if (clm1 < strtm.Length)
                    {
                        clm1 = strtm.Length;
                    }
                    if (clm2 < tmLine[2].Length)
                    {
                        clm2 = tmLine[2].Length;
                    }
                    if (clm3 < tmLine[3].Length)
                    {
                        clm3 = tmLine[3].Length;
                    }
                    if (clm4 < tmLine[4].Length)
                    {
                        clm4 = tmLine[4].Length;
                    }
                    if (clm5 < tmLine[5].Length)
                    {
                        clm5 = tmLine[5].Length;
                    }
                    if (clm6 < tmLine[6].Length)
                    {
                        clm6 = tmLine[6].Length;
                    }
                    if (clm7 < tmLine[7].Length)
                    {
                        clm7 = tmLine[7].Length;
                    }
                    if (clm8 < tmLine[8].Length)
                    {
                        clm8 = tmLine[8].Length;
                    }

                    dictTraceMatrix[strtm] = strtm + "#"+
                    tmLine[2] + "#" +
                    tmLine[3] + "#" +
                    tmLine[4] + "#" +
                    tmLine[5] + "#" +
                    tmLine[6] + "#" +
                    tmLine[7] + "#" +
                    tmLine[8] + "#";

                }
                string strResult = string.Empty;
                string strSpace = string.Empty;
                string strHeader = string.Empty;


                string hdr1 = "Requirement ID".PadRight(("Requirement ID").Length > clm1 ? ("Requirement ID").Length : clm1 + 5);
                string hdr2 = "Code Files".PadRight(("Code Files").Length > clm2 ? ("Code Files").Length : clm2 + 5);
                string hdr3 = "Unit Test".PadRight(("Unit Test").Length > clm3 ? ("Unit Test").Length : clm3 + 5);
                string hdr4 = "Functional Test".PadRight(("Functional Test").Length > clm4 ? ("Functional Test").Length : clm4 + 5);
                string hdr5 = "Integration Test".PadRight(("Integration Test").Length > clm5 ? ("Integration Test").Length : clm5 + 5);
                string hdr6 = "HLD".PadRight(("HLD").Length > clm6 ? ("HLD").Length : clm6 + 5);
                string hdr7 = "LLD".PadRight(("LLD").Length > clm7 ? ("LLD").Length : clm7 + 5);
                string hdr8 = "User Manual";

                strHeader = hdr1 + hdr2 + hdr3 + hdr4 + hdr5 + hdr6 + hdr7 + hdr8 + "\r\n";

                int intCount = 0;
                int intLineSize = 0;

                foreach (string str in dictTraceMatrix.Keys)
                {
                    strResult = string.Empty;
                    arrayLine = dictTraceMatrix[str].Trim().Split(new string[] { "#" }, StringSplitOptions.None);

                    for (int i = 0; i < 8; i++)
                    {
                        intCount = 0;

                        if (i==0)
                        {
                            intCount = clm1;

                            if (hdr1.Length > intCount)
                            {
                                intCount = hdr1.Length;
                            }

                        }
                        else if (i == 1)
                        {
                            intCount = clm2;

                            if (hdr2.Length > intCount)
                            {
                                intCount = hdr2.Length;
                            }

                        }
                        else if (i == 2)
                        {
                            intCount = clm3;

                            if (hdr3.Length > intCount)
                            {
                                intCount = hdr3.Length;
                            }
                        }
                        else if (i == 3)
                        {
                            intCount = clm4;

                            if (hdr4.Length > intCount)
                            {
                                intCount = hdr4.Length;
                            }
                        }
                        else if (i == 4)
                        {
                            intCount = clm5;

                            if (hdr5.Length > intCount)
                            {
                                intCount = hdr5.Length;
                            }
                        }
                        else if (i == 5)
                        {
                            intCount = clm6;

                            if (hdr6.Length > intCount)
                            {
                                intCount = hdr6.Length;
                            }
                        }
                        else if (i == 6)
                        {
                            intCount = clm7;

                            if (hdr7.Length > intCount)
                            {
                                intCount = hdr7.Length;
                            }
                        }

                        strResult += arrayLine[i].Trim().PadRight(intCount);
                    }
                    if(intLineSize< strResult.Length  )
                    {
                        intLineSize = strResult.Length;
                    }
                    result += strResult + "\r\n"; //dictTraceMatrix[str].Trim() + "\r\n";
                }

                strHeader += new string('-', intLineSize);
                result = strHeader + "\r\n" + result;


            }



        if (result is not null)
            {     
            // await _DabaseFunctionality.SaveOrUpdateProjectAsync(requestModel.id, "Doc_TraceabilityMatrix");
            }
            return Ok(result);
        }

    }
    
    
}
