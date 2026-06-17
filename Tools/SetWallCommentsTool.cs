using Autodesk.Revit.DB;
using DesignAutomationFramework;
using RevitMcpTools.Utils;

namespace RevitMcpTools.Tools
{
    /// <summary>
    /// Tool to set the Comments parameter to "Testing" for all Wall instances in the document.
    /// </summary>
    [RevitMcpTool("set_wall_comments")]
    public class SetWallCommentsTool : IRevitMcpTool
    {
        public bool Execute(DesignAutomationData data, Document doc)
        {
            try
            {
                ArgumentNullException.ThrowIfNull(data);
                ArgumentNullException.ThrowIfNull(doc);

                var walls = new FilteredElementCollector(doc)
                    .OfClass(typeof(Wall))
                    .WhereElementIsNotElementType()
                    .Cast<Wall>()
                    .ToList();

                Console.WriteLine($"*** Walls found: {walls.Count} ***");

                Transaction t = new(doc, "Set Wall Comments");
                t.Start();

                int updatedCount = 0;
                int skippedCount = 0;

                foreach (var wall in walls)
                {
                    var commentsParam = wall.get_Parameter(BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS);

                    if (commentsParam == null || commentsParam.IsReadOnly)
                    {
                        skippedCount++;
                        continue;
                    }

                    commentsParam.Set("Testing");
                    updatedCount++;
                }

                t.Commit();

                Console.WriteLine($"*** Wall comments update complete. Updated: {updatedCount}, Skipped: {skippedCount} ***");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"*** Exception in SetWallCommentsTool: {ex} ***");
                return false;
            }
        }
    }
}
