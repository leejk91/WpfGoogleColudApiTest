using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoogleCluodApiTest.Models
{
    public class DocAiResult
    {
        public string? Text { get; set; }
        public string? RawJson { get; set; }
        public string? Summary { get; set; }
        public int PageCount { get; set; }
        public string? MimeType { get; set; }
        public string? FileName { get; set; }
    }
}
