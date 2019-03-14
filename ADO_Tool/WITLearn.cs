using Microsoft.TeamFoundation.WorkItemTracking.WebApi;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.WebApi;
using Microsoft.VisualStudio.Services.WebApi.Patch;
using Microsoft.VisualStudio.Services.WebApi.Patch.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ADO_Tool
{
    public class WITLearn:WITBase
    {
        GetInput _getInput = null;
        public WITLearn(GetInput input)
        {
            // Read Input: Operation and Fields Value list
            _getInput = input;
        }

        public WorkItem CreateLearningPathUsingClientLib()
        {
            // Call WITBase CreateWIT to create level 1 work item type
            var WorkItemType = _getInput.GetValue("WITypeLevel1");
            if (WorkItemType != null)
            {
                var jsonPatchDocument = ConstructJsonPatchDocument(WorkItemType);
                // Add the Title which is required
                if(_getInput.Operation!=null)
                {
                    jsonPatchDocument.Add(new JsonPatchOperation()
                    {
                        Operation = Operation.Add,
                        Path = "/fields/System.Title",
                        Value = WorkItemType+": "+ _getInput.Operation.LearningPathDisplayName
                    });
                }
                return CreateWIT(jsonPatchDocument, WorkItemType);
            }
            return null;                      
        }

        public WorkItem CreateModuleUsingClientLib(int order)
        {
            // Call WITBase CreateWIT to create level 2 work item type
            var WorkItemType = _getInput.GetValue("WITypeLevel2");
            if (WorkItemType != null)
            {
                var jsonPatchDocument = ConstructJsonPatchDocument(WorkItemType);
                // Add the Title which is required
                if (_getInput.Operation != null)
                {
                    jsonPatchDocument.Add(new JsonPatchOperation()
                    {
                        Operation = Operation.Add,
                        Path = "/fields/System.Title",
                        Value = _getInput.Operation.LearningPathDisplayName + " - " + WorkItemType + " " + order
                    });
                }
                return CreateWIT(jsonPatchDocument, WorkItemType);
            }
            return null;
        }

        public WorkItem CreateUnitUsingClientLib(int order)
        {
            // Call WITBase CreateWIT to create level 3 work item type
            var WorkItemType = _getInput.GetValue("WITypeLevel3");
            if (WorkItemType != null)
            {
                var jsonPatchDocument = ConstructJsonPatchDocument(WorkItemType);
                // Add the Title which is required
                if (_getInput.Operation != null)
                {
                    jsonPatchDocument.Add(new JsonPatchOperation()
                    {
                        Operation = Operation.Add,
                        Path = "/fields/System.Title",
                        Value = _getInput.Operation.LearningPathDisplayName + " - " + WorkItemType + " " + order
                    });
                }
                return CreateWIT(jsonPatchDocument, WorkItemType);
            }
            return null;
        } 

        public void UpdateWorkItemUsingClientLib(int workItemId)
        {
            WorkItem workItem = GetWITByID(workItemId);
            if (workItem != null)
            {
                var workItemTypeName = workItem.Fields["System.WorkItemType"].ToString();
                JsonPatchDocument document = ConstructJsonPatchDocument(workItemTypeName);
                UpdateWIT(document, workItemId);
            }
        }

        /// <summary>
        /// Construct JsonPatchDocument from input FieldValue file per work item type
        /// </summary>
        /// <param name="wiType">Work Item Type: Learning Path, Module, Unit</param>
        /// <returns></returns>
        public JsonPatchDocument ConstructJsonPatchDocument(string wiType)
        {
            JsonPatchDocument document = new JsonPatchDocument();

            // Read input file to get the fields and values in a list format
            if(_getInput.WITFieldRecords!=null)
            {
                foreach (WITFieldEntity item in _getInput.WITFieldRecords)
                {
                    if(item.WorkItemType.Trim().ToLower()==wiType.Trim().ToLower())
                    {
                        document.Add(new JsonPatchOperation()
                        {
                            Operation = Operation.Add,
                            Path = string.Format("/fields/{0}",item.FieldName),
                            Value=item.FieldValue
                        });
                    }
                }
            }      
            

            return document;
        }
    }
}
