using System.Text.Json;
using System.Text.Json.Serialization;

namespace backend.Models
{

    public class SolidifySolutionStrcutureMetadata
    {
       
        public string SolutionName { get; set; }

        
        public string RootFolder { get; set; }

        public List<ProjectMetadata> Project { get; set; }
    }

    public class ProjectMetadata
    {
       
        public string ProjectName { get; set; }

        public List<ProjectPaths> Paths { get; set; }
    }

    public class ProjectPaths
    {
        
        public string ProjectPath { get; set; }

        public List<FileMetadata> Files { get; set; }
    }

    public class FileMetadata
    {
        
        public string FileName { get; set; }


        public string Purpose { get; set; }

        
        public string ImplementationDetails { get; set; }

        public DependentDetails DependentDetails { get; set; }
    }

    public class DependentDetails
    {
        
        public List<string> Modules { get; set; }

        
        public List<string> Services { get; set; }
    }

}

