using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;
using Microsoft.VisualStudio.Services.WebApi;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using Microsoft.VisualStudio.Services.WebApi.Patch.Json;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi;
using Microsoft.VisualStudio.Services.WebApi.Patch;

namespace ADO_Tool
{
    public class WITBase
    {
        public VssConnection Connection { get; private set; }
        public string Project { get; private set; }
        public WITBase()
        {
            string ado_url = string.Empty;
            string ado_pats = string.Empty;
            string ado_project = string.Empty;
            // Read the appsetings
            var appSettings = ConfigurationManager.AppSettings;
            if (appSettings.Count == 0)
            {
                Console.WriteLine("appSettings is empty.");
            }
            else
            {
                foreach (var key in appSettings.AllKeys)
                {
                    if (key.ToString() == "AzureDevOpsURI")
                    {
                        ado_url = appSettings[key];
                    }
                    else if (key.ToString() == "PATs")
                    {
                        ado_pats = appSettings[key];
                    }
                    else if (key.ToString() == "TeamProject")
                    {
                        ado_project = appSettings[key];
                    }
                }
            }

            // Setup the connection
            if (!string.IsNullOrEmpty(ado_url) && !string.IsNullOrEmpty(ado_pats) && !string.IsNullOrEmpty(ado_project))
            {
                VssBasicCredential credential = new VssBasicCredential(string.Empty, ado_pats);
                this.Connection = new VssConnection(new Uri(ado_url), credential);
                this.Project = ado_project;
            }
        }

        // WIT Operations
        /// <summary>
        /// Bypass rules incase there are required fields are not provided
        /// </summary>
        /// <param name="document">// Construct the object containing field values required for the new work item</param>
        /// <param name="WITypeName">Work Item Type Name: Learning Path / Module / Unit</param>
        /// <returns></returns>
        public WorkItem CreateWIT(JsonPatchDocument document, string WITypeName)
        {

            WorkItemTrackingHttpClient witHttpClient = this.Connection.GetClient<WorkItemTrackingHttpClient>();

            try
            {
                WorkItem result = witHttpClient.CreateWorkItemAsync(document, this.Project, WITypeName,bypassRules:true).Result;
                Console.WriteLine("{0} Successfully Created: # {1}", WITypeName, result.Id);
                WriteLog.WriteLogLine("{0} Successfully Created: # {1}", WITypeName, result.Id);
                return result;
            }
            catch (AggregateException ex)
            {
                Console.WriteLine("Error creating {0}: {1}", WITypeName, ex.InnerException.Message);
                WriteLog.WriteLogLine("Error creating {0}: {1}", WITypeName, ex.InnerException.Message);
                return null;
            }

        }

        /// <summary>
        /// Bypass rules incase there are required fields are not provided
        /// </summary>
        /// <param name="document">/ Construct the object containing field values required for updating work item</param>
        /// <param name="WIId">The specific work item Id need to update</param>
        /// <returns></returns>
        public WorkItem UpdateWIT(JsonPatchDocument document, int WIId)
        {
            WorkItemTrackingHttpClient witHttpClient = this.Connection.GetClient<WorkItemTrackingHttpClient>();

            try
            {
                WorkItem result = witHttpClient.UpdateWorkItemAsync(document, WIId,bypassRules:true).Result;
                Console.WriteLine("Successfully Updated Work Item: # {0}", result.Id);
                WriteLog.WriteLogLine("Successfully Updated Work Item: # {0}", result.Id);
                return result;
            }
            catch (AggregateException ex)
            {
                Console.WriteLine("Failed to Update Work Item: # {0} - Error Message: {1}", WIId, ex.InnerException.Message);
                WriteLog.WriteLogLine("Failed to Update Work Item: # {0} - Error Message: {1}", WIId, ex.InnerException.Message);
                return null;
            }

        }

        public WorkItem GetWITByID(int WIId)
        {
            WorkItemTrackingHttpClient witHttpClient = this.Connection.GetClient<WorkItemTrackingHttpClient>();

            try
            {
                WorkItem result = witHttpClient.GetWorkItemAsync(WIId,expand:WorkItemExpand.Links|WorkItemExpand.Relations).Result;
                return result;
            }
            catch(AggregateException ex)
            {
                Console.WriteLine("The work item id # {0} does not exist. Error - {1}",WIId,ex.InnerException.Message);
                WriteLog.WriteLogLine("The work item id # {0} does not exist. Error - {1}", WIId, ex.InnerException.Message);
                return null;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="WIId">The specific work item Id need to delete</param>
        /// <returns></returns>
        public WorkItemDelete DeleteWITByID(int WIId)
        {
            WorkItemTrackingHttpClient witHttpClient = this.Connection.GetClient<WorkItemTrackingHttpClient>();

            try
            {
                WorkItemDelete result = witHttpClient.DeleteWorkItemAsync(WIId, destroy: false).Result;
                Console.WriteLine("Successfully Deleted Work Item: # {0}", result.Id);
                WriteLog.WriteLogLine("Successfully Deleted Work Item: # {0}", result.Id);
                return result;
            }
            catch (AggregateException ex)
            {
                Console.WriteLine("Failed to delete Work Item: # {0} - Error Message: {1}", WIId, ex.InnerException.Message);
                WriteLog.WriteLogLine("Failed to delete Work Item: # {0} - Error Message: {1}", WIId, ex.InnerException.Message);
                return null;
            }

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sourceWIId">Parent work item</param>
        /// <param name="targetWIId">Child work item</param>
        /// <returns></returns>
        public WorkItem LinkToOtherWorkItem(int sourceWIId, int targetWIId)
        {
            WorkItemTrackingHttpClient workItemTrackingClient = this.Connection.GetClient<WorkItemTrackingHttpClient>();

            // Get work target work item
            WorkItem targetWorkItem = workItemTrackingClient.GetWorkItemAsync(targetWIId).Result;

            if (targetWorkItem == null)
            {
                Console.WriteLine("Tried to link two work items, but target work item # {0} does not exist.", targetWIId);
                WriteLog.WriteLogLine("Tried to link two work items, but target work item # {0} does not exist.", targetWIId);
                return null;
            }
            JsonPatchDocument patchDocument = new JsonPatchDocument();
            patchDocument.Add(
                new JsonPatchOperation()
                {
                    Operation = Operation.Add,
                    Path = "/relations/-",
                    Value = new
                    {
                        rel = "System.LinkTypes.Hierarchy-Forward",
                        url = targetWorkItem.Url,
                        attributes = new
                        {
                            comment = "Making a new link for the dependency"
                        }
                    }
                }
            );
            try
            {

                WorkItem result = workItemTrackingClient.UpdateWorkItemAsync(patchDocument, sourceWIId).Result;
                Console.WriteLine("Successfully linked source item # {0} to target item # {1}.", sourceWIId, targetWIId);
                WriteLog.WriteLogLine("Successfully linked source item # {0} to target item # {1}.", sourceWIId, targetWIId);
                return result;
            }
            catch (AggregateException ex)
            {
                Console.WriteLine("Failed to link source item # {0} to target item # {1}.", sourceWIId, targetWIId);
                WriteLog.WriteLogLine("Failed to link source item # {0} to target item # {1}.", sourceWIId, targetWIId);
                return null;
            }

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="parentId">this work item id</param>
        /// <param name="childIds">Return all the work item ids with child relation to this item</param>
        public void GetChildWorkItemByParentWorkItemId(int parentId, ref List<int> childIds)
        {
            WorkItemTrackingHttpClient witClient = this.Connection.GetClient<WorkItemTrackingHttpClient>();
            WorkItem parentWorkItem = null;

            try
            {
                parentWorkItem = witClient.GetWorkItemAsync(parentId, expand: WorkItemExpand.Relations).Result;
            }
            catch (AggregateException ex)
            {
                Console.WriteLine("The work item id # {0} does not exist. Error - {1}", parentId, ex.InnerException.Message);
                WriteLog.WriteLogLine("The work item id # {0} does not exist. Error - {1}", parentId, ex.InnerException.Message);
            }

            if (parentWorkItem!=null&&parentWorkItem.Relations != null)
            {
                foreach (var relation in parentWorkItem.Relations)
                {
                    //get the child links
                    if (relation.Rel == "System.LinkTypes.Hierarchy-Forward")
                    {
                        var lastIndex = relation.Url.LastIndexOf("/");
                        var itemId = relation.Url.Substring(lastIndex + 1);
                        childIds.Add(Convert.ToInt32(itemId));

                        GetChildWorkItemByParentWorkItemId(Convert.ToInt32(itemId), ref childIds);
                    };
                }
            }

        }

        /// <summary>
        /// Display all fields and relations of a specific work item
        /// </summary>
        /// <param name="wiId"></param>
        public void DisplayAllFieldsOfSpecificWorkItemById(int wiId)
        {
            WorkItem workItem = GetWITByID(wiId);
            foreach (var field in workItem.Fields)
            {
                Console.WriteLine(" {0}: {1}",field.Key, field.Value);
            }
            if(workItem.Relations!=null)
            {
                foreach (var relation in workItem.Relations)
                {
                    Console.WriteLine("Relation {0}: {1}",relation.Rel, relation.Url);
                }
            }
        }
    }
}
