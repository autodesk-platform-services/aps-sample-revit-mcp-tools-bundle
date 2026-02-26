using Autodesk.Revit.DB;
using DesignAutomationFramework;
using Newtonsoft.Json;
using RevitMcpTools.Utils;

namespace RevitMcpTools.Tools
{
    /// <summary>
    /// Tool to add and remove Revit links from a cloud model
    /// </summary>
    [RevitMcpTool("link_models")]
    public class LinkModelsTool : IRevitMcpTool
    {
        public bool Execute(DesignAutomationData data, Document doc)
        {
            try
            {
                ArgumentNullException.ThrowIfNull(data);
                ArgumentNullException.ThrowIfNull(doc);

                string inputJsonPath = "toolinputs.json";
                if (!File.Exists(inputJsonPath))
                {
                    Console.WriteLine($"*** Input JSON file not found: {inputJsonPath} ***");
                    return false;
                }

                var inputData = LinkModelsInputData.Parse(inputJsonPath);
                if (inputData == null)
                {
                    Console.WriteLine("*** Failed to deserialize input JSON! ***");
                    return false;
                }

                Console.WriteLine($"*** Links to add: {inputData.LinksToAdd.Count} ***");
                Console.WriteLine($"*** Links to remove: {inputData.LinksToRemove.Count} ***");

                // Working document is already opened by the toolbox
                Transaction t = new(doc, "Manage Revit Links");
                t.Start();

                int addedCount = 0;
                int removedCount = 0;
                int failedCount = 0;

                // Remove links first
                if (inputData.LinksToRemove != null && inputData.LinksToRemove.Count > 0)
                {
                    Console.WriteLine($"*** Removing {inputData.LinksToRemove.Count} link(s)... ***");

                    foreach (var linkToRemove in inputData.LinksToRemove)
                    {
                        try
                        {
                            Console.WriteLine($"*** Attempting to remove link: {linkToRemove.ModelName} ***");

                            bool linkRemoved = RemoveLink(doc, linkToRemove.ModelName);

                            if (linkRemoved)
                            {
                                Console.WriteLine($"*** Successfully removed link: {linkToRemove.ModelName} ***");
                                removedCount++;
                            }
                            else
                            {
                                Console.WriteLine($"*** Link not found or already removed: {linkToRemove.ModelName} ***");
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"*** Failed to remove link '{linkToRemove.ModelName}': {ex.Message} ***");
                            failedCount++;
                        }
                    }
                }

                // Add links
                if (inputData.LinksToAdd != null && inputData.LinksToAdd.Count > 0)
                {
                    Console.WriteLine($"*** Adding {inputData.LinksToAdd.Count} link(s)... ***");
                    RevitLinkOptions linkOptions = new(false);

                    foreach (var linkToAdd in inputData.LinksToAdd)
                    {
                        try
                        {
                            Console.WriteLine($"*** Adding link: {linkToAdd.ModelName} (GUID: {linkToAdd.ModelGuid}) ***");

                            ModelPath linkPath = ModelPathUtils.ConvertCloudGUIDsToCloudPath(
                                inputData.Region,
                                Guid.Parse(inputData.ProjectGuid),
                                Guid.Parse(linkToAdd.ModelGuid));

                            var linkType = RevitLinkType.Create(doc, linkPath, linkOptions);

                            if (linkType.LoadResult != LinkLoadResultType.LinkLoaded)
                            {
                                Console.WriteLine($"*** Failed to load link '{linkToAdd.ModelName}': {linkType.LoadResult} ***");
                                failedCount++;
                                continue;
                            }

                            RevitLinkInstance linkInstance = RevitLinkInstance.Create(doc, linkType.ElementId, ImportPlacement.Shared);

                            if (linkInstance == null)
                            {
                                Console.WriteLine($"*** Failed to create link instance for '{linkToAdd.ModelName}' ***");
                                failedCount++;
                                continue;
                            }

                            Console.WriteLine($"*** Successfully added link: {linkToAdd.ModelName} ***");
                            addedCount++;
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"*** Failed to add link '{linkToAdd.ModelName}': {ex.Message} ***");
                            failedCount++;
                        }
                    }
                }

                t.Commit();

                Console.WriteLine($"*** Link management complete. Added: {addedCount}, Removed: {removedCount}, Failed: {failedCount} ***");

                // Note: Model saving is now handled by the toolbox based on revitmodel.json
                return failedCount == 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"*** Exception in LinkModelsTool: {ex} ***");
                return false;
            }
        }

        private bool RemoveLink(Document doc, string linkName)
        {
            RevitLinkType linkType = new FilteredElementCollector(doc)
                .OfClass(typeof(RevitLinkType))
                .Cast<RevitLinkType>()
                .FirstOrDefault(x => x.Name.Contains(linkName));

            if (linkType == null)
            {
                return false;
            }

            Console.WriteLine($"*** Found link with name: {linkType.Name}, deleting... ***");

            // Deleting the link type automatically deletes all instances
            doc.Delete(linkType.Id);

            return true;
        }

        private class LinkModelsInputData
        {
            [JsonProperty(PropertyName = "region")]
            public string Region { get; set; }

            [JsonProperty(PropertyName = "projectGuid")]
            public string ProjectGuid { get; set; }

            [JsonProperty(PropertyName = "linksToAdd")]
            public List<LinkInfo> LinksToAdd { get; set; } = new();

            [JsonProperty(PropertyName = "linksToRemove")]
            public List<LinkInfo> LinksToRemove { get; set; } = new();

            public static LinkModelsInputData Parse(string jsonPath)
            {
                try
                {
                    string jsonContents = File.ReadAllText(jsonPath);
                    return JsonConvert.DeserializeObject<LinkModelsInputData>(jsonContents);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"*** Exception when parsing json file: {ex} ***");
                    return null;
                }
            }
        }

        private class LinkInfo
        {
            [JsonProperty(PropertyName = "modelName")]
            public string ModelName { get; set; }

            [JsonProperty(PropertyName = "modelGuid")]
            public string ModelGuid { get; set; }
        }
    }
}
