using Autodesk.Revit.DB;
using DesignAutomationFramework;

namespace RevitMcpTools.Utils
{
    /// <summary>
    /// Interface for all Revit MCP tools that can be executed via Automation API
    /// </summary>
    public interface IRevitMcpTool
    {
        /// <summary>
        /// Executes the tool with the provided Automation API data and working document
        /// </summary>
        /// <param name="data">Automation API data containing Revit application context</param>
        /// <param name="doc">The opened working Revit document</param>
        /// <returns>True if the tool executed successfully, false otherwise</returns>
        bool Execute(DesignAutomationData data, Document doc);
    }
}

