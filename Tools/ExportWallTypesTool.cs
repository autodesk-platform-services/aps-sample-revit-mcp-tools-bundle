using Autodesk.Revit.DB;
using DesignAutomationFramework;
using Newtonsoft.Json;
using RevitMcpTools.Utils;

namespace RevitMcpTools.Tools
{
    /// <summary>
    /// Tool to collect all wall types and their properties and export them to output.json.
    /// </summary>
    [RevitMcpTool("export_wall_types")]
    public class ExportWallTypesTool : IRevitMcpTool
    {
        public bool Execute(DesignAutomationData data, Document doc)
        {
            try
            {
                ArgumentNullException.ThrowIfNull(data);
                ArgumentNullException.ThrowIfNull(doc);

                var wallTypes = new FilteredElementCollector(doc)
                    .OfClass(typeof(WallType))
                    .Cast<WallType>()
                    .ToList();

                Console.WriteLine($"*** Wall types found: {wallTypes.Count} ***");

                var results = wallTypes
                    .Select(wt => BuildWallTypeData(doc, wt))
                    .ToList();

                string json = JsonConvert.SerializeObject(results, Formatting.Indented);
                File.WriteAllText("result.json", json);

                Console.WriteLine($"*** Wall types exported to results.json ({results.Count} types) ***");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"*** Exception in ExportWallTypesTool: {ex} ***");
                return false;
            }
        }

        private static WallTypeData BuildWallTypeData(Document doc, WallType wallType)
        {
            var data = new WallTypeData
            {
                Name = wallType.Name,
                Kind = wallType.Kind.ToString(),
                Function = wallType.Function.ToString(),
                TotalWidthMm = UnitUtils.ConvertFromInternalUnits(wallType.Width, UnitTypeId.Millimeters),
                Layers = []
            };

            var structure = wallType.GetCompoundStructure();
            if (structure != null)
            {
                foreach (var layer in structure.GetLayers())
                {
                    string materialName = string.Empty;
                    if (layer.MaterialId != ElementId.InvalidElementId)
                    {
                        var material = doc.GetElement(layer.MaterialId) as Material;
                        materialName = material?.Name ?? string.Empty;
                    }

                    data.Layers.Add(new WallLayerData
                    {
                        Function = layer.Function.ToString(),
                        ThicknessMm = UnitUtils.ConvertFromInternalUnits(layer.Width, UnitTypeId.Millimeters),
                        MaterialName = materialName
                    });
                }
            }

            return data;
        }

        private class WallTypeData
        {
            [JsonProperty("name")]
            public string Name { get; set; }

            [JsonProperty("kind")]
            public string Kind { get; set; }

            [JsonProperty("function")]
            public string Function { get; set; }

            [JsonProperty("totalWidthMm")]
            public double TotalWidthMm { get; set; }

            [JsonProperty("layers")]
            public List<WallLayerData> Layers { get; set; }
        }

        private class WallLayerData
        {
            [JsonProperty("function")]
            public string Function { get; set; }

            [JsonProperty("thicknessMm")]
            public double ThicknessMm { get; set; }

            [JsonProperty("materialName")]
            public string MaterialName { get; set; }
        }
    }
}
