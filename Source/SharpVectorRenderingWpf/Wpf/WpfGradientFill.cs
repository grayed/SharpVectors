﻿using System;
using System.Xml;
using System.Windows;
using System.Windows.Media;

using SharpVectors.Dom.Svg;
using SharpVectors.Renderers.Utils;
using SharpVectors.Runtime;

namespace SharpVectors.Renderers.Wpf
{
    public sealed class WpfGradientFill : WpfFill
    {
        #region Private Fields

        private SvgGradientElement _gradientElement;

        #endregion

        #region Constructors and Destructor

        public WpfGradientFill(SvgGradientElement gradientElement)
        {
            _gradientElement = gradientElement;
        }

        #endregion

        #region Public Methods

        public override Brush GetBrush(Rect elementBounds, WpfDrawingContext context, Transform viewTransform)
        {
            SvgLinearGradientElement linearGrad = _gradientElement as SvgLinearGradientElement;
            if (linearGrad != null)
            {
                return GetLinearGradientBrush(linearGrad, viewTransform);
            }

            SvgRadialGradientElement radialGrad = _gradientElement as SvgRadialGradientElement;
            if (radialGrad != null)
            {
                return GetRadialGradientBrush(radialGrad);
            }

            return new SolidColorBrush(Colors.Black);
        }

        #endregion

        #region Private Methods

        private Brush GetLinearGradientBrush(SvgLinearGradientElement res, Transform viewBoxTransform = null)
        {
            double x1 = res.X1.AnimVal.Value;
            double x2 = res.X2.AnimVal.Value;
            double y1 = res.Y1.AnimVal.Value;
            double y2 = res.Y2.AnimVal.Value;

            GradientStopCollection gradientStops = GetGradientStops(res.Stops);
            if (gradientStops == null || gradientStops.Count == 0)
            {
                return null;
            }

            LinearGradientBrush brush = new LinearGradientBrush(gradientStops,
                new Point(x1, y1), new Point(x2, y2));

            SvgSpreadMethod spreadMethod = SvgSpreadMethod.Pad;
            if (res.SpreadMethod != null)
            {
                spreadMethod = (SvgSpreadMethod)res.SpreadMethod.AnimVal;

                if (spreadMethod != SvgSpreadMethod.None)
                {
                    brush.SpreadMethod = WpfConvert.ToSpreadMethod(spreadMethod);
                }
            }

            SvgUnitType mappingMode = SvgUnitType.ObjectBoundingBox;
            if (res.GradientUnits != null)
            {
                mappingMode = (SvgUnitType)res.GradientUnits.AnimVal;
                if (mappingMode == SvgUnitType.ObjectBoundingBox)
                {
                    brush.MappingMode = BrushMappingMode.RelativeToBoundingBox;
                }
                else if (mappingMode == SvgUnitType.UserSpaceOnUse)
                {
                    brush.MappingMode = BrushMappingMode.Absolute;

                    //if (viewBoxTransform == null || viewBoxTransform.Value.IsIdentity)
                    //{
                    //    if (!elementBounds.IsEmpty)
                    //    {
                    //        viewBoxTransform = FitToViewbox(new SvgRect(x1, y1,
                    //            Math.Abs(x2 - x1), Math.Abs(y2 - y1)), elementBounds);
                    //    }
                    //}
                }
            }

            string colorInterpolation = res.GetPropertyValue("color-interpolation");
            if (!string.IsNullOrWhiteSpace(colorInterpolation))
            {
                if (colorInterpolation == "linearRGB")
                {
                    brush.ColorInterpolationMode = ColorInterpolationMode.ScRgbLinearInterpolation;
                }
                else
                {
                    brush.ColorInterpolationMode = ColorInterpolationMode.SRgbLinearInterpolation;
                }
            }

            MatrixTransform transform = GetTransformMatrix(res);
            if (transform != null && !transform.Matrix.IsIdentity)
            {
                if (viewBoxTransform != null && !viewBoxTransform.Value.IsIdentity)
                {
                    TransformGroup group = new TransformGroup();
                    group.Children.Add(viewBoxTransform);
                    group.Children.Add(transform);

                    brush.Transform = group;
                }
                else
                {
                    brush.Transform = transform;
                    //brush.StartPoint = new Point(0, 0.5);
                    //brush.EndPoint = new Point(1, 0.5);
                }

                //brush.StartPoint = new Point(0, 0);
                //brush.EndPoint = new Point(1, 1);
            }
            else
            {
                float fLeft   = (float)res.X1.AnimVal.Value;
                float fRight  = (float)res.X2.AnimVal.Value;
                float fTop    = (float)res.Y1.AnimVal.Value;
                float fBottom = (float)res.Y2.AnimVal.Value;

                if (mappingMode == SvgUnitType.ObjectBoundingBox)
                {
                    if (!fTop.Equals(fBottom) && !fLeft.Equals(fRight))
                    {
                        var drawingBrush = new DrawingBrush();
                        drawingBrush.Stretch = Stretch.Fill;
                        drawingBrush.Viewbox = new Rect(0, 0, 1, 1);
                        var DrawingRect = new GeometryDrawing(brush, null, new RectangleGeometry(new Rect(0, 0, 1, 1)));
                        drawingBrush.Drawing = DrawingRect;
                        return drawingBrush;
                    }
                }

                if (fTop.Equals(fBottom))
                {
                    //mode = LinearGradientMode.Horizontal;

                    //brush.StartPoint = new Point(0, 0.5);
                    //brush.EndPoint = new Point(1, 0.5);
                }
                else
                {
                    if (fLeft.Equals(fRight))
                    {
                        //mode = LinearGradientMode.Vertical;

                        //brush.StartPoint = new Point(0.5, 0);
                        //brush.EndPoint = new Point(0.5, 1);
                    }
                    else
                    {
                        if (fLeft < fRight)
                        {
                            if (viewBoxTransform != null && !viewBoxTransform.Value.IsIdentity)
                            {
                                //TransformGroup group = new TransformGroup();
                                //group.Children.Add(viewBoxTransform);
                                //group.Children.Add(new RotateTransform(45, 0.5, 0.5));

                                //brush.Transform = group;
                                brush.Transform = viewBoxTransform;
                            }
                            //else
                            //{
                            //    brush.RelativeTransform = new RotateTransform(45, 0.5, 0.5);
                            //}

                            //mode = LinearGradientMode.ForwardDiagonal;
                            //brush.EndPoint = new Point(x1, y1 + 1);

                            //brush.StartPoint = new Point(0, 0);
                            //brush.EndPoint = new Point(1, 1);
                        }
                        else
                        {
                            //mode = LinearGradientMode.BackwardDiagonal;
                            if (viewBoxTransform != null && !viewBoxTransform.Value.IsIdentity)
                            {
                                //TransformGroup group = new TransformGroup();
                                //group.Children.Add(viewBoxTransform);
                                //group.Children.Add(new RotateTransform(-45, 0.5, 0.5));

                                //brush.Transform = group;
                                brush.Transform = viewBoxTransform;
                            }
                            //else
                            //{
                            //    brush.RelativeTransform = new RotateTransform(-45, 0.5, 0.5);
                            //}

                            //brush.StartPoint = new Point(0, 0);
                            //brush.EndPoint = new Point(1, 1);
                        }
                    }
                }
            }

            return brush;
        }

        private Brush GetRadialGradientBrush(SvgRadialGradientElement res)
        {
            double centerX = res.Cx.AnimVal.Value;
            double centerY = res.Cy.AnimVal.Value;
            double focusX  = res.Fx.AnimVal.Value;
            double focusY  = res.Fy.AnimVal.Value;
            double radius  = res.R.AnimVal.Value;

            GradientStopCollection gradientStops = GetGradientStops(res.Stops);
            if (gradientStops == null || gradientStops.Count == 0)
            {
                return null;
            }

            RadialGradientBrush brush = new RadialGradientBrush(gradientStops);

            brush.RadiusX = radius;
            brush.RadiusY = radius;
            brush.Center  = new Point(centerX, centerY);
            brush.GradientOrigin = new Point(focusX, focusY);

            if (res.SpreadMethod != null)
            {
                SvgSpreadMethod spreadMethod = (SvgSpreadMethod)res.SpreadMethod.AnimVal;

                if (spreadMethod != SvgSpreadMethod.None)
                {
                    brush.SpreadMethod = WpfConvert.ToSpreadMethod(spreadMethod);
                }
            }
            if (res.GradientUnits != null)
            {
                SvgUnitType mappingMode = (SvgUnitType)res.GradientUnits.AnimVal;
                if (mappingMode == SvgUnitType.ObjectBoundingBox)
                {
                    brush.MappingMode = BrushMappingMode.RelativeToBoundingBox;
                }
                else if (mappingMode == SvgUnitType.UserSpaceOnUse)
                {
                    brush.MappingMode = BrushMappingMode.Absolute;

                    if (res.Fx.AnimVal.UnitType == SvgLengthType.Percentage)
                    {
                        brush.GradientOrigin = brush.Center;
                    }
                }
            }

            MatrixTransform transform = GetTransformMatrix(res);
            if (transform != null && !transform.Matrix.IsIdentity)
            {
                brush.Transform = transform;
            }

            string colorInterpolation = res.GetPropertyValue("color-interpolation");
            if (!string.IsNullOrWhiteSpace(colorInterpolation))
            {
                if (colorInterpolation == "linearRGB")
                {
                    brush.ColorInterpolationMode = ColorInterpolationMode.SRgbLinearInterpolation;
                }
                else
                {
                    brush.ColorInterpolationMode = ColorInterpolationMode.ScRgbLinearInterpolation;
                }
            }

            return brush;
        }

        private MatrixTransform GetTransformMatrix(SvgGradientElement gradientElement)
        {
            SvgMatrix svgMatrix = 
                ((SvgTransformList)gradientElement.GradientTransform.AnimVal).TotalMatrix;

            MatrixTransform transformMatrix = new MatrixTransform(svgMatrix.A, svgMatrix.B, svgMatrix.C,
                svgMatrix.D, svgMatrix.E, svgMatrix.F);

            return transformMatrix;
        }

        private GradientStopCollection GetGradientStops(XmlNodeList stops)
        {
            int itemCount = stops.Count;
            if (itemCount == 0)
            {
                return new GradientStopCollection();
            }
            GradientStopCollection gradientStops = new GradientStopCollection(itemCount);

            double lastOffset = 0;
            for (int i = 0; i < itemCount; i++)
            {
                SvgStopElement stop = (SvgStopElement)stops.Item(i);
                string prop = stop.GetAttribute("stop-color");
                string style = stop.GetAttribute("style");
                Color color = Colors.Transparent; // no auto-inherited...
                if (!string.IsNullOrWhiteSpace(prop) || !string.IsNullOrWhiteSpace(style))
                {
                    WpfSvgColor svgColor = new WpfSvgColor(stop, "stop-color");
                    color = svgColor.Color;
                }
                else
                {
                    color = Colors.Black; // the default color...
                    double alpha = 255;
                    string opacity;

                    opacity = stop.GetAttribute("stop-opacity"); // no auto-inherit
                    if (opacity == "inherit") // if explicitly defined...
                    {
                        opacity = stop.GetPropertyValue("stop-opacity");
                    }
                    if (opacity != null && opacity.Length > 0)
                        alpha *= SvgNumber.ParseNumber(opacity);

                    alpha = Math.Min(alpha, 255);
                    alpha = Math.Max(alpha, 0);

                    color = Color.FromArgb((byte)Convert.ToInt32(alpha), color.R, color.G, color.B);
                }

                double offset = stop.Offset.AnimVal;

                offset /= 100;
                offset = Math.Max(lastOffset, offset);

                gradientStops.Add(new GradientStop(color, offset));
                lastOffset = offset;
            }

            if (itemCount == 0)
            {
                gradientStops.Add(new GradientStop(Colors.Black, 0));
                gradientStops.Add(new GradientStop(Colors.Black, 1));
            }

            return gradientStops;
        }

        private Transform FitToViewbox(SvgRect viewBox, Rect rectToFit)
        {
            SvgPreserveAspectRatioType alignment = SvgPreserveAspectRatioType.XMidYMid;

            double[] transformArray = FitToViewBox(alignment, viewBox, 
                new SvgRect(rectToFit.X, rectToFit.Y, rectToFit.Width, rectToFit.Height));

            double translateX = transformArray[0];
            double translateY = transformArray[1];
            double scaleX     = transformArray[2];
            double scaleY     = transformArray[3];

            Transform translateMatrix = null;
            Transform scaleMatrix = null;

            if (SvgObject.IsValid(translateX) && SvgObject.IsValid(translateY) 
                && (!translateX.Equals(0) || !translateY.Equals(0)))
            {
                translateMatrix = new TranslateTransform(translateX, translateY);
            }

            if (SvgObject.IsValid(scaleX) && SvgObject.IsValid(scaleY)
                && (!scaleX.Equals(1.0f) || !scaleY.Equals(1.0f)))
            {
                scaleMatrix = new ScaleTransform(scaleX, scaleY);
            }

            if (translateMatrix != null && scaleMatrix != null)
            {
                // Create a TransformGroup to contain the transforms
                // and add the transforms to it.
                if (translateMatrix.Value.IsIdentity && scaleMatrix.Value.IsIdentity)
                {
                    return null;
                }
                if (translateMatrix.Value.IsIdentity)
                {
                    return scaleMatrix;
                }
                if (scaleMatrix.Value.IsIdentity)
                {
                    return translateMatrix;
                }

                TransformGroup transformGroup = new TransformGroup();
                transformGroup.Children.Add(scaleMatrix);
                transformGroup.Children.Add(translateMatrix);

                return transformGroup;
            }

            if (translateMatrix != null)
            {
                return translateMatrix.Value.IsIdentity ? null : translateMatrix;
            }

            if (scaleMatrix != null)
            {
                return scaleMatrix.Value.IsIdentity ? null : scaleMatrix;
            }

            return null;
        }

        private double[] FitToViewBox(SvgPreserveAspectRatioType alignment, SvgRect viewBox, SvgRect rectToFit)
        {
            double translateX = 0;
            double translateY = 0;
            double scaleX = 1;
            double scaleY = 1;

            if (!viewBox.IsEmpty)
            {
                // calculate scale values for non-uniform scaling
                scaleX = rectToFit.Width / viewBox.Width;
                scaleY = rectToFit.Height / viewBox.Height;

                if (alignment != SvgPreserveAspectRatioType.None)
                {
                    // uniform scaling
                    scaleX = Math.Max(scaleX, scaleY);

                    scaleY = scaleX;

                    if (alignment == SvgPreserveAspectRatioType.XMidYMax ||
                      alignment == SvgPreserveAspectRatioType.XMidYMid ||
                      alignment == SvgPreserveAspectRatioType.XMidYMin)
                    {
                        // align to the Middle X
                        translateX = (rectToFit.X + rectToFit.Width / 2) - scaleX * (viewBox.X + viewBox.Width / 2);
                    }
                    else if (alignment == SvgPreserveAspectRatioType.XMaxYMax ||
                      alignment == SvgPreserveAspectRatioType.XMaxYMid ||
                      alignment == SvgPreserveAspectRatioType.XMaxYMin)
                    {
                        // align to the right X
                        translateX = (rectToFit.Width - viewBox.Width * scaleX);
                    }

                    if (alignment == SvgPreserveAspectRatioType.XMaxYMid ||
                      alignment == SvgPreserveAspectRatioType.XMidYMid ||
                      alignment == SvgPreserveAspectRatioType.XMinYMid)
                    {
                        // align to the Middle Y
                        translateY = (rectToFit.Y + rectToFit.Height / 2) - scaleY * (viewBox.Y + viewBox.Height / 2);
                    }
                    else if (alignment == SvgPreserveAspectRatioType.XMaxYMax ||
                      alignment == SvgPreserveAspectRatioType.XMidYMax ||
                      alignment == SvgPreserveAspectRatioType.XMinYMax)
                    {
                        // align to the bottom Y
                        translateY = (rectToFit.Height - viewBox.Height * scaleY);
                    }
                }
                else
                {
                    translateX = -viewBox.X * scaleX;
                    translateY = -viewBox.Y * scaleY;
                }
            }

            return new double[]{ translateX, translateY, scaleX, scaleY };
        }

        

        #endregion
    }
}