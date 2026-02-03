using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace backend.Models
{
    public class CodeSynthesisRequestModel
    {
        
        public string? Filename { get; set; }
        public string? FileContent { get; set; }
        public int I { get; set; }
    }

    // public class CodeSynthesisRequestModel
    // {
    //     public FolderNode FolderStructure { get; set; }
    // }

    // public class FolderNode
    // {
    //     public string Name { get; set; }
    //     public string Type { get; set; } // 'folder' or 'file'
    //     public string Content { get; set; }
    //     public bool Expanded { get; set; }
    //     public List<FolderNode> Children { get; set; } = new List<FolderNode>();
    //     public string Code { get; internal set; }
    // }
}