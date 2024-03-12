using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.GraphicsInterface;
using System;

namespace Bundles.Overrule_Grip
{
    /// <summary>
    /// Code credits: keanw.com
    /// The original code is found <see href="https://www.keanw.com/2012/09/overriding-the-grips-of-an-autocad-polyline-to-maintain-fillet-segments-using-net.html">here</see>:
    /// A custom GripData derived class to enhance grip functionality,
    /// including custom rendering and behavior during grip editing.
    /// </summary>
    public class CustomGripData : GripData
    {
        // The ObjectId of the original entity associated with this grip.
        public ObjectId m_key = ObjectId.Null;

        // An identifier to track the order or position of this grip amongst others.
        public int m_index = 0;

        // Stores the radius associated with the grip point, useful for arcs or rounded corners.
        public Double m_radius = 0.0;

        // The original location of the grip point before any user manipulation.
        public Point3d m_original_point = Point3d.Origin;

        /// <summary>
        /// Handles the change in grip status, such as when a grip operation is aborted.
        /// </summary>
        /// <param name="entityId">The ObjectId of the entity associated with this grip.</param>
        /// <param name="newStatus">The new grip status.</param>
        public override void OnGripStatusChanged(ObjectId entityId, GripData.Status newStatus)
        {
            // If the grip operation is aborted, reset the grips to their original positions.
            if (newStatus == Status.GripAbort)
            {
                GripVectorOverrule.ResetGrips(entityId);
            }
        }

        /// <summary>
        /// Custom drawing function for grips, allowing for unique visual representation.
        /// </summary>
        /// <param name="worldDraw">The drawing context.</param>
        /// <param name="entityId">The ObjectId of the entity associated with this grip.</param>
        /// <param name="type">The type of drawing operation.</param>
        /// <param name="imageGripPoint">The location of the grip point in the drawing.</param>
        /// <param name="gripSizeInPixels">The size of the grip symbol in pixels.</param>
        /// <returns>True if the default grip symbol should be drawn after this method, false otherwise.</returns>
        public override bool ViewportDraw(ViewportDraw worldDraw, ObjectId entityId, 
            GripData.DrawType type, Point3d? imageGripPoint, int gripSizeInPixels)
        {
            // Calculate the glyph size in the World Coordinate System (WCS).
            Point2d glyphSize = worldDraw.Viewport.GetNumPixelsInUnitSquare(this.GripPoint);
            Double glyphHeight = (gripSizeInPixels / glyphSize.Y);

            // Transform the grip point to the viewport coordinates.
            Matrix3d e2w = worldDraw.Viewport.EyeToWorldTransform;
            Point3d pt = this.GripPoint.TransformBy(e2w);

            // Define a simple triangular glyph to represent the grip.
            Point3dCollection pnts = new Point3dCollection();
            pnts.Add(new Point3d(pt.X - glyphHeight, pt.Y + glyphHeight, pt.Z));
            pnts.Add(new Point3d(pt.X, pt.Y - glyphHeight, pt.Z));
            pnts.Add(new Point3d(pt.X + glyphHeight, pt.Y + glyphHeight, pt.Z));
            pnts.Add(new Point3d(pt.X - glyphHeight, pt.Y + glyphHeight, pt.Z));

            // Draw the custom grip glyph.
            worldDraw.Geometry.DeviceContextPolygon(pnts);

            // Return false to indicate that the default grip symbol should not be drawn.
            return false;
        }
    }
}