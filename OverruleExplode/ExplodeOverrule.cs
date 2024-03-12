using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;
using Bundles.Commands;

namespace Bundles.ExplodeOverrule
{
    /// <summary>
    /// Custom overrule to modify the behavior of the explode operation.
    /// </summary>
    public class ExplodeOverrule : TransformOverrule
    {
        // Name used for the extension dictionary entry.
        // This ensures our overrule is applied only to objects with this specific Xrecord.
        private string _entryName;

        // Static reference to the explode overrule instance.
        private static ExplodeOverrule _explodeOverrule;

        // Static flag to track the activation state of the overrule
        private static bool _isOverruleActive = false;

        public ExplodeOverrule(string entryName = null)
        {
            _entryName = entryName ?? OverruleSettings.EntryName;

            // Initialize the overrule by setting a filter based on
            // the extension dictionary entry.
            // This ensures that the overrule is applied only to objects
            // that have the specified extension dictionary entry.
            SetExtensionDictionaryEntryFilter(_entryName);
        }

        /// <summary>
        /// Activates the custom explode overrule.
        /// </summary>
        public static void StartOverrule(string entryName)
        {
            // Check if the overrule is already active to prevent redundant activation
            if (!_isOverruleActive)
            {
                _explodeOverrule = new ExplodeOverrule(entryName);
                Overrule.AddOverrule(RXObject.GetClass(typeof(Polyline)), _explodeOverrule, false);
                Overrule.Overruling = true;
                _isOverruleActive = true; // Update the overrule activation state
            }
        }

        /// <summary>
        /// Deactivates the custom explode overrule.
        /// </summary>
        public static void EndOverrule()
        {
            // Check if the overrule is active before deactivating it
            if (_isOverruleActive)
            {
                Overrule.RemoveOverrule(RXObject.GetClass(typeof(Polyline)), _explodeOverrule);
                Overrule.Overruling = false;
                _isOverruleActive = false; // Update the overrule activation state
            }
        }

        /// <summary>
        /// Overrides the explode operation for entities.
        /// </summary>
        /// <param name="e">The entity being exploded.</param>
        /// <param name="objs">The collection to hold the resulting exploded objects.</param>
        public override void Explode(Entity e, DBObjectCollection objs)
        {
            // Check if the entity is a Polyline.
            if (e is Polyline polyline)
            {
                // Prevent the explode operation for polylines and inform the user.
                Document doc = Application.DocumentManager.MdiActiveDocument;
                Editor ed = doc.Editor;
                ed.WriteMessage("\nThis object cannot be exploded as it will lose its properties.");

                // Add the original polyline back to the collection to prevent it from disappearing.
                objs.Add(e);
            }
            else
            {
                // For other entity types, proceed with the default explode behavior.
                base.Explode(e, objs);
            }
        }
    }
}
