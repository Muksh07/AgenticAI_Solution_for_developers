using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace backend.Models
{
    public class CodeSynRequestModel
    {
        public string? Filename { get; set; }
        public string? FileContent { get; set; }
        public int i { get; set; }
        public string DataFlow { get; set; }
        public string SolutionOverview { get; set; }

        public string commonFunctionalities { get; set; }
        public string? requirementSummary { get; set; }
        public string UploadChecklist { get; set; }
        public string UploadBestPractice { get; set; }
        public string EnterLanguageType { get; set; }
        public string? code { get; set; }
        public string? description { get; set; }
        public int id { get; set; }

    }
}