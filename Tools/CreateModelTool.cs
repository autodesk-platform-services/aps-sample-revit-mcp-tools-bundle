using Autodesk.Revit.DB;
using DesignAutomationFramework;
using Newtonsoft.Json;
using RevitMcpTools.Utils;

namespace RevitMcpTools.Tools
{
    /// <summary>
    /// Tool to create a single Revit cloud model from a template
    /// </summary>
    [RevitMcpTool("create_model")]
    public class CreateModelTool : IRevitMcpTool
    {
        public bool Execute(DesignAutomationData data, Document doc)
        {            
            try
            {
                ArgumentNullException.ThrowIfNull(data);
                ArgumentNullException.ThrowIfNull(data.RevitApp);
                ArgumentNullException.ThrowIfNull(doc);

                string inputJsonPath = "toolinputs.json";
                if (!File.Exists(inputJsonPath))
                {
                    Console.WriteLine($"*** Input JSON file not found: {inputJsonPath} ***");
                    return false;
                }

                var inputData = SingleModelInputData.Parse(inputJsonPath);
                if (inputData == null)
                {
                    Console.WriteLine("*** Failed to deserialize input JSON! ***");
                    return false;
                }

                Console.WriteLine($"*** Creating model: {inputData.ModelName} from template: {doc.Title} ***");

                // Save template as local file
                string templateFilePath = @"template.rvt";
                doc.SaveAs(templateFilePath);

                // Open the local template file
                Document newDoc = data.RevitApp.OpenDocumentFile(templateFilePath);
                if (newDoc == null)
                {
                    Console.WriteLine($"*** Failed to open template file: {templateFilePath} ***");
                    return false;
                }

                if (inputData.EnableWorksharing)
                {
                    Console.WriteLine("*** Enabling worksharing on the model. ***");
                    newDoc.EnableWorksharing("Shared Levels and Grids", "Workset1");
                }

                newDoc.SaveAsCloudModel(inputData.AccountId, inputData.ProjectId, inputData.FolderId, inputData.ModelName);
                Console.WriteLine($"*** Successfully created model: {inputData.ModelName} ***");

                doc = newDoc;

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"*** Exception in CreateModelTool: {ex} ***");
                return false;
            }
        }

        private class SingleModelInputData
        {
            [JsonProperty(PropertyName = "enableWorksharing", Required = Required.Default)]
            public bool EnableWorksharing { get; set; } = true;

            [JsonProperty(PropertyName = "accountId", Required = Required.Always)]
            public Guid AccountId { get; set; }

            [JsonProperty(PropertyName = "projectId", Required = Required.Always)]
            public Guid ProjectId { get; set; }

            [JsonProperty(PropertyName = "folderId", Required = Required.Always)]
            public string FolderId { get; set; }

            [JsonProperty(PropertyName = "modelName", Required = Required.Always)]
            public string ModelName { get; set; }

            public static SingleModelInputData Parse(string jsonPath)
            {
                try
                {
                    string jsonContents = File.ReadAllText(jsonPath);
                    return JsonConvert.DeserializeObject<SingleModelInputData>(jsonContents);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"*** Exception when parsing json file: {ex} ***");
                    return null;
                }
            }
        }
    }
}
