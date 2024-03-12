using Autodesk.AutoCAD.Geometry;
using System;

namespace Bundles.Overrule_Grip
{
    /// <summary>
    /// Code credits: keanw.com
    /// The original code is found <see href="https://www.keanw.com/2012/09/overriding-the-grips-of-an-autocad-polyline-to-maintain-fillet-segments-using-net.html">here</see>:
    /// A class representing a vertex in a polyline that may have a fillet (an arc connecting two segments).
    /// </summary>
    public class CustomVertex
    {
        // The fillet radius. This could be the radius that was requested by the user, or the actual radius calculated.
        public Double radius;

        // The original point of the vertex before filleting.
        public Point3d pOrg;

        // The start point of the arc that represents the fillet. This is the end point of the first line segment.
        public Point3d p1;

        // The end point of the arc that represents the fillet. This is the start point of the second line segment.
        public Point3d p2;

        // The midpoint of the arc. This point lies on the bisector of the angle formed by the two line segments.
        public Point3d pc;

        // The bulge parameter of the arc segment. In AutoCAD, the bulge is defined as the tangent of one fourth 
        // of the included angle for the arc, which is a way to represent arcs using only the start and end points.
        public Double bulge;

        /// <summary>
        /// Default constructor initializing the vertex points to the origin and setting radius and bulge to 0.
        /// </summary>
        public CustomVertex()
        {
            pOrg = p1 = p2 = pc = Point3d.Origin; // Initialize all points to the origin point.
            radius = bulge = 0.0; // Set the initial radius and bulge values to 0.
        }

        /// <summary>
        /// Overloaded constructor that initializes all vertex points to a given point and sets radius and bulge to 0.
        /// </summary>
        /// <param name="pInit">The initial point to set all vertex points to.</param>
        public CustomVertex(Point3d pInit)
        {
            pOrg = p1 = p2 = pc = pInit; // Initialize all points to the provided initial point.
            radius = bulge = 0.0; // Set the initial radius and bulge values to 0.
        }
    }
}
