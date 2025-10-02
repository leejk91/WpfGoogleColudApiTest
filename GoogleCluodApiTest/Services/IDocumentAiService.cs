using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GoogleCluodApiTest.Models;

namespace GoogleCluodApiTest.Services
{
    public interface IDocumentAiService
    {
        Task<DocAiResult> ProcessAsync(string filePath, CancellationToken ct = default);
    }
}
