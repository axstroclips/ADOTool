using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;
using System.Diagnostics;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.WebApi;
using Microsoft.VisualStudio.Services.WebApi.Patch.Json;
using Microsoft.VisualStudio.Services.WebApi.Patch;
using System.IO;

namespace ADO_Tool
{
    class Program
    {
        static void Main(string[] args)
        {
            ProgramRun run = new ProgramRun();
            run.Run();

            Console.WriteLine("All Finished!");
            Console.ReadLine();
        }
    }
}
