using Newtonsoft.Json;

namespace RevitMcpTools.Utils
{
    /// <summary>
    /// Configuration for the Revit model to open and operate on
    /// </summary>
    public class ModelConfiguration
    {
        [JsonProperty(PropertyName = "region", Required = Required.Always)]
        public string Region { get; set; }

        [JsonProperty(PropertyName = "projectGuid", Required = Required.Always)]
        public Guid ProjectGuid { get; set; }

        [JsonProperty(PropertyName = "modelGuid", Required = Required.Always)]
        public Guid ModelGuid { get; set; }

        [JsonProperty(PropertyName = "toolName", Required = Required.Always)]
        public string ToolName { get; set; }

        [JsonProperty(PropertyName = "save", Required = Required.Default)]
        public bool Save { get; set; } = true;

        public static ModelConfiguration Parse(string jsonPath)
        {
            try
            {
                if (!File.Exists(jsonPath))
                {
                    Console.WriteLine($"*** Model configuration file not found: {jsonPath} ***");
                    return null;
                }

                string jsonContents = File.ReadAllText(jsonPath);
                return JsonConvert.DeserializeObject<ModelConfiguration>(jsonContents);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"*** Exception when parsing model configuration: {ex} ***");
                return null;
            }
        }
    }
}
