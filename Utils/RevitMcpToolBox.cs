using System.Diagnostics;
using Autodesk.Revit.DB;
using DesignAutomationFramework;

namespace RevitMcpTools.Utils
{
    /// <summary>
    /// Contains and executes Revit MCP tools on a model
    /// </summary>
    public class RevitMcpToolBox
    {
        private readonly Dictionary<string, Func<IRevitMcpTool>> _toolRegistry;
        private readonly DesignAutomationData _data;

        internal RevitMcpToolBox(Dictionary<string, Func<IRevitMcpTool>> toolRegistry, DesignAutomationData data)
        {
            _toolRegistry = toolRegistry ?? throw new ArgumentNullException(nameof(toolRegistry));
            _data = data ?? throw new ArgumentNullException(nameof(data));
        }

        /// <summary>
        /// Creates a new RevitMcpToolBoxBuilder for configuring tools
        /// </summary>
        /// <param name="data">Automation API data</param>
        /// <returns>A new RevitMcpToolBoxBuilder instance</returns>
        public static RevitMcpToolBoxBuilder CreateBuilder(DesignAutomationData data)
        {
            return new RevitMcpToolBoxBuilder(data);
        }

        /// <summary>
        /// Opens the model, executes the tool, and saves if configured
        /// </summary>
        /// <param name="modelConfigPath">Path to the revitmodel.json configuration file</param>
        /// <returns>True if the tool executed successfully, false otherwise</returns>
        public bool Execute(string modelConfigPath = "revitmodel.json")
        {
            Document doc = null;
            try
            {
                // 1. Load model configuration
                Console.WriteLine($"*** Loading model configuration from: {modelConfigPath} ***");
                var modelConfig = ModelConfiguration.Parse(modelConfigPath);
                if (modelConfig == null)
                {
                    Console.WriteLine("*** Failed to parse model configuration. ***");
                    return false;
                }

                Console.WriteLine($"*** Model - Region: {modelConfig.Region}, ProjectGuid: {modelConfig.ProjectGuid}, ModelGuid: {modelConfig.ModelGuid} ***");
                Console.WriteLine($"*** Tool requested: {modelConfig.ToolName} ***");
                Console.WriteLine($"*** Open option requested: {modelConfig.OpenOption} ***");

                // 2. Open the model
                var cloudModelPath = ModelPathUtils.ConvertCloudGUIDsToCloudPath(
                    modelConfig.Region,
                    modelConfig.ProjectGuid,
                    modelConfig.ModelGuid);

                Console.WriteLine("*** Opening Revit Cloud Model... ***");

                switch (modelConfig.OpenOption)
                {
                    case OpenOption.CloseAllWorksets:
                        {
                            var openOptions = new OpenOptions();
                            openOptions.SetOpenWorksetsConfiguration(new WorksetConfiguration(WorksetConfigurationOption.CloseAllWorksets));
                            Console.WriteLine("*** Opening with all worksets closed. ***");

                            Stopwatch closeAllStopwatch = Stopwatch.StartNew();
                            doc = _data.RevitApp.OpenDocumentFile(cloudModelPath, openOptions);
                            closeAllStopwatch.Stop();

                            Console.WriteLine($"*** Open with CloseAllWorksets completed in {closeAllStopwatch.Elapsed.TotalSeconds:F2} seconds. ***");
                            break;
                        }
                    case OpenOption.CloseWorksetsWithRevitLinks:
                        {
                            Stopwatch closeLinksTotalStopwatch = Stopwatch.StartNew();

                            // First open: all worksets closed
                            var firstOpenOptions = new OpenOptions();
                            firstOpenOptions.SetOpenWorksetsConfiguration(new WorksetConfiguration(WorksetConfigurationOption.CloseAllWorksets));
                            Console.WriteLine("*** First open with all worksets closed. ***");

                            Stopwatch firstOpenStopwatch = Stopwatch.StartNew();
                            doc = _data.RevitApp.OpenDocumentFile(cloudModelPath, firstOpenOptions);
                            firstOpenStopwatch.Stop();

                            if (doc == null)
                            {
                                Console.WriteLine("*** Failed to open model (first pass). ***");
                                return false;
                            }

                            Console.WriteLine($"*** First open completed in {firstOpenStopwatch.Elapsed.TotalSeconds:F2} seconds. ***");

                            var allUserWorksets = new FilteredWorksetCollector(doc)
                                .OfKind(WorksetKind.UserWorkset)
                                .ToWorksets()
                                .ToList();

                            var linkWorksetIds = new HashSet<int>();
                            var linkTypes = new FilteredElementCollector(doc)
                                .OfClass(typeof(RevitLinkType))
                                .Cast<RevitLinkType>()
                                .ToList();

                            Console.WriteLine($"*** Revit link types found: {linkTypes.Count} ***");

                            foreach (var linkType in linkTypes)
                            {
                                int worksetId = linkType.get_Parameter(BuiltInParameter.ELEM_PARTITION_PARAM)?.AsInteger() ?? -1;
                                linkWorksetIds.Add(worksetId);

                                string worksetName = allUserWorksets.FirstOrDefault(ws => ws.Id.IntegerValue == worksetId)?.Name
                                    ?? $"Id {worksetId}";

                                Console.WriteLine($"*** Revit link type '{linkType.Name}' belongs to workset '{worksetName}' ({worksetId}). ***");
                            }

                            var worksetsWithoutLinks = allUserWorksets
                                .Where(ws => !linkWorksetIds.Contains(ws.Id.IntegerValue))
                                .Select(ws => ws.Id)
                                .ToList();

                            Console.WriteLine($"*** Worksets without links: {worksetsWithoutLinks.Count} ***");

                            // Close first-pass document without save
                            doc.Close(false);
                            doc = null;

                            // Second open: only worksets without links
                            var secondOpenOptions = new OpenOptions();
                            var secondConfig = new WorksetConfiguration(WorksetConfigurationOption.CloseAllWorksets);
                            if (worksetsWithoutLinks.Count > 0)
                            {
                                secondConfig.Open(worksetsWithoutLinks);
                            }

                            secondOpenOptions.SetOpenWorksetsConfiguration(secondConfig);
                            Console.WriteLine("*** Second open with only worksets without links. ***");

                            Stopwatch secondOpenStopwatch = Stopwatch.StartNew();
                            doc = _data.RevitApp.OpenDocumentFile(cloudModelPath, secondOpenOptions);
                            secondOpenStopwatch.Stop();

                            if (doc == null)
                            {
                                Console.WriteLine("*** Failed to open model (second pass). ***");
                                return false;
                            }

                            closeLinksTotalStopwatch.Stop();
                            Console.WriteLine($"*** Second open completed in {secondOpenStopwatch.Elapsed.TotalSeconds:F2} seconds. ***");
                            Console.WriteLine($"*** Open with CloseWorksetsWithRevitLinks total completed in {closeLinksTotalStopwatch.Elapsed.TotalSeconds:F2} seconds. ***");
                            break;
                        }
                    case OpenOption.OpenAllWorksets:
                    default:
                        {
                            Stopwatch openAllStopwatch = Stopwatch.StartNew();
                            doc = _data.RevitApp.OpenDocumentFile(cloudModelPath, new OpenOptions());
                            openAllStopwatch.Stop();
                            Console.WriteLine($"*** Open with OpenAllWorksets completed in {openAllStopwatch.Elapsed.TotalSeconds:F2} seconds. ***");
                            break;
                        }
                }

                if (doc == null)
                {
                    Console.WriteLine("*** Failed to open model. ***");
                    return false;
                }

                Console.WriteLine($"*** Successfully opened model: {doc.Title} ***");

                // 3. Get the tool
                if (!_toolRegistry.TryGetValue(modelConfig.ToolName, out var toolFactory))
                {
                    Console.WriteLine($"*** Unknown tool: {modelConfig.ToolName} ***");
                    Console.WriteLine($"*** Available tools: {string.Join(", ", _toolRegistry.Keys)} ***");
                    return false;
                }

                IRevitMcpTool tool = toolFactory();
                Console.WriteLine($"*** Executing tool: {modelConfig.ToolName} ***");

                // 4. Execute the tool with the opened document
                bool success = tool.Execute(_data, doc);

                // 5. Save if configured and tool succeeded
                if (success && modelConfig.Save)
                {
                    Console.WriteLine("*** Saving model... ***");

                    if (doc.IsWorkshared)
                    {
                        SynchronizeWithCentralOptions swc = new();
                        swc.SetRelinquishOptions(new RelinquishOptions(true));
                        doc.SynchronizeWithCentral(new TransactWithCentralOptions(), swc);
                        Console.WriteLine("*** Syncing to Central is done! ***");
                    }
                    else
                    {
                        doc.SaveCloudModel();
                        Console.WriteLine("*** Cloud model saved! ***");
                    }
                }
                else if (!success)
                {
                    Console.WriteLine("*** Tool execution failed. Model will not be saved. ***");
                }
                else
                {
                    Console.WriteLine("*** Model save skipped (save = false). ***");
                }

                return success;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"*** Exception in RevitMcpToolBox: {ex} ***");
                return false;
            }
            finally
            {
                // 6. Close the document
                if (doc != null)
                {
                    Console.WriteLine("*** Closing model... ***");
                    doc.Close(false);
                }
            }
        }

        /// <summary>
        /// Registers a new tool in the toolbox
        /// </summary>
        /// <param name="toolName">Name of the tool (case-insensitive)</param>
        /// <param name="toolFactory">Factory function to create the tool instance</param>
        public void RegisterTool(string toolName, Func<IRevitMcpTool> toolFactory)
        {
            _toolRegistry[toolName] = toolFactory;
        }
    }
}

