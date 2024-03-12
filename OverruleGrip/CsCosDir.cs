using Autodesk.AutoCAD.Geometry;
using System;

namespace Bundles.Overrule_Grip
{
    /// <summary>
    /// Code credits: keanw.com
    /// The original code is found <see href="https://www.keanw.com/2012/09/overriding-the-grips-of-an-autocad-polyline-to-maintain-fillet-segments-using-net.html">here</see>:
    /// A support class for handling directional cosines in both 2D and 3D, providing a way to represent
    /// the direction of a vector without depending on trigonometric functions.
    /// </summary>
    public class CsCosDir
    {
        // Directional cosines for the x, y, and z axes.
        public Double cx;
        public Double cy;
        public Double cz;

        /// <summary>
        /// Default constructor that initializes directional cosines to zero,
        /// representing a lack of direction.
        /// </summary>
        public CsCosDir()
        {
            cx = cy = cz = 0.0;
        }

        /// <summary>
        /// Constructor for 2D directional cosines calculated from two points.
        /// </summary>
        /// <param name="p1">The start point of the vector.</param>
        /// <param name="p2">The end point of the vector.</param>
        public CsCosDir(Point2d p1, Point2d p2)
        {
            // Calculate differences in x and y coordinates.
            Double dx = p2.X - p1.X;
            Double dy = p2.Y - p1.Y;
            // Calculate the magnitude of the 2D vector.
            Double dd = Math.Sqrt(dx * dx + dy * dy);

            // If the magnitude is significantly greater than zero, calculate directional cosines.
            if (dd > CsMath.dEpsilon)
            {
                cx = dx / dd;
                cy = dy / dd;
                // cz remains 0 in 2D space.
            }
        }

        /// <summary>
        /// Constructor for 3D directional cosines calculated from two points.
        /// </summary>
        /// <param name="p1">The start point of the vector in 3D space.</param>
        /// <param name="p2">The end point of the vector in 3D space.</param>
        public CsCosDir(Point3d p1, Point3d p2)
        {
            // Calculate differences in x, y, and z coordinates.
            Double dx = p2.X - p1.X;
            Double dy = p2.Y - p1.Y;
            Double dz = p2.Z - p1.Z;
            // Calculate the magnitude of the 3D vector.
            Double dd = Math.Sqrt(dx * dx + dy * dy + dz * dz);

            // If the magnitude is significantly greater than zero, calculate directional cosines.
            if (dd > CsMath.dEpsilon)
            {
                cx = dx / dd;
                cy = dy / dd;
                cz = dz / dd;
            }
        }
    }
}
