using System.Reflection;
using DesignAutomationFramework;

namespace RevitMcpTools.Utils
{
    /// <summary>
    /// Builder for creating a configured RevitMcpToolBox instance
    /// </summary>
    public class RevitMcpToolBoxBuilder
    {
        private readonly Dictionary<string, Func<IRevitMcpTool>> _toolRegistry;
        private readonly DesignAutomationData _data;

        internal RevitMcpToolBoxBuilder(DesignAutomationData data)
        {
            _toolRegistry = new Dictionary<string, Func<IRevitMcpTool>>(StringComparer.OrdinalIgnoreCase);
            _data = data ?? throw new ArgumentNullException(nameof(data));
        }

        /// <summary>
        /// Adds a tool to the registry using the tool name from the RevitMcpToolAttribute
        /// </summary>
        /// <typeparam name="TTool">The tool type that implements IRevitMcpTool</typeparam>
        /// <returns>The ToolBoxBuilder instance for method chaining</returns>
        /// <exception cref="InvalidOperationException">Thrown when the tool doesn't have a RevitMcpToolAttribute</exception>
        public RevitMcpToolBoxBuilder AddTool<TTool>() where TTool : IRevitMcpTool, new()
        {
            var toolType = typeof(TTool);
            var attribute = toolType.GetCustomAttribute<RevitMcpToolAttribute>();

            if (attribute == null)
            {
                throw new InvalidOperationException(
                    $"Tool type '{toolType.Name}' must be decorated with [RevitMcpTool] attribute.");
            }

            _toolRegistry[attribute.Name] = () => new TTool();
            Console.WriteLine($"*** Registered tool: {attribute.Name} ({toolType.Name}) ***");

            return this;
        }

        /// <summary>
        /// Builds and returns the configured RevitMcpToolBox instance
        /// </summary>
        /// <returns>A configured RevitMcpToolBox instance</returns>
        public RevitMcpToolBox Build()
        {
            return new RevitMcpToolBox(_toolRegistry, _data);
        }
    }
}
