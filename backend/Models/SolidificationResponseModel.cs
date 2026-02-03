using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace backend.Models
{
    public class SolidificationResponseModel
    {
        public string? SolidificationOutput { get; set; }
        public List<string> SelectedTabs { get; set; }
        public int? id { get; set; }


    }
}