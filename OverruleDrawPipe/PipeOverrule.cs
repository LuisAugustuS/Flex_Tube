using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.GraphicsInterface;
using Autodesk.AutoCAD.Runtime;
using Bundles.Commands;
using Bundles.ExplodeOverrule;
using Bundles.ExtensionMethods;
using System;
using Polyline = Autodesk.AutoCAD.DatabaseServices.Polyline;

namespace Bundles.DrawPipeOverrule
{
    /// <summary>
    /// Base class for overruling the drawing behavior of objects to represent them as pipes.
    /// Code credits: keanw.com
    /// The original code is found <see href="https://www.keanw.com/2012/09/overriding-the-grips-of-an-autocad-polyline-to-maintain-fillet-segments-using-net.html">here</see>:
    /// </summary>
    public class PipeOverrule : DrawableOverrule
    {
        // Name used for the extension dictionary entry.
        // This ensures our overrule is applied only to objects with this specific Xrecord.
        private string _entryName;

        // Singleton instance of the overrule.
        private static PipeOverrule _pipeOverrule;

        // Static flag to track the activation state of the overrule
        private static bool _isOverruleActive = false;

        public PipeOverrule(string entryName = null)
        {
            _entryName = entryName ?? OverruleSettings.EntryName;

            // Constructor that initializes the overrule by setting
            // a filter based on the extension dictionary entry.
            // This filter ensures that the overrule is applied only to objects
            // that have the specified extension dictionary entry.
            SetExtensionDictionaryEntryFilter(_entryName);
        }

        /// <summary>
        /// Activates the custom LinePipe overrule.
        /// </summary>
        public static void StartOverrule(string entryName)
        {
            if (!_isOverruleActive)
            {
                _pipeOverrule = new PipeOverrule(entryName);
                Overrule.AddOverrule(RXObject.GetClass(typeof(Polyline)), _pipeOverrule, false);
                Overrule.Overruling = true;
                _isOverruleActive = true; // Update the overrule activation state
            }
        }

        /// <summary>
        /// Deactivates the custom LinePipe overrule.
        /// </summary>
        public static void EndOverrule()
        {
            if (_isOverruleActive)
            {
                Overrule.RemoveOverrule(RXObject.GetClass(typeof(Polyline)), _pipeOverrule);
                Overrule.Overruling = false;
                _isOverruleActive = false; // Update the overrule activation state
            }
        }

        /// <summary>
        /// Custom drawing logic to represent a polyline as a pipe.
        /// </summary>
        /// <param name="d">The drawable object, expected to be a polyline.</param>
        /// <param name="wd">World drawing context.</param>
        /// <returns>True if custom drawing was performed, false to fall back to default drawing.</returns>
        public override bool WorldDraw(Drawable d, WorldDraw wd)
        {
            double internalDiameter = 0.0;

            // Attempt to get the internal diameter from the drawable object if it's a DBObject.
            if (d is DBObject) internalDiameter = PipeDiameterForObject((DBObject)d, _entryName);

            if (internalDiameter > 0.0)
            {
                Polyline line = d as Polyline;

                // Check if the drawable is a non-closed polyline.
                if (line != null && !line.Closed)
                {
                    // Draw the original polyline with any existing overrules.
                    base.WorldDraw(line, wd);

                    // Proceed only if the polyline has a valid ID and non-zero length.
                    if (!line.Id.IsNull && line.Length > 0.0)
                    {
                        Document doc = Application.DocumentManager.MdiActiveDocument;
                        Editor edt = doc.Editor;

                        // Save and set the desired color traits for drawing.
                        EntityColor c = wd.SubEntityTraits.TrueColor;
                        wd.SubEntityTraits.Color = 6; // Arbitrary color index.
                        wd.SubEntityTraits.TrueColor = c;

                        // Clone the polyline for manipulation.
                        Polyline clonedPline = d.Clone() as Polyline;
                        Polyline path = d.Clone() as Polyline;

                        // Calculate the spacing and dimensions for the visual representation of the pipe.
                        var spaceBetweenElements = internalDiameter / 10.0;
                        var externalDiameter = internalDiameter + spaceBetweenElements;
                        var fullLengthCurve = clonedPline.Length;
                        var width3Segments = spaceBetweenElements * 3.0;
                        var segments = Math.Round(fullLengthCurve / width3Segments) * 3.0;
                        segments++;
                        var widthAllSegments = segments * spaceBetweenElements;

                        // Adjust segment count if necessary.
                        if (widthAllSegments > fullLengthCurve) segments -= 3.0;

                        if (segments >= 3)
                        {
                            clonedPline.ConstantWidth = internalDiameter;
                            wd.SubEntityTraits.Color = 251; // Arbitrary color index.
                            clonedPline.WorldDraw(wd);
                            clonedPline.Dispose();

                            var remainder = fullLengthCurve - (segments * spaceBetweenElements);
                            spaceBetweenElements += remainder / segments;
                            var elementWidth = spaceBetweenElements * 2.0;
                            var stepCounter = spaceBetweenElements;

                            // Draw segments along the polyline to represent the pipe.
                            while (!IsApproximatelyEqualTo(stepCounter, fullLengthCurve, 0.0001) && stepCounter < fullLengthCurve)
                            {
                                // Calculate segment endpoints and directions.
                                Point3d pt1 = path.GetPointAtDist(stepCounter);
                                stepCounter += elementWidth;
                                Point3d pt2 = path.GetPointAtDist(stepCounter);

                                // Determine angles and positions for the pipe representation.
                                Vector3d dir = path.GetFirstDerivative(path.GetClosestPointTo(pt1, false))
                                    .TransformBy(edt.CurrentUserCoordinateSystem);

                                var ang = Vector3d.XAxis.GetAngleTo(dir, Vector3d.ZAxis);
                                var positiveAng = ang + (Math.PI * 0.5);
                                var negativeAng = ang - (Math.PI * 0.5);
                                var tubeRadius = externalDiameter / 2;

                                // Calculate corner points for the pipe segment.
                                Point3d corner_1 = PolarPoint(pt1, positiveAng, tubeRadius);
                                Point3d corner_2 = PolarPoint(pt1, negativeAng, tubeRadius);

                                dir = path.GetFirstDerivative(path.GetClosestPointTo(pt2, false)).TransformBy(edt.CurrentUserCoordinateSystem);
                                ang = Vector3d.XAxis.GetAngleTo(dir, Vector3d.ZAxis);
                                positiveAng = ang + (Math.PI * 0.5);
                                negativeAng = ang - (Math.PI * 0.5);

                                Point3d corner_3 = PolarPoint(pt2, positiveAng, tubeRadius);
                                Point3d corner_4 = PolarPoint(pt2, negativeAng, tubeRadius);
                                Point3d elementStart = RibMidPoint(corner_1, corner_3);
                                Point3d elementEnd = RibMidPoint(corner_2, corner_4);

                                // Determine if the segment should be constructed based on its width.
                                var minWidth = elementWidth / 4;
                                var maxWidth = elementWidth + (minWidth * 3);
                                var widthElementStart = corner_1.DistanceTo(corner_3);
                                var widthElementEnd = corner_2.DistanceTo(corner_4);

                                bool constructRib = true;

                                if (Math.Round(widthElementStart, 2) < Math.Round(minWidth, 2) ||
                                    Math.Round(widthElementStart, 2) > Math.Round(maxWidth, 2) ||
                                    Math.Round(widthElementEnd, 2) < Math.Round(minWidth, 2) ||
                                    Math.Round(widthElementEnd, 2) > Math.Round(maxWidth, 2))
                                    constructRib = false;

                                // Draw the pipe segment if applicable.
                                if (constructRib)
                                {
                                    Polyline rib = new Polyline();
                                    rib.AddVertexAt(0, new Point2d(elementStart.X, elementStart.Y), 0, 0, 0);
                                    rib.AddVertexAt(1, new Point2d(elementEnd.X, elementEnd.Y), 0, 0, 0);
                                    rib.SetStartWidthAt(0, widthElementStart);
                                    rib.SetEndWidthAt(0, widthElementEnd);

                                    wd.SubEntityTraits.Color = 8; // Arbitrary color index.

                                    rib.WorldDraw(wd);
                                    rib.Dispose();
                                }

                                stepCounter += spaceBetweenElements;
                            }

                            path.Dispose();

                            // Create and draw start and end circles for the pipe.
                            double radius = internalDiameter / 10;
                            Circle startCircle = new Circle(line.StartPoint, line.Normal, radius);
                            startCircle.WorldDraw(wd);
                            startCircle.Dispose();

                            Circle endCircle = new Circle(line.EndPoint, line.Normal, radius);
                            endCircle.WorldDraw(wd);
                            endCircle.Dispose();

                            // Calculate and draw a label for the pipe's length.
                            Point3d midpoint = line.GetPointAtDist(line.Length / 2);
                            Vector3d direction = line.GetFirstDerivative(line.GetClosestPointTo(midpoint, false)).TransformBy(edt.CurrentUserCoordinateSystem);
                            var angle = Vector3d.XAxis.GetAngleTo(direction, Vector3d.ZAxis);

                            // Calculate text insertion point with an offset.
                            Point3d textInsertionPoint;
                            if (angle > (Math.PI * 0.5) && angle < (Math.PI * 1.5))
                            {
                                textInsertionPoint = PolarPoint(midpoint, angle - (Math.PI * 0.5), (internalDiameter / 2) + 1.5);
                                angle += Math.PI;
                            }
                            else
                            {
                                textInsertionPoint = PolarPoint(midpoint, angle + (Math.PI * 0.5), (internalDiameter / 2) + 1.5);
                            }

                            // Format and draw the length label.
                            string lengthAsString = Math.Round(line.Length).ToString() + "{\\H0.8x;mm}";
                            MText mText = new MText();

                            // The font size can be changed using the TEXTSIZE variable
                            // In my opinion, the best scenario would be to create your own text style for the labels.
                            mText.SetDatabaseDefaults();
                            mText.Attachment = AttachmentPoint.BottomCenter;
                            mText.Location = textInsertionPoint;
                            mText.Contents = lengthAsString;
                            mText.Rotation = angle;
                            mText.Width = 0;
                            mText.Height = 0;
                            mText.WorldDraw(wd);

                            // Clean up.
                            line.Dispose();
                            mText.Dispose();
                        }
                    }

                    return true; // Indicate custom drawing was performed.
                }
            }
            return base.WorldDraw(d, wd); // Fall back to default drawing if conditions not met.
        }

        /// <summary>
        /// Helper method to determine if two double values are approximately equal.
        /// </summary>
        /// <param name="initialValue">The first value to compare.</param>
        /// <param name="value">The second value to compare.</param>
        /// <param name="maximumDifferenceAllowed">The maximum allowed difference between the values.</param>
        /// <returns>True if the values are approximately equal, false otherwise.</returns>
        public bool IsApproximatelyEqualTo(double initialValue, double value, double maximumDifferenceAllowed)
        {
            // Compares two floating-point values with a tolerance for minor differences.
            return (Math.Abs(initialValue - value) < maximumDifferenceAllowed);
        }

        /// <summary>
        /// Calculates a new point at a specified distance and angle from a base point.
        /// </summary>
        /// <param name="basepoint">The point from which to calculate the new point.</param>
        /// <param name="angle">The angle in radians from the base point to the new point.</param>
        /// <param name="distance">The distance from the base point to the new point.</param>
        /// <returns>The calculated point.</returns>
        public Point3d PolarPoint(Point3d basepoint, double angle, double distance)
        {
            // Uses polar coordinates to calculate a point given a base point, distance, and angle.
            return new Point3d(
                basepoint.X + (distance * Math.Cos(angle)),
                basepoint.Y + (distance * Math.Sin(angle)),
                basepoint.Z
            );
        }

        /// <summary>
        /// Calculates the midpoint between two points.
        /// </summary>
        /// <param name="a">The first point.</param>
        /// <param name="b">The second point.</param>
        /// <returns>The midpoint between the two points.</returns>
        public Point3d RibMidPoint(Point3d a, Point3d b)
        {
            // Calculates the midpoint by averaging the coordinates of two points.
            return new Point3d(
                (a.X + b.X) / 2,
                (a.Y + b.Y) / 2,
                a.Z
            );
        }

        /// <summary>
        /// Retrieves the "pipe diameter" from the Xrecord associated with a given object, if it exists.
        /// </summary>
        /// <param name="obj">The database object to query for pipe diameter.</param>
        /// <returns>The pipe diameter if available; otherwise, 0.0.</returns>
        public static double PipeDiameterForObject(DBObject obj, string _entryName)
        {
            double res = 0.0; // Default result if no diameter is found.
            using (var tr = new OpenCloseTransaction())
            {
                // Attempt to get Xrecord data from the object's extension dictionary using a specific entry name.
                if (obj.TryGetXDictionaryXrecordData(tr, _entryName, out ResultBuffer rb))
                {
                    using (rb) // Ensure the ResultBuffer is disposed properly.
                    {
                        foreach (TypedValue tv in rb)
                        {
                            // TypeCode 40 represents double data in DXF code. If found,
                            // we interpret it as the pipe diameter.
                            if (tv.TypeCode == 40)
                            {
                                res = (double)tv.Value; // Cast the value to double and break out of the loop.
                                break;
                            }
                        }
                    }
                }
            }

            return res; // Return the result, which will be the diameter if found, or 0.0 otherwise.
        }

        /// <summary>
        /// Sets the "pipe diameter" in an Xrecord in the extension dictionary of a specified object.
        /// </summary>
        /// <param name="tr">The active transaction.</param>
        /// <param name="obj">The database object on which to set the pipe diameter.</param>
        /// <param name="radius">The radius of the pipe to set.</param>
        public static void SetPipeRadiusOnObject(Transaction tr, DBObject obj, double radius, string _entryName)
        {
            // Create a new ResultBuffer containing a TypedValue representing the pipe diameter (DXF code 40).
            using (ResultBuffer rb = new ResultBuffer(new TypedValue(40, radius)))
            {
                // Use the extension method to set this Xrecord data on the object's extension dictionary
                // under our predefined entry name.
                obj.SetXDictionaryXrecordData(tr, _entryName, rb);
            }
        }
    }
}