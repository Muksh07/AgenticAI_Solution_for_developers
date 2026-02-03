using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace backend.Models
{
    public class CodeReviewModel
    {
        public string? Filename { get; set; }
        public string? code { get; set; }
        public string? UploadChecklist { get; set; }
        public string? UploadBestPractice { get; set; }
        public string? EnterLanguageType { get; set; }
        
    }
}