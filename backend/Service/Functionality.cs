using Azure;
using Azure.Core;
using backend.Models;
using FuzzySharp;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.Extensions.FileSystemGlobbing.Internal;
using Microsoft.Extensions.Hosting;
using Microsoft.Identity.Client;
using Microsoft.VisualBasic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Serilog.Core;
using System;
using System.Buffers.Text;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Diagnostics.SymbolStore;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Net.Security;
using System.Reflection.Emit;
using System.Reflection.Metadata;
using System.Runtime.ConstrainedExecution;
using System.Runtime.Intrinsics.X86;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using System.Xml.Linq;
using System.Xml.Serialization;
using static Azure.Core.HttpHeader;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;
using static System.Collections.Specialized.BitVector32;
using static System.Net.Mime.MediaTypeNames;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace backend.Service
{
    public class Functionality
    {
        private int chunkSize = 2048;  // Adjust as needed
        private string prompt = string.Empty;
        int GrandTotalToken = 0;
        private string strInsights = string.Empty;
        // Properties for the Blueprinting sub-tabs
        Dictionary<string, string> details = new Dictionary<string, string>
        {            { "requirementSummary", "" },
            { "solutionOverview", "" },
            { "projectStructure", "" },
            { "dataFlow", "" },
            { "unitTesting", "" },
            { "commonFunctionalities", "" },
            { "databaseScripts", "" }
        };

        public string DataFlow = string.Empty;
        public string SolutionOvervieww = string.Empty;
        private string brdtrdtemplate = string.Empty;
        // Azure AD Authentication settings
        // private string _AZURE_TENANT_ID = string.Empty;
        // private string _AZURE_CLIENT_ID = string.Empty;
        // private string _AZURE_CLIENT_SECRET = string.Empty;
        private int contextLimit = 0;
        private string llmmodel = string.Empty;
        private string ApiEndpoint = string.Empty;
        // private string AzureOpenAI = string.Empty;
        private string LoopContentHLD = string.Empty;
        private string LoopContentLLD = string.Empty;
        private string LoopContentUserManual = string.Empty;
        private int AddDelayForLLM = 0;

        // personal
        private string cachedApiKey = null;
        private string cachedApiEndpoint = null;

        private ILogger<Functionality> _logger;
        public Functionality(IConfiguration configuration, ILogger<Functionality> logger)
        {
            _logger = logger;
            // _AZURE_TENANT_ID = configuration["ApiSettings:AZURE_TENANT_ID"];
            // _AZURE_CLIENT_ID = configuration["ApiSettings:AZURE_CLIENT_ID"];
            // _AZURE_CLIENT_SECRET = configuration["ApiSettings:AZURE_CLIENT_SECRET"];
            contextLimit = int.Parse(configuration["ApiSettings:ContextLimit"].ToString());
            // llmmodel = configuration["ApiSettings:llmmodel"];
            // ApiEndpoint = configuration["ApiSettings:ApiEndpoint"];
            // AzureOpenAI = configuration["ApiSettings:AzureOpenAI"];
            brdtrdtemplate= configuration["ApiSettings:brdtrdtemplate"];
            LoopContentLLD = configuration["ApiSettings:LoopContentLLD"];
            LoopContentUserManual = configuration["ApiSettings:LoopContentUserManual"];
            AddDelayForLLM = int.Parse(configuration["ApiSettings:AddDelayForLLM"].ToString());

            cachedApiKey = configuration["ApiSettings:ApiKey"];
            cachedApiEndpoint = "https://api.openai.com/v1/chat/completions";
            llmmodel = configuration["ApiSettings:llmmodel"];
        }

        // public string GetApiEndpoint()
        // {
        //     return ApiEndpoint;
        // }

        // private async Task<string> GetApiKey()
        // {
        //     // Configure the MSAL client
        //     IConfidentialClientApplication app = ConfidentialClientApplicationBuilder
        //         .Create(_AZURE_CLIENT_ID)
        //         .WithTenantId(_AZURE_TENANT_ID)
        //         .WithClientSecret(_AZURE_CLIENT_SECRET)
        //         .Build();

        //     // Request token for Azure OpenAI 
        //     string[] scopes = new string[] { AzureOpenAI };
        //     AuthenticationResult result = await app.AcquireTokenForClient(scopes).ExecuteAsync();

        //     return result.AccessToken;
        // }

        

        private async Task<(string apiKey, string apiEndpoint)> GetApiCredentials()
        {
            // if (string.IsNullOrEmpty(cachedApiKey) || string.IsNullOrEmpty(cachedApiEndpoint))
            // {
            //     // Get fresh credentials from the helper
            //     cachedApiKey = await GetApiKey();
            //     cachedApiEndpoint = GetApiEndpoint();
            // }
            return (cachedApiKey, cachedApiEndpoint);
        }




        public async Task<string> AnalyzeBRD(string txtprompt, string strtask, int opt)
        {

            if (string.IsNullOrEmpty(txtprompt) || string.IsNullOrEmpty(strtask))
            {
                _logger.LogWarning("AnalyzeBRD: Missing txtprompt or strtask");
                return "Please provide Business Requirement and Task to perform";
            }

            int CompletionTokens = 0;
            int PromptTokens = 0;
            int TotalTokens = 0;
            string finalSummary = string.Empty;
            try
            {
                // Get credentials before starting the work
                 var (ApiKey, ApiEndpoint) = await GetApiCredentials();

                string context = string.Empty;
                chunkSize = (contextLimit * 3) - 100;
                if (chunkSize > 0 && contextLimit > 0)
                {
                    _logger.LogInformation("Splitting text into chunks.");

                    List<string> chunks = SplitTextIntoChunks(txtprompt, chunkSize);
                    var summaries = new List<string>();

                    foreach (var chunk in chunks)
                    {
                        prompt = BuildPrompt(context, strtask, chunk);
                        _logger.LogDebug("Calling GPT API with prompt size: {Length}", prompt.Length);

                        await Task.Delay(AddDelayForLLM);

                        var _APIResponse = await GetChatGPTResponseAsync(contextLimit, prompt, ApiKey, ApiEndpoint, llmmodel);
                        if (_APIResponse == null )//|| string.IsNullOrEmpty(_APIResponse.Response)
                        {
                            _logger.LogError("API response is null or empty");
                            throw new Exception("API response is null or empty");
                        }

                        summaries.Add(_APIResponse.Response);

                        context += "\n" + _APIResponse.Response;

                        CompletionTokens += _APIResponse.CompletionTokens;
                        PromptTokens += _APIResponse.PromptTokens;
                        TotalTokens += _APIResponse.TotalTokens;
                    }

                    GrandTotalToken += TotalTokens;
                    finalSummary = string.Join("\n\n", summaries);
                    _logger.LogInformation("AnalyzeBRD completed. TotalTokens: {TotalTokens}", TotalTokens);
                }

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception in AnalyzeBRD");
                finalSummary = $"Error: {ex.Message}";
            }

            return finalSummary;
        }
        public string GetSectionData(string strValue, string strSection)
        {
            string finalSummary = string.Empty;
            string sectionContent = string.Empty;
            try
            {
                

            string[] knownSections = brdtrdtemplate.Split(",",StringSplitOptions.RemoveEmptyEntries);

            var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            var lines = strValue.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
            string currentHeader = null;
            int currentHeaderLine = -1;


                for (int i = 0; i < lines.Length; i++)
                {
                    string line = lines[i].Trim();
                    //----------------------
                    if (line.ToLower().Replace(" ","") == "<header>module&submodulehierarchy")
                    {
                        line = "module&submodulehierarchy";
                    }
                    //------------------------
                    // Case 1: <Header> Business Requirement (inline)
                    if (line.StartsWith("<Header>", StringComparison.OrdinalIgnoreCase))
                    {
                        string headerText = line.Substring("<Header>".Length).Trim(':', ' ');

                        // If the header is on the next line
                        if (string.IsNullOrWhiteSpace(headerText) && i + 1 < lines.Length)
                        {
                            string nextLine = lines[i + 1].Trim().TrimEnd(':');
                            if (!string.IsNullOrWhiteSpace(nextLine))
                            {
                                headerText = nextLine;
                                i++; // Skip next line because it was used
                            }
                        }

                        currentHeader = headerText;
                        currentHeaderLine = i + 1;
                        result[currentHeader] = string.Empty;
                    }
                    else if (currentHeader != null)
                    {
                        // Append lines under the current section
                        result[currentHeader] += line + Environment.NewLine;
                    }
                }

            // Trim all values
            foreach (var key in result.Keys.ToList())
            {
                result[key] = result[key].Trim();
            }

            result.TryGetValue(strSection, out sectionContent);

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception in GetSectionData");
                finalSummary = $"Error: {ex.Message}";
            }

            return sectionContent;
        }
        
        public Dictionary<string, string> GenerateBluePrintDetails(
        string requirementSummary,            // kept for compatibility
        string strSolidification, string greenbrown)
        {

            Console.WriteLine(strSolidification);
            //------------------------------------------------------------------
            // 0.  result container
            //------------------------------------------------------------------
            var details = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            //------------------------------------------------------------------
            // 1.  canonical section names *in the order you expect them*
            //------------------------------------------------------------------
           
            string[] headers = 
            {
                   "Solution Overview",
                   "Solution Structure",
                   "Data Flow",
                    "Common Functionalities",
                    "RequirementSummary"
                    };

            var headerKeyMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["Solution Overview"] = "solutionOverview",
                ["Solution Structure"] = "projectStructure",
                ["Data Flow"] = "dataFlow",
                ["Common Functionalities"] = "commonFunctionalities",
                ["RequirementSummary"] = "requirementSummary"
            };

            if (greenbrown=="brown")
            {
                headers = new string[]
           {
                   "Solution Overview",
                   "Solution Structure",
                   "Data Flow",
                    "Common Functionalities"
                    };

                headerKeyMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    ["Solution Overview"] = "solutionOverview",
                    ["Solution Structure"] = "projectStructure",
                    ["Data Flow"] = "dataFlow",
                    ["Common Functionalities"] = "commonFunctionalities"
                };
            }


            //------------------------------------------------------------------
            // 2.  helper that builds a tolerant, line-anchored pattern
            //------------------------------------------------------------------
            static string BuildPattern(string header)
            {
                /*  ^\s*                 → leading blanks at start of the line
                 *  [\p{P}\p{S}]*\s*     → any number of punctuation / symbol chars
                 *                          (#, *, -, —, •, …) followed by spaces
                 */
                const string leadingSymbols = @"[\p{P}\p{S}]*\s*";

                /*  optional numbering   → 1.   III.   C.   12)   …
                 *                          finally followed by blanks
                 */
                const string numbering = @"(?:(?:\d+|[IVXLCDM]+|[A-Za-z])[.)])?\s*";

                /*  delimiter            → either  ':'  '-'  '–'
                 *                          OR (if no delimiter) the *end of line*
                 *  In both cases every blank / tab is eaten so the body starts with
                 *  its very first real character.
                 */
                const string delimiter =
                    @"(?:" +
                       @"(?:[:\u2013\-])\s*" +          // ':' or dash
                     @"|" +
                       @"\s*(?:\r?\n|\r|$)" +           // …or just EOL
                    @")";

                /*  Multiline mode (^ = start-of-line) will be supplied when the regex
                 *  is executed. */
                return $@"^\s*{leadingSymbols}{numbering}{Regex.Escape(header)}\s*{delimiter}";
            }

            //------------------------------------------------------------------
            // 3.  find all section headers in one single regex pass
            //------------------------------------------------------------------
            string combinedPattern = string.Join("|", headers.Select(BuildPattern));

            var matches = Regex.Matches(
                strSolidification,
                combinedPattern,
                RegexOptions.IgnoreCase | RegexOptions.Multiline);

            if (matches.Count == 0)
                return details;                             // nothing recognised

            //------------------------------------------------------------------
            // 4.  slice every section body and fill the dictionary
            //------------------------------------------------------------------
            for (int i = 0; i < matches.Count; i++)
            {
                int bodyStart = matches[i].Index + matches[i].Length; // past delimiter/EOL
                int bodyEnd = (i + 1 < matches.Count)
                              ? matches[i + 1].Index
                              : strSolidification.Length;

                string body = strSolidification.Substring(bodyStart, bodyEnd - bodyStart)
                                               .Trim();

                // Which canonical header was it?
                string canonicalHeader = headers.First(h =>
                    Regex.IsMatch(matches[i].Value, BuildPattern(h),
                                  RegexOptions.IgnoreCase | RegexOptions.Multiline));

                string key = headerKeyMap[canonicalHeader];
                details[key] = body;

                // maintain your original backing fields
                if (key.Equals("solutionOverview", StringComparison.OrdinalIgnoreCase))
                    SolutionOvervieww = body;
                if (key.Equals("dataFlow", StringComparison.OrdinalIgnoreCase))
                    DataFlow = body;
            }
            Console.WriteLine(details);
            return details;


        }


        public string GetPromptText(string filename, string filecontent, string Dataflow, string solutionOverview)
        {
            string finalSummary = string.Empty;
            string promptText = string.Empty;
            try
            {
                _logger.LogInformation("GetPromptText called for file: {Filename}", filename);
                promptText = "Solution Overview:\n" + solutionOverview +
                            "\nData Flow:\n" + Dataflow +
                            "\nFile Name:\n" + filename +
                            "\nFile Metadata:\n" + filecontent;
                _logger.LogDebug("Generated prompt text of length: {Length}", promptText.Length);

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception in GetPromptText");
                finalSummary = $"Error: {ex.Message}";
            }

            return promptText;
        }

        public List<object> getDetails(string name, string content, int k)
        {
            _logger.LogDebug("getDetails called with name={Name}, length={Length}", name, content.Length);
            return [name, content, k];
        }

        public bool IsEqualTwoString(string string1, string string2)
        {
            bool yn = false;
            string finalSummary = string.Empty;
           
            try
            {

                int score = Fuzz.Ratio(string1.ToLower(), string2.ToLower());
                yn = score >= 85;

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception in IsEqualTwoString");
                finalSummary = $"Error: {ex.Message}";
            }

            return yn;

        }
        public List<string> SplitTextIntoChunks(string text, int chunkSize)
        {
            var chunks = new List<string>();
            for (int i = 0; i < text.Length; i += chunkSize)
            {
                if (i + chunkSize > text.Length)
                {
                    chunkSize = text.Length - i;
                }
                chunks.Add(text.Substring(i, chunkSize));
            }
            return chunks;
        }
        public string BuildPrompt(string context, string task, string chunk)
        {
            string strBuildPrompt = string.Empty;

            strBuildPrompt = "Context:\n" + context + "\nTask:\n" + task + "\nText:\n" + chunk;

            return strBuildPrompt;
        }

        public async Task<APIResponse> SummerizeContext(int contextlimit, string context, string apikey, string endpoint, string llmmodel)
        {
            APIResponse _APIResponse = new APIResponse
            {
                Response = string.Empty,
                CompletionTokens = 0,
                PromptTokens = 0,
                TotalTokens = 0
            };
            string finalSummary = string.Empty;

            try
            {
                if (context.Length >= contextlimit)
                {
                    string summryPrompt = "Context: \n" + context + "\nTask: \nsummerize the Context to make it concise for further processing. within length " + contextlimit / 3;
                    _APIResponse = await GetChatGPTResponseAsync(contextlimit, summryPrompt, apikey, endpoint, llmmodel);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception in SummerizeContext");
                finalSummary = $"Error: {ex.Message}";
            }

            return _APIResponse;
        }


        public async Task<APIResponse> GetChatGPTResponseAsync(int contextlimit, string question, string apikey, string endpoint, string llmmodel)
        {
            string generatedcontent2 = string.Empty;
            List<string> FinalContents = new List<string>();
            string QUESTION = question;
            APIResponse _APIResponse = new APIResponse
            {
                Response = string.Empty,
                CompletionTokens = 0,
                PromptTokens = 0,
                TotalTokens = 0
            };
            string finalSummary = string.Empty;

            try
            {
                string finishresponse = string.Empty;
                var handler = new HttpClientHandler();
                handler.ServerCertificateCustomValidationCallback = (sender, cert, chain, SslPolicyErrors) => true;
                using (var httpClient = new HttpClient(handler))
                { 
                //var httpClient = HttpClientProvider.Client;
                //if (httpClient.DefaultRequestHeaders.Authorization is null)
                //{
                    httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {apikey}");
                //}

                    var payload = new
                    {
                        model = llmmodel,
                        messages = new[]
                        {
                            new
                            {
                                role = "user",
                                content = QUESTION
                            }
                        },
                        temperature = 0,//0.7,
                        top_p = 1.0, //0.95,
                        stream = false
                    };

                    int attempt = 0;
                    int delay = 2000;
                    int maxtry = 3;

                    while (attempt < maxtry)
                    {
                        try
                        {
                            using (HttpResponseMessage response = await httpClient.PostAsync(endpoint, new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json")))
                            {
                                if (response.IsSuccessStatusCode)
                                {
                                    dynamic responseObject = JsonConvert.DeserializeObject<object>(await response.Content.ReadAsStringAsync());
                                    if (responseObject != null)
                                    {
                                        attempt = maxtry;

                                        if (((JArray)responseObject.choices).Count > 0)
                                        {
                                            finishresponse = ((string)responseObject.choices[0].finish_reason).Trim();
                                            generatedcontent2 = ((string)responseObject.choices[0].message.content).Trim();
                                        }

                                        FinalContents.Add(generatedcontent2);
                                        _APIResponse.Response = generatedcontent2;
                                        dynamic usageproperty = responseObject.usage;
                                        _APIResponse.Finishreason = finishresponse;
                                        _APIResponse.CompletionTokens = (int)usageproperty.completion_tokens;
                                        _APIResponse.PromptTokens = (int)usageproperty.prompt_tokens;
                                        _APIResponse.TotalTokens = (int)usageproperty.total_tokens;
                                        if (!(finishresponse.ToLower() == "length"))
                                        {
                                            break;
                                        }

                                        int strlenth = generatedcontent2.Length;
                                        int startindex = ((strlenth > 100) ? (strlenth - 100) : 0);
                                        QUESTION = " continue from: " + generatedcontent2.Substring(startindex);
                                    }


                                }
                                else
                                {
                                    if (IsTransientError(response.StatusCode))
                                    {
                                        attempt++;
                                        if (attempt < maxtry)
                                        {
                                            await Task.Delay(delay);
                                            delay *= 2;
                                            continue;
                                        }
                                    }
                                    else
                                    {
                                        generatedcontent2 = response.StatusCode.ToString() + " ----" + response.ReasonPhrase;
                                        FinalContents.Add(generatedcontent2);
                                        break;
                                    }
                                }

                            }


                        }
                        catch (TaskCanceledException ex)
                        {
                            attempt++;
                            if (attempt < maxtry)
                            {
                                await Task.Delay(delay);
                                delay *= 2;
                                continue;
                            }
                            throw;
                        }
                        catch (HttpRequestException ex) when (IsTransientException(ex))
                        {
                            attempt++;
                            if (attempt < maxtry)
                            {
                                await Task.Delay(delay);
                                delay *= 2;
                                continue;
                            }
                            throw;
                        }

                    }
                }

                generatedcontent2 = string.Join("\n", FinalContents);
                _APIResponse.Response = generatedcontent2;

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception in GetChatGPTResponseAsync");
                finalSummary = $"Error: {ex.Message}";
            }
            return _APIResponse;
        }


        private bool IsTransientError(HttpStatusCode statuscode)
        {

            return statuscode == HttpStatusCode.RequestTimeout ||
                statuscode == HttpStatusCode.Unauthorized ||
                statuscode == HttpStatusCode.TooManyRequests ||
                ((int)statuscode >= 500 && (int)statuscode < 600);
        }

        private bool IsTransientException(HttpRequestException ex)
        {
            return true;
        }

        public string GetSubmoduleInsightInformation(Submodule objsubmodule, string strval)
        {
            if (strval == string.Empty)
            {
                strInsights = strval;
            }
            string content = string.Empty;
            if (objsubmodule.Name != null)
            {
                strInsights += "\r\n" + objsubmodule.Name + "\r\n";
                content = objsubmodule.Content;
                if (!string.IsNullOrEmpty (content))
                {
                    //Insightresult = await AnalyzeBRD(content, strtask, 0);
                    strInsights += objsubmodule.Name + "\r\n" + content + "\r\n";
                }

            }

            if (objsubmodule.Submodules != null)
            {
                if (objsubmodule.Submodules.Count > 0)
                {
                    foreach (var submodule in objsubmodule.Submodules)
                    {
                        GetSubmoduleInsightInformation(submodule, "yes");
                    }
                }
            }
            return (strInsights);
        }
         

        public string GetHeaderFormatedforUDO(string strValue)
        {
            string[] patterns = brdtrdtemplate.Split(",").Select(s=>s.Trim()).ToArray();
            
            string pattern = string.Join("|", patterns);

            string apattern = $@"^\s*({pattern})\s*$";
            strValue = Regex.Replace(strValue, apattern, "<Header> $1",RegexOptions.IgnoreCase|RegexOptions.Multiline );

            return strValue;
        }

        public Dictionary<string,string> GetkeyValuePairs (string strInput,string strsearch)
        {
            var result = new Dictionary<string, string>();
            var regex = new Regex($@"(?<key>{strsearch}\d+)\s+(?<value>.*?)(?={strsearch}\d+|\z)", RegexOptions.Singleline | RegexOptions.IgnoreCase);

            var matches = regex.Matches(strInput);

            foreach (Match match in matches)
            {
                string key = match.Groups["key"].Value.Trim();
                string value = match.Groups["value"].Value.Trim();
                if (!string.IsNullOrEmpty(value) )
                {
                    if (value.Length > 100 && !value.ToLower().Contains("no relevant information"))
                    {
                        result[key] = value;
                    }
                }
            }

            return result;
        }
        public bool IsNeeded(string strdocumenttype)
        {
            bool isneeded = false;
            string[] loopContent = null;
            if (strdocumenttype == "lld")
            {
                loopContent = LoopContentLLD.Split(",", StringSplitOptions.RemoveEmptyEntries);
            }
            else if (strdocumenttype == "user manual")
            {
                loopContent = LoopContentUserManual.Split(",", StringSplitOptions.RemoveEmptyEntries);
            }
            else if (strdocumenttype == "hld")
            {
                loopContent = LoopContentHLD.Split(",", StringSplitOptions.RemoveEmptyEntries);
            }
            if (loopContent != null)
            {
                isneeded = (loopContent.Length > 0);
            }

            return isneeded;
        }

        public bool TextFoundFromList(string input, HashSet<string> sectionHeaders)
        {
            bool found = false;

            foreach (string header in sectionHeaders)
            {
                if(input.ToLower().StartsWith( header.ToLower()))
                {
                    found = true; ;
                    break;
                }
            }
            return found; 
        }

        public Dictionary<string, string> GetTextSplitedbySections(string input, HashSet<string> sectionHeaders)
        {
        
            Dictionary<string, string> sections = new Dictionary<string, string>();
            string currentSection = null;
            List<string> buffer = new List<string>();

            string[] lines = input.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);

            foreach (var line in lines)
            {
                string trimmed = line.Trim();

                if (sectionHeaders.Contains(trimmed) || TextFoundFromList(trimmed,sectionHeaders))
                {
                    if (currentSection != null)
                    {
                        // Save the previous section content
                        sections[currentSection] = string.Join(Environment.NewLine, buffer).Trim();
                        buffer.Clear();
                    }

                    currentSection = trimmed;
                }
                else if (currentSection != null)
                {
                    buffer.Add(line);
                }
            }

            // Add the last section
            if (currentSection != null)
            {
                sections[currentSection] = string.Join(Environment.NewLine, buffer).Trim();
            }
                        

            return sections;
        }




        public async Task<string> GetSectionWiseContent(string solutionOverview, string solutionStructure, string requirementsSummary, string documentTemplate, string strdocumenttype, string generatedCode)
        {
            string strresult = string.Empty;
            string finalSummary = string.Empty;
            try
            {

                string[] strSectionContents = documentTemplate.Trim().Split("section", StringSplitOptions.RemoveEmptyEntries);

                string[] loopContent = null;

                if (strdocumenttype == "lld")
                {
                    loopContent = LoopContentLLD.Split(",", StringSplitOptions.RemoveEmptyEntries);
                }
                else if (strdocumenttype == "user manual")
                {
                    loopContent = LoopContentUserManual.Split(",", StringSplitOptions.RemoveEmptyEntries);
                }
                else if (strdocumenttype == "hld")
                {
                    loopContent = LoopContentHLD.Split(",", StringSplitOptions.RemoveEmptyEntries);
                }

                if (loopContent != null)
                {
                    int indexint = -1;
                    int indxcount = 0;
                    bool needNested = false;
                    foreach (string sectionDetails in strSectionContents)
                    {
                        indexint = -1;
                        needNested = false;
                        string strprompt = string.Empty;
                        string result = string.Empty;

                        if (!string.IsNullOrEmpty(sectionDetails) && sectionDetails.Length > 20)
                        {
                            string[] sectiontitle = sectionDetails.Split("\r\n", StringSplitOptions.RemoveEmptyEntries);

                            foreach (string str in loopContent)
                            {
                                string[] strSectionloop = str.Split(" ", StringSplitOptions.RemoveEmptyEntries);

                                if (strSectionloop.Length > 0)
                                {
                                    indexint = int.Parse(strSectionloop[strSectionloop.Length - 1]);
                                    if (indexint > -1)
                                    {
                                        if (indxcount == indexint - 1)
                                        {
                                            needNested = true;
                                            break;
                                        }
                                    }
                                }

                            }

                            if (needNested)
                            {
                                string[] generatedCodeSections = generatedCode.Split("section", StringSplitOptions.RemoveEmptyEntries);
                                string strprompttask = string.Empty;

                                if (strdocumenttype == "lld" || strdocumenttype == "hld")
                                {
                                    strprompttask = GetPromptTask(14, 3);

                                }
                                else if (strdocumenttype == "user manual")
                                {
                                    strprompttask = GetPromptTask(14, 4);
                                }

                                foreach (string str in generatedCodeSections)
                                {
                                    if (str.Trim().Length > 20)
                                    {
                                        strprompt = "solution overview:\r\n" + solutionOverview + "\r\nsolution structure:\r\n" + str + "\r\ndocument template:\r\n" + sectionDetails;

                                        result = await AnalyzeBRD(strprompt, strprompttask, 3) + "\r\n \r\n";
                                        string[] arrayResults = result.ToLower().Split(new string[] { "duplicateformatfound=yes", "duplicateformatfound=no" }, StringSplitOptions.RemoveEmptyEntries);
                                        result = string.Empty;
                                        foreach (string strk in arrayResults)
                                        {
                                            if (result.Length < strk.Length)
                                            {
                                                result = strk.Trim();
                                            }
                                        }

                                        strresult += result + "\r\n";
                                    }
                                }

                            }
                            else
                            {
                                if (strdocumenttype == "hld")
                                {
                                    strprompt = "solution overview:\r\n" + solutionOverview + "\r\nsolution structure:\r\n" + solutionStructure; // + "\r\ndocument template start:\r\n" + sectionDetails  +"\r\ndocument template end:";
                                    result = await AnalyzeBRD(strprompt, GetPromptTask(14, 5, strdocumenttype, sectionDetails), 3) + "\r\n";

                                    string[] arrayResults = result.ToLower().Split(new string[] { "duplicateformatfound=yes", "duplicateformatfound=no" }, StringSplitOptions.RemoveEmptyEntries);
                                    result = string.Empty;
                                    foreach (string strk in arrayResults)
                                    {
                                        if (result.Length < strk.Length)
                                        {
                                            result = strk.Trim();
                                        }
                                    }

                                    strresult += result + "\r\n";
                                }
                                else
                                {
                                    strprompt = "solution overview:\r\n" + solutionOverview + "\r\nsolution structure:\r\n" + solutionStructure;// + "\r\ndocument template:\r\n" + sectionDetails;
                                    result = await AnalyzeBRD(strprompt, GetPromptTask(14, 5, strdocumenttype, sectionDetails), 3) + "\r\n";
                                    string[] arrayResults = result.ToLower().Split(new string[] { "duplicateformatfound=yes", "duplicateformatfound=no" }, StringSplitOptions.RemoveEmptyEntries);
                                    result = string.Empty;
                                    foreach (string strk in arrayResults)
                                    {
                                        if (result.Length < strk.Length)
                                        {
                                            result = strk.Trim();
                                        }
                                    }

                                    strresult += result + "\r\n";
                                }

                            }
                            indxcount++;
                        }

                    }

                }


            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception in GetSectionWiseContent");
                finalSummary = $"Error: {ex.Message}";
            }
            return strresult;

        }
        public string GetStringTrimStart(string strinput)
        {
            string[] linesDB = strinput.ToLower().Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
            int i = 0;
            char[] MyChar = { '-', ' ', '.', '*' };
            var outputLines = new List<string>();

            while (i < linesDB.Length)
            {
                string line = linesDB[i].Trim().TrimStart(MyChar).Trim();
                if (line.Length > 0)
                {
                    outputLines.Add(line);
                }
                i++;
            }

            strinput = string.Join(Environment.NewLine, outputLines);

            return strinput;
        }
        public string GetOneItterationAllContent(string input, string strsearch1, string strsearch2)
        {

            string[] lines = input.ToLower().Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
            var outputLines = new List<string>();
            var seenDbmsBlock = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            bool firstHeaderWritten = false;
            int i = 0;
            char[] MyChar = { '-', ' ', '.', '*'};
            string output = string.Empty;
            string finalSummary = string.Empty;

            try
            {
                while (i < lines.Length)
                {
                    string line = lines[i].Trim().TrimStart(MyChar);
                    outputLines.Add(line);
                    i++;
                }

                output = string.Join(Environment.NewLine, outputLines);
                lines = output.ToLower().Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);

                outputLines = new List<string>();
                i = 0;
                while (i < lines.Length)
                {
                    string line = lines[i].Trim();

                    if (line.StartsWith(strsearch1, StringComparison.OrdinalIgnoreCase))
                    {
                        string blockKey = lines[i].Trim();
                        int skip = 1;

                        if (i + 1 < lines.Length && lines[i + 1].Trim().StartsWith(strsearch2, StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(strsearch2))
                        {
                            blockKey += "\n" + lines[i + 1].Trim();
                            skip = 2;
                        }

                        if (!firstHeaderWritten)
                        {
                            firstHeaderWritten = true;

                            for (int j = 0; j < skip; j++)
                                outputLines.Add(lines[i + j]);

                            outputLines.Add(""); // Add blank line after header block
                        }

                        i += skip;
                    }
                    else
                    {
                        outputLines.Add(lines[i]);
                        i++;
                    }
                }
                output = string.Join(Environment.NewLine, outputLines);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception in GetOneItterationAllContent");
                finalSummary = $"Error: {ex.Message}";
            }

            return (output);
        }

        public async Task<Dictionary<string, string>> GenerateTestcases(string strSolutionOverview, string strSolutionStructure, string strCommonFuncitonalities,
            bool needUnitTest, bool needFunctionalTest, bool needIntegrationTest, Dictionary<string, string> result)
        {
            string solutionoverview = strSolutionOverview;
            string commonfuncitonalities = strCommonFuncitonalities;
            string strsolutionstructure = strSolutionStructure;
            Dictionary<string, string> dictmetadatapart = new Dictionary<string, string>();
            Dictionary<string, string> dictmetadata = new Dictionary<string, string>();// databse object, unit, funtional, integration metadata
            
            string[] lines = null;
            string finalSummary = string.Empty;
            try
            {
                if (!string.IsNullOrEmpty(solutionoverview) && !string.IsNullOrEmpty(strsolutionstructure) && (needUnitTest || needFunctionalTest || needIntegrationTest))
                {

                    lines = strsolutionstructure.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);

                    int index = Array.FindIndex(lines, line => line.Contains("root folder"));
                    string strSolutionStrcutures = string.Empty;
                    string SolutionName = string.Empty;
                    if (index >= 0 && index < lines.Length - 1 - 1)
                    {
                        string[] linesafter = lines.Skip(index + 1).ToArray();
                        strSolutionStrcutures = string.Join(Environment.NewLine, linesafter);
                        string lineat0 = lines[0].ToLower();
                        if (lineat0.Contains("solution name:"))
                        {
                            linesafter = lineat0.Split(new string[] { "solution name:", "solution name" }, StringSplitOptions.RemoveEmptyEntries);
                            if (linesafter.Length > 0)
                            {
                                SolutionName = linesafter[linesafter.Length - 1].Trim();
                            }
                            else
                            {
                                SolutionName = lineat0;
                            }
                        }

                    }

                    if (!string.IsNullOrEmpty(strSolutionStrcutures) && (needUnitTest || needFunctionalTest || needIntegrationTest))
                    {
                        string[] strProjects = strSolutionStrcutures.Split(new string[] { "project name:"}, StringSplitOptions.RemoveEmptyEntries);

                        string projectdetails = string.Empty;
                        int filecount = 0;

                        foreach (string strProject in strProjects)
                        {
                            projectdetails += strProject + "\r\n";

                            string[] strProjectPaths = strProject.ToString().Trim().Split(new string[] { "project path:" }, StringSplitOptions.RemoveEmptyEntries);
                            string projectpathdetails = string.Empty;
                            int count = 0;

                            foreach (string strProjectPath in strProjectPaths)
                            {
                                if (count == 0)
                                {
                                    count++;
                                    continue;
                                }
                                projectpathdetails += strProjectPath + "\r\n";

                                string[] strFileNames = strProjectPath.ToString().Trim().Split(new string[] { "file name:"}, StringSplitOptions.RemoveEmptyEntries);
                                string filename = string.Empty;

                                foreach (string strFileName in strFileNames)
                                {

                                    if (strFileName.ToLower().Contains("implementation details"))
                                    {
                                        string[] metadatalines = strFileName.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);

                                        if (metadatalines.Length > 0)
                                        {
                                            filename = metadatalines[0].ToString();

                                            string[] linesafter = metadatalines.Skip(1).ToArray();
                                            string filemetadata = string.Join(Environment.NewLine, linesafter);

                                            filemetadata = "File Name:" + filename + "\r\n" + filemetadata;


                                            string strdbscript = await AnalyzeBRD(filemetadata, GetPromptTask(13, 0), 2);


                                            string[] strdbscriptArray = strdbscript.Split(new string[] { "unit test", "functional test", "integration test", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);

                                            bool canadd = false;

                                            foreach (string str in strdbscriptArray)
                                            {
                                                if (!string.IsNullOrEmpty(str.Trim()) && str.Trim().Length > 100 && !str.ToLower().Contains("no relevant information")
                                                    && !str.ToLower().Contains("not contain any feature or functionality"))
                                                {
                                                    canadd = true;
                                                }
                                            }
                                            if (canadd)
                                            {
                                                dictmetadata.Add(filecount.ToString() + ":" + filename, strdbscript);
                                            }

                                            filecount++;
                                            //await Task.Delay(2000);
                                        }

                                    }


                                }

                            }
                        }

                    }


                    string sremetadataunit = string.Empty;
                    string stemetadatafunc = string.Empty;
                    string strmetadataint = string.Empty;
                    var resultmetadata = new Dictionary<string, string>();
                    int dictintcount = 0;

                    foreach (string str in dictmetadata.Keys)
                    {
                        string strmetadata = dictmetadata[str].ToString().ToLower();

                        string[] metadatalines = strmetadata.ToLower().Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);

                        var sortedlist = new SortedList<int, string>();

                        int unitindex = Array.FindIndex(metadatalines, line => line.Contains("unit test"));
                        if (!sortedlist.ContainsKey(unitindex))
                        {
                            sortedlist.Add(unitindex, "unit");
                        }
                        int funcindex = Array.FindIndex(metadatalines, line => line.Contains("functional test"));
                        if (!sortedlist.ContainsKey(funcindex))
                        {
                            sortedlist.Add(funcindex, "func");
                        }
                        int intindex = Array.FindIndex(metadatalines, line => line.Contains("integration test"));
                        if (!sortedlist.ContainsKey(intindex))
                        {
                            sortedlist.Add(intindex, "int");
                        }

                        int eintdb = 0;
                        int sintunit = 0;
                        int eintunit = 0;
                        int sintfunc = 0;
                        int eintfunc = 0;
                        int sintint = 0;
                        int eintint = 0;

                        eintdb = -1;
                        sintunit = -1;
                        eintunit = -1;
                        sintfunc = -1;
                        eintfunc = -1;
                        sintint = -1;
                        eintint = -1;

                        foreach (var ky in sortedlist)
                        {

                            if (ky.Value == "unit")
                            {
                                if (ky.Key >= 0)
                                {
                                    eintdb = ky.Key;
                                    sintunit = ky.Key;
                                }
                            }
                            if (ky.Value == "func")
                            {
                                if (ky.Key >= 0)
                                {
                                    eintunit = ky.Key;
                                    sintfunc = ky.Key;
                                }
                            }
                            if (ky.Value == "int")
                            {
                                if (ky.Key >= 0)
                                {
                                    eintfunc = ky.Key;
                                    sintint = ky.Key;
                                    eintint = metadatalines.Length;
                                }
                            }
                        }

                        if (sintunit > -1 && eintunit > -1)
                        {
                            string[] slicetext = metadatalines.Skip(sintunit + 1).Take(eintunit - (sintunit + 1)).ToArray();
                            sremetadataunit += "unit test metadata:" + dictintcount.ToString() + "\r\n" + string.Join("\r\n", slicetext) + "\r\n";


                        }
                        if (sintfunc > -1 && eintfunc > -1)
                        {
                            string[] slicetext = metadatalines.Skip(sintfunc + 1).Take(eintfunc - (sintfunc + 1)).ToArray();
                            stemetadatafunc += "functional test metadata:" + dictintcount.ToString() + "\r\n" + string.Join("\r\n", slicetext) + "\r\n";


                        }
                        if (sintint > -1 && eintint > -1)
                        {
                            string[] slicetext = metadatalines.Skip(sintint + 1).Take(eintint - (sintint + 1)).ToArray();
                            strmetadataint += "integration test metadata:" + dictintcount.ToString() + "\r\n" + string.Join("\r\n", slicetext) + "\r\n";


                        }

                        dictintcount++;
                    }


                    if (needUnitTest)
                    {
                        _logger.LogInformation("Generating unit tests...");

                        dictmetadatapart = new Dictionary<string, string>();

                        dictmetadatapart = GetkeyValuePairs(sremetadataunit, "unit test metadata:");

                        result["unitTesting"] = string.Empty;

                        Dictionary<string, string> dictUnitTest = new Dictionary<string, string>();
                        string strPrevCreatedFiles = string.Empty;
                        strPrevCreatedFiles += "\r\n" + "instruction to avoid duplicate:\tavoid generating duplicate file name referring bellow previously generated information:\r\n";
                        int needAgain = 0;
                        string resultUnitTest = string.Empty;

                        foreach (string strkey in dictmetadatapart.Keys)
                        {
                            sremetadataunit = dictmetadatapart[strkey].ToLower().Trim();

                            if (sremetadataunit == string.Empty || sremetadataunit.Length < 100 || sremetadataunit.Contains("no relevant information"))
                            {
                                continue;
                            }
                            string strfilename = string.Empty;
                            
                            var prompttext = "Solution Overview:\r\n" + solutionoverview + "\r\nunit test metadata:\r\n" + sremetadataunit +
                                "\r\ncommon functionalities:\r\n" + commonfuncitonalities + strPrevCreatedFiles;
                            needAgain = 0;
                            resultUnitTest = string.Empty;
                            while (needAgain < 2)
                            {
                                resultUnitTest = await AnalyzeBRD(prompttext, GetPromptTask(5, 0, "", SolutionName), 2) + "\r\n";
                                resultUnitTest = GetStringTrimStart(resultUnitTest);
                                string[] arrayMetadata = resultUnitTest.Trim().ToLower().Split(new string[] { "project name:", "project name"}, StringSplitOptions.RemoveEmptyEntries);

                                foreach (string strM in arrayMetadata)
                                {
                                    if (strM.ToLower().Contains("test case id"))
                                    {
                                        resultUnitTest = "project name:"+ strM;

                                        arrayMetadata = resultUnitTest.Trim().Split(new string[] { "file name:", "file name", "unit testing tech stack:", "unit testing tech stack" }, StringSplitOptions.RemoveEmptyEntries);

                                        if (arrayMetadata.Length > 2)
                                        {
                                            strfilename = arrayMetadata[1].Trim().ToLower();
                                            if (!string.IsNullOrEmpty(strfilename))
                                            {
                                                strPrevCreatedFiles += strfilename + "\r\n";

                                                if (dictUnitTest.ContainsKey(strfilename))
                                                {
                                                    dictUnitTest[strfilename] += "\r\n" + resultUnitTest;
                                                }
                                                else
                                                {
                                                    dictUnitTest.Add(strfilename, resultUnitTest);
                                                }

                                            }

                                        }
                                        needAgain = 10;
                                        break;
                                    }
                                    else
                                    {
                                        needAgain++;
                                    }
                                }
                                needAgain++;
                            }
                        }

                        foreach (string strk in dictUnitTest.Keys)
                        {
                            if (dictUnitTest[strk].ToLower().Contains("test case id"))
                            {
                                result["unitTesting"] += dictUnitTest[strk] + "\r\n";
                            }
                        }

                        if (!string.IsNullOrEmpty(result["unitTesting"].ToString().Trim()))
                        {
                            result["unitTesting"] = GetOneItterationAllContent(result["unitTesting"].Trim().ToLower(), "project name:", "");
                        }
                    }


                    if (needFunctionalTest) //needFunctionalTest || needIntegrationTest
                    {
                        _logger.LogInformation("Generating functional tests...");

                        dictmetadatapart = new Dictionary<string, string>();

                        dictmetadatapart = GetkeyValuePairs(stemetadatafunc, "functional test metadata:");
                        result["FunctionalTesting"] = string.Empty;
                        var prompttext = string.Empty;
                        string strFunctionalTest = string.Empty;

                        foreach (string strkey in dictmetadatapart.Keys)
                        {
                            stemetadatafunc = dictmetadatapart[strkey].ToString().Trim();

                            if (stemetadatafunc.Trim() == string.Empty || stemetadatafunc.Trim().Length < 100 || stemetadatafunc.ToLower().Contains("no relevant information")
                                || stemetadatafunc.ToLower().Contains("not contain any feature or functionality"))
                            {
                                continue;
                            }

                            prompttext = "Solution Overview:\n" + solutionoverview + "\ncommon functionalities:\n" + commonfuncitonalities + "\nfunctional test metadata:\n" + stemetadatafunc;
                            strFunctionalTest = await AnalyzeBRD(prompttext, GetPromptTask(9, 0, "", SolutionName), 2) + "\r\n";
                            strFunctionalTest = GetStringTrimStart(strFunctionalTest);
                            if (strFunctionalTest.ToLower().Contains("test case id"))
                            {
                                result["FunctionalTesting"] += strFunctionalTest;
                            }
                        }

                        if (!string.IsNullOrEmpty(result["FunctionalTesting"].ToString().Trim()))
                        {
                            result["FunctionalTesting"] = GetOneItterationAllContent(result["FunctionalTesting"].Trim().ToLower(), "project name:", "");

                        }

                        string[] arrayusecase = result["FunctionalTesting"].Split("use case:", StringSplitOptions.RemoveEmptyEntries);

                        if (arrayusecase.Length > 0)
                        {
                            string projectname = arrayusecase[0].ToString();
                            Dictionary<string, string> strusecases = new Dictionary<string, string>();
                            string strusecaseresult = string.Empty;
                            for (int i = 1; i < arrayusecase.Length - 1; i++)
                            {
                                string[] arrayusercase = arrayusecase[i].Split("\r\n", StringSplitOptions.RemoveEmptyEntries);
                                string strusecase = arrayusercase[0];
                                string strusecasebody = string.Join(Environment.NewLine, arrayusercase.Skip(1).ToArray());

                                if (!strusecases.ContainsKey("use case: " + strusecase))
                                {
                                    strusecases.Add("use case: " + strusecase, strusecasebody);
                                }
                                else
                                {
                                    strusecases["use case: " + strusecase] += "\r\n" + strusecasebody;
                                }
                            }

                            result["FunctionalTesting"] = projectname;

                            foreach (string strk in strusecases.Keys)
                            {
                                string strusecasebody = strusecases[strk];

                                string[] strusecasebodyArray = strusecasebody.Split(new string[] { "file name:", "user story:", "functional testing tech stack:" }, StringSplitOptions.RemoveEmptyEntries);

                                Dictionary<string, string> similarfileusestory = new Dictionary<string, string>();

                                int inttoggle = 0;
                                string filename = string.Empty;
                                string userstory = string.Empty;
                                string strrest = string.Empty;
                                strusecasebody = string.Empty;
                                foreach (string str in strusecasebodyArray)
                                {
                                    if (inttoggle == 0 && !str.Trim().Contains(" ") && str.Trim().Split(".").Length == 2)
                                    {
                                        filename = str.Trim();
                                        inttoggle = 1;

                                    }
                                    else if (inttoggle == 1)
                                    {
                                        userstory = str.Trim();
                                        inttoggle = 2;

                                    }
                                    else if (inttoggle == 2)
                                    {
                                        strrest = str.Trim();
                                        inttoggle = 0;
                                        bool canadd = true;

                                        foreach (string strkey in similarfileusestory.Keys)
                                        {
                                            if (IsEqualTwoString(strkey, userstory))
                                            {
                                                canadd = false;
                                                break;
                                            }
                                        }

                                        if (canadd)
                                        {
                                            if (strrest.Trim().Length > 100)
                                            {
                                                similarfileusestory.Add(userstory, strrest);

                                                strusecasebody += "user story: " + userstory + "\r\n";
                                                strusecasebody += "functional testing tech stack: " + strrest + "\r\n";
                                            }
                                        }
                                    }
                                }
                                if (filename.Trim() != "")
                                {
                                    strusecasebody = "file name: " + filename + "\r\n" + strusecasebody;

                                    strusecaseresult = GetStringTrimStart(strusecasebody);

                                    if (!strusecaseresult.StartsWith("error"))
                                    {
                                        result["FunctionalTesting"] += strk + "\r\n" + strusecaseresult + "\r\n";
                                    }
                                    else
                                    {
                                        result["FunctionalTesting"] += strk + "\r\n" + strusecasebody + "\r\n";
                                    }
                                }
                            }

                        }
                    }

                    if (needIntegrationTest)
                    {
                        _logger.LogInformation("Generating Integration Tests...");

                        dictmetadatapart = new Dictionary<string, string>();

                        dictmetadatapart = GetkeyValuePairs(strmetadataint, "integration test metadata:");
                        result["IntegrationTesting"] = string.Empty;
                        string strIntegrationTest = string.Empty;

                        foreach (string strkey in dictmetadatapart.Keys)
                        {
                            strmetadataint = dictmetadatapart[strkey].ToLower().Trim();

                            if (strmetadataint == string.Empty || strmetadataint.Trim().Length < 100 || strmetadataint.ToLower().Contains("no relevant information")
                                || strmetadataint.ToLower().Contains("not contain any feature or functionality"))
                            {
                                continue;
                            }
                            var prompttext = "Solution Overview:\n" + solutionoverview + "\ncommon functionalities:\n" + commonfuncitonalities + "\nintegration test metadata:\n" + strmetadataint;
                            strIntegrationTest = await AnalyzeBRD(prompttext, GetPromptTask(11, 0, "", SolutionName), 2) + "\r\n";
                            strIntegrationTest = GetStringTrimStart(strIntegrationTest);
                            if (strIntegrationTest.ToLower().Contains("test case id"))
                            {
                                result["IntegrationTesting"] += strIntegrationTest;
                            }
                        }

                        if (!string.IsNullOrEmpty(result["IntegrationTesting"].ToString().Trim()))
                        {
                            result["IntegrationTesting"] = GetOneItterationAllContent(result["IntegrationTesting"].Trim().ToLower(), "project name:", "");
                        }

                        string[] arrayusecase = result["IntegrationTesting"].Split("integration title:", StringSplitOptions.RemoveEmptyEntries);

                        if (arrayusecase.Length > 0)
                        {
                            string projectname = arrayusecase[0].ToString();
                            Dictionary<string, string> strusecases = new Dictionary<string, string>();
                            string strusecaseresult = string.Empty;
                            for (int i = 1; i < arrayusecase.Length - 1; i++)
                            {
                                string[] arrayusercase = arrayusecase[i].Split("\r\n", StringSplitOptions.RemoveEmptyEntries);
                                string strusecase = arrayusercase[0];
                                string strusecasebody = string.Join(Environment.NewLine, arrayusercase.Skip(1).ToArray());
                                if (!strusecases.ContainsKey("integration title: " + strusecase))
                                {
                                    strusecases.Add("integration title: " + strusecase, strusecasebody);
                                }
                                else
                                {
                                    strusecases["integration title: " + strusecase] += "\r\n" + strusecasebody;
                                }
                            }

                            result["IntegrationTesting"] = projectname;

                            foreach (string strk in strusecases.Keys)
                            {
                                string strusecasebody = strusecases[strk];

                                string[] strusecasebodyArray = strusecasebody.Split(new string[] { "file name:", "title:", "integration testing tech stack:" }, StringSplitOptions.RemoveEmptyEntries);

                                Dictionary<string, string> similarfileusestory = new Dictionary<string, string>();

                                int inttoggle = 0;
                                string filename = string.Empty;
                                string userstory = string.Empty;
                                string strrest = string.Empty;
                                strusecasebody = string.Empty;
                                foreach (string str in strusecasebodyArray)
                                {
                                    if (inttoggle == 0 && !str.Trim().Contains(" ") && str.Trim().Split(".").Length == 2)
                                    {
                                        filename = str.Trim();
                                        inttoggle = 1;

                                    }
                                    else if (inttoggle == 1)
                                    {
                                        userstory = str.Trim();
                                        inttoggle = 2;

                                    }
                                    else if (inttoggle == 2)
                                    {
                                        strrest = str.Trim();
                                        inttoggle = 0;
                                        bool canadd = true;

                                        foreach (string strkey in similarfileusestory.Keys)
                                        {
                                            if (IsEqualTwoString(strkey, userstory))
                                            {
                                                canadd = false;
                                                break;
                                            }
                                        }

                                        if (canadd)
                                        {
                                            if (strrest.Trim().Length > 100)
                                            {
                                                similarfileusestory.Add(userstory, strrest);

                                                strusecasebody += "title: " + userstory + "\r\n";
                                                strusecasebody += "integration testing tech stack: " + strrest + "\r\n";
                                            }
                                        }
                                    }
                                }
                                if (filename.Trim() != "")
                                {
                                    strusecasebody = "file name: " + filename + "\r\n" + strusecasebody;

                                    strusecaseresult = GetStringTrimStart(strusecasebody);

                                    if (!strusecaseresult.StartsWith("error"))
                                    {
                                        result["IntegrationTesting"] += strk + "\r\n" + strusecaseresult + "\r\n";
                                    }
                                    else
                                    {
                                        result["IntegrationTesting"] += strk + "\r\n" + strusecasebody + "\r\n";
                                    }
                                }
                            }

                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception in GenerateTestcases");
                finalSummary = $"Error: {ex.Message}";
            }
            return result;
        }

        public async Task<Dictionary<string, dynamic> >ReverseEngMetadataFromFiles(Dictionary<string, dynamic> result, FileNode folderStructure, string UploadChecklist, string UploadBestPractice, string EnterLanguageType)
        {
            try
            {

                // reverse eng and generate meta data
                string solutionDescription = result["SolutionDescription"].ToString();
                string solutionDescriptiontrace = solutionDescription;

                folderStructure = await TraverseFolderStructureImplementationDetails(folderStructure);
                TraverseFolderStructureforDescription(folderStructure, ref solutionDescription);

                result["SolutionDescription"] = solutionDescription;
                result["FolderStructure"] = folderStructure;

                string strtask = GetPromptTask(15, 1);
                string solutionOverview = await AnalyzeBRD(solutionDescription, strtask, 1);

                strtask = GetPromptTask(0, 6);
                string userflow = await AnalyzeBRD(solutionOverview, strtask, 0);


                strtask = GetPromptTask(1, 11);
                string archDiagram = await AnalyzeBRD(userflow, strtask, 1);


                if (!solutionOverview.StartsWith("Solution Overview:", StringComparison.OrdinalIgnoreCase))
                {
                    result["SolutionOverview"] = "solution overview:" + "\r\n" + solutionOverview + "\r\n" + "solution structure:" + "\r\n" + solutionDescription;
                }
                else
                {
                    result["SolutionOverview"] = solutionOverview + "\r\n" + "solution structure:" + "\r\n" + solutionDescription;
                }

                strtask = GetPromptTask(0, 5);
                string requirementTraceIDs = await AnalyzeBRD(result["SolutionOverview"], strtask, 1);


                if (requirementTraceIDs.ToLower().Trim().StartsWith("error") || string.IsNullOrEmpty(requirementTraceIDs.Trim()))
                {
                    throw new Exception("API response is null or empty");
                }
                result["requirementSummary"] = requirementTraceIDs;

                string[] arraySolDes = result["SolutionOverview"].Split("solution structure:", StringSplitOptions.RemoveEmptyEntries);

                if (arraySolDes.Length == 2)
                {
                    string taggedSolutionOverview = "solution structure:"  +"\r\n" + arraySolDes[1].Trim() + "\r\n" + "RequirementSummary:" + "\r\n" + requirementTraceIDs;

                    strtask = GetPromptTask(15, 4);

                    taggedSolutionOverview = await AnalyzeBRD(taggedSolutionOverview, strtask, 2);

                    if (arraySolDes[0].Contains("ArchitectureDiagram:"))
                    {
                        string[] arrayforArch = arraySolDes[0].Split(new string[] { "ArchitectureDiagram:", "Technology Stack:" }, StringSplitOptions.RemoveEmptyEntries);

                        if (arrayforArch.Length == 3)
                        {
                            taggedSolutionOverview = arrayforArch[0] + "\r\n" + archDiagram + "\r\n" + "Technology Stack:" + "\r\n" + arrayforArch[2] + "\r\n" + taggedSolutionOverview;
                        }
                    }

                    arraySolDes = taggedSolutionOverview.Split("solution structure:", StringSplitOptions.RemoveEmptyEntries);

                    if (arraySolDes.Length == 2)
                    {
                        result["SolutionOverview"] = taggedSolutionOverview;
                        result["SolutionDescription"] = arraySolDes[1];

                    }

                    folderStructure = await TraverseFolderStructureforDescriptionTracing(folderStructure, result["SolutionDescription"], 0, "");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception in ReverseEngMetadataFromFiles");

            }
            return result;
        }
        
        public async Task<string> CodeReviewForFilesWithOverallSummary(FileNode folderStructure)
        {
            try
            {

                Dictionary<string, string> dictCountwithPer = new Dictionary<string, string>();
                string overallSummary = string.Empty;
                string overallSummaryDetails = string.Empty;

                await Task.Run(() => TraverseFolderStructureforCodeReviewSummary(dictCountwithPer, folderStructure, ref overallSummary, ref overallSummaryDetails));

                int intPercntage = 0;
                int intLineCount = 0;
                int sumoflinewithper = 0;
                int sumLineCount = 0;
                int overallcomp = 0;
                foreach (string strk in dictCountwithPer.Keys)
                {
                    string countper = dictCountwithPer[strk].ToString();
                    string[] arraycountper = countper.Split(':', StringSplitOptions.RemoveEmptyEntries);

                    if (arraycountper.Length == 2)
                    {

                        intLineCount = int.Parse(arraycountper[0]);
                        intPercntage = int.Parse(arraycountper[1]);
                        sumoflinewithper += intLineCount * intPercntage;
                        sumLineCount += intLineCount;

                    }

                }

                if (sumLineCount > 0 && sumoflinewithper > 0)
                {
                    overallcomp = sumoflinewithper / sumLineCount;

                }

                overallSummary = "Overall % of code compliance: " + overallcomp.ToString() + "\r\n \r\n" +
                   "individual file code compliance:" + "\r\n \r\n" + overallSummary + "\r\n \r\n" +
                   "file wise detail code compliance:" + "\r\n \r\n" + overallSummaryDetails;

                 return overallSummary;
            }

            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception in FinalSummary");
               

            }
           return "";

        }

        public async Task<FileNode> TraverseFolderStructureImplementationDetails(FileNode node, int depth = 0)
        {

            if (node != null)
            {
                if (!string.IsNullOrEmpty(node.code))
                {
                    string strprompt = "\r\n" + node.code;

                    string strResponse = await AnalyzeBRD(strprompt, GetPromptTask(15), 2);

                    string[] arrayResults = strResponse.ToLower().Split(new string[] { "duplicateformatfound=yes", "duplicateformatfound=no" }, StringSplitOptions.RemoveEmptyEntries);
                    strResponse = string.Empty;
                    foreach (string strk in arrayResults)
                    {
                        if (strResponse.Length < strk.Trim().Length)
                        {
                            strResponse = strk.Trim();
                        }
                    }
                    //if (Regex.Matches(strResponse, Regex.Escape("Implementation Details:"), RegexOptions.IgnoreCase).Count > 1)
                    //{
                    //    strResponse = await AnalyzeBRD(strResponse, GetPromptTask(15, 3), 2);
                    //}

                    node.content = strResponse;

                }
                if (node.children != null)
                {
                    foreach (var child in node.children)
                    {
                        await TraverseFolderStructureImplementationDetails(child, depth + 1);
                    }
                }
            }
            else
            {
                node = new FileNode();
            }
            return node;
        }

        public string TraverseFolderStructureforDescription(FileNode node, ref string solutionDescription, int depth = 0)
        {

            if (node != null)
            {
                if (!string.IsNullOrEmpty(node.code))
                {
                    if (!string.IsNullOrEmpty(node.content))
                    {
                        string strsearch = "filename:" + node.name;

                        solutionDescription = UpdateSolutionDescription(solutionDescription, node.content, strsearch);
                    }
                }
                if (node.children != null)
                {
                    foreach (var child in node.children)
                    {
                        TraverseFolderStructureforDescription(child, ref solutionDescription, depth + 1);
                    }
                }
            }

            return solutionDescription;
        }

        public async Task<FileNode> TraverseFolderStructureforDescriptionTracing(FileNode node, string solutionDescription, int depth = 0,string nodepath="")
        {

            if (node != null)
            {
                if(node.type=="folder")
                {
                    nodepath = node.content;
                }
                
                if (!string.IsNullOrEmpty(node.code))
                {
                    if (!string.IsNullOrEmpty(node.content))
                    {
                        string strfilename = "filename:" + node.name;

                        string fileContent =  GetContentReqTracing(nodepath,strfilename, solutionDescription); 
                        node.content = fileContent;

                    }
                }
                if (node.children != null)
                {
                    foreach (var child in node.children)
                    {
                        await TraverseFolderStructureforDescriptionTracing(child, solutionDescription, depth + 1, nodepath);
                    }
                }
            }

            else
            {
                node = new FileNode();
            }
            return node;
        }
        public string GetContentReqTracing(string filepath, string filename, string solutionOverview)
        {
            string fileContent = string.Empty;
            try
            {
                if (!string.IsNullOrEmpty(filepath) && !string.IsNullOrEmpty(filename))
                {

                    string[] arrayProjectPaths = solutionOverview.ToLower().Split(new string[] { "project path:" }, StringSplitOptions.RemoveEmptyEntries);

                    filename = filename.ToLower().Replace("filename:", ""); 

                    foreach (string path in arrayProjectPaths)
                    {
                        
                        if(path.ToLower().Contains("file name"))
                        {
                            string[] arrayFileNames = path.ToLower().Split(new string[] { "file name:", "purpose:" }, StringSplitOptions.RemoveEmptyEntries);

                            if (arrayFileNames.Length > 2)
                            {
                                
                                if (filepath.ToLower().Trim()== Regex.Replace(arrayFileNames[0], @"req-\d+(\.\d+)*", "", RegexOptions.IgnoreCase).ToLower().Trim() && 
                                    filename.ToLower().Trim() == Regex.Replace(arrayFileNames[1], @"req-\d+(\.\d+)*", "", RegexOptions.IgnoreCase).ToLower().Trim())
                                {
                                    fileContent = "purpose:" + string.Join(Environment.NewLine, arrayFileNames.Skip(2).ToArray());
                                    break;
                                }
                            }

                        }
                    }
                    
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception in GetContentReqTracing");

            }
            return fileContent;
        }
        public async Task<FileNode> TraverseFolderStructureCodeReview(FileNode node, string strLanguageType, string strchecklist, string strbestpractice, int depth = 0)
        {

            if (node != null)
            {
                if (!string.IsNullOrEmpty(node.code) && !string.IsNullOrEmpty(node.name))
                {
                    string strprompt = "\r\n" + node.code;

                    string strResponse = await GetCodeReview(node.name,node.code, strLanguageType, strchecklist, strbestpractice);

                    node.codeReview = strResponse;

                }
                if (node.children != null)
                {
                    foreach (var child in node.children)
                    {
                        await TraverseFolderStructureCodeReview(child, strLanguageType, strchecklist, strbestpractice, depth + 1);
                    }
                }
            }
            else
            {
                node = new FileNode();
            }
            return node;
        }
        public string UpdateSolutionDescription(string solutionDescription, string strImplementationDetails, string strsearch)
        {
            string updatedSolutionDescription = string.Empty;

            try
            {
                string[] lines = solutionDescription.ToLower().Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);

                int i = 0;

                while (i < lines.Length)
                {
                    string line = lines[i].Trim();

                    if (line.Trim().Replace(" ", "").StartsWith(strsearch, StringComparison.OrdinalIgnoreCase))
                    {
                        updatedSolutionDescription += line + "\r\n" + strImplementationDetails + "\r\n";

                    }
                    else
                    {
                        updatedSolutionDescription += line + "\r\n";

                    }
                    i++;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception in UpdateSolutionDescription");

            }
            return updatedSolutionDescription;
        }

        public async Task<FileNode> CodeReview(FileNode node, Dictionary<string, dynamic> result, string UploadChecklist, string UploadBestPractice, string EnterLanguageType)
        {
            string strLanguageType = ".cs, c#, application code using c#" + "\r\n" +
                                     ".ts, typescript, application code using angular typescript";
            string strchecklist = ""; // GetChecklist();
            string strbestpractice = " follow microsoft code best practice ";

            if (UploadChecklist != null)
            {
                strchecklist = UploadChecklist;
            }
            if (UploadBestPractice != null)
            {
                strbestpractice = UploadBestPractice;
            }
            if (EnterLanguageType != null)
            {
                strLanguageType = EnterLanguageType;
            }

            try
                {
                    if (node != null)
                    {
                        node = await TraverseFolderStructureCodeReview(node, strLanguageType, strchecklist, strbestpractice);
                    }
                    else
                    {
                        node = new FileNode();
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Exception in CodeReview");

                }

            return node;
        }
        public string TraverseFolderStructureforCodeReviewSummary(Dictionary<string,string> dictCountwithPer, FileNode node, ref string overallSummary, ref string overallSummaryDetails, int depth = 0)
        {

            if (node != null)
            {
                if (!string.IsNullOrEmpty(node.code))
                {
                    if (!string.IsNullOrEmpty(node.codeReview))
                    {
                        string strfilename = "filename:" + node.name;

                        overallSummaryDetails += strfilename + "\r\n" + node.codeReview + "\r\n" ;

                        overallSummary += strfilename + "\r\n" ;

                        UpdateCodeReviewSummary(dictCountwithPer,ref overallSummary, ref overallSummaryDetails, node.codeReview, strfilename);

                    }
                }
                if (node.children != null)
                {
                    foreach (var child in node.children)
                    {
                        TraverseFolderStructureforCodeReviewSummary(dictCountwithPer,child, ref overallSummary,ref overallSummaryDetails , depth + 1);
                    }
                }
            }

            return overallSummary;
        }

        public void UpdateCodeReviewSummary(Dictionary<string, string> dictCountwithPer,ref string overallSummary, ref string overallSummaryDetails, string strfileCodeReview, string strfilename)
        {
            int intPercntage = 0;
            int intLineCount = 0;
            try
            {

                
                string[] lines = GetStringTrimStart(strfileCodeReview).ToLower().Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);

                int i = 0;

                while (i < lines.Length)
                {
                    string line = lines[i].Trim();

                    if (IsEqualTwoString(line, "best practice compliance score")) //line.Trim().Contains("Best practice compliance", StringComparison.OrdinalIgnoreCase))
                    {
                        overallSummary += line + "\r\n";

                        Match match = Regex.Match(line, @"\b\d+");
                        if(match.Success)
                        {
                            intPercntage = int.Parse(match.Value);
                        }

                    }
                    else if (IsEqualTwoString(line, "total number of line of code")) //if (line.Trim().Contains("Total number of line", StringComparison.OrdinalIgnoreCase))
                    {
                        overallSummary += line + "\r\n";

                        Match match = Regex.Match(line, @"\b\d+");
                        if (match.Success)
                        {
                            intLineCount = int.Parse(match.Value);
                        }

                    }
                    if(intPercntage > 0 && intLineCount >0)
                    {
                        break;
                    }
                    i++;
                }

                if (!string.IsNullOrEmpty(strfilename))
                {
                    if (!dictCountwithPer.ContainsKey(strfilename))
                    {
                        string strinfo = intLineCount.ToString() + ":" + intPercntage.ToString();

                        dictCountwithPer.Add(strfilename, strinfo);
                        
                    }
                }

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception in UpdateCodeReviewSummary");

            }
           
        }

        

        public async Task<string> GetCodeReview(string fileName, string fileCode, string strLanguageType, string strchecklist, string strbestpractice)
        {

            string linesCodeWithIleNum = string.Empty;
            string codeReviewSummary = string.Empty;
            try
            {

                if (!string.IsNullOrEmpty(strchecklist) && !string.IsNullOrEmpty(strbestpractice)
                    && !string.IsNullOrEmpty(fileCode) && !string.IsNullOrEmpty(fileName) && !string.IsNullOrEmpty(strLanguageType))
                {

                    string[] linesLanguageType = strLanguageType.ToLower().Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);

                    int i = 0;

                    string prompt = GetPromptTask(16, 1);
                    string strtask = GetPromptTask(16, 0);

                    while (i < linesLanguageType.Length)
                    {
                        string lineLang = linesLanguageType[i].Trim();

                        string[] arrayLanguageType = lineLang.ToLower().Split(new[] { "," }, StringSplitOptions.None);

                        string strFileExt = Path.GetExtension(fileName);

                        if (arrayLanguageType.Length > 0)
                        {
                            if (string.Equals(strFileExt.ToLower(), arrayLanguageType[0].ToLower()))
                            {
                                prompt = prompt.Replace("{{fileinfo}}", string.Join(Environment.NewLine, arrayLanguageType.Skip(1).ToArray()));
                                break;
                            }
                        }

                        i++;
                    }

                    string[] linesCode = fileCode.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
                    i = 1;
                    while (i < linesCode.Length)
                    {
                        linesCodeWithIleNum += "line " + i.ToString() + " " + linesCode[i] + "\r\n";

                        i++;
                    }

                    prompt = prompt.Replace("{{checklist}}", strchecklist);
                    prompt = prompt.Replace("{{bestpractice}}", strbestpractice);
                    prompt = prompt.Replace("{{code}}", linesCodeWithIleNum);

                    codeReviewSummary = await AnalyzeBRD(prompt, strtask, 3);


                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception in GetCodeReview");

            }
            return codeReviewSummary;
        }
        public async Task<Dictionary<string, string>> TraverseFolderStructureforTraceabilityMatrixBase(FileNode node, string fieldType)
        {
            Dictionary<string, string> dictTrace = new Dictionary<string, string>();

            if (node.children !=null)
            {
                string strResult = string.Empty;
                
                string pattern = @"req-\d+(\.\d+)*";//@"Req-\d+";
                foreach (FileNode child in node.children)
                {
                    if (child.content.ToLower().Trim() == "root folder")
                    {
                        foreach (FileNode childroot in child.children)
                        {
                            // code file
                            if (childroot.name.ToLower().Trim() != "unittest" &&
                                childroot.name.ToLower().Trim() != "functionaltest" &&
                                childroot.name.ToLower().Trim() != "integrationtest" &&
                                childroot.name.ToLower().Trim() != "documentation" &&
                                childroot.name.ToLower().Trim() != "code review summary"
                                )
                            {

                                await TraverseFolderStructureforTraceabilityMatrix(dictTrace, childroot, "code", pattern, fieldType);
                            }
                            //testing
                            else if (childroot.name.ToLower().Trim() == "unittest" ||
                                childroot.name.ToLower().Trim() == "functionaltest" ||
                                childroot.name.ToLower().Trim() == "integrationtest"
                                )
                            {

                                await TraverseFolderStructureforTraceabilityMatrix(dictTrace, childroot, childroot.name.ToLower().Trim(), pattern, fieldType);
                            }
                            //documenttion
                            else if (childroot.name.ToLower().Trim() == "documentation")
                            {

                                foreach (FileNode childdocs in childroot.children)
                                {
                                    if (childdocs.name.ToLower().Trim() == "hld" ||
                                childdocs.name.ToLower().Trim() == "lld" ||
                                childdocs.name.ToLower().Trim() == "user manual"
                                )
                                    {

                                        await TraverseFolderStructureforTraceabilityMatrix(dictTrace, childdocs, childdocs.name.ToLower().Trim(), pattern, fieldType);
                                    }
                                }
                            }

                        }
                    }
                }
            }
            return dictTrace;
            
        }
        public async Task TraverseFolderStructureforTraceabilityMatrix(Dictionary<string, string> dictTrace,FileNode node, string searchType, string pattern,string fieldType, int depth = 0)
        {
            string strResult = string.Empty;

            if (node != null)
            {
                if (searchType == "code")
                {
                    if (!string.IsNullOrEmpty(node.code) )
                    {
                        if (node.type == "file")
                        {
                            strResult = "filename:" + node.name.Trim() + "="; //+"tagged" + searchType + ":" + "\r\n";

                            MatchCollection matches=null;

                            if (fieldType.Trim().ToLower() == "green")
                            {
                                matches = Regex.Matches(node.code, pattern, RegexOptions.IgnoreCase);
                            }
                            else if (fieldType.Trim().ToLower() == "brown")
                            {
                                matches = Regex.Matches(node.content, pattern, RegexOptions.IgnoreCase);
                            }
                            if (matches != null)
                            {
                                foreach (Match match in matches)
                                {
                                    if (!strResult.Contains(match.Value))
                                    {
                                        strResult += match.Value + ",";
                                    }
                                }

                                strResult += "\r\n";

                                if (dictTrace.ContainsKey("code"))
                                {
                                    dictTrace["code"] += strResult;
                                }
                                else
                                {
                                    dictTrace.Add("code", strResult);
                                }
                            }
                        }
                    }
                }
                else if (searchType == "unittest" || searchType == "functionaltest" || searchType == "integrationtest")
                {
                    if (!string.IsNullOrEmpty(node.content) )
                    {
                        if (node.type == "file")
                        {
                            string filename = Regex.Replace(node.name.Trim(), pattern, "", RegexOptions.IgnoreCase).ToLower().Trim();

                            strResult = "filename:" + filename + "=";// + "tagged" + searchType + ":" + "\r\n";

                            MatchCollection matches = Regex.Matches(node.content, pattern, RegexOptions.IgnoreCase);

                            foreach (Match match in matches)
                            {
                                if (!strResult.Contains(match.Value))
                                {
                                    strResult += match.Value + ",";
                                }
                            }
                            strResult += "\r\n";

                            if (dictTrace.ContainsKey(searchType))
                            {
                                dictTrace[searchType] += strResult;
                            }
                            else
                            {
                                dictTrace.Add(searchType, strResult);
                            }
                        }
                    }
                }
                else if (searchType == "hld" || searchType == "lld" || searchType == "user manual")
                {
                    if (!string.IsNullOrEmpty(node.description))
                    {
                        if (node.type == "file")
                        {
                            string strLbl = string.Empty;

                            if(searchType == "hld")
                            {
                                strLbl = "hld-";
                            }
                            else if (searchType == "lld")
                            {
                                strLbl = "lld-";
                            }
                            else if (searchType == "user manual")
                            {
                                strLbl = "um-";
                            }
                            Dictionary<string, List<string>> mapping = GetMappingSectionsWithReq(node.description.ToLower(), strLbl); 

                            //string strDocResult = await AnalyzeBRD(node.description, GetPromptTask(17, 0, strLbl), 3) + "\r\n";

                            string filename = Regex.Replace(node.name.Trim(), pattern, "", RegexOptions.IgnoreCase).ToLower().Trim();

                            strResult = "filename:" + filename + "=";// + "tagged" + searchType + ":" + "\r\n";

                            string strDocMappResult = "";

                            foreach (string strk in mapping.Keys)
                            {
                                strDocMappResult += strk + " = " + string.Join(',',mapping[strk])+"\r\n"; 
                            }

                            if (dictTrace.ContainsKey(searchType))
                            {
                                dictTrace[searchType] += strDocMappResult;//strDocResult;
                            }
                            else
                            {
                                dictTrace.Add(searchType, strDocMappResult); //strDocResult);
                            }
                        }
                    }
                }

                if (node.children != null)
                {
                    foreach (var child in node.children)
                    {
                       await TraverseFolderStructureforTraceabilityMatrix(dictTrace,child, searchType, pattern, fieldType, depth + 1);
                    }
                }
            }

            
        }

        public Dictionary<string, List<string>> GetMappingSectionsWithReq(string text,string strlbl)
        {
            // Dictionary to store reqId => list of sections
            Dictionary<string, List<string>> mapping = new Dictionary<string, List<string>>();

            // Regex to match sections (like "1. Introduction")
            Regex sectionRegex = new Regex(@"^(\d+)\.\s.*$", RegexOptions.Multiline);

            // Regex to match requirement IDs (req-<number> or req-<number.number>)
            Regex reqRegex = new Regex(@"req-\d+(\.\d+)*");

            MatchCollection sectionMatches = sectionRegex.Matches(text);

            for (int i = 0; i < sectionMatches.Count; i++)
            {
                string sectionNumber = sectionMatches[i].Groups[1].Value;
                string umCode = $"{strlbl}{sectionNumber}";

                // Find section text until the next section or end of text
                int sectionStart = sectionMatches[i].Index;
                int sectionEnd = (i + 1 < sectionMatches.Count)
                    ? sectionMatches[i + 1].Index
                    : text.Length;

                string sectionText = text.Substring(sectionStart, sectionEnd - sectionStart);

                // Find all requirement IDs in the section
                MatchCollection reqMatches = reqRegex.Matches(sectionText);
                foreach (Match req in reqMatches)
                {
                    string reqId = req.Value;

                    if (!mapping.ContainsKey(reqId))
                    {
                        mapping[reqId] = new List<string>();
                    }

                    if (!mapping[reqId].Contains(umCode))
                    {
                        mapping[reqId].Add(umCode);
                    }
                }
            }

            // Print results
            foreach (var kvp in mapping)
            {
                Console.WriteLine($"{kvp.Key} = {string.Join(", ", kvp.Value)}");
            }
            return mapping;
        }

        public string GetGreenBrownField(string solutionOverview)
        {
            string greenbrown = string.Empty;
            string[] linesSol = solutionOverview.Split(new string[] { "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);            
            bool canNext = false;
            foreach (string str in linesSol)
            {
                if (IsEqualTwoString(str.Trim(), "greenfield/brownfield:") || (canNext))
                {
                    greenbrown = str.Replace("greenfield/brownfield:", "").Trim().ToLower();

                    if (!string.IsNullOrEmpty(greenbrown))
                    {
                        break;
                    }

                    canNext = !canNext;

                }
            }

            return greenbrown;
        }

        public string GetPromptTask(int opt, int prep = 0, string strSection = "", string SolutionName = "")
        {
            string PromptTask = string.Empty;
            switch (opt)
            {
                case 0:
                    if (prep == 0) // udo: suggession
                    {
                        PromptTask = "derive, optimize then structure the text with as in the format " + "\r\n" +
                            "important instruction:\r\n " + "\r\n" +
                            "do not omit, summerize or rephrase in a way that loses any details." + "\r\n" +
                            "do not merge, delete or skip any points, steps or sub-points." + "\r\n" +
                            "output the rewritten version only. do not include any explanation." + "\r\n" +
                            "if any section is missing or blank, generate out of the text." + "\r\n" +
                            "- do not inlcude format labels in the values\r\n" +
                            " provide your response without any extra symbols , do not use any markdown formatting and tab indented" + "\r\n" +
                            "Format:" + "\r\n" +
                            "- Business Objectives" + "\r\n" +
                            "- structure Business Requirements using " + "\r\n" +
                            "module, sub module, " + "\r\n" +
                            "section, sub section, " + "\r\n" +
                            "feature, functionality, " + "\r\n" +
                            "menu, sub menu," + "\r\n" +
                            "UI screen if any with its navigation, element details, " + "\r\n" +
                            "data required, validation, " + "\r\n" +
                            "rule, service," + "\r\n" +
                            "user flow, data flow, " + "\r\n" +
                            "technical points " + "\r\n" +
                            "- Functional Requirements" + "\r\n" +
                            "- Technical Requirements " + "\r\n" +
                            "- Data Requirements" + "\r\n" +
                            "- Scope of Work" + "\r\n" +
                            "- Assumptions and Constraints" + "\r\n" +
                            "- Non-Functional Requirements" + "\r\n" +
                            "- Security Access Control" + "\r\n" +
                            "- Project Deliverables" + "\r\n" +
                            "- Milestones" + "\r\n" +
                            "- Success Criteria" + "\r\n" +
                            "- Other Key Points";

                    }
                    else if (prep == 1) // Retrieve required section content
                    {
                        if (strSection == "Other Key Points")
                        {
                            PromptTask = "refer the text,derive insights only for Other Key Points " + "\r\n" +
                                 "exclude bellow details" + "\r\n" +
                                 "- Business Objectives" + "\r\n" +
                                 "- Business Requirements" + "\r\n" +
                                "- Functional Requirements" + "\r\n" +
                                "- Technical Requirements " + "\r\n" +
                                "- Data Requirements" + "\r\n" +
                                "- Scope of Work" + "\r\n" +
                                "- Assumptions and Constraints" + "\r\n" +
                                "- Non-Functional Requirements" + "\r\n" +
                                "- Security Access Control" + "\r\n" +
                                "- Project Deliverables" + "\r\n" +
                                "- Milestones" + "\r\n" +
                                "- Success Criteria" + "\r\n" +
                                "Provide response without loosing any Detail and without any extra symbols, do not use any markdown formatting and tab indented";
                        }
                        else
                        {
                            PromptTask = "refer the text,extracts details only for section: " + strSection + "\r\n" +
                                "do not include label " + strSection + "\r\n" +
                                "do not omit or rephrase or derive or change in a way that loses any details." + "\r\n" +
                                "inlcude each and every point given in the text with refinement but without changing its meaning" +"\r\n" +
                                "Provide response without loosing any Detail and without any extra symbols, do not use any markdown formatting and tab indented";
                        }
                    }

                    else if (prep == 2) //Insight Elicitation - format json to generate YAML solution structure
                    {

                        PromptTask = " Refer the below text and reorganize as per the below format:" + "\r\n" +
                        //"{\r\n  \"SolutionName\": \"\",\r\n  \"RootFolder\": \"\",\r\n  \"Project\": [\r\n    {\r\n      \"ProjectName\": \"\",\r\n      \"Paths\": [\r\n        {\r\n          \"ProjectPath\": \"\",\r\n          \"Files\": [\r\n            {\r\n              \"FileName\": \"\",\r\n              \"Purpose\": \"\",\r\n              \"ImplementationDetails\": \"\",\r\n              \"DependentDetails\": {\r\n                \"Modules\": [\"\", \"\"],\r\n                \"Services\": [\"\", \"\"]\r\n              }\r\n            }\r\n          ]\r\n        }\r\n      ]\r\n    }\r\n  ]\r\n}" + "\r\n" +
                        "important instruction:" + "\r\n" +
                        "- do not skip any layers, projects, folders, files with nested details per level" + "\r\n" +
                        "- RootFolder always be the SolutionName and a single word without any space" + "\r\n" +
                        "- Project Path always be ProjectName + relative Path " + "\r\n" +
                        "- while generating ImplementationDetails keep logic with the mapping with Req-<sequence number> from input text given" + "\r\n" +
                        "- do not ask me to complete or continue rest of the components and files in similary pattern, rather you provide all without skip,omit  " + "\r\n" +
                        "- Provide response in the below format and without any extra symbols,do not use any markdown formatting and tab indented." + "\r\n" +
                        "Format:" + "\r\n" +
                        "SolutionName:\r\nRootFolder:\r\nProject:\r\n  - ProjectName:\r\n    Paths:\r\n      - ProjectPath:\r\n        Files:\r\n          - FileName:\r\n            Purpose:\r\n            ImplementationDetails:\r\n            DependentDetails:\r\n              Modules:\r\n              Services:\r\n";


                    }
                    else if (prep == 3) //Insight Elicitation - development ready format to generate YAML
                    {
                        PromptTask = "You are a senior solution architect. refer the text and generate complete full-stack development ready solution structure, " + "\r\n" +
                        "include each requirement given in Req-<sequence number> between taggedrequirements:start and taggedrequirements:end" + "\r\n" +
                        "- solution structure should be layered as per Solution Layer with a clear separation of concerns " + "\r\n" +
                        "- do not ask me to complete or Continue or Repeat the pattern similary rest of the components and files, rather you provide all without skip,omit " + "\r\n" +
                        "- if each Req-<sequence number> has UIneeded = yes then must include all necessary UI components and files such as UI structure html, styling, UI behavior, responsive, " + "\r\n" +
                        "code behind logic, view model, routing(if needed), configuration (if needed),other files as per UI layer implementation logic and technology stack in the respective layer." + "\r\n" +
                        "- if the solution is a backend or microservice or API or serverles system with no direct UI, omit UI section and focus on " + "\r\n" +
                        "(Service interfaces,Data Contarcts,Service orchestration, event/message handling, security,error handling and logging) only if applicable " + "\r\n" +
                        "output should be only solution structure using the below format:" + "\r\n" +
                        "format:" + "\r\n" +
                        "Solution Name: Name\r\nLayer Name: Name\r\nmodule or submodule name: name\r\nFiles:\r\n- File name with extension\r\n\t- ImplementationDetails:\r\n\t \t- Implementation logic in pseudocode,Req-< sequence number > \r\n\t\t- modules\r\n\t\t- methods\r\n\t\t- services\r\n\t- dependent details:\r\n\t\t- external methods\r\n\t\t- external services";

                    }

                    else if (prep == 4) // Optimize the text
                    {
                        PromptTask = "refer the text, rewrite the text to improve clarity and readability." + "\r\n" +
                            "important instruction:" + "\r\n" +
                            "do not omit, summerize or rephrase in a way that loses any details" + "\r\n" +
                            "only improve grammer, phrasing and flow" + "\r\n" +
                            "do not merge, delete or skip any points, steps or sub-points" + "\r\n" +
                            "output the rewritten version only. do not include any explanation" + "\r\n" +
                            ". Provide response without loosing any Detail and without any extra symbols, do not use any markdown formatting and tab indented";
                    }
                    else if (prep == 5) // listing out Requirements and tagging with unique IDs like Req-<sequence number> 
                    {
                        PromptTask = "You are a requirements analyst. extract requirements as Req-<sequence number> with the format below from the text that has business, technical requirements and Modules and Sub Modules hierachy." + "\r\n" +
                        "Format:" + "\r\n" +
                        "Req-<sequence number>" + "\r\n" +
                        "Title: title " + "\r\n" +
                        "Description: description" + "\r\n" +
                        "Requirement Detail:<include pointwise detailed requirments>" + "\r\n" +
                        "Data Requirement:<Data entity(data attributes, data types, constraints)>" + "\r\n" +
                        "Entity Relationship:<include er relationships with other data entities if any>" + "\r\n" +
                        "Implementation detail:<include detailed implementation logic for all applicable layers for this requirement, include data entity crud processing logic>" +
                        "Depedent Details: <interdepedent on other modules,services>" +"\r\n"+
                        "Type: (business requirement,technical requirement,functional/non-functional requirement)" + "\r\n" +
                        "Layers: <which architecture layers are impacted>" + "\r\n" +
                        "FilestoCreate: <what specific files or components to be created in each impacted layer>" + "\r\n" +
                        "UIneeded:<UI needed for interaction>" +"\r\n"+
                        "Source: <input text section with section number if any>" + "\r\n" +
                        "Technology Stack: <what is the technology stack used for each layer to create these files or component, provide like (layer, technology stack)> \r\n" +
                        "important instruction:" + "\r\n" +
                        "- you must refer and follow the Modules and Sub Modules hierachy if given in the text and organize requirements accordingly" + "\r\n" +
                        "- requirements must include Req-<sequence number> matching with the Modules and Sub Modules hierachy" + "\r\n" +
                        "- align each requirement a unique ID like Req-<sequence number> as per Modules, Sub Modules and nested sub modules given in the Modules and Sub Modules hierachy" + "\r\n" +
                        "- requirement is not a duplicate or rephrasing of an earler requirement" + "\r\n" +
                        "- return the output in given format: " + "\r\n" +
                        //"- Requirement Detail should include concise details of business, functional,non-functional, technical, data requirement in Requirement Detail" +
                        "- do not skip, omit, merge any module or sub module,features under each sub module while identify requirements\r\n" +
                        "- include nested sub modules while identify requirements \r\n" +
                        "- keep each module, sub module,nested sub modules, functional,non-functional requirements as a separate requirement \r\n" +
                        "- do not ask me to complete or continue rest of the components and files in similary pattern, rather you provide all without skip,omit" + "\r\n" +
                        "- Provide response without loosing any Detail and without any extra symbols, do not use any markdown formatting and tab indented";
                    }
                    else if (prep == 6) // reorganize as per the module and sub module Hierarchy
                    {
                        PromptTask = "You are a senior solution architect.Analyze the Text: \r\n" +
                        "important instruction: \r\n" +
                        "- extract Solution Layers, Data flow, Best Practice from the text" + "\r\n" +
                        "- how data flow happens for a user/service between these layers'" + "\r\n" +
                        "- output in response would be userdataflow as example bellow" + "\r\n" +
                        "- refer Data Requirements to create database objects in the database is in scope then set dbobject:yes else no " + "\r\n" +
                        "example:" + "\r\n" +
                        "userdataflow:\r\nLayer Name: \r\n\tpurpose (include all files/components that will be part of this layer)\r\nData Flow:\r\nBest Practice:\r\ndbobject:<is database object creation in scope?>" + "\r\n" +
                        "Provide response without any extra symbols,do not use any markdown formatting and tab indented";
                    }
                    else if (prep == 7) // 
                    {
                        PromptTask = "You are a senior solution architect. refer the text and generate complete full-stack development ready project structure, " + "\r\n" +
                        "- first read the requirement given in Req-<sequence number> between taggedrequirements:start and taggedrequirements:end" + "\r\n" +
                        "- second refer userdataflow for Layer Name, always take project name as layer name from userdataflow" + "\r\n" +
                        "_ while generating project structure generate for Title: only and also refer solution guidance:" + "\r\n" +
                        "- while generating project structure value of  ProjectName=layer name,  ModuleSubModuleName= module or sub module name, type= same as the type in the Req-<sequence number>," +
                        " file name = file name with extension (ensure unique name referring 'instruction to avoid duplicate' for previously generated structure metadata)" + "\r\n" +
                        "- if there is a value of ParentModule: then make same value for ParentModule: in other Layer Name for each Req-<sequence number>" + "\r\n" +
                        "- include fields for associcated data entity and entity relationship with other entity to generate Implementation Details" + "\r\n" +
                        "- do not ask me to complete or Continue or Repeat the pattern similary rest of the components and files, rather you provide all without skip,omit " + "\r\n" +
                        "- if each Req-<sequence number> has UIneeded = yes then must include all necessary UI components and files such as UI structure html, styling, UI behavior, responsive, " + "\r\n" +
                        "code behind logic, view model, routing(if needed), configuration (if needed),other files as per UI layer implementation logic and technology stack in the respective layer." + "\r\n" +
                        "- if the solution is a backend or microservice or API or serverles system with no direct UI, omit UI section and focus on " + "\r\n" +
                        "(Service interfaces,Data Contarcts,Service orchestration, event/message handling, security,error handling and logging) only if applicable " + "\r\n" +
                        "- refer section 'instruction to avoid duplicate' given in the text to avoid creating duplicate ModuleSubModuleName" + "\r\n" +
                        "- exclude testing, database, deployment projects files " + "\r\n" +
                        "provide response without any extra symbols,do not use any markdown formatting and tab indented" + "\r\n" +
                        "output should be only project structure using the below format:" + "\r\n" +
                        "format:" + "\r\n" +
                        "ProjectName: <Layer Name>\r\n  ModuleSubModuleName: <module or sub module name>\r\n     ParentModule:<path of ModuleSubModule from root as per hierarchy (use '/' as separator) put blank if no parent >\r\n     " +
                        "Files:\r\n       FileName: <file name with extension>\r\n\t Type:<type of requirement>\r\n         Purpose: (<purpose>,<refer to previously created ModuleSubModuleName as applicable>,Req-<sequence number>)\r\n         " +
                        "ImplementationDetails: <Implementation Details in pseudocode, Req-<sequence number>>\r\n\t   Methods: <method name>\r\n         DependentDetails:\r\n           Modules: <external module name used>\r\n           Methods: <external method used>\r\n           Services: <external service used>\r\n           DataEntity: <must include Entity associated and its Entity Relationship with other entity>\r\n";
                        
                    }
                    else if (prep == 8) // findout solution  name 
                    {
                        PromptTask = "You are a senior software architect. Analyze the bellow text." + "\r\n" +
                            "output will be in the format: \r\nsolution name:<suggest the solution name>" + "\r\n" +
                            "Provide response without any extra symbols,do not use any markdown formatting and tab indented,each in a separated line.";
                    }
                    else if (prep == 9) // summerise and concise to avoid duplicate
                    {
                        PromptTask = "You are a senior software architect. Analyze the bellow text." + "\r\n" +
                            "summerize the text to feed as a feedback to LLM for every subsiquent time to avoid duplicate" + "\r\n" +
                            "summerization should be concise without loosing key points in the bellow output format" + "\r\n" +
                            "ModuleSubModuleName:<ModuleSubModuleName>\r\nFileName:<FileName>,\r\n< summerized contents of this file>" + "r\n" +
                            //"ModuleSubModuleName:<ModuleSubModuleName>,\r\nFileName:<FileName>,\r\nPurpose:<Purpose>,\r\nImplementationDetails:<ImplementationDetails>,\r\nDependentDetails:<DependentDetails>\r\n" + "\r\n" +
                            "Provide response without any extra symbols,do not use any markdown formatting and tab indented,each in a separated line.";
                    }
                    else if (prep == 10) // module and sub module
                    {
                        PromptTask = "Analyze the bellow text. extract module and submodule name" + "\r\n" +
                            "output will be in the format: \r\n ModSubName:Name " + "\r\n" +
                            "ignore module, modules, submodules, submodule, sub module,sub modules like word as name" + "\r\n" +
                            "Provide response without any extra symbols,do not use any markdown formatting and tab indented,each in a separated line.";
                    }
                    else if (prep == 11) // module and sub module
                    {
                        PromptTask = "Analyze the bellow text.derive requirement points given in the text only for moduleandsubmodule:" + strSection + "\r\n" +
                            "Requirements must include actor/user role wise all the requirements (fucntional and non-functional) for each solution layers, what and how data flows from one layer to another for only " + strSection  + "\r\n"+
                            //"also include solution layers would be part of this moduleandsubmodule implementation " + "\r\n" +
                            //"if the layer has user interface form to interact with for input then outline the form design aspect with all form fields, associated data entities, validations,entity relationship entity" + "\r\n" +
                            "outline the output in a bulleted numbered list with format: (Requirements:<pointwise details requirement given>,(EntityAssociated:,EntityDetails:,Entity Relationship with other entities:,Solution Layers:,Functionalities:,Output:)) " + "\r\n" +
                            "Provide response without any extra symbols,do not use any markdown formatting and tab indented,each in a separated line.";
                    }
                    else if (prep == 12) // module and sub module
                    {
                        PromptTask = "You are a senior software architect. Analyze the bellow text. the text has requirement for moduleandsubmodule:(" + strSection +") for (" + SolutionName + ")\r\n" +
                            "userdataflow: has each solution layer name and details" + "\r\n" +
                            "redesign the solution with implementation detail, data/entities involved, validations for the layer of this moduleandsubmodule, interaction and data flow between layers outlining (what data receive with fileds details, the logic how it processe, what data share to next layer with field details )  " + "\r\n" +
                            "if the layer has user interface form to interact then must include form design details with all form fields, associated data entities, validations,entity relationship entity"+"\r\n"+
                            "output would be redisgn of the moduleandsubmodule layer wise implementation detail" + "\r\n" +
                            "Provide response without any extra symbols,do not use any markdown formatting and tab indented,each in a separated line.";
                    }
                    else if (prep == 13) // listing out Requirements and tagging with unique IDs like Req-<sequence number> 
                    {
                        PromptTask = "You are a requirements analyst. rearrange the requirement without changing with the format below " + "\r\n" +
                        "Format:" + "\r\n" +
                        //"Req-<sequence number>" + "\r\n" +
                        "Title: title " + "\r\n" +
                        "Description: description" + "\r\n" +
                        "Requirement Detail:<summerize, concise the Requirements>" + "\r\n" +
                        "Data Requirement:<Data entity(data attributes, data types, constraints)>" + "\r\n" +
                        "Entity Relationship:<include er relationships with other data entities if any>" + "\r\n" +
                        "Implementation detail:<leave it as blank>" +
                        "Depedent Details: <interdepedent on other modules,services>" + "\r\n" +
                        "Type: functional/non-functional requirement" + "\r\n" +
                        "Layers: <which architecture layers are impacted>" + "\r\n" +
                        "FilestoCreate: <what specific files or components to be created in each impacted layer>,< files or components also include what methods,services,data fields used >" + "\r\n" +
                        "UIneeded:<UI needed for interaction(yes/no)>" + "\r\n" +
                        "Source: <input text section with section number if any>" + "\r\n" +
                        "Technology Stack: <what is the technology stack used for each layer to create these files or component, provide like (layer, technology stack)> \r\n" +
                        "important instruction:" + "\r\n" +
                        "- Type = functional requirement" + "\r\n" +
                        "- do not ask me to complete or continue rest of the components and files in similary pattern, rather you provide all without skip,omit" + "\r\n" +
                        "- Provide response without loosing any Detail and without any extra symbols, do not use any markdown formatting and tab indented";
                    }
                    else if (prep == 14) // dbobject detail
                    {
                        PromptTask = "You are a senior software architect. Analyze the bellow text, " + "\r\n" +
                            "extract details realted to only for database objects.focus on what type of data needs to be stored, " +
                            "realtionships between entities, operations (CRUD, reporting), constraints, data validations. " +
                            "do not list testing related requirements, only the requirements that will drive the database design " + "\r\n" +
                            "output format: " + "\r\n" +
                            "requirement:<list of requirements>" +"\r\n" +
                            "entityname:," + "\r\n" +
                            "entity relationship:," + "\r\n" +
                            "entityfields:(field name, data type,constraints,fk)," + "\r\n" +
                            "list of operations:<describe each crud operation with required fields involved for implementation> " + "\r\n" +
                            "Provide response without any extra symbols,do not use any markdown formatting and tab indented,each in a separated line.";


                        //PromptTask = "Given the following requirements, generate database objects for the specified database type." + "\r\n" +
                        //    "If SQL:" + "\r\n" +
                        //    "1.Create tables with appropriate data types, primary keys, and foreign keys." + "\r\n" +
                        //    "2.Generate views if useful for summarizing or reporting." + "\r\n" +
                        //    "3.Generate stored procedures for CRUD operations." + "\r\n" +
                        //    "4.Generate functions if derived / computed values are needed." + "\r\n" +
                        //    "If NoSQL:" + "\r\n" +
                        //    "1.Create collections with appropriate schema(documents, fields, data types)." + "\r\n" +
                        //    "2.Define indexes for query optimization." + "\r\n" +
                        //    "3.Suggest aggregations or map - reduce pipelines for reporting." + "\r\n" +
                        //    "4.Provide sample queries for CRUD operations." + "\r\n" +
                        //"Provide response without any extra symbols,do not use any markdown formatting and tab indented,each in a separated line.";
                    }
                    else if (prep == 15) // listing out db Requirements and tagging with unique IDs like Req-<sequence number> 
                    {
                        PromptTask = "You are a requirements analyst. Analyze the text and extracts possible database objects in the below format" + "\r\n" +
                        "Format:" + "\r\n" +
                        "Req-<sequence number>" + "\r\n" +
                        "entityname: <entity name> " + "\r\n" +
                        "entityfields:<entity field name, data type, constraints, validations>" + "\r\n" +
                        "purpose: <purpose>" + "\r\n" +
                        "operationslist:<describe each crud operation with required fields involved for implementation>" + "\r\n" +
                        "entityrelationship:<include entity relationship with other data entities if any with related fields>" + "\r\n" +
                        "Type: database requirement" + "\r\n" +
                        "important instruction:" + "\r\n" +
                        "- Type = database requirement" + "\r\n" +
                        "- Req<sequence number> start after " + strSection + "\r\n" +
                        "- combine similar entities into one and entity name should be distinct in the output response" + "\r\n"+
                        "- do not ask me to complete or continue rest of the components and files in similary pattern, rather you provide all without skip,omit" + "\r\n" +
                        "- Provide response without loosing any Detail and without any extra symbols, do not use any markdown formatting and tab indented";
                    }
                    break;
                case 1: //Solidification


                    if (prep == 1) // extract and optimize business, functional , technical other details
                    {
                        PromptTask = "You are a senior solution architect.Analyze the Text: given below, then extract details using bellow format\r\n" +
                       "important instruction:\r\n" +
                       "- extract referring text given in requirements:start and requirements:end" + "\r\n" +
                       "- infer and derive details if not given in the text directly" + "\r\n" +
                       "- include all module, sub module and nested sub modules recursively in the ModuleSubModule Hierarchy in bulleted numbered list" + "\r\n" +
                       "- derive all possible Solution Layers from requirements:start and requirements:end, derive value of greenfield/brownfield from requirements" + "\r\n" +
                       "- do not inlcude format labels in the values\r\n" +
                       //"- output will be one set up Solution Overview details" + "\r\n" +
                       "Provide response without any extra symbols,do not use any markdown formatting and tab indented"+ "\r\n" +
                       "Format:" + "\r\n" +
                       "(Solution Overview: <outline steps in bulleted point from user/actor prospect with operations to do,processing logic, data entities with data elements,ER with other entities,business rules, validations, actions all that required for a solution>\r\n" +  //solution overview (each point in separate line)
                       "Application Name:<Application Name>\r\n" +
                       "Description:<Description>\r\n" +
                       "Architecture:<Architecture>\r\n" +
                       "ArchitectureDiagram:" + "\r\n" +
                       "Technology Stack:<Technology Stack>\r\n" +
                       "Technical Requirements:<Technical Requirements>\r\n" +
                       "Technical Instructions:<Technical Instructions>" + "\r\n" +
                       "Solution Layers:<Solution Layers>\r\n" +
                       "Security and Compliance:<Security and Compliance>\r\n" +
                       "Scalability and Performance:<Scalability and Performance>\r\n" +
                       "Assumption and Constraints:<Assumption and Constraints>" + "\r\n" +
                       "Unit Testing:<Unit Testing tech stack>\r\n" +
                       "Functional Testing:<Functional Testing>\r\n" +
                       "Integration Testing:<Integration Testing>" + "\r\n" +
                       "ModuleSubModule Hierarchy:<module and sub module hierarchy>" + "\r\n" +
                       "greenfield/brownfield:<green/brown>" +
                       "Data Flow:(refering solution layers contextual flow:,logical flow:,physical flow:)" + "\r\n" +
                       "Common Functionalities:<Common Functionalities>)";
                    }


                    else if (prep == 2) // extract requirements
                    {
                        PromptTask = "You are a senior solution architect.Analyze the Text: given below\r\n" +
                            "- tag wherever applicable in the text with Req-<sequence number> referring requirements from taggedrequirements:start and taggedrequirements:end " + "\r\n" +
                            "- output response would be tagged text with Req-<sequence number> with same format " + "\r\n" +
                            "Provide response without any extra symbols,do not use any markdown formatting and tab indented";



                    }
                    else if (prep == 3) // summerize to retrieve technical requirements only for implementation details
                    {
                        PromptTask = "You are a senior software developer. Analyze the given text and response as per the format with each value in a separate new line." + "\r\n" + 
                            "format:\r\n" +
                            "technical requirements:" + "\r\n" +
                            "necessary for designing the solution:\r\n"+
                            "writing the application code:\r\n"+
                            "creating database objects and scripts:\r\n"+
                            "DBMS name:\r\n" +
                            "Suggested database name:" + "\r\n" +
                            "Provide response without any extra symbols,do not use any markdown formatting and tab indented,each in a separated line.";
                    }
                    else if (prep == 4) // generate implementation details
                    {
                        PromptTask = "You are a senior software developer. Analyze the bellow text and generate Implementation Details referring Solution Name, Project Name for File Name. " + "\r\n" +
                            "output must include " + "\r\n" +
                            "Implementation Details:" + "\r\n" +
                            "- Req-<sequence number> (derive from Requirements:) \r\n" +
                            "- Responsibilities:describe all responsibilities of this file" + "\r\n" +
                            "- Logic:include actual Implementation Details in pseudocode outline (features, use cases, user stories, functionalities, data processing thorugh each solution layers) implemented in this file" + "\r\n" +
                            "- Entity Associated:include all entities with entity name with fields, data type, constraints with other details required" + "\r\n" +
                            "- Entity Relationship:<include er relationships with other data entities if any refering Requirements:>" + "\r\n" +
                            "- Input/Output Data:clearly specify input/output data,data validations, Data Dependecies:dependecies <services, models, repositories>" + "\r\n" +
                            "- Include exception handling rules" + "\r\n" +
                            "- Processing Steps:explain processing steps or Algorithms Involved:algorithms involved" + "\r\n" +
                            "- Data Access:" + "\r\n" +
                            "- Authorization Requirements:" + "\r\n" +
                            "- if the Implementation has user interface form to interact then must include form design details with all form fields, associated data entities, validations,entity relationship entity" + "\r\n" +
                            //"- DBObjects:<if applicable for this layer list out possible database objects and their detailed implementations as example for sql tables,stored procedures,views,funtions>"+"\r\n"+
                            "important instruction:" + "\r\n" +
                            "- do not include actual code - only actionable design logic in enough details to generate code from it later. " + "\r\n" +
                            "- refer technical requirements:, Requirements: in the given text while generating each data value in the Implementation Details:" + "\r\n" +
                            "- Provide response that contains only 'Implementation Details:' without loosing any Detail." + "\r\n" +
                            "- Remove any extra symbols,do not use any markdown formatting." + "\r\n" +
                            "- Tab indented response.";
                    }
                    else if (prep == 5)
                    {
                        PromptTask = "You are a senior database architect. Analyze the text and extract relevant database objects:" + "\r\n" +
                            "important instruction:" + "\r\n" +
                            "- Identify tables, stored procedures, views, functions might be required based on the file Implementation Details" + "\r\n" +
                            "- Each table name, view name, stored procedure name, function name must reflect the scenarios or context or purpose it belongs to" + "\r\n" +
                            "- Indentify any data entities involved and extract the fields (name, type, constraints, validations,pk,fk)" + "\r\n" +
                            "- Detect entity relationship if referenced" + "\r\n" +
                            "- Identify CRUD operations or custom data queries if file defines repository/service logic for stored procedures," + "\r\n" +
                            "- identify and suggest views for each report including attributes used, " + "\r\n" +
                            "  functions that may be used in stored procedures" + "\r\n" +
                            "- each object logic should include all implementation and dependent object details" + "\r\n" +
                            "- group all tables within 'tables:', all views within 'views:', all functions within 'functions:',all stored procedures in 'stored procedures:' " + "\r\n" +
                            "- while listing objects example tables, first table name: then name of the table likewise other objects as per format" + "\r\n" +
                            "- skip files that are not relevant for database object to derive\r\n" +
                            "- each table name,view name,sp name,function name is not a duplicate or rephrasing of an earler table name,view name,sp name,function name \r\n" +
                            "- provided text does not contain any relevant information for deriving database object response output will be empty" + "\r\n" +
                            "Remove any extra symbols,do not use any markdown formatting." + "\r\n" +
                            "Tab indented response." +"\r\n" +
                            " format:" + "\r\n" +
                            "- \r\ndatabase object\r\n \r\n" +
                            "- dbms: dbms name\r\n" +
                            "- database name:database name" + "\r\n" +
                            "- tables:\r\n" +
                            "  table name:table name,\r\n" +
                            "  (object type,purpose,data attributes with (data types with validations and constraints, pk,fk))\r\n" +
                            "- views:\r\n" +
                            "  view name:view name, \r\n" +
                            "  (object type,purpose, logic)\r\n" +
                            "- stored procedures:\r\n" +
                            "  sp name: stored procedure name, \r\n" +
                            "  (object type,purpose,logic)\r\n" +
                            "- functions:\r\n" +
                            "  function name:function name,\r\n" +
                            "  (object type,purpose,logic)";
                            
                    }
                    else if (prep == 6) // extract requirements solution guidance
                    {
                        PromptTask = "You are a senior solution architect.Analyze the Text: given below response in the below format\r\n" +
                        "important instruction: \r\n" +
                        "- the input given in the text: contains two sections one is requirements:start and requirements:end and second is taggedrequirements:start and taggedrequirements:end" + "\r\n" +
                        "- read the requirements from taggedrequirements:start and taggedrequirements:end" + "\r\n" +
                        "- extract 'solution guidance' add guidance points referring all the requirements from best practice point of view in separate lines" + "\r\n" +
                        "- tag Req-<sequence number> whereever applicable in the solution guidance referring each requirement from taggedrequirements:start and taggedrequirements:end\r\n" +
                        "Provide response without any extra symbols,do not use any markdown formatting and tab indented" + "\r\n" +
                        "Format:" + "\r\n" +
                        "solution guidance:" + "\r\n";
                    }
                    else if (prep == 7) // extract the module and sub module Hierarchy
                    {
                        PromptTask = "You are a senior solution architect. Analyze the Text given \r\n" +
                        "important instruction: \r\n" +
                        "- extract ModuleSubModule Hierarchy given in the text as it is without any rephrase or update but remove Req-<sequence number>, in bulleted numbered list and maintaining the Hierarchy indented" + "\r\n" +
                        "Provide response without any extra symbols,do not use any markdown formatting and tab indented" + "\r\n" +
                        "Format:" + "\r\n" +
                        "ModuleSubModule Hierarchy:"; 

                    }
                    else if (prep == 8) //  solution overview story telling
                    {
                        PromptTask = "You are a senior solution architect. Analyze the bellow text which is the BRD with TRD." + "\r\n" +
                            "generate who,what,how by each module and submodule then foe all solution layers using the output format" + "\r\n" +
                            "output format example:" + "\r\n" +
                            "module and submodule:\r\nsolution layer:\r\n  - layer name:\r\n    input data:\r\n    data entity associated:\r\n      - data entity:\r\n          fields:\r\n    ER entity:\r\n    operations:\r\n    processing logic:\r\n    business rule:\r\n    output data\r\n" + "\r\n" +
                            "Provide response without any extra symbols,do not use any markdown formatting and tab indented,each in a separated line.";

                    }
                    else if (prep == 9) // findout dbms name and suggest database name
                    {
                        PromptTask = "You are a senior software developer. Analyze the bellow text." + "\r\n" +
                            "output will be in the format: \tdbms:<dbms name>\r\n\tdatabase name:<database name>" + "\r\n" +
                            "Provide response without any extra symbols,do not use any markdown formatting and tab indented,each in a separated line.";
                    }
                    else if (prep == 10)
                    {
                        PromptTask = "You are a senior database architect. Analyze the text and extract relevant database objects:" + "\r\n" +
                            "important instruction:" + "\r\n" +
                            "- Identify tables, stored procedures, views, functions might be required based on the file Implementation Details" + "\r\n" +
                            "- Each table name, view name, stored procedure name, function name must reflect the scenarios or context or purpose it belongs to" + "\r\n" +
                            "- Indentify any data entities involved and extract the fields (name, type, constraints, validations,pk,fk)" + "\r\n" +
                            "- Detect entity relationship if referenced" + "\r\n" +
                            "- Identify CRUD operations or custom data queries if file defines repository/service logic for stored procedures," + "\r\n" +
                            "- identify and suggest views for each report including attributes used, " + "\r\n" +
                            "  functions that may be used in stored procedures" + "\r\n" +
                            "- each object logic should include all implementation and dependent object details" + "\r\n" +
                            "- group all tables within 'tables:', all views within 'views:', all functions within 'functions:',all stored procedures in 'stored procedures:' " + "\r\n" +
                            "- while listing objects example tables, first table name: then name of the table likewise other objects as per format" + "\r\n" +
                            "- skip files that are not relevant for database object to derive\r\n" +
                            "- each table name,view name,sp name,function name is not a duplicate or rephrasing of an earler table name,view name,sp name,function name \r\n" +
                            "- provided text does not contain any relevant information for deriving database object response output will be empty" + "\r\n" +
                            "Remove any extra symbols,do not use any markdown formatting." + "\r\n" +
                            "Tab indented response." + "\r\n" +
                            " format:" + "\r\n" +
                            "tables:\r\n" +
                            "  table name:<table name>\r\n" +
                            "  table detail:(object type,purpose,point wise detailed logic to implement, data attributes with (data types with validations and constraints, pk,fk))\r\n" +
                            "views:\r\n" +
                            "  view name:<view name>\r\n" +
                            "  view detail:(object type,purpose, point wise detailed logic to implement)\r\n" +
                            "stored procedures:\r\n" +
                            "  sp name: <stored procedure name>\r\n" +
                            "  sp detail:(object type,purpose,point wise detailedlogic to implement)\r\n" +
                            "functions:\r\n" +
                            "  function name:<function name>\r\n" +
                            "  function detail:(object type,purpose,point wise detailed logic to implement)";
                            
                    }
                    else if (prep == 11)
                    {
                        PromptTask = "refering the text, include an ASCII art architecture diagrams to illustrate the system architecture.\r\n" +
                            "Diagram should show the interconnected each solution layers, components, and interactions." + "\r\n" +
                            "format:" + "\r\n" +
                            "ArchitectureDiagram: <architecture diagram>" +"\r\n" +
                            "Describe:(Solution_layer:Solution layer,purpose:purpose,data_flow:data flow,best_practice:best practice)" + "\r\n" +
                            "important instruction:" + "\r\n" +
                            "provide response following the format, without any extra symbols,do not use any markdown formatting but bulleted points\r\n";
                    }
                    else if (prep == 12)
                    {
                        PromptTask = "You are a senior database architect. Analyze the text given and derive details as per the below output format " + "\r\n" +
                            "important instruction:" + "\r\n" +
                            "- derive possible table and its details referring entityfields:, entityfields:,entityrelationship:" + "\r\n" +
                            "- possible stored procedures, views and functions from referring from operationslist:, purpose:" + "\r\n" +
                            "Remove any extra symbols,do not use any markdown formatting." + "\r\n" +
                            "output format:" + "\r\n" +
                            "tables:\r\n" +
                            "  table name:<table name>\r\n" +
                            "  table detail:(object type,purpose,point wise detailed logic to implement, data attributes with (data types with validations and constraints, pk,fk))\r\n" +
                            "views:\r\n" +
                            "  view name:<view name>\r\n" +
                            "  view detail:(object type,purpose, point wise detailed logic to implement)\r\n" +
                            "stored procedures:\r\n" +
                            "  sp name: <stored procedure name>\r\n" +
                            "  sp detail:(object type,purpose,point wise detailedlogic to implement)\r\n" +
                            "functions:\r\n" +
                            "  function name:<function name>\r\n" +
                            "  function detail:(object type,purpose,point wise detailed logic to implement)";

                    }
                    break;
                case 2: //Blueprinting - RequirementSummary
                    if (prep == 0)
                    {
                        PromptTask = "summarize the text without loosing its context and any essential details," + "\r\n" +
                        " provide response without any extra symbols,do not use any markdown formatting but bulleted points\r\n";
                    }
                    else if (prep == 1)
                    {
                        PromptTask = "summarize the text to make it concise without loosing its context and any essential details," + "\r\n" +
                            "This responce will be used to create test cases, functional & Integration test cases, Database object logic and objects" + "\r\n" +
                            "provide response without any extra symbols,do not use any markdown formatting but bulleted points\r\n";
                    }
                    break;
                case 3: //Blueprinting - database script
                    if (prep == 0)
                    {


                        PromptTask = "You are a senior database architect. Analyse the text and organize the database objects using the below format:\r\n" +
                                "important instruction:" + "\r\n" +
                                "- Refer the text and database objects to improve and correct the identified object" + "\r\n" +
                                "- do not skip.omit any table name in database object metadata" + "\r\n" +
                                "- keep the Database Name as " + SolutionName + "DB" + "\r\n" +
                                "provide response without any extra symbols,do not use any markdown formatting but bulleted points with tab indented" + "\r\n" +
                                "Format : " + "\r\n" +
                                "Database Script(" + "\r\n" +
                                "DBMS:DBMS name" + "\r\n" +
                                "Database Name:Database Name,(" + "\r\n" +
                                "Tables:(Table Name:Table Name,(must include object type,objective,purpose,details to create it)," + "\r\n" +
                                "List of all tables with fields, data types, primary keys, nullability, constraints" + "\r\n" +
                                "Foreign key relationships" + "\r\n" +
                                "Index requirements (unique, non-unique, composite)" + "\r\n" +
                                "Constraints (default values, checks, not null))" + "\r\n" +
                                "Views:(View Name: View Name,(must include object type,objective,purpose,details to create it))" + "\r\n" +
                                "Stored Procedures:(SP Name:Stored Procedure Name, (must include object type,objective,purpose,details needed to create it))" + "\r\n" +
                                "Functions:(Function Name:Function Name, (must include object type,objective,purpose,details needed to create it)).";
                                
                    }
                    else if (prep == 1)
                    {

                        PromptTask = "You are a senior database architect. remove duplicate and regenerate the database meta data following the format:, " + "\r\n" +
                            "important instruction:" + "\r\n" +
                            "- Analyze each pair of duplicate objects to determine the more comprehensive or correct version based on schema, data integrity, and application requirements." + "\r\n" +
                            "- For duplicate tables, decide which table has the most up-to-date schema and migrate data if necessary." + "\r\n" +
                            "- Update all related stored procedures, functions." + "\r\n" +
                            "- Drop the redundant table." + "\r\n" +
                            "- For duplicate stored procedures, review the logic of each and consolidate into a single version with any necessary enhancements." + "\r\n" +
                            "- For duplicate functions, ensure the logic is consistent and consolidate into a single version with any necessary enhancements." + "\r\n" +
                            "- Group database objects under a single heading using DBMS and database name only once\r\n" +
                            "- each fomarted information should start with 'database object metadata:<sequence number>' in a separate line.\r\n" +
                            "provide response without any extra symbols,do not use any markdown formatting but bulleted points\r\n" +
                            "Format:\r\n" +
                            "- table name:table name,(purpose,data attributes, data types with validations and constraints, pk,fk)" + "\r\n" +
                            "- views:view name,(purpose, logic)" + "\r\n" +
                            "- stored proedures:stored procedure name,(purpose,logic)" + "\r\n" +
                            "- functions: function name,(purpose,logic)"; 
                            
                    }
                    else if (prep == 2)
                    {
                        PromptTask = "You are a senior database architect. Analyse the text and organize the database objects using the below format:\r\n" + "\r\n" +
                            "important instruction:" + "\r\n" +
                            "- Refer the text and database objects to improve and correct the identified object" + "\r\n" +
                            "- do not skip.omit any table name in database object metadata" + "\r\n" +
                            "- keep the Database Name as " + SolutionName + "DB" + "\r\n" +
                            "provide response without any extra symbols,do not use any markdown formatting but bulleted points with tab indented" + "\r\n" +
                            "Format : " + "\r\n" +
                            "Database Script(" + "\r\n" +
                            "DBMS:DBMS name" + "\r\n" +
                            "Database Name:Database Name,(" + "\r\n" +
                            "Tables:(Table Name:Table Name,(must include object type,objective,purpose,details to create it)," + "\r\n" +
                            "List of all tables with fields, data types, primary keys, nullability, constraints" + "\r\n" +
                            "Foreign key relationships" + "\r\n" +
                            "Index requirements (unique, non-unique, composite)" + "\r\n" +
                            "Constraints (default values, checks, not null))" + "\r\n" +
                            "Views:(View Name: View Name,(must include object type,objective,purpose,details to create it))" + "\r\n" +
                            "Stored Procedures:(SP Name:Stored Procedure Name, (must include object type,objective,purpose,details needed to create it))" + "\r\n" +
                            "Functions:(Function Name:Function Name, (must include object type,objective,purpose,details needed to create it)).";
                            

                    }
                    break;

                case 4: // Generate business logic functional code in code synthesis

                    PromptTask = "Your are a senior software engineer, Refer Solution Overview,Data Flow,File Name,File Metadata and Generate production quality code using technology specified in the File Metadata and " + "\r\n" +
                        "apply bellow principles for every part of the generated code:" + "\r\n" +
                        "- must consider points given in File Metadata section while generating code" + "\r\n" +
                        "- include all data entities for the operation" + "\r\n" +
                        "- language specific best coding practice" + "\r\n" +
                        "- security best practice when applicable" + "\r\n" +
                        "- inline code, comments documentation" + "\r\n" +
                        "- must include Req-<sequence number> mapping with requirements for each section from Metadata \r\n" +
                        ", provide response without any extra symbols,do not use any markdown formatting with tab indented";

                    break;

                case 5: //Blueprinting - unit test cases
                    if (prep == 0)
                    {
                        PromptTask = "You are a senior test engineer. generate unit test cases \r\n " +
                           "important instruction:" + "\r\n" +
                           "- refer the text that has unit testing tech stack, unit test metadata carefully" + "\r\n" +
                           "- generate test cases from files,methods refering the unit test metadata" + "\r\n" +
                           "- do not create same file name given in 'instruction to avoid duplicate' for previously created file names" + "\r\n" +
                           "- File Name should be unique and a new name with extension referring Title and unit test tech stack and follow the format" + "\r\n" +
                           "- do not skip,omit any file, method if given in the unit test metadata" + "\r\n" +
                           //"- tag the response with Req-<sequence number> from the text whereever applicable" + "\r\n" +
                           "- keep Project Name as " + SolutionName + "\r\n" +
                        "inlcude:\r\n" +
                        "(Cover positive, negative, and boundary scenarios" + "\r\n" +
                        ",Include(exception handling)" + "\r\n" +
                        ",Follow the Arrange Act Assert pattern" + "\r\n" +
                        ",Use clear test names like Method Name Condition Expected Result" + "\r\n" +
                        ",Include assertions that verify expected outcomes" + "\r\n" +
                        ",Keep each test case focused on a single behavior" + "\r\n" +
                        ",Use mocks stubs where dependencies exist)." + "\r\n" +
                        "for each Project Name and File Name use this format (Project Name: Project Name\r\n(File Name: <new File Name to create unit testing code>\r\n " +
                        "unit testing tech stack:\r\nobjective:\r\npurpose:\r\n" + "\r\n" +
                        "\r\nTest Case ID:TC-<sequence number>\r\nTitle:\r\nDescription:<description with Req-<sequence number>>\r\nPre-Conditions:\r\nTest Steps:\r\nTest Data:\r\nExpected Result:)).\r\n" +
                        "provide response without any extra symbols,do not use any markdown formatting but bulleted points with tab indented.";
                    }
                    else if (prep == 1)
                    {
                        PromptTask = "You are a senior test engineer.extract required information for unit test relevant metadata but do not generate test case" +
                        "This responce will be used to create unit test cases" + "\r\n" +
                        "provide response without any extra symbols,do not use any markdown formatting but bulleted points";

                    }

                    break;
                case 6: //Generate database script code in code synthesis


                    PromptTask = "Your are a senior database architect, Refer File Metadata and Generate database object script code and " + "\r\n" +
                        "apply bellow principles for every part of the generated code:" + "\r\n" +
                        "- language specific best coding practice" + "\r\n" +
                        "- security best practice when applicable" + "\r\n" +
                        "- inline code explanations" + "\r\n" +
                        ", provide response without any extra symbols,do not use any markdown formatting with tab indented";


                    break;

                case 7://Generate unit test script code in code synthesis


                    PromptTask = "Your are a senior QA engineer, Refer File Metadata and Generate unit test code and " + "\r\n" +
                        "apply bellow principles for every part of the generated code:" + "\r\n" +
                        "- language specific best coding practice" + "\r\n" +
                        "- security best practice when applicable" + "\r\n" +
                        "- inline code explanations" + "\r\n" +
                        "- must include Req-<sequence number> mapping with requirements for each section from Metadata \r\n" +
                        ", provide response without any extra symbols,do not use any markdown formatting with tab indented";



                    break;
                case 8://Generate code documentation in code synthesis


                    PromptTask = "Refer the text and describe the code in detail, include the following in your explanation:" + "\r\n" +
                        "- purpose of the code file" + "\r\n" +
                        "- key components (classs,methods,properties,functions)" + "\r\n" +
                        "- responsibilites of each componets" + "\r\n" +
                        "- any special consideration (security, validations, external integrations, dependencies, error handling etc.)" + "\r\n" +
                        "- must include Req-<sequence number> mapping with requirements for each section from referred text \r\n" +
                        "provide response without any extra symbols,do not use any markdown formatting";

                    break;
                case 9: //Blueprinting - functional test cases
                    if (prep == 0)
                    {
                        PromptTask = "You are a senior QA engineer. Generate functional test cases \r\n" +
                        "important instruction:\r\n" +
                        "- refer Solution Overview and common functionalities given in the text but generate test cases only for functional test metadata" + "\r\n" +
                        "- do not skip, omit any functional test scenario mentioned in the metadata \r\n" +
                        "- consider all test case scenarios from each functional test metadata \r\n" +
                        "- File Name should be a new name with extension as per funtional test tech stack referring use case " + "\r\n" +
                        "- keep Project Name as " + SolutionName + "\r\n" +
                        "include :\r\n" +
                        "(Positive, negative, boundary, validation, and error handling scenarios " + "\r\n" +
                        ",Role based access and integration checks(if applicable)" + "\r\n" +
                        ",Field level and business rule validations)." + "\r\n" +
                        "for each Project Name, User Cases, User Story use this format (Project Name: Project Name \r\n, (Use Case: use case with Req-<sequence number> \r\n,File Name: <new File Name to create functional testing code>\r\n," +
                        "(User story:user story,\n" +
                        "functional testing tech stack,Test Case ID:FC-<sequence number>, Title, Description with Req-<sequence number>, Pre-Conditions, Test Steps, Test Data, Expected Result)))." + "\r\n" +
                        "provide response without any extra symbols,do not use any markdown formatting but bulleted points with tab indented.";
                    }
                    else if (prep == 1)
                    {
                        PromptTask = "You are a senior QA engineer. refer the text for functional test cases for the " + SolutionName + "\r\n" +
                        "important instruction:\r\n" +
                        "- identify and remove duplicates or near duplicates user story. for similar user story, keep the most comprehensive or clearly written one" + "\r\n" +
                        "- consolidate file name " +
                        "- file name should be renamed with extension as per user story following with functional test stack" + "\r\n" +
                        "This responce will be used to create functional test case scripts" + "\r\n" +
                        "provide response without any extra symbols,do not use any markdown formatting";

                    }
                    break;
                case 10://Generate functional test code in code synthesis

                    PromptTask = "Your are a senior QA engineer, Refer File Metadata and Generate functional test code and " + "\r\n" +
                        "apply bellow principles for every part of the generated code:" + "\r\n" +
                        "- language specific best coding practice" + "\r\n" +
                        "- security best practice when applicable" + "\r\n" +
                        "- inline code explanations" + "\r\n" +
                        "- must include Req-<sequence number> mapping with requirements for each section from Metadata \r\n" +
                        ", provide response without any extra symbols,do not use any markdown formatting with tab indented";


                    break;

                case 11: //Blueprinting - integration test cases
                    if (prep == 0)
                    {
                        PromptTask = "You are a senior QA engineer. Generate integration test cases referring integration testing tech stack and integration test metadata," + "\r\n" +
                        "important instruction:\r\n" +
                        "- refer Solution Overview, integration testing tech stack,common functionalities, integration test metadata carefully" + "\r\n" +
                        "- do not skip, omit any end-to-end flow in integration test metadata\r\n" +
                        "- File Name should be a new name with extension referring integration title and integration test tech stack and follow the format" + "\r\n" +
                        "- keep Project Name as " + SolutionName + "\r\n" +
                        "provide response without any extra symbols,do not use any markdown formatting but bulleted points with tab indented." + "\r\n" +
                        "use format (Project Name: Project Name\r\n,(Integration Title:integration title with Req-<sequence number>\r\n,File Name: <new File Name to create integration testing code>\r\n," +
                        "(Title: title,integration testing tech stack, Test Case ID:IC-<sequence number>," + "\r\n" +
                        ",Integration Points (which modules/services are interacting)" + "\r\n" +
                        ",Integration Point Details" + "\r\n" +
                        ",Objective (what this test is validating) with Req-<sequence number>" + "\r\n" +
                        ",Preconditions (environment/data setup)" + "\r\n" +
                        ",Test Steps" + "\r\n" +
                        ",Test Data (sample input used)" + "\r\n" +
                        ",Expected Result (including response, data update, etc.)" + "\r\n" +
                        ",Error Handling Scenario (what should happen if a dependent service/module fails)" + "\r\n" +
                        ",Security Aspects (token/role validation if applicable)" + "\r\n" +
                        ",Validation Points (what will be asserted)" + "\r\n" +
                        ",Dependencies (services or components required)" + "\r\n" +
                        ",Logs/Monitoring (optional)" + "\r\n" +
                        ",Priority))).";
                        
                    }
                    else if (prep == 1)
                    {

                        PromptTask = "You are a senior test engineer. refer the text to remove duplicate integration test cases." + "\r\n" +
                        "important instruction:\r\n" +
                        "- remove duplicate Integration Title " + "\r\n" +
                        "- file name should be renamed with extension as per Integration Title following with integration test stack" +
                        "This responce will be used to create integration test case scripts" + "\r\n" +
                        "provide response without any extra symbols,do not use any markdown formatting but bulleted points";


                    }

                    break;
                case 12://Generate integration test code in code synthesis


                    PromptTask = "Your are a senior QA engineer, Refer File Metadata and Generate integration test code and " + "\r\n" +
                        "apply bellow principles for every part of the generated code:" + "\r\n" +
                        "- language specific best coding practice" + "\r\n" +
                        "- security best practice when applicable" + "\r\n" +
                        "- inline code explanations" + "\r\n" +
                        "- must include Req-<sequence number> mapping with requirements for each section from Metadata \r\n" +
                        ", provide response without any extra symbols,do not use any markdown formatting with tab indented";


                    break;
                case 13:// getting Blueprinting metadata for database script, unit , functional and integration

                    PromptTask = "Analyze the text and extract relevant metadata as follows:" + "\r\n" +
                    "for unit test:\r\n" +
                    "- extract required information for unit test, relevant metadata but do not generate test case " + "\r\n" +
                    "- only consider files that contains a class, method, service logic, UI logic, business logic, controller" + "\r\n" +
                    "- do not extract information skip from file types : configuration, view/template, stylesheets, no behavior or log to test," + "\r\n" +
                    "(.html,.css,.scss,.env,.config.json.yaml,.xml), infra setup, static UI asssets,output/log/archive,existing test files \r\n" +
                    "follow the format:\r\n" +
                    "- unit test\r\n" +
                    "- file name, method name, logic, Req-<sequence number> mapping with metadata, input/outpout, business rules, exceptions, dependecies" + "\r\n" +
                    "\r\nThis responce will be used to create unit test cases\r\n" +
                    "for functional test:\r\n" +
                    "- extract required information for functional test, relevant metadata but do not generate test case " + "\r\n" +
                    "- consider files related to features or functionalities implemented like API,controller, services, DTO,logic" + "\r\n" +
                    "  otherwise add the section but keep it as blank" + "\r\n" +
                    "- do not extract information skip for file types : configuration, view/template,stylesheets, no behavior or log to test, " + "\r\n" +
                    "(.html,.css,.scss,.env,.config.json.yaml,.xml),infra setup, static UI asssets,output/log/archive,existing test files \r\n" +
                    "- skip files that are not relevant for functional test " + "\r\n" +
                    "follow the format:\r\n" +
                    "- functional test\r\n" +
                    "- function / scenarios name, Req-<sequence number> mapping with metadata, entry point (API or controller), HTTP method/endpoint,Input payload," + "\r\n" +
                    "validation rules, expected output/ status, auth/role required" +
                    "\r\nThis responce will be used to create functional test cases\r\n" +
                    "for integration test:\r\n" +
                    "- extract required information for integration test, relevant metadata but do not generate test case" + "\r\n" +
                    "- consider files related to logic,controller, service class, repository/DAO, API clients, event handlers/consumer, inter service connectors, middleware/filters,DB layers" + "\r\n" +
                    " otherwise add the section but keep it as blank" + "\r\n" +
                    "- do not extract information skip from file types : configuration, view/template,stylesheets, no behavior or log to test, " + "\r\n" +
                    "(.html,.css,.scss,.env,.config.json.yaml,.xml),infra setup, static UI asssets,output/log/archive,existing test files \r\n" +
                    "- skip files that are not relevant for integration test\r\n" +
                    "Follow the format:\r\n" +
                    "- integration test\r\n" +
                    "- end-to-end flow name, Req-<sequence number> mapping with metadata,entry point to service to report to client, input to expected output, which services are mocked," + "\r\n" +
                    "external dependecies, side effects / DB state change, failure cases" +
                    "\r\nThis responce will be used to create integration test cases\r\n" +
                    "provide response without any extra symbols,do not use any markdown formatting but bulleted points\r\n";
                    break;

                case 14: // documentation

                    if (prep == 0)
                    {
                        //PromptTask = "you are a senior technical writer. your task is to generate content for a section for " + strSection + "\r\n" +
                        //    " from solution overview and solution structure, follow the instruction carefully" + "\r\n" +
                        //    "important instruction:\r\n" +
                        //    "- input text includes document template with a section for " + strSection + "\r\n" +
                        //    "- read the instruction in the section from document template then extract or infer information for specific to this section only" + "\r\n" +
                        //    "- must include Req-<sequence number> mapping with requirements for each section from solution overview, solution structure \r\n" +
                        //    "- do not ask me to complete or continue or repeat  similary rest of the content, rather you provide all without skip,omit " + "\r\n" +
                        //    "- provide response without any extra symbols,do not use any markdown formatting";

                    }
                    else if (prep == 1) //LLD
                    {
                        PromptTask = "you are a senior technical writer. derive the information using below format, " + "\r\n" +
                        "format: " + "\r\n" +
                        "module:\r\n  submodules:\r\n    - submodule:\r\n        features:\r\n          - feature:\r\n              use_cases:\r\n                - use_case:\r\n                  user_stories:\r\n                    - user_story:\r\n                        functionalities:\r\n                          - functionality:\r\n                              implementation_details:\r\n                                - implementation_detail:\r\n" +
                        "\r\nimportant instruction:\r\n" +
                        "- input text includes solution overview,solution structure,document template for " + strSection + "\r\n" +
                        "- extract module and submodule information from the input text 'modulesubmodule hierarchy:' to extract information as per the format given" + "\r\n" +
                        "- each module and submodule information should start with 'section:<sequence number>' in a separate line" + "\r\n" +
                        "- must include Req-<sequence number> mapping with requirements for each section from solution overview, solution structure \r\n" +
                        "- do not leave blank for module,submodule,feature,use_case,user_story,functionality,implementation_detail" + "\r\n" +
                        "- do not ask me to complete or continue or repeat  similary rest of the content, rather you provide all without skip,omit " + "\r\n" +
                        "- provide response in bulleted format without any extra symbols,do not use any markdown formatting";


                    }
                    else if (prep == 2) //UserManual
                    {
                        PromptTask = "you are a senior technical writer. derive the information using below format, " + "\r\n" +
                        "format: " + "\r\n" +
                        "module:\r\n  submodules:\r\n    - submodule:\r\n        features:\r\n          - feature:\r\n              use_cases:\r\n                - use_case:\r\n                  user_stories:\r\n                    - user_story:\r\n                        functionalities:\r\n                          - functionality:\r\n                              implementation_details:\r\n                                - implementation_detail:\r\n                                    ui_files:\r\n                                      - ui_file:\r\n                                          logic: \r\n" +
                        "\r\nimportant instruction:\r\n" +
                        "- input text includes solution overview, solution structure, document template for " + strSection + "\r\n" +
                        "- extract module and submodule information from the input 'modulesubmodule hierarchy:'" + "\r\n" +
                        "- each module and submodule information should start with 'section:<sequence number>' in a separate line" + "\r\n" +
                        "- must include Req-<sequence number> mapping with requirements for each section from solution overview, solution structure \r\n" +
                        "- do not leave blank for module,submodule,feature,use_case,user_story,functionality,implementation_detail,ui_file,logic" + "\r\n" +
                        "- do not skip, omit, merge any module or sub module,features under each sub module with nested sub modules per level \r\n" +
                        "- do not ask me to complete or continue or repeat  similary rest of the content, rather you provide all without skip,omit " + "\r\n" +
                        "- provide response in bulleted format without any extra symbols,do not use any markdown formatting";


                    }
                    else if (prep == 3) //Section content hld n lld
                    {


                        PromptTask = "you are a senior technical writer. your task is to generate technical documentation for a section " + strSection + "\r\n" +
                        //"as per the document template given in document template: of the text" + "\r\n" +
                        "important instruction:\r\n" +
                        //"- input text includes solution overview, solution structure and document template for a section of " + strSection + "\r\n" +
                        "- generate content for the section given in 'document template:' referring solution overview, solution structure" + "\r\n" +
                        "- output response only inculde points as per the 'document template:' format" + "\r\n" +
                        "- tag Req-<sequence number> in the generated content from solution overview, solution structure \r\n" +
                        "- analyse all the generated output before giving response,if response has multiple occurence of format: found " +
                        "then provide at the end of duplicateformatfound=yes/no" + "\r\n" +
                        "- do not skip, omit, merge any module or sub module,features under each sub module with nested sub modules per level \r\n" +
                        "- do not ask me to complete or continue or repeat  similary rest of the content, rather you provide all without skip,omit " + "\r\n" +
                        "- do not include important instruction: in the response" + "\r\n" +
                        "- provide response without any extra symbols,do not use any markdown formatting";

                    }
                    else if (prep == 4) //Section content user manual
                    {

                        PromptTask = "you are a senior technical writer. your task is to generate technical documentation for a section " + strSection + "\r\n" +
                        //"as per the document template given in document template: of the text" + "\r\n" +
                        "important instruction:\r\n" +
                        //"- input text includes solution overview, solution structure and document template for a section of " + strSection + "\r\n" +
                        "- generate content for the section given in 'document template:' referring solution overview, solution structure" + "\r\n" +
                        "- output response only inculde points as per the format 'document template:' format" + "\r\n" +
                        "- tag Req-<sequence number> in the generated content from solution overview, solution structure \r\n" +
                        "- analyse all the generated output before giving response,if response has multiple occurence of format: found " +
                        "then provide at the end of duplicateformatfound=yes/no" + "\r\n" +
                        "- do not skip, omit, merge any module or sub module,features under each sub module with nested sub modules per level \r\n" +
                        "- do not ask me to complete or continue or repeat  similary rest of the content, rather you provide all without skip,omit " + "\r\n" +
                        "- do not include important instruction: in the response" + "\r\n" +
                        "- provide response without any extra symbols,do not use any markdown formatting";


                    }
                    else if (prep == 5) //hld
                    {

                        PromptTask = "you are a senior technical writer. generate content as per the format below" + "\r\n" +
                            "format:" + "\r\n" + SolutionName.Trim() + "\r\n" +
                            "important instruction:\r\n" +
                            "- tag Req-<sequence number> in the generated content referring the Text:\r\n" +
                            "- analyse all the generated output before giving response,if response has multiple occurence of format: found " +
                            "then provide at the end of duplicateformatfound=yes/no" + "\r\n" +
                            "- do not include important instruction: in the response" + "\r\n" +
                            "- provide response without any extra symbols,do not use any markdown formatting";

                    }

                    break;

                case 15: // generate implementation details for reverse enineering

                    if (prep == 0)
                    {
                        PromptTask = "You are a senior software developer. Analyze the bellow text and use below format for response "+ "\r\n" +
                            //"and generate Implementation Details " + "\r\n" +
                            "- Format:" + "\r\n" +
                            "Purpose:" + "\r\n" +
                            "Implementation Details:" + "\r\n" +
                            "Responsibilities:<describe all responsibilities of this implemented code>" + "\r\n" +
                            "Logic:<include Details in pseudocode outline (features, use cases, user stories, functionalities) implemented in this code>" + "\r\n" +
                            "Entity Associated:<entiry name with fileds, data type, constraints>" + "\r\n" +
                            "Input/Output Data:<clearly specify input/output data,data validations> " + "\r\n" +
                            "Data Validations:<dependecies <services, models, repositories>>" + "\r\n" +
                            "Exception handling:<include exception handling rules if any>" + "\r\n" +
                            "Processing Steps:<explain processing steps or Algorithms Involved:algorithms involved>" + "\r\n" +
                            "Data Access:" + "\r\n" +
                            "Authorization Requirements:" + "\r\n" +
                            "- important instruction: \r\n" +
                            "do not repeat only one occurrence of data points given in the format in th eresponse" + "\r\n" +
                            "do not include actual code from the file but details as per the format" + "\r\n"+
                             "- analyse all the generated output before giving response,if response has multiple occurence of format: found " +
                            "then provide at the end of duplicateformatfound=yes/no" + "\r\n" +
                            //"combine the outpout into one occurrence of the data points given in the format  " + "\r\n" +
                            "Remove any extra symbols,do not use any markdown formatting." + "\r\n" +
                            "Tab indented response."; 
                    }
                    else if (prep == 1)
                    {
                        PromptTask = "You are a senior solution architect.Analyze the text to extract the details for the bellow format\r\n" +
                            "Format:" + "\r\n" +
                            "(Solution Overview: <solution overview (each point in separate line)>\r\n" +
                            "Application Name:<Application Name>\r\n" +
                            "Description:<Description>\r\n" +
                            "Architecture:<Architecture>\r\n" +
                            "ArchitectureDiagram:" + "\r\n" +
                            "Technology Stack:<Technology Stack>\r\n" +
                            "Technical Requirements:<Technical Requirements>\r\n" +
                            "Technical Instructions:<Technical Instructions>" + "\r\n" +
                            "Solution Layers:<Solution Layers>\r\n" +
                            "Security and Compliance:<Security and Compliance>\r\n" +
                            "Scalability and Performance:<Scalability and Performance>\r\n" +
                            "Assumption and Constraints:<Assumption and Constraints>" + "\r\n" +
                            "Unit Testing:<Unit Testing tech stack>\r\n" +
                            "Functional Testing:<Functional Testing>\r\n" +
                            "Integration Testing:<Integration Testing>" + "\r\n" +
                            "ModuleSubModule Hierarchy:<module and sub module hierarchy>" + "\r\n" +
                            "greenfield/brownfield:brown\r\n" +
                            "Data Flow:(refering solution layers contextual flow:,logical flow:,physical flow:)" + "\r\n" +
                            "Common Functionalities:<Common Functionalities>)" +
                            "important instruction: \r\n" +
                            "- create ModuleSubModule Hierarchy from the text and all module, sub module and nested sub modules recursively in the ModuleSubModule Hierarchy in bulleted numbered list" + "\r\n" +
                            "- derive all possible Solution Layers from the text, put the value of greenfield/brownfield = brown" + "\r\n" +
                            "- infer and derive details if not given in the text directly" + "\r\n" +
                            "- do not inlcude format labels in the values\r\n" +
                            "- do not skip, omit, merge any module or sub module,features under each sub module with nested sub modules per level \r\n" +
                            "Provide response without any extra symbols,do not use any markdown formatting and tab indented";


                    }
                    else if (prep == 2)
                    {
                        PromptTask = "You are a senior software developer. Analyze the bellow text for \"File Content:\" referring  \"Solution Overview:\" " + "\r\n" +
                            "- there is a Implementation Details: section in the file content which implements business requirement mapped with Solution Overview's each usecase as Req-<sequence number> " + "\r\n" +
                            "- add Req-<sequence number> as a additional line in the Implementation Details: section of the file content to map with Solution Overview" + "\r\n" +
                            "Provide response that contains only 'Implementation Details:' without loosing any Detail." + "\r\n" +
                            "Remove any extra symbols,do not use any markdown formatting." + "\r\n" +
                            "Tab indented response.";

                    }
                    else if (prep == 3)
                    {
                        PromptTask = "analyse the text and combine each label into one if multiple occurrence are there" +"\r\n" +
                            "Provide response without any extra symbols,do not use any markdown formatting and tab indented";

                    }
                    else if (prep == 4)
                    {
                        PromptTask = "the text given to analyse has two sections, one is  solution structure: and second is RequirementSummary: " + "\r\n" +
                            "do not omit, summerize or rephrase in a way that loses any details." + "\r\n" +
                            "refer RequirementSummary: to read each Req-<sequence number> then tag solution structure: with Req-<sequence number> whereever is applicable" + "\r\n" +
                            "output is solution structure: section tagged with Req-<sequence number> " + "\r\n" +
                            "Provide response without any extra symbols,do not use any markdown formatting and tab indented";

                    }
                    break;
                case 16:  // for Code Review
                    if (prep == 0)
                    {
                        PromptTask = "You are a senior code reviwer. use the provided code review checklist and best practices to evaluate the following code." + "\r\n" +
                        "the code is annotated with line numbers. provide constructive, structured feedback using output format." + "\r\n" +
                        "Provide response without any extra symbols,do not use any markdown formatting and tab indented" + "\r\n";
                    }
                    else if (prep == 1)
                    {
                        PromptTask = "file information:" + "\r\n" +
                        "{{fileinfo}}" + "\r\n" +
                        "code review checklist:" + "\r\n" +
                        "{{checklist}}" + "\r\n" +
                        "best practice:" + "\r\n" +
                        "{{bestpractice}}" + "\r\n" +
                        "output format:" + "\r\n" +
                        "- summary" + "\r\n" +
                        "- issue found with line number and suggessions for improvement" + "\r\n" +
                        "- total number of line of code:<do not count commented lines>" +
                        "- best practice compliance score in % " + "\r\n" +
                        "code:" + "\r\n" +
                        "{{code}}";


                    }
                    break;
                case 17:  // Traceability
                    if (prep == 0)
                    {
                        PromptTask = "You are a traceability engine.extract mapping of Req-<sequence number> with the text sections" + "\r\n" +
                        "example of the output response:" + "\r\n" +
                        "Req-1 = "+ strSection + "-1" + "," + strSection + "-1.2" + "\r\n" +
                        "Req-2 = "+ strSection + "-1" + "," + strSection + "-2" + strSection + "," + strSection + "-3.2" + "\r\n" +
                        "Req-3.1 = " + strSection + "-1" + "," + strSection + "-1.2" + "\r\n" +
                        "Provide response without any extra symbols,do not use any markdown formatting and tab indented" + "\r\n";
                    }
                    else if (prep == 1)
                    {
                        PromptTask = "Analyse the given text to arrange in the below tabular format" + "\r\n" +
                        "format:" + "\r\n" +
                        "Requirement ID,Code Files,Unit Test,Functional Test,Integration Test,HLD,LLD,User Manual" + "\r\n" +
                        "important instruction:\r\n" +
                        "arrange the text in tabular format with columns given in the format" + "\r\n" +
                        "in the input text each line starts with a 'column name=' then the value and '#' used as column separater for values" + "\r\n" +
                        "arrange space between columns as per each column value like a tabular report" +"\r\n" +
                         "each column value should start from where its header starts" + "\r\n" +
                        "Provide response without any extra symbols,do not use any markdown formatting and tab indented" + "\r\n";
                    }
                    else if (prep == 2)
                    {
                        PromptTask = "You are a traceability engine.\r\nbelow are the inputs given" + "\r\n" +
                        "1. requirement list with Req-<sequence number>" + "\r\n" +
                        "2. mapping of code files tagged with requirements Req-<sequence number>" + "\r\n" +
                        "3. test cases associated with requirements Req-<sequence number>" + "\r\n" +
                        "important instruction:\r\n" +
                        "generate a full traceability matrix in tabular format with columns: " + "\r\n" +
                        "| Requirement ID | Code Files | Test Cases | HLD/LLD | User Manual |" + "\r\n" +
                        "Provide response without any extra symbols,do not use any markdown formatting and tab indented" + "\r\n";
                    }
                    else if (prep == 3)
                    {
                        PromptTask = "You are a traceability engine.\r\nbelow are the inputs given" + "\r\n" +
                        "1. requirement list with Req-<sequence number>" + "\r\n" +
                        "2. mapping of code files tagged with requirements Req-<sequence number>" + "\r\n" +
                        "3. test cases associated with requirements Req-<sequence number>" + "\r\n" +
                        "4. documentation tagged with requirements Req-<sequence number>" + "\r\n" +
                        "important instruction:\r\n" +
                        "generate a full traceability matrix in tabular format with columns: " + "\r\n" +
                        "| Requirement ID | Code Files | Test Cases | HLD/LLD | User Manual |" + "\r\n" +
                        "Provide response without any extra symbols,do not use any markdown formatting and tab indented" + "\r\n";
                    }
                    else if (prep == 5)
                    {
                        PromptTask = "You are a traceability engine.\r\nbelow are the inputs given" + "\r\n" +
                        "1. requirement list with Req-<sequence number>" + "\r\n" +
                        "2. mapping of code files tagged with requirements Req-<sequence number>" + "\r\n" +
                        "3. test cases associated with requirements Req-<sequence number>" + "\r\n" +
                        "4. documentation tagged with requirements Req-<sequence number>" + "\r\n" +
                        "important instruction:\r\n" +
                        "generate a full traceability matrix in tabular format with columns: " + "\r\n" +
                        "| Requirement ID | Code Files | Test Cases | HLD/LLD | User Manual |" + "\r\n" +
                        "Provide response without any extra symbols,do not use any markdown formatting and tab indented" + "\r\n";


                    }
                    break;
                case 18:
                    if(prep==0)
                    {
                        PromptTask = "Generate content for Business Requirment Document with Technical Requirement from the given text using the below format " + "\r\n" +
                            "important instruction:\r\n " + "\r\n" +
                            "output the rewritten version only. do not include any explanation." + "\r\n" +
                            "if any section is missing or blank, generate out of the text." + "\r\n" +
                            "- do not inlcude format labels in the values\r\n" +
                            " provide your response without any extra symbols , do not use any markdown formatting and tab indented" + "\r\n" +
                            "Format:" + "\r\n" +
                            "- Business Objectives:" + "\r\n" +
                            "- Business Requirements: description," + "\r\n" +
                            "Module&SubModulehierarchy: <generate possible module and submodule hierarchy>" + "\r\n" +
                            ", describe each " + "\r\n" +
                            " (module, sub module, " + "\r\n" +
                            "section, sub section) with " + "\r\n" +
                            "feature and functionalities, " + "\r\n" +
                            "data required and its validation, " + "\r\n" +
                            "business rules," + "\r\n" +
                            "user flow, data flow, " + "\r\n" +
                            "technical points" + "\r\n" +
                            "- Functional Requirements" + "\r\n" +
                            "- Technical Requirements " + "\r\n" +
                            "- Data Requirements" + "\r\n" +
                            "- Scope of Work" + "\r\n" +
                            "- Assumptions and Constraints" + "\r\n" +
                            "- Non-Functional Requirements" + "\r\n" +
                            "- Security Access Control" + "\r\n" +
                            "- Project Deliverables" + "\r\n" +
                            "- Milestones" + "\r\n" +
                            "- Success Criteria" + "\r\n" +
                            "- Other Key Points";
                    }
                    break;
            }
            return PromptTask;
        }

    }

    public class APIResponse
    {
        public string Response { get; set; }
        public string Finishreason { get; set; }
        public int CompletionTokens { get; set; }
        public int PromptTokens { get; set; }
        public int TotalTokens { get; set; }

    }

    public class OpenAIStreamChunk
    {
        public List<Choice> Choices { get; set; }
        public class Choice { public Delta Delta { get; set; } }

        public class Delta { public string Content { get; set; } }

    }
}