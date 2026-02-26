namespace RevitMcpTools.Utils
{
    /// <summary>
    /// Attribute to specify the tool name for a Revit MCP tool
    /// </summary>
    /// <remarks>
    /// Initializes a new instance of the RevitToolAttribute
    /// </remarks>
    /// <param name="name">The name of the tool (case-insensitive)</param>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class RevitMcpToolAttribute(string name) : Attribute
    {
        /// <summary>
        /// Gets the name of the tool
        /// </summary>
        public string Name { get; } = name ?? throw new ArgumentNullException(nameof(name));
    }
}
