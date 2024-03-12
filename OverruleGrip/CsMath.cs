using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;

namespace Bundles.Overrule_Grip
{
    // Common math functions 
    public static class CsMath
    {
        public const Double PI = 3.141592653589793; // More decimal places than Math.PI
        public const Double dEpsilon = 0.001; // General precision allowed for identity

        /// <summary>
        /// Code credits: keanw.com
        /// The original code is found <see href="https://www.keanw.com/2012/09/overriding-the-grips-of-an-autocad-polyline-to-maintain-fillet-segments-using-net.html">here</see>:
        /// Check if the polyline topology is compatible with a
        /// sequence of segments with internal rounded edges.
        /// Used to activate an alternate grip handling where
        /// user can control segment vertexes through apparent
        /// intersections.
        /// </summary>
        /// <param name="p">Polyline to check</param>
        /// <returns>Compatibility with alternate grip handling</returns>
        public static bool IsPolylineRoundedEdge(Polyline p)
        {
            bool result = false;

            if (p != null)
            {
                // Quick check if it contains at least one arc and two
                // segments, that means at least 4 points also check
                // for planar entity
                if (p.IsPlanar && p.HasBulges && (p.NumberOfVertices > 3))
                {
                    SegmentType prevType = p.GetSegmentType(0);

                    // First segment must be a line
                    if (prevType == SegmentType.Line)
                    {
                        result = true;
                        for (int i = 1; i < p.NumberOfVertices; i++)
                        {
                            SegmentType currType = p.GetSegmentType(i);
                            if (currType == SegmentType.Line || currType == SegmentType.Arc)
                            {
                                if (currType == prevType)
                                {
                                    result = false;
                                    break;
                                }

                                prevType = currType;
                            }
                        }

                        // Check if also the last segment is a line, or if
                        // it's an arc, the polyline must be closed, like
                        // a rounded box
                        if (prevType == SegmentType.Arc && !p.Closed)
                            result = false;
                    }
                }
            }
            return result;
        }

        /// <summary>
        /// Rebuild polyline with new grip information
        /// </summary>
        /// <param name="polyline">Polyline to be rebuilt</param>
        /// <param name="original_grips">Grips collection</param>
        public static void RebuildPolyline(Polyline p, GripDataCollection grips)
        {
            try
            {
                int nPnt = grips.Count;

                // Create vertex info from gripdata collection
                CustomVertex[] vFill = new CustomVertex[nPnt];

                // Initializa first and last vertex with first and
                // last grip points
                vFill[0] = new CustomVertex(grips[0].GripPoint);
                vFill[nPnt - 1] = new CustomVertex(grips[grips.Count - 1].GripPoint);

                // Retrieve adjacent segments (three consecutive points)
                for (int i = 1; i < (nPnt - 1); i++)
                {
                    CustomGripData gd_prev = grips[i - 1] as CustomGripData;
                    CustomGripData gd_center = grips[i] as CustomGripData;
                    CustomGripData gd_next = grips[i + 1] as CustomGripData;

                    // New vertex info
                    vFill[i] = new CustomVertex();
                    vFill[i].radius = gd_center.m_radius;

                    // Calc fillet information, if possible with current
                    // radius and segments length
                    if (!CsMath.LinesFillet(gd_center.GripPoint, gd_prev.GripPoint,
                        gd_next.GripPoint, ref vFill[i], true)
                    )
                    {
                        // Unable to find a solution, remove fillet information
                        vFill[i].p1 = vFill[i].p2 = vFill[i].pc = 
                            vFill[i].pOrg = gd_center.GripPoint;
                        
                        vFill[i].radius = 0.0;
                    }
                }

                if (p.Closed)
                {
                    // Add vertex information on last grip
                    CustomGripData gd_prev = grips[grips.Count - 2] as CustomGripData;
                    CustomGripData gd_center = grips[grips.Count - 1] as CustomGripData;
                    CustomGripData gd_next = grips[0] as CustomGripData;

                    // Last point may coincident with first or not
                    if (gd_center.GripPoint.DistanceTo(gd_prev.GripPoint) < CsMath.dEpsilon)
                        gd_prev = grips[grips.Count - 3] as CustomGripData;

                    vFill[nPnt - 1] = new CustomVertex();
                    vFill[nPnt - 1].radius = gd_center.m_radius;

                    // Calc fillet information, if possible with current
                    // radius and segments length
                    if (!CsMath.LinesFillet(gd_center.GripPoint, gd_prev.GripPoint,
                        gd_next.GripPoint, ref vFill[nPnt - 1], false))
                    {
                        // Unable to find a solution, remove fillet information
                        vFill[nPnt - 1].p1 = vFill[nPnt - 1].p2 =
                          vFill[nPnt - 1].pc = vFill[nPnt - 1].pOrg = gd_center.GripPoint;

                        vFill[nPnt - 1].radius = 0.0;
                    }

                    // Recalc vertex information on first grip
                    gd_prev = grips[grips.Count - 1] as CustomGripData;
                    gd_center = grips[0] as CustomGripData;
                    gd_next = grips[1] as CustomGripData;

                    // Last point may coincident with first or not
                    if (gd_center.GripPoint.DistanceTo(gd_prev.GripPoint) < CsMath.dEpsilon)
                        gd_prev = grips[grips.Count - 2] as CustomGripData;

                    vFill[0] = new CustomVertex();
                    vFill[0].radius = gd_center.m_radius;

                    // Calc fillet information, if possible with
                    // current radius and segments length
                    if (!CsMath.LinesFillet(gd_center.GripPoint, gd_prev.GripPoint,
                        gd_next.GripPoint, ref vFill[0], false))
                    {
                        // Unable to find a solution, remove fillet information
                        vFill[0].p1 = vFill[0].p2 = vFill[0].pc =
                            vFill[0].pOrg = gd_center.GripPoint;

                        vFill[0].radius = 0.0;
                    }
                }

                // Everything seem ok, rebuild polyline
                bool bIsClosed = p.Closed; // remember if original was closed

                // Clear current points definitions
                p.Closed = false;
                while (p.NumberOfVertices > 1)
                    p.RemoveVertexAt(0); // Cannot completely clear points

                // Add new points and segments definition
                for (int i = 0; i < (nPnt - 1); i++)
                {
                    // Add linesegment only if lenght is not null
                    if (vFill[i].p2.DistanceTo(vFill[i + 1].p1) > CsMath.dEpsilon)
                        p.AddVertexAt(p.NumberOfVertices, new Point2d(vFill[i].p2.X, 
                            vFill[i].p2.Y), 0, 0, 0);

                    // End point always valid with bulge information if needed
                    p.AddVertexAt(p.NumberOfVertices, new Point2d(vFill[i + 1].p1.X, 
                        vFill[i + 1].p1.Y), vFill[i + 1].bulge, 0, 0);
                }

                p.RemoveVertexAt(0); // Remove last of old points

                // If closed re-add first point with bulge
                if (bIsClosed)
                {
                    // Add linesegment only if length is not null
                    if (vFill[nPnt - 1].p2.DistanceTo(vFill[0].p1) > CsMath.dEpsilon)
                        p.AddVertexAt(p.NumberOfVertices, 
                            new Point2d(vFill[nPnt - 1].p2.X, vFill[nPnt - 1].p2.Y), 0, 0, 0);

                    // End point always valid with bulge information if needed
                    p.AddVertexAt(p.NumberOfVertices, 
                        new Point2d(vFill[0].p1.X, vFill[0].p1.Y), vFill[0].bulge, 0, 0);

                    p.Closed = true; // Restore closed status
                }
            }
            catch { }
        }

        /// <summary>
        /// Compute fillet information for two converging segments.
        /// The fillet information i3s returned through a myVertex class
        /// that will hold information about the arc start point on the
        /// first segment, the end point on the second segment, the arc
        /// origin and the arc midpoint on the bisetrix
        /// </summary>
        /// <param name="pOrg">Intersection point of the segments</param>
        /// <param name="p1">Start point of the first segment</param>
        /// <param name="p2">End point of the second segment</param>
        /// <param name="v">Info class with requested radius to be
        /// filled with fillet info</param>
        /// <returns>True if a fillet is possible, false if no solution
        /// can be found</returns>
        public static bool LinesFillet(Point3d pOrg, Point3d p1, Point3d p2, 
            ref CustomVertex v, bool bAllowRadiusReduction)
        {
            try
            {
                bool bInversionOccured = false;

                // Check point validity
                Double d1 = pOrg.DistanceTo(p1);
                Double d2 = pOrg.DistanceTo(p2);
                Double dd = p1.DistanceTo(p2);

                if (d1 < CsMath.dEpsilon)
                    return false; // Segment p1-porg null
                if (d2 < CsMath.dEpsilon)
                    return false; // Segment p2-porg null
                if (dd < CsMath.dEpsilon)
                    return false; // Overlapping segments
                if (Math.Abs(dd - d1 - d2) < CsMath.dEpsilon)
                    return false; // Co-linear segments
                if (v.radius < CsMath.dEpsilon)
                    return false; // Radius too small

                // Sort points to keep the smaller angle always as p1-pOrg-p2
                if (CsMath.DistFromLine(pOrg, p1, p2) < 0)
                {
                    // Point p2 is on the left side of vector pOrg->p1
                    // should be on the right: switch points
                    Point3d pp = p2;
                    p2 = p1;
                    p1 = pp;
                    bInversionOccured = true;  // Mark that inversion occurred
                }

                // Get sine/cosine coefficients
                CsCosDir r1 = new CsCosDir(pOrg, p1);
                CsCosDir r2 = new CsCosDir(pOrg, p2);

                // Get bisetrix where arc origin and midpoint lay
                CsCosDir rb = new CsCosDir();

                // Shouldn't happen, co-linear segments already checked
                if (!CsMath.FindBisect(r1, r2, ref rb))
                    return false;

                // The bisetrix and the projection of the arc origin on
                // either segment forms a rect triangle. Align segment
                // coefficient to X axis, so the radius would be aligned
                // to Y axis
                Double cxr = rb.cx * r1.cx + rb.cy * r1.cy;
                Double cyr = -rb.cx * r1.cy + rb.cy * r1.cx;

                // Get hypotenuse given the radius and sine coefficient
                Double ipo = v.radius / cyr;

                // Get other catete: distance from pOrg and the fillet
                // points on segments
                Double l1 = ipo * cxr;

                // If allowed, check if fillet point lays outside the
                // smallest segment lenght
                if (bAllowRadiusReduction)
                {
                    Double dmin = (d1 < d2) ? d1 : d2;

                    if (l1 > dmin)
                    {
                        // Reduce radius to keep fillet inside segment
                        v.radius = dmin * cyr / cxr;

                        // Use new radius for computation
                        ipo = v.radius / cyr;
                        l1 = ipo * cxr;
                    }
                }

                // Given the length, get arc start and end points on
                // each segment. Beware of segment switch, if occurred
                if (bInversionOccured)
                {
                    v.p2 = new Point3d(pOrg.X + l1 * r1.cx, pOrg.Y + l1 * r1.cy, 0.0);
                    v.p1 = new Point3d(pOrg.X + l1 * r2.cx, pOrg.Y + l1 * r2.cy, 0.0);
                }
                else
                {
                    v.p1 = new Point3d(pOrg.X + l1 * r1.cx, pOrg.Y + l1 * r1.cy, 0.0);
                    v.p2 = new Point3d(pOrg.X + l1 * r2.cx, pOrg.Y + l1 * r2.cy, 0.0);
                }

                // Get arc midpoint on bisetrix
                v.pc = new Point3d(pOrg.X + (ipo - v.radius) * rb.cx,
                    pOrg.Y + (ipo - v.radius) * rb.cy, 0.0);

                // Get arc origin
                v.pOrg = new Point3d(pOrg.X + ipo * rb.cx, pOrg.Y + ipo * rb.cy, 0.0);

                // Compute bulge using the formula B = 2*H/D, where D is
                // the chord and H the distance of the chord midpoint
                // and the arc midpoint
                Double D = v.p1.DistanceTo(v.p2);
                Double H = Math.Abs(CsMath.DistFromLine(v.p1, v.p2, v.pc));

                if (D > CsMath.dEpsilon) v.bulge = 2 * H / D;

                // Bulge should be positive for counterclockwise arcs and
                // negative for clockwise. Adjust sign according with
                // segment order switch, if occurred
                if (!bInversionOccured) v.bulge = -v.bulge;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("[LinesFillet]" + ex.Message);
                return false;
            }

            return true;
        }

        /// <summary>
        /// Get plausible fillet radius from two converging linesegments
        /// </summary>
        /// <param name="l1">First segment</param>
        /// <param name="l2">Second segment</param>
        /// <param name="pPlane">Working plane</param>
        /// <param name="pOut">Resulting point, if available</param>
        /// <returns></returns>
        public static Double GetFilletRadius(LineSegment3d l1, LineSegment3d l2, Plane pPlane)
        {
            Double result = 0.0;

            // Get 2d points on working plane
            Point2d p1 = l1.StartPoint.Convert2d(pPlane);
            Point2d p2 = l1.EndPoint.Convert2d(pPlane);

            Point2d q1 = l2.StartPoint.Convert2d(pPlane);
            Point2d q2 = l2.EndPoint.Convert2d(pPlane);

            Point2d pInt = Point2d.Origin;

            // Get intersection point
            IntersectLinesState res = IntersectLines(p1, p2, q1, q2, out pInt);
            if (res == IntersectLinesState.ApparentIntersect 
                || res == IntersectLinesState.RealIntersect)
            {
                // Get the endpoints closest to the intersection
                Point2d l1_end = p1.GetDistanceTo(pInt) < p2.GetDistanceTo(pInt) ? p1 : p2;
                Point2d l2_end = q1.GetDistanceTo(pInt) < q2.GetDistanceTo(pInt) ? q1 : q2;

                // Throw a perpendicular vector on the endpoints closest
                // to the intersection
                Line2d lp1 = new LineSegment2d(p1, p2).GetPerpendicularLine(l1_end);
                Line2d lp2 = new LineSegment2d(q1, q2).GetPerpendicularLine(l2_end);

                // Get intersection of projection lines (center of fillet,
                // if segments already have fillet)
                Point2d[] pInts = lp1.IntersectWith(lp2);
                if (pInts != null)
                {
                    // Get distance of center of fillet from endpoints
                    Double r1 = l1_end.GetDistanceTo(pInts[0]);
                    Double r2 = l2_end.GetDistanceTo(pInts[0]);

                    // If the two segments were already rounded, the two
                    // distances should be the same and equals to the fillet
                    // radius. If the segments have been moved or stretched,
                    // the distances may differ. We take the lower radius
                    // as the minimum fillet radius available
                    result = Math.Min(r1, r2);
                }
            }

            return result;
        }


        /// <summary>
        /// Check intersection for two segments on plane pPlane
        /// </summary>
        /// <param name="l1">First segment</param>
        /// <param name="l2">Second segment</param>
        /// <param name="pPlane">Working plane</param>
        /// <param name="pOut">Resulting point, if available</param>
        /// <returns></returns>
        public static bool CheckIntersect(LineSegment3d l1, LineSegment3d l2, 
            Plane pPlane, ref Point3d pOut)
        {
            bool result = false;

            // Get 2d points on working plane
            Point2d p1 = l1.StartPoint.Convert2d(pPlane);
            Point2d p2 = l1.EndPoint.Convert2d(pPlane);
            Point2d q1 = l2.StartPoint.Convert2d(pPlane);
            Point2d q2 = l2.EndPoint.Convert2d(pPlane);
            Point2d pInt = Point2d.Origin;

            IntersectLinesState res = IntersectLines(p1, p2, q1, q2, out pInt);
            if (res == IntersectLinesState.ApparentIntersect 
                || res == IntersectLinesState.RealIntersect)
            {
                pOut = new Point3d(pPlane, pInt);
                result = true;
            }

            return result;
        }

        /// <summary>
        /// Possible results for IntersectLines() function
        /// </summary>
        public enum IntersectLinesState
        {
            InvalidPoints = -1,    // Invalid points (coincident?)
            RealIntersect = 0,     // Real intersection found, pInt valid
            ApparentIntersect = 1, // Apparent inters. found, pInt valid
            NoIntersection = 2,    // Segments are parallel
            OverLapping = 3,       // Segments are overlapping
            Colinear = 4           // Segments are co-linear
        }

        /// <summary>
        /// Try to get intersection point of two segment.
        /// The intersection may be real or apparent.
        /// The resulting point is
        /// </summary>
        /// <param name="p1">Start point of first segment</param>
        /// <param name="p2">End point of first segment</param>
        /// <param name="q1">Start point of second segment</param>
        /// <param name="q2">End point of second segment</param>
        /// <param name="pInt">[out] Resulting intersection point</param>
        /// <returns>Result validity state</returns>
        public static IntersectLinesState IntersectLines(
          Point2d p1, Point2d p2, // First segment
          Point2d q1, Point2d q2, // Second segment
          out Point2d pInt        // Intersecting point
        )
        {
            IntersectLinesState result = IntersectLinesState.NoIntersection;
            pInt = Point2d.Origin;

            // Get sine/cosine coefficients for the two segments
            CsCosDir r1 = new CsCosDir(p1, p2);
            CsCosDir r2 = new CsCosDir(q1, q2);

            // Check coefficients, if points are coincident, segments are null
            if ((r1.cx == 0.0 && r1.cy == 0.0) 
                || (r2.cx == 0.0 && r2.cy == 0.0))
            {
                // Coincident points? Invalid segments data
                return IntersectLinesState.InvalidPoints;
            }

            // Intersection coefficients
            Double a1 = r1.cx, a2 = r1.cy;
            Double b1 = -r2.cx, b2 = -r2.cy;
            Double c1 = q1.X - p1.X, c2 = q1.Y - p1.Y;

            // Get denominator, if null, segments have same direction
            Double dden = a1 * b2 - a2 * b1;

            if (Math.Abs(dden) > CsMath.dEpsilon)
            {
                // Valid denominator, lines are not parallels or co-linear
                // now, intersection linear parameter may be get either
                // from first or second segment. Do both to check for
                // real or apparent intersection.

                // Linear parameter for second segment
                Double tt = (c1 * b2 - c2 * b1) / dden;

                // Linear parameter for first segment
                Double vv = (c2 * a1 - c1 * a2) / dden;

                // Intersection point from first segment parameter
                pInt = new Point2d(q1.X + r2.cx * vv, q1.Y + r2.cy * vv);

                // To be 'real', intersection point must lay on both
                // segments (parameter from 0 to 1). Otherwise, we set
                // it as 'apparent'. Get normalized linear parameter
                // (at this stage denominator are intrinsicly valid,
                // no need to check for DIVBYZERO)
                tt = tt / p1.GetDistanceTo(p2);
                vv = vv / q1.GetDistanceTo(q2);

                // Check if both coefficients lay within 0 and 1
                if (tt > -CsMath.dEpsilon && tt < (1 + CsMath.dEpsilon) 
                    && vv > -CsMath.dEpsilon && vv < (1 + CsMath.dEpsilon))
                {
                    result = IntersectLinesState.RealIntersect;
                }
                else
                {
                    result = IntersectLinesState.ApparentIntersect;
                }
            }
            else
            {
                // Segments are parallel or co-linear (have same direction).
                // Check coefficients for a new connecting segment with
                // points taken from both original segment. If coefficients
                // are the same, segments are co-linear, otherwise are
                // parallel
                CsCosDir rx;

                // Avoid coincident points
                if (p1.GetDistanceTo(q1) > CsMath.dEpsilon)
                    rx = new CsCosDir(p1, q1);
                else
                    rx = new CsCosDir(p1, q2);

                if (Math.Abs(rx.cx - r1.cx) < CsMath.dEpsilon &&
                    Math.Abs(rx.cy - r1.cy) < CsMath.dEpsilon)
                {
                    // Same coefficient, segments lay on the same vector.
                    // Check if there is any overlapping by checking distances
                    // from the two ends of a segments and another end.
                    // Sum of distances must be equal or higher than segment
                    // length, otherwise they are partially overlapping
                    Double ll = p2.GetDistanceTo(p1);

                    bool bOver1 = q1.GetDistanceTo(p1) + q1.GetDistanceTo(p2) 
                        > ll + CsMath.dEpsilon;
                    bool bOver2 = q2.GetDistanceTo(p1) + q2.GetDistanceTo(p2) 
                        > ll + CsMath.dEpsilon;

                    result = bOver1 && bOver2 ? IntersectLinesState.Colinear :
                        IntersectLinesState.OverLapping;
                }
                else
                {
                    // Parallel segments, no intersection
                    result = IntersectLinesState.NoIntersection;
                }
            }

            return result;
        }

        /// <summary>
        /// Given a vector going from L1 to L2, get the projected
        /// distance of point P from the vector.
        /// If distance is positive, the point is on the right side
        /// of the vector, if distance is negative, the point is on
        /// the left side of the vector.
        /// Function assume all points lay on the same plane
        /// </summary>
        /// <param name="l1">Vector origin</param>
        /// <param name="l2">Vector direction</param>
        /// <param name="p">Point to get distance from</param>
        /// <returns>Projected distance of P from vector L1->L2</returns>
        public static Double DistFromLine(Point3d l1, Point3d l2, Point3d p)
        {
            CsCosDir c = new CsCosDir(l1, l2);
            return (((p.Y - l1.Y) * c.cx) - ((p.X - l1.X) * c.cy));
        }

        /// <summary>
        /// Get bisetrix coefficients of two vectors.
        /// Vectors are provided as coefficients and the order must
        /// be given in counterclockwise order.
        /// This is a 2d computation, vectors must lay on the same plane
        /// </summary>
        /// <param name="r1">First vector</param>
        /// <param name="r2">Second vector</param>
        /// <param name="rb">[out]Resulting coefficients</param>
        /// <returns>True if bisetrix has been found, false if not
        /// (parallel vectors?)</returns>
        public static bool FindBisect(CsCosDir r1, CsCosDir r2, ref CsCosDir rb)
        {
            bool result = true;

            // Coefficients may be considered as points of a limited
            // space that goes from -1 to 1
            // Origin 0,0 is the intersection of the two vectors and
            // origin of the bisetrix

            // Quick check for vectors (almost) co-linear
            Double diste = Math.Sqrt((r2.cx - r1.cx) * (r2.cx - r1.cx) 
                + (r2.cy - r1.cy) * (r2.cy - r1.cy));
            if (diste < CsMath.dEpsilon)
            {
                // Vectors have very similar coefficients, use alternate
                // algorithm for a borderline situation
                Double dcx = (r2.cx + r1.cx) * 0.5;
                Double dcy = (r2.cy + r1.cy) * 0.5;
                Double dd = Math.Sqrt(dcx * dcx + dcy * dcy);

                if (dd < CsMath.dEpsilon)
                {
                    // Really same coefficients, could be co-linear or
                    // parallel but cannot say because we don't have
                    // points on segments, just direction
                    result = false;
                }
                else
                {
                    // Denominator valid, use point distance from vector
                    // to get position of the first vector
                    Double distc = r1.cx * (r2.cy - r1.cy) - r1.cy * (r2.cx - r1.cx);
                    Double dSign = (distc < 0) ? -1.0 : 1.0;

                    rb.cx = dSign * dcx / dd;
                    rb.cy = dSign * dcy / dd;
                }
            }
            else
            {
                // Vectors normally spaced, use simpler formula
                Double dcx = (r2.cx - r1.cx);
                Double dcy = (r2.cy - r1.cy);
                Double dd = Math.Sqrt(dcx * dcx + dcy * dcy);

                rb.cx = dcy / dd;
                rb.cy = -dcx / dd;
            }

            return result;
        }
    }
}
