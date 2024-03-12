using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.GraphicsInterface;
using Autodesk.AutoCAD.Runtime;
using Polyline = Autodesk.AutoCAD.DatabaseServices.Polyline;

namespace Bundles.Commands
{
    public class Commands : IExtensionApplication
    {
        // Define a default diameter for the pipes
        private double _diameter = 10.0;

        // Flag to indicate if overrules are active
        private bool _overrule = false;

        public void Initialize()
        {
            // Apply overrules
            OverruleStart();
        }

        public void Terminate() { }

        /// <summary>
        /// Enable or disable overrules for drawable objects and grips in the document.
        /// </summary>
        /// <param name="enable">True to enable overrules, false to disable.</param>
        public void Overrule(bool enable)
        {
            // Set the global flags for overruling drawable objects and grips
            DrawableOverrule.Overruling = enable;
            GripOverrule.Overruling = enable;

            // Update the local flag to reflect the current overrule state
            _overrule = enable;

            // Force a regeneration of the drawing to apply the overrule changes
            Document doc = Application.DocumentManager.MdiActiveDocument;
            doc.SendStringToExecute("REGEN\n", true, false, false);
            doc.Editor.Regen();
        }

        /// <summary>
        /// Initializes and applies all necessary overrules when the command is first executed.
        /// </summary>
        public void OverruleStart()
        {
            // Check if overrules have already been applied to avoid duplication
            if (!_overrule)
            {
                // Apply a custom overrule for the explode operation
                ExplodeOverrule.ExplodeOverrule.StartOverrule(OverruleSettings.EntryName);

                // Apply a custom overrule for polyline drawing
                DrawPipeOverrule.PipeOverrule.StartOverrule(OverruleSettings.EntryName);

                // Code to apply a custom grip overrule is commented out.
                //Overrule_Grip.GripVectorOverrule.StartOverrule(OverruleSettings.EntryName);

                // Enable overrules
                Overrule(true);
            }
        }

        /// <summary>
        /// Removes all applied overrules and restores default behavior.
        /// </summary>
        [CommandMethod("REMOVEOVERRULES")]
        public void OverruleEnd()
        {
            // Only remove overrules if they are currently active
            if (_overrule)
            {
                // Remove custom overrule for polyline drawing
                DrawPipeOverrule.PipeOverrule.EndOverrule();

                // Remove custom overrule for the explode operation
                ExplodeOverrule.ExplodeOverrule.EndOverrule();

                // Remove custom grip overrule if previously enabled
                //Overrule_Grip.GripVectorOverrule.EndOverrule();    

                // Disable overrules
                Overrule(false);
            }
        }

        /// <summary>
        /// Command to create a pipe representation from selected polylines with specified diameter.
        /// </summary>
        [CommandMethod("FLEX", CommandFlags.UsePickSet)]
        public void MakePipe()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            // Define selection criteria for polylines
            SelectionFilter filter = new SelectionFilter(new[] { new TypedValue(0, "LWPOLYLINE") });
            PromptSelectionOptions pso = new PromptSelectionOptions();
            pso.AllowDuplicates = false;
            pso.MessageForAdding = "\nSelect objects: ";

            // Prompt the user to select polylines
            PromptSelectionResult selRes = ed.GetSelection(pso, filter);

            // Exit if no selection is made
            if (selRes.Status != PromptStatus.OK) return;

            // Get the selected set of objects
            SelectionSet ss = selRes.Value;

            // Prompt for the internal diameter of the pipe
            PromptDoubleOptions pdo = new PromptDoubleOptions("\nSpecify pipe internal diameter:");
            if (_diameter > 0.0)
            {
                pdo.DefaultValue = _diameter;
                pdo.UseDefaultValue = true;
            }

            pdo.AllowNegative = false;
            pdo.AllowZero = false;

            // Get the diameter from the user input
            PromptDoubleResult pdr = ed.GetDouble(pdo);
            if (pdr.Status != PromptStatus.OK) return;

            // Update the diameter based on user input
            _diameter = pdr.Value;

            // Start a transaction to modify the drawing
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                foreach (SelectedObject so in ss)
                {
                    Polyline pline = tr.GetObject(so.ObjectId, OpenMode.ForWrite) as Polyline;
                    if (pline != null && !pline.Closed) // Ensure the polyline is open
                    {
                        // Apply the custom pipe representation to the polyline
                        DrawPipeOverrule.PipeOverrule.SetPipeRadiusOnObject(tr, pline, _diameter, OverruleSettings.EntryName);
                    }
                }

                // Commit changes to the database
                tr.Commit();
            }

            // Apply overrules to reflect changes
            OverruleStart();

            // Force regeneration of the drawing to update the display
            doc.SendStringToExecute("REGEN\n", true, false, false);
            ed.Regen();
        }
    }
}