using RevitMcpTools.Tools;

namespace RevitMcpTools.Utils
{
    /// <summary>
    /// Extension methods for ToolBoxBuilder to register all MCP tools
    /// </summary>
    public static class RevitMcpToolBoxBuilderExtensions
    {
        /// <summary>
        /// Registers all available MCP tools
        /// </summary>
        /// <param name="builder">The ToolBoxBuilder instance</param>
        /// <returns>The builder for method chaining</returns>
        public static RevitMcpToolBoxBuilder AddMcpTools(this RevitMcpToolBoxBuilder builder)
        {
            return builder
                .AddTool<CreateModelTool>()
                .AddTool<LinkModelsTool>()
                .AddTool<CreateSheetsTool>();
        }
    }
}
