using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ADO_Tool
{
    public class ProgramRun
    {
        GetInput _getInput = null;
        public ProgramRun()
        {
            _getInput = new GetInput();
        }

        public void Run()
        {
            if (_getInput != null)
            {
                WITLearn witLearn = new WITLearn(_getInput);
                if (_getInput.Operation.Operation == OperationType.Add)
                {
                    var level1WorkItemResult = witLearn.CreateLearningPathUsingClientLib();
                    List<WorkItem> level2WorkItemList = new List<WorkItem>();
                    // If there are level 2 work items need be create
                    if(_getInput.Operation.LearningPathModulesInfo!=null)
                    {
                        int level2WorkItemCount = _getInput.Operation.LearningPathModulesInfo.Length;
                        for(int i=0; i<level2WorkItemCount; i++)
                        {
                            var level2WorkItemResult = witLearn.CreateModuleUsingClientLib(i);
                            level2WorkItemList.Add(level2WorkItemResult);

                            // If there are level 3 work items need be create
                            if(_getInput.Operation.LearningPathModulesInfo[i]!=null)
                            {
                                int level3WorkItemCount = _getInput.Operation.LearningPathModulesInfo[i].Length;
                                for(int j=0; j<level3WorkItemCount; j++)
                                {
                                    var level3WorkItemResult = witLearn.CreateUnitUsingClientLib(j);

                                    // Link the level 2 work item to it's descendants level 3 work items
                                    witLearn.LinkToOtherWorkItem(Int32.Parse(level2WorkItemResult.Id.ToString()), Int32.Parse(level3WorkItemResult.Id.ToString()));
                                }
                            }
                        }
                    }

                    // Link the level 1 work item to it's deceding level 2 work items
                    foreach (WorkItem level2Item in level2WorkItemList)
                    {
                        witLearn.LinkToOtherWorkItem(Int32.Parse(level1WorkItemResult.Id.ToString()), Int32.Parse(level2Item.Id.ToString()));
                    }
                }
                else if(_getInput.Operation.Operation == OperationType.Update)
                {
                    if(_getInput.Operation.WorkItemIDsToUpdate!=null)
                    {
                        foreach (int workItemId in _getInput.Operation.WorkItemIDsToUpdate)
                        {
                            if(_getInput.Operation.IsCascadeUpdating)
                            {
                                // Get descendants work items of the workItemId
                                List<int> childWorkItemIds = new List<int>();
                                witLearn.GetChildWorkItemByParentWorkItemId(workItemId, ref childWorkItemIds);

                                foreach (int childId in childWorkItemIds)
                                {
                                    witLearn.UpdateWorkItemUsingClientLib(childId);
                                }
                            }
                            // update the work item
                            witLearn.UpdateWorkItemUsingClientLib(workItemId);
                        }
                        
                    }
                }
                else if(_getInput.Operation.Operation == OperationType.Delete)
                {
                    if(_getInput.Operation.WorkItemIDsToDelete!=null)
                    {
                        foreach (int workItemId in _getInput.Operation.WorkItemIDsToDelete)
                        {
                            
                            if (_getInput.Operation.IsCascadeDeleting)
                            {
                                // Get descendants work items of the workItemId
                                List<int> childWorkItemIds = new List<int>();
                                witLearn.GetChildWorkItemByParentWorkItemId(workItemId, ref childWorkItemIds);

                                foreach (int childId in childWorkItemIds)
                                {
                                    witLearn.DeleteWITByID(childId);
                                }
                            }

                            // delete the work item
                            witLearn.DeleteWITByID(workItemId);
                        }
                    }
                }
            }
        }
    }

    public enum OperationType
    {
        Add,
        Update,
        Delete,
        Query,
        QueryUpdate
    }

    public class OperationInformation
    {
        public OperationType Operation { get; set; }
        public string LearningPathDisplayName { get; set; }
        public int[][] LearningPathModulesInfo { get; set; }
        public List<int> WorkItemIDsToDelete { get; set; }
        public List<int> WorkItemIDsToUpdate { get; set; }
        public bool IsCascadeDeleting { get; set; }
        public bool IsCascadeUpdating { get; set; }
    }
}
