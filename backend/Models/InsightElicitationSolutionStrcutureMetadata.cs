using System.Text.Json;
using System.Text.Json.Serialization;

namespace backend.Models
{
    public class InsightElicitationSolutionStrcutureMetadata
    {
        public List<Section> Section { get; set; }
    }
   
    public class Section
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Content { get; set; }
        public List<Module> Modules { get; set; }
    }

    public class Module
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Type { get; set; }
        public string Content { get; set; }
        public string ParentName { get; set; }
        public List<Submodule> Submodules { get; set; }
    }

    public class Submodule
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Type { get; set; }
        public string Content { get; set; }
        public string ParentName { get; set; }
        public List<Submodule> Submodules { get; set; }
    }

    

}
