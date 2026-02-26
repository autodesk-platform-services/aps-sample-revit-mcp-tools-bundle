using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using DesignAutomationFramework;
using RevitMcpTools.Utils;

namespace RevitMcpTools
{
    [Regeneration(RegenerationOption.Manual)]
    [Transaction(TransactionMode.Manual)]
    public class RevitMcpToolsHandler : IExternalDBApplication
    {
        public ExternalDBApplicationResult OnStartup(ControlledApplication application)
        {
            Console.WriteLine("*** Startup Revit MCP Tools Handler ***");
            DesignAutomationBridge.DesignAutomationReadyEvent += HandleDesignAutomationReadyEvent;
            return ExternalDBApplicationResult.Succeeded;
        }

        public ExternalDBApplicationResult OnShutdown(ControlledApplication application)
        {
            Console.WriteLine("*** Shutdown Revit MCP Tools Handler ***");
            return ExternalDBApplicationResult.Succeeded;
        }

        private void HandleDesignAutomationReadyEvent(object? sender, DesignAutomationReadyEventArgs e)
        {
            Console.WriteLine("*** Revit Automation is ready. Processing tool configuration... ***");
            
            e.Succeeded = RevitMcpToolBox.CreateBuilder(e.DesignAutomationData)
                .AddMcpTools()
                .Build()
                .Execute();
        }
    }
}
