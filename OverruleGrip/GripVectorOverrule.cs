using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using Bundles.Commands;
using Bundles.ExplodeOverrule;
using System;
using System.Collections.Generic;

namespace Bundles.Overrule_Grip
{
    /// <summary>
    /// Code credits: keanw.com
    /// The original code is found <see href="https://www.keanw.com/2012/09/overriding-the-grips-of-an-autocad-polyline-to-maintain-fillet-segments-using-net.html">here</see>:
    /// This class is instantiated by AutoCAD for each document when
    /// a command is called by the user the first time in the context
    /// of a given document. In other words, non static data in this
    /// class is implicitly per-document!
    /// </summary>
    public class GripVectorOverrule : GripOverrule
    {
        // Name used for the extension dictionary entry.
        // This ensures our overrule is applied only to objects with this specific Xrecord.
        private string _entryName;

        // Static reference to the grip overrule instance.
        private static GripVectorOverrule _gripOverrule;

        // Static flag to track the activation state of the overrule
        private static bool _isOverruleActive = false;

        // gripdata for each selected polyline
        static Dictionary<ObjectId, GripDataCollection> _ents_handled =
          new Dictionary<ObjectId, GripDataCollection>();

        public GripVectorOverrule(string entryName = null)
        {
            _entryName = entryName ?? OverruleSettings.EntryName;

            // Initialize the overrule by setting a filter based on
            // the extension dictionary entry.
            // This ensures that the overrule is applied only to objects
            // that have the specified extension dictionary entry.
            SetExtensionDictionaryEntryFilter(_entryName);
        }

        /// <summary>
        /// Activates the custom Grip overrule.
        /// </summary>
        public static void StartOverrule(string entryName)
        {
            if (!_isOverruleActive)
            {
                _gripOverrule = new GripVectorOverrule(entryName);
                ObjectOverrule.AddOverrule(RXClass.GetClass(typeof(Polyline)), _gripOverrule, true);
                Overrule.Overruling = true;
                _isOverruleActive = true; // Update the overrule activation state
            }
        }

        /// <summary>
        /// Deactivates the custom Grip overrule.
        /// </summary>
        public static void EndOverrule()
        {
            if (_isOverruleActive)
            {
                Overrule.RemoveOverrule(RXObject.GetClass(typeof(Entity)), _gripOverrule);  // Adjust to target entity type as needed
                Overrule.Overruling = false;
                _isOverruleActive = false; // Update the overrule activation state
                ResetAllGrips();  // Consider calling ResetAllGrips to clean up when overrule is deactivated
            }
        }

        public GripVectorOverrule()
        {
            // Set event handlers on documents to get access to
            // OnImpliedSelectionChanged
            DocumentCollection dm = Application.DocumentManager;

            dm.DocumentCreated += new DocumentCollectionEventHandler(dm_DocumentCreated);
            dm.DocumentToBeDestroyed += new DocumentCollectionEventHandler(dm_DocumentToBeDestroyed);

            // Attach handler to currently loaded documents
            foreach (Document doc in dm)
            {
                doc.ImpliedSelectionChanged += new EventHandler(doc_ImpliedSelectionChanged);
            }
        }

        void dm_DocumentCreated(object sender, DocumentCollectionEventArgs e)
        {
            e.Document.ImpliedSelectionChanged += new EventHandler(doc_ImpliedSelectionChanged);
        }

        void dm_DocumentToBeDestroyed(object sender, DocumentCollectionEventArgs e)
        {
            e.Document.ImpliedSelectionChanged -= new EventHandler(doc_ImpliedSelectionChanged);
        }

        void doc_ImpliedSelectionChanged(object sender, EventArgs e)
        {
            // Check for empty selection on current document
            Document doc = Application.DocumentManager.MdiActiveDocument;
            PromptSelectionResult res = doc.Editor.SelectImplied();

            // If nothing selected, it's a good time to reset GripData dictionary
            if (res != null)
                if (res.Value == null) GripVectorOverrule.ResetAllGrips();
        }

        /// <summary>
        /// Called when entity is first selected.
        /// Analyze it and return alternative grips data collection if
        /// we must handle it.
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="grips"></param>
        /// <param name="curViewUnitSize"></param>
        /// <param name="gripSize"></param>
        /// <param name="curViewDir"></param>
        /// <param name="bitFlags"></param>
        public override void GetGripPoints(Entity entity, GripDataCollection grips, 
            double curViewUnitSize, int gripSize, Vector3d curViewDir, GetGripPointsFlags bitFlags)
        {
            bool bValid = false;

            // get polyline object3
            Polyline p = entity as Polyline;

            // check if the polyline is of the type we search for.
            // In the final version it may contain some xdata, maybe
            // holding a fixed radius value, to allow other plines to
            // work as expected.
            // However, we must always check if the geometry is still
            // valid. We should also check if the linear segments are
            // tangent to the arcs, but for the moment, we'll check if
            // we have a pattern of alternating line and arc segments,
            // starting with a line
            bValid = CsMath.IsPolylineRoundedEdge(p);

            if (bValid)
            {
                // seems right, gather line segments
                LineSegment3d[] lsegs = new LineSegment3d[p.NumberOfVertices];
                int nSegs = 0;

                for (int i = 0; i < p.NumberOfVertices; i++)
                {
                    if (p.GetSegmentType(i) == SegmentType.Line)
                    {
                        lsegs[nSegs++] = p.GetLineSegmentAt(i);
                    }
                }

                // Gather start, end and intersection points
                Point3d[] pnts = new Point3d[p.NumberOfVertices];

                // Current radius for inner intersection
                Double[] rads = new Double[p.NumberOfVertices];
                int nPnts = 0;

                // At least two linear segments are needed
                if (nSegs > 1)
                {
                    // Working plane for intersecting segments
                    Plane pPlane = p.GetPlane();

                    // Temporary intersecting point
                    Point3d pnt = Point3d.Origin;

                    // First point: if the polyline id closed, the first pt
                    // is the intersection of the first and last segments
                    if (p.Closed)
                    {
                        if (CsMath.CheckIntersect(lsegs[0], lsegs[nSegs - 1], pPlane, ref pnt))
                        {
                            pnts[nPnts] = pnt;
                            rads[nPnts] = CsMath.GetFilletRadius(lsegs[0], lsegs[nSegs - 1], pPlane);
                            nPnts++;
                        }
                        else
                        {
                            // Polyline with overlapping segments? Not good for us
                            bValid = false;
                        }
                    }
                    else
                        pnts[nPnts++] = lsegs[0].StartPoint;

                    // Add intersection points for internal segments
                    for (int i = 0; i < (nSegs - 1); i++)
                    {
                        if (CsMath.CheckIntersect(lsegs[i], lsegs[i + 1], pPlane, ref pnt))
                        {
                            pnts[nPnts] = pnt;
                            rads[nPnts] = CsMath.GetFilletRadius(lsegs[i], lsegs[i + 1], pPlane);
                            nPnts++;
                        }
                        else
                        {
                            // No intersection, overlapping or co-linear segments?
                            bValid = false;
                        }
                    }

                    // Last point: add if not a closed pline
                    if (!p.Closed) pnts[nPnts++] = lsegs[nSegs - 1].EndPoint;

                    if (bValid) // Still valid?
                    {
                        // Everything seems ok, add grip points
                        // Use also a private GripDataCollection, don't mess
                        // with AutoCAD's
                        GripDataCollection myGrips = new GripDataCollection();

                        for (int i = 0; i < nPnts; i++)
                        {
                            CustomGripData gd = new CustomGripData();
                            gd.m_index = i;
                            gd.m_key = entity.ObjectId;
                            gd.GripPoint = gd.m_original_point = pnts[i];
                            gd.m_radius = rads[i];
                            gd.GizmosEnabled = true;
                            grips.Add(gd);
                            myGrips.Add(gd);
                        }

                        // Check for same entity already in list. If so,
                        // remove it
                        _ents_handled.Remove(entity.ObjectId);

                        // Add to our managed list
                        _ents_handled.Add(entity.ObjectId, myGrips);
                    }
                }
                else
                {
                    bValid = false;
                }
            }

            if (!bValid)
            {
                // Polyline not good for us, let it be treated as usual
                base.GetGripPoints(entity, grips, curViewUnitSize, gripSize, curViewDir, bitFlags);
            }
        }

        /// <summary>
        /// Handler called during grip streching.
        /// Rebuild polyline on the fly given modified grips
        /// </summary>
        /// <param name="entity">Clone of the original entity to
        /// manage</param>
        /// <param name="grips">Altered grips</param>
        /// <param name="offset">Vector of displacement from original
        /// grip position</param>
        /// <param name="bitFlags"></param>
        public override void MoveGripPointsAt(Entity entity, GripDataCollection grips,
            Vector3d offset, MoveGripPointsFlags bitFlags)
        {
            // Retrieve from the streched grips the ObjectId/dictionary
            // key
            // shouldn't happen. Programmer's paranoia
            if (grips.Count == 0) return;

            CustomGripData gda = grips[0] as CustomGripData;

            if (gda != null) // It's one of our grips
            {
                if (_ents_handled.ContainsKey(gda.m_key))
                {
                    // Retrieve our original grip collection
                    GripDataCollection original_grips = _ents_handled[gda.m_key];

                    // Correct grips with offset information
                    foreach (CustomGripData gdo in grips)
                    {
                        // Retrieve original grip and set current
                        // dragged location
                        CustomGripData gd = original_grips[gdo.m_index] as CustomGripData;
                        gd.GripPoint = gd.m_original_point + offset;
                    }

                    // Recalc polyline from new sets of grips
                    CsMath.RebuildPolyline(entity as Polyline, original_grips);

                    // Done, don't fall into standard handling
                    return;
                }
            }

            // Revert to standard handling
            base.MoveGripPointsAt(entity, grips, offset, bitFlags);
        }

        /// <summary>
        /// Reset grip position for the selected entity.
        /// Revert position to initial location, useful when aborting
        /// a grip handling.
        /// </summary>
        /// <param name="entity_id">objectid/key of the selected
        /// entity</param>
        public static void ResetGrips(ObjectId entity_id)
        {
            // Reset grips to their original point
            if (_ents_handled.ContainsKey(entity_id))
            {
                GripDataCollection grips = _ents_handled[entity_id];

                foreach (CustomGripData gdo in grips)
                {
                    gdo.GripPoint = gdo.m_original_point;
                }
            }
        }

        public static void ResetAllGrips()
        {
            // Clear handled list, to be called when selection is cleared
            _ents_handled.Clear();
        }
    }
}
