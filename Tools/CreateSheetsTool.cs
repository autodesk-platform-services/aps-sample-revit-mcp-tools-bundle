using Autodesk.Revit.DB;
using DesignAutomationFramework;
using Newtonsoft.Json;
using RevitMcpTools.Utils;

namespace RevitMcpTools.Tools
{
    /// <summary>
    /// Tool to create sheets in a Revit document from a list of sheet definitions and an optional title block template.
    /// Input JSON example:
    /// {
    ///   "titleBlockName": "E1 30x42 Horizontal",   // optional – omit or leave empty to use the default title block
    ///   "sheets": [
    ///     { "sheetNumber": "A101", "sheetName": "Ground Floor Plan" },
    ///     { "sheetNumber": "A102", "sheetName": "First Floor Plan"  }
    ///   ]
    /// }
    /// </summary>
    [RevitMcpTool("create_sheets")]
    public class CreateSheetsTool : IRevitMcpTool
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

                var inputData = CreateSheetsInputData.Parse(inputJsonPath);
                if (inputData == null)
                {
                    Console.WriteLine("*** Failed to deserialize input JSON! ***");
                    return false;
                }

                Console.WriteLine($"*** Sheets to create: {inputData.Sheets.Count} ***");
                Console.WriteLine($"*** Title block: {(string.IsNullOrWhiteSpace(inputData.TitleBlockName) ? "default" : inputData.TitleBlockName)} ***");

                ElementId titleBlockId = GetTitleBlockId(doc, inputData.TitleBlockName);

                Transaction t = new(doc, "Create Sheets");
                t.Start();

                int createdCount = 0;
                int failedCount = 0;

                foreach (var sheetInfo in inputData.Sheets)
                {
                    try
                    {
                        Console.WriteLine($"*** Creating sheet: {sheetInfo.SheetNumber} - {sheetInfo.SheetName} ***");

                        ViewSheet newSheet = ViewSheet.Create(doc, titleBlockId);

                        if (!string.IsNullOrWhiteSpace(sheetInfo.SheetNumber))
                            newSheet.SheetNumber = sheetInfo.SheetNumber;

                        if (!string.IsNullOrWhiteSpace(sheetInfo.SheetName))
                            newSheet.Name = sheetInfo.SheetName;

                        Console.WriteLine($"*** Successfully created sheet: {newSheet.SheetNumber} - {newSheet.Name} ***");
                        createdCount++;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"*** Failed to create sheet '{sheetInfo.SheetNumber} - {sheetInfo.SheetName}': {ex.Message} ***");
                        failedCount++;
                    }
                }

                t.Commit();

                Console.WriteLine($"*** Sheet creation complete. Created: {createdCount}, Failed: {failedCount} ***");
                return failedCount == 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"*** Exception in CreateSheetsTool: {ex} ***");
                return false;
            }
        }

        private static ElementId GetTitleBlockId(Document doc, string titleBlockName)
        {
            var titleBlocks = new FilteredElementCollector(doc)
                .OfCategory(BuiltInCategory.OST_TitleBlocks)
                .WhereElementIsElementType()
                .ToElements();

            Element titleBlock;

            if (!string.IsNullOrWhiteSpace(titleBlockName))
            {
                titleBlock = titleBlocks.FirstOrDefault(x => x.Name.Equals(titleBlockName, StringComparison.OrdinalIgnoreCase));

                if (titleBlock == null)
                {
                    Console.WriteLine($"*** Title block '{titleBlockName}' not found, falling back to default ***");
                    titleBlock = titleBlocks.FirstOrDefault();
                }
            }
            else
            {
                titleBlock = titleBlocks.FirstOrDefault();
            }

            if (titleBlock != null)
            {
                Console.WriteLine($"*** Using title block: {titleBlock.Name} ***");
                return titleBlock.Id;
            }

            Console.WriteLine("*** No title block found in document, creating sheet without title block ***");
            return ElementId.InvalidElementId;
        }

        private class CreateSheetsInputData
        {
            [JsonProperty(PropertyName = "titleBlockName")]
            public string TitleBlockName { get; set; }

            [JsonProperty(PropertyName = "sheets")]
            public List<SheetInfo> Sheets { get; set; } = [];

            public static CreateSheetsInputData Parse(string jsonPath)
            {
                try
                {
                    string jsonContents = File.ReadAllText(jsonPath);
                    return JsonConvert.DeserializeObject<CreateSheetsInputData>(jsonContents);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"*** Exception when parsing json file: {ex} ***");
                    return null;
                }
            }
        }

        private class SheetInfo
        {
            [JsonProperty(PropertyName = "sheetNumber")]
            public string SheetNumber { get; set; }

            [JsonProperty(PropertyName = "sheetName")]
            public string SheetName { get; set; }
        }
    }
}
