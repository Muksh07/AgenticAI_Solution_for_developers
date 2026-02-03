using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace backend.Models
{
   public class ProjectFeedback
    {
        [Key]
        public int FeedbackID { get; set; }
       
        [ForeignKey("ProjectLifecycle")]
        public int ProjectID { get; set; }
 
        [Column(TypeName = "decimal(18,2)")]
        public decimal? CodeCoverageScore { get; set; }
 
        [Column(TypeName = "decimal(18,2)")]
        public decimal? CodeQualityScore { get; set; }
 
        [Column(TypeName = "nvarchar(max)")]
        public string? HLD { get; set; }
        [Column(TypeName = "nvarchar(max)")]
        public string? LLD { get; set; }
        [Column(TypeName = "nvarchar(max)")]
        public string? User_Manual { get; set; }
        [Column(TypeName = "nvarchar(max)")]
        public string? Traceability { get; set; }
 
        [Column(TypeName = "nvarchar(max)")]
        public string? ReviewerComments { get; set; }
 
        public DateTime? FeedbackDate { get; set; }
   
    }
 
}