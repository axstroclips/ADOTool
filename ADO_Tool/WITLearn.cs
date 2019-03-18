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
                document.Add(new JsonPatchOperation()
                {
                    Operation = Operation.Add,
                    Path = "/fields/System.History",
                    Value = "Changed by ADO Tool"
                });
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

        public IEnumerable<WorkItem> GetWorkItemsByQuery(List<WITFieldEntity> entities)
        {
            StringBuilder queryText = new StringBuilder();
            queryText.AppendLine("Select * From WorkItems ");
            if (entities.Count >= 1)
                queryText.AppendLine(string.Format(" Where [{0}]='{1}' ", entities[0].FieldName, entities[0].FieldValue));
            if(entities.Count>=2)
            {
                for(int i=1; i<entities.Count; i++)
                {
                    queryText.AppendLine(string.Format(" And [{0}]='{1}'", entities[i].FieldName, entities[i].FieldValue));
                }
            }

            string query = queryText.ToString();
            WorkItemQueryResult queryResult = ExecuteByWiql(query);

            if(queryResult.WorkItems.Count()==0)
            {
                return new List<WorkItem>();
            }
            else
            {
                int[] workItemIds = queryResult.WorkItems.Select<WorkItemReference, int>(wif => { return wif.Id; }).ToArray();

                string[] fields = new[]
                {
                    "System.WorkItemType",
                    "System.Id",
                    "System.Title",
                    "System.State",
                    "System.AssignedTo"
                };

                var witClient = this.Connection.GetClient<WorkItemTrackingHttpClient>();
                IEnumerable<WorkItem> workItems = witClient.GetWorkItemsAsync(
                    workItemIds,
                    fields,
                    queryResult.AsOf).Result;

                // Log to CSV
                if(workItems!=null)
                {
                    StringBuilder sbText = new StringBuilder();
                    if (workItems.Count() != 0)
                    {
                        Console.WriteLine("ID, Work Item Type, Title, State, Assign To");
                        sbText.AppendLine("ID, Work Item Type, Title, State, Assign To");
                        foreach (WorkItem item in workItems)
                        {
                            if(!item.Fields.ContainsKey("System.AssignedTo"))
                            {
                                item.Fields["System.AssignedTo"] = "UnAssigned";
                            }
                            Console.WriteLine(item.Id + ", "  + item.Fields["System.WorkItemType"] + ", " + item.Fields["System.Title"].ToString().Replace(",", ";") + ", " + item.Fields["System.State"] + ", " + item.Fields["System.AssignedTo"]);
                            sbText.AppendLine(item.Id + ", "  + item.Fields["System.WorkItemType"] + ", " + item.Fields["System.Title"].ToString().Replace(",", ";") + ", " + item.Fields["System.State"] + ", " + item.Fields["System.AssignedTo"]);
                        }
                    }

                    using (System.IO.StreamWriter writer = new System.IO.StreamWriter("QueryResults.csv"))
                    {
                        writer.Write(sbText.ToString());
                        writer.Flush();
                    }
                }
                return workItems;
            }
        }
    }
}
