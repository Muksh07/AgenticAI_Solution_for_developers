using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace backend.Models
{
    public class ProjectLifecycle
    {
        [Key]
        public int ProjectID { get; set; }

        [Required]
        [MaxLength(200)]
        public string? ProjectName { get; set; }
        
        [MaxLength(50)]
        public string? ProjectType { get; set; }
        public DateTime? CreatedDate { get; set; }

        [MaxLength(100)]
        public string? CreatedBy { get; set; }

        [MaxLength(50)]
        public string? InsightElicitationStatus { get; set; }

        [MaxLength(50)]
        public string? SolidificationStatus { get; set; }

        [MaxLength(50)]
        public string? BlueprintingStatus { get; set; }

        [MaxLength(50)]
        public string? CodeSynthesisStatus { get; set; }

        [MaxLength(50)]
        public string? Code { get; set; }

        [MaxLength(50)]
        public string? description { get; set; }

        [MaxLength(50)]
        public string? Testing_Unit { get; set; }

        [MaxLength(50)]
        public string? Testing_Functional { get; set; }

        [MaxLength(50)]
        public string? Testing_Integration { get; set; }

        [MaxLength(50)]
        public string? Doc_HLD { get; set; }

        [MaxLength(50)]
        public string? Doc_LLD { get; set; }

        [MaxLength(50)]
        public string? Doc_UserManual { get; set; }

        [MaxLength(50)]
        public string? Doc_TraceabilityMatrix { get; set; }

        [MaxLength(50)]
        public string? CodeReview { get; set; }

        public DateTime? LastUpdated { get; set; }


        //public ICollection<ProjectFeedback> Feedbacks { get; set; }
    }
}