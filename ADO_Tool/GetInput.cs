using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using CsvHelper;

namespace ADO_Tool
{
    public class GetInput
    {
        string _learningPathFile = string.Empty;
        string _metadataFile = string.Empty;

        public string _learningPathName = string.Empty;
        public int[][] _modules;

        private List<WITFieldEntity> _witFieldRecords;
        public List<WITFieldEntity> WITFieldRecords {
            get
            {
                if(_witFieldRecords==null)
                {
                    this.ReadCommonMetadataInformation();
                }
                return _witFieldRecords;
            }
            private set { }
        }

        private OperationInformation _operation;
        public OperationInformation Operation
        {
            get
            {
                if(_operation==null)
                {
                    this.ReadLearningPathInformation();
                }

                return _operation;
            }
            private set { }
        }
        public Dictionary<string, string> Properties { get; set; } = new Dictionary<string, string>();
        public void SetValue(string key, string value)
        {
            Properties[key] = value;
        }

        public string GetValue(string key)
        {
            return Properties[key];
        }

        public void RemoveValue(string key)
        {
            Properties.Remove(key);
        }
        public GetInput()
        {
            var appSettings = ConfigurationManager.AppSettings;
            if (appSettings.Count == 0)
            {
                Debug.WriteLine("appSettings is empty.");
            }
            else
            {
                foreach (var key in appSettings.AllKeys)
                {
                    if (key.ToString() == "LearningPathFile")
                    {
                        _learningPathFile = appSettings[key];
                    }
                    else if (key.ToString() == "MetadataFile")
                    {
                        _metadataFile = appSettings[key];
                    }
                    else if (key.ToString() == "WITypeLevel1")
                    {
                        this.SetValue("WITypeLevel1", appSettings[key]);
                    }
                    else if(key.ToString()== "WITypeLevel2")
                    {
                        this.SetValue("WITypeLevel2", appSettings[key]);
                    }
                    else if(key.ToString()== "WITypeLevel3")
                    {
                        this.SetValue("WITypeLevel3", appSettings[key]);
                    }
                }
            }
        }
        private int ReadLearningPathInformation()
        {
            string csvDelimiter = ",";

            if (!File.Exists(_learningPathFile))
            {
                Console.WriteLine("Failed to read input file - {0}", _learningPathFile);
                WriteLog.WriteLogLine("Failed to read input file - {0}", _learningPathFile);
                return 0;
            }

            _operation = new OperationInformation();

            using (StreamReader reader = new StreamReader(_learningPathFile))
            {
                string csvContent = reader.ReadToEnd();
                if (!string.IsNullOrEmpty(csvContent))
                {
                    string[] csvLines = csvContent.Split(new string[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
                    string[] parameters = csvLines[0].Split(new string[] { csvDelimiter }, StringSplitOptions.RemoveEmptyEntries);
                    if (parameters.Length > 0)
                    {
                        string operation = parameters[0];

                        switch (operation.Trim().ToLower())
                        {
                            case "a":
                            case "add":
                                {

                                    int colCount = parameters.Length;
                                    if (colCount == 2)
                                    {
                                        // Add a single Learning Path only
                                        _operation.Operation = OperationType.Add;
                                        _operation.LearningPathDisplayName = parameters[1];
                                        _operation.LearningPathModulesInfo = null;
                                    }
                                    else if (colCount >= 3)
                                    {
                                        int moduleCount = 0;
                                        bool parseModuleCountSuccess = Int32.TryParse(parameters[2], out moduleCount);
                                        if (!parseModuleCountSuccess)
                                        {
                                            Console.WriteLine("Invalid input file - {0}, the module count(third column) for 'Add' should be an integer.", _learningPathFile);
                                            WriteLog.WriteLogLine("Invalid input file - {0}, the module count(third column) for 'Add' should be an integer.", _learningPathFile);
                                            return 0;
                                        }
                                        if (colCount == 3)
                                        {
                                            // Add a learning path with modules only
                                            _operation.Operation = OperationType.Add;
                                            _operation.LearningPathDisplayName = parameters[1];
                                            _operation.LearningPathModulesInfo = new int[moduleCount][];
                                            for (int i = 0; i < moduleCount; i++)
                                            {
                                                _operation.LearningPathModulesInfo[i] = null;
                                            }
                                        }
                                        else
                                        {
                                            // Add a learning path with modules, with units
                                            if (moduleCount + 3 == colCount)
                                            {
                                                bool passValidation = true;
                                                for (int i = 0; i < moduleCount; i++)
                                                {
                                                    // Match 1.20
                                                    string pattern = @"\d*[.]\d*";
                                                    bool isMatch = Regex.IsMatch(parameters[i + 3], pattern);
                                                    if (!isMatch)
                                                    {
                                                        passValidation = false;
                                                        Console.WriteLine("Invalid input file - {0}, parameter {1} does not match parttern {digits}.{digits}.", _learningPathFile, parameters[i + 3]);
                                                        WriteLog.WriteLogLine("Invalid input file - {0}, parameter {1} does not match parttern {digits}.{digits}.", _learningPathFile, parameters[i + 3]);
                                                        return 0;
                                                    }
                                                }
                                                if (passValidation)
                                                {
                                                    _operation.Operation = OperationType.Add;
                                                    _operation.LearningPathDisplayName = parameters[1];
                                                    _operation.LearningPathModulesInfo = new int[moduleCount][];
                                                    for (int i = 0; i < moduleCount; i++)
                                                    {
                                                        string[] digits = parameters[i + 3].Split(new string[] { "." }, StringSplitOptions.RemoveEmptyEntries);
                                                        _operation.LearningPathModulesInfo[i] = new int[Int32.Parse(digits[1])];
                                                    }
                                                }
                                            }
                                        }
                                    }
                                    else
                                    {
                                        Console.WriteLine("Invalid input file - {0}, at least 2 input values are required for 'Add'.", _learningPathFile);
                                        WriteLog.WriteLogLine("Invalid input file - {0}, at least 2 input values are required for 'Add'.", _learningPathFile);
                                        return 0;
                                    }
                                }; break;
                            case "u":
                            case "update":
                                {
                                    int colCount = parameters.Length;
                                    if (colCount >=3)
                                    {
                                        _operation.Operation = OperationType.Update;
                                        _operation.IsCascadeUpdating = false;
                                        _operation.WorkItemIDsToUpdate = new List<int>();
                                        var isCascadingUpdate = parameters[1].Trim().ToLower();
                                        if(isCascadingUpdate=="false"|| isCascadingUpdate=="true")
                                        {
                                            _operation.IsCascadeUpdating = Boolean.Parse(isCascadingUpdate);
                                        }

                                        for(int i=2; i<colCount; i++)
                                        {
                                            int theId = 0;
                                            Int32.TryParse(parameters[i], out theId);

                                            //add to list
                                            if (theId != 0)
                                                _operation.WorkItemIDsToUpdate.Add(theId);
                                        }
                                    }
                                    else
                                    {
                                        Console.WriteLine("Invalid input file - {0}, at least 3 input values are required for 'Update'.", _learningPathFile);
                                        WriteLog.WriteLogLine("Invalid input file - {0}, at least 3 input values are required for 'Update'.", _learningPathFile);
                                        return 0;
                                    }
                                }; break;
                            case "d":
                            case "delete":
                                {
                                    int colCount = parameters.Length;
                                    if(colCount>=3)
                                    {
                                        _operation.Operation = OperationType.Delete;
                                        _operation.IsCascadeDeleting = false;
                                        _operation.WorkItemIDsToDelete = new List<int>();
                                        var isCascadingDelete = parameters[1].Trim().ToLower();
                                        if (isCascadingDelete == "false"|| isCascadingDelete == "true")
                                        {
                                            _operation.IsCascadeDeleting = Boolean.Parse(isCascadingDelete);
                                        }
                                        for(int i=2; i<colCount; i++)
                                        {
                                            int theId = 0;
                                            Int32.TryParse(parameters[i], out theId);

                                            //add to list
                                            if(theId!=0)
                                                _operation.WorkItemIDsToDelete.Add(theId);
                                        }
                                    }
                                    else
                                    {
                                        Console.WriteLine("Invalid input file - {0}, at least 3 input values are required for 'Delete'.", _learningPathFile);
                                        WriteLog.WriteLogLine("Invalid input file - {0}, at least 3 input values are required for 'Delete'.", _learningPathFile);
                                    }
                                }; break;
                            case "q":
                            case "query":
                                {

                                }; break;
                            case "qu":
                            case "queryupdate":
                                {

                                }; break;
                            default:
                                {
                                    Console.WriteLine("Invalid input file - {0}, operation '{1}' is not support yet.", _learningPathFile, operation);
                                    WriteLog.WriteLogLine("Invalid input file - {0}, operation '{1}' is not support yet.", _learningPathFile, operation);
                                    return 0;
                                };
                        }
                    }

                }
                else
                {
                    Console.WriteLine("Invalid input file - {0}, content is empty.", _learningPathFile);
                    WriteLog.WriteLogLine("Invalid input file - {0}, content is empty.", _learningPathFile);
                    return 0;
                }
            }

            return 0;
        }
        private int ReadCommonMetadataInformation()
        {
            if (!File.Exists(_metadataFile))
            {
                Console.WriteLine("Failed to read metadata file - {0}", _metadataFile);
                WriteLog.WriteLogLine("Failed to read metadata file - {0}", _metadataFile);
                return 0;
            }
            using (StreamReader reader = new StreamReader(_metadataFile))
            using (CsvReader csvReader = new CsvReader(reader))
            {
                csvReader.Configuration.Delimiter = "::";
                csvReader.Configuration.HasHeaderRecord = false;

                _witFieldRecords = new List<WITFieldEntity>();
                while (csvReader.Read())
                {
                    var record = new WITFieldEntity
                    {
                        WorkItemType = csvReader.GetField<string>(0),
                        FieldName = csvReader.GetField<string>(1),
                        FieldValue = csvReader.GetField<string>(2)
                    };

                    _witFieldRecords.Add(record);
                }

                return 1;
            }
        }
    }
}
