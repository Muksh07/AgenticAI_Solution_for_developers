using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace backend.Models
{
    public class ReveseEngineeringRequestModel
    {
        public string? SolutionDescription { get; set; }
        public FileNode? FolderStructure { get; set; }
        public string? UploadChecklist { get; set; }
        public string? UploadBestPractice { get; set; }
        public string? EnterLanguageType { get; set; }

        public bool generateBRDWithTRD { get; set; }
        public bool reverseEngineeringAlreadyCalled { get; set; }
        public string? solutionOverview { get; set; }
        public string? requirementSummary { get; set; }

    }

    public class FileNode
    {
        public string? name { get; set; }
        public string? type { get; set; }  // "file" or "folder" 
        public string? code { get; set; }
        public string? description { get; set; }
        public string? content { get; set; }  // for folders, this may be path; for files, actual content
        public string? codeReview { get; set; }
        public bool expanded { get; set; }  // optional, only for UI purposes
        public List<FileNode>? children { get; set; } = new List<FileNode>();
    }

    // public class TraceabilityRequest
    // {
    //     public FileNode FileNode { get; set; }
    //     public string? requirementSummary { get; set; }
    // }

}
