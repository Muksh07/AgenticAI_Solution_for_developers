using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace backend.Models
{
    public class TraceabilityRequest
    {
        public FileNode FileNode { get; set; }
        public string? requirementSummary { get; set; }

        public string? field { get; set; }
        public int id { get; set; }

    }
}