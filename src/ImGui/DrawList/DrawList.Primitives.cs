﻿using System;
using System.Collections.Generic;
using ImGui.Common;
using ImGui.Common.Primitive;

namespace ImGui
{
    internal partial class DrawList
    {

        #region primitives

        private List<Point> _Path = new List<Point>();

        //primitives part

        public void AddLine(Point a, Point b, Color col, double thickness = 1.0)
        {
            if (MathEx.AmostZero(col.A))
                return;
            PathLineTo(a + new Vector(0.5, 0.5));
            PathLineTo(b + new Vector(0.5, 0.5));
            PathStroke(col, false, thickness);
        }

        public void AddPolyline(IList<Point> points, Color col, bool closed, double thickness, bool anti_aliased = false)
        {
            var points_count = points.Count;
            if (points_count < 2)
                return;

            int count = points_count;
            if (!closed)
                count = points_count - 1;

            bool thick_line = thickness > 1.0f;

            if (anti_aliased)
            {

            }
            else
            {
                // Non Anti-aliased Stroke
                int idx_count = count*6;
                int vtx_count = count*4;      // FIXME-OPT: Not sharing edges
                ShapeMesh.PrimReserve(idx_count, vtx_count);

                for (int i1 = 0; i1 < count; i1++)
                {
                    int i2 = (i1+1) == points_count ? 0 : i1+1;
                    Point p1 = points[i1];
                    Point p2 = points[i2];
                    Vector diff = p2 - p1;
                    diff *= MathEx.InverseLength(diff, 1.0f);
                
                    float dx = (float)(diff.X * (thickness * 0.5f));
                    float dy = (float)(diff.Y * (thickness * 0.5f));
                    var vertex0 = new DrawVertex { pos = new PointF(p1.X + dy, p1.Y - dx), uv = PointF.Zero, color = (ColorF)col };
                    var vertex1 = new DrawVertex { pos = new PointF(p2.X + dy, p2.Y - dx), uv = PointF.Zero, color = (ColorF)col };
                    var vertex2 = new DrawVertex { pos = new PointF(p2.X - dy, p2.Y + dx), uv = PointF.Zero, color = (ColorF)col };
                    var vertex3 = new DrawVertex { pos = new PointF(p1.X - dy, p1.Y + dx), uv = PointF.Zero, color = (ColorF)col };
                    ShapeMesh.AppendVertex(vertex0);
                    ShapeMesh.AppendVertex(vertex1);
                    ShapeMesh.AppendVertex(vertex2);
                    ShapeMesh.AppendVertex(vertex3);

                    ShapeMesh.AppendIndex(0);
                    ShapeMesh.AppendIndex(1);
                    ShapeMesh.AppendIndex(2);
                    ShapeMesh.AppendIndex(0);
                    ShapeMesh.AppendIndex(2);
                    ShapeMesh.AppendIndex(3);

                    ShapeMesh.currentIdx += 4;
                }
            }
        }
        
        public void AddConvexPolyFilled(IList<Point> points, Color col, bool anti_aliased)
        {
            var points_count = points.Count;
            anti_aliased = false;
            //if (.KeyCtrl) anti_aliased = false; // Debug

            if (anti_aliased)
            {

            }
            else
            {
                // Non Anti-aliased Fill
                int idx_count = (points_count-2)*3;
                int vtx_count = points_count;
                ShapeMesh.PrimReserve(idx_count, vtx_count);
                for (int i = 0; i < vtx_count; i++)
                {
                    ShapeMesh.AppendVertex(new DrawVertex { pos = (PointF)points[i], uv = PointF.Zero, color = (ColorF)col });
                }
                for (int i = 2; i < points_count; i++)
                {
                    ShapeMesh.AppendIndex(0);
                    ShapeMesh.AppendIndex(i-1);
                    ShapeMesh.AppendIndex(i);
                }
                ShapeMesh.currentIdx += vtx_count;
            }
        }
        
        // Fully unrolled with inline call to keep our debug builds decently fast.
        public void PrimRect(Point a, Point c, Color col)
        {
            Point b = new Point(c.X, a.Y);
            Point d = new Point(a.X, c.Y);
            Point uv = Point.Zero;

            ShapeMesh.AppendVertex(new DrawVertex { pos = (PointF)a, uv = PointF.Zero, color = (ColorF)col });
            ShapeMesh.AppendVertex(new DrawVertex { pos = (PointF)b, uv = PointF.Zero, color = (ColorF)col });
            ShapeMesh.AppendVertex(new DrawVertex { pos = (PointF)c, uv = PointF.Zero, color = (ColorF)col });
            ShapeMesh.AppendVertex(new DrawVertex { pos = (PointF)d, uv = PointF.Zero, color = (ColorF)col });

            ShapeMesh.AppendIndex(0);
            ShapeMesh.AppendIndex(1);
            ShapeMesh.AppendIndex(2);
            ShapeMesh.AppendIndex(0);
            ShapeMesh.AppendIndex(2);
            ShapeMesh.AppendIndex(3);

            ShapeMesh.currentIdx += 4;
        }

        void PrimRectUV(Point a, Point c, Point uv_a, Point uv_c, Color col)
        {
            Point b = new Point(c.X, a.Y);
            Point d = new Point(a.X, c.Y);
            Point uv_b = new Point(uv_c.X, uv_a.Y);
            Point uv_d = new Point(uv_a.X, uv_c.Y);

            ShapeMesh.AppendVertex(new DrawVertex { pos = (PointF)a, uv = (PointF)uv_a, color = (ColorF)col });
            ShapeMesh.AppendVertex(new DrawVertex { pos = (PointF)b, uv = (PointF)uv_b, color = (ColorF)col });
            ShapeMesh.AppendVertex(new DrawVertex { pos = (PointF)c, uv = (PointF)uv_c, color = (ColorF)col });
            ShapeMesh.AppendVertex(new DrawVertex { pos = (PointF)d, uv = (PointF)uv_d, color = (ColorF)col });

            ShapeMesh.AppendIndex(0);
            ShapeMesh.AppendIndex(1);
            ShapeMesh.AppendIndex(2);
            ShapeMesh.AppendIndex(0);
            ShapeMesh.AppendIndex(2);
            ShapeMesh.AppendIndex(3);

            ShapeMesh.currentIdx += 4;
        }




        // a: upper-left, b: lower-right. we don't render 1 px sized rectangles properly.
        public void AddRect(Point a, Point b, Color col, float rounding = 0.0f, int rounding_corners = 0x0F, float thickness = 1.0f)
        {
            if (MathEx.AmostZero(col.A))
                return;
            PathRect(a + new Vector(0.5f,0.5f), b - new Vector(0.5f,0.5f), rounding, rounding_corners);
            PathStroke(col, true, thickness);
        }

        public void AddRectFilled(Point a, Point b, Color col, float rounding = 0.0f, int rounding_corners = 0x0F)
        {
            if (MathEx.AmostZero(col.A))
                return;
            if (rounding > 0.0f)
            {
                PathRect(a, b, rounding, rounding_corners);
                PathFill(col);
            }
            else
            {
                ShapeMesh.PrimReserve(6, 4);
                PrimRect(a, b, col);
            }
        }

        void AddTriangleFilled(Point a, Point b, Point c, Color col)
        {
            if (MathEx.AmostZero(col.A))
                return;

            PathLineTo(a);
            PathLineTo(b);
            PathLineTo(c);
            PathFill(col);
        }

        #endregion

        #region stateful path constructing methods

        static void PathBezierToCasteljau(IList<Point> path, double x1, double y1, double x2, double y2, double x3, double y3, double x4, double y4, double tess_tol, int level)
        {
            double dx = x4 - x1;
            double dy = y4 - y1;
            double d2 = ((x2 - x4) * dy - (y2 - y4) * dx);
            double d3 = ((x3 - x4) * dy - (y3 - y4) * dx);
            d2 = (d2 >= 0) ? d2 : -d2;
            d3 = (d3 >= 0) ? d3 : -d3;
            if ((d2 + d3) * (d2 + d3) < tess_tol * (dx * dx + dy * dy))
            {
                path.Add(new Point(x4, y4));
            }
            else if (level < 10)
            {
                double x12 = (x1 + x2) * 0.5f, y12 = (y1 + y2) * 0.5f;
                double x23 = (x2 + x3) * 0.5f, y23 = (y2 + y3) * 0.5f;
                double x34 = (x3 + x4) * 0.5f, y34 = (y3 + y4) * 0.5f;
                double x123 = (x12 + x23) * 0.5f, y123 = (y12 + y23) * 0.5f;
                double x234 = (x23 + x34) * 0.5f, y234 = (y23 + y34) * 0.5f;
                double x1234 = (x123 + x234) * 0.5f, y1234 = (y123 + y234) * 0.5f;

                DrawList.PathBezierToCasteljau(path, x1, y1, x12, y12, x123, y123, x1234, y1234, tess_tol, level + 1);
                DrawList.PathBezierToCasteljau(path, x1234, y1234, x234, y234, x34, y34, x4, y4, tess_tol, level + 1);
            }
        }

        const double CurveTessellationTol = 1.25;
        void PathBezierCurveTo(Point p2, Point p3, Point p4, int num_segments = 0)
        {
            Point p1 = _Path[_Path.Count-1];
            if (num_segments == 0)
            {
                // Auto-tessellated
                PathBezierToCasteljau(_Path, p1.X, p1.Y, p2.X, p2.Y, p3.X, p3.Y, p4.X, p4.Y, CurveTessellationTol, 0);
            }
            else
            {
                float t_step = 1.0f / (float)num_segments;
                for (int i_step = 1; i_step <= num_segments; i_step++)
                {
                    float t = t_step * i_step;
                    float u = 1.0f - t;
                    float w1 = u * u * u;
                    float w2 = 3 * u * u * t;
                    float w3 = 3 * u * t * t;
                    float w4 = t * t * t;
                    _Path.Add(new Point(w1* p1.X + w2* p2.X + w3* p3.X + w4* p4.X, w1* p1.Y + w2* p2.Y + w3* p3.Y + w4* p4.Y));
                }
            }
        }

        public void PathRect(Point rect_min, Point rect_max, float rounding = 0.0f, int rounding_corners = 0x0F)
        {
            double r = rounding;
            r = System.Math.Min(r, System.Math.Abs(rect_max.X-rect_min.X) * ( ((rounding_corners&(1|2))==(1|2)) || ((rounding_corners&(4|8))==(4|8)) ? 0.5f : 1.0f ) - 1.0f);
            r = System.Math.Min(r, System.Math.Abs(rect_max.Y-rect_min.Y) * ( ((rounding_corners&(1|8))==(1|8)) || ((rounding_corners&(2|4))==(2|4)) ? 0.5f : 1.0f ) - 1.0f);

            if (r <= 0.0f || rounding_corners == 0)
            {
                PathLineTo(rect_min);
                PathLineTo(new Point(rect_max.X, rect_min.Y));
                PathLineTo(rect_max);
                PathLineTo(new Point(rect_min.X, rect_max.Y));
            }
            else
            {
                var r0 = (rounding_corners & 1) != 0 ? r : 0.0f;
                var r1 = (rounding_corners & 2) != 0 ? r : 0.0f;
                var r2 = (rounding_corners & 4) != 0 ? r : 0.0f;
                var r3 = (rounding_corners & 8) != 0 ? r : 0.0f;
                PathArcToFast(new Point(rect_min.X+r0, rect_min.Y+r0), r0, 6, 9);
                PathArcToFast(new Point(rect_max.X-r1, rect_min.Y+r1), r1, 9, 12);
                PathArcToFast(new Point(rect_max.X-r2, rect_max.Y-r2), r2, 0, 3);
                PathArcToFast(new Point(rect_min.X+r3, rect_max.Y-r3), r3, 3, 6);
            }
        }
        
        //inline
        public void PathStroke(Color col, bool closed, double thickness = 1)
        {
            AddPolyline(_Path, col, closed, thickness);
            PathClear();
        }

        //inline
        public void PathFill(Color col)
        {
            AddConvexPolyFilled(_Path, col, true);
            PathClear();
        }

        public void PathClear()
        {
            _Path.Clear();
        }

        public void PathMoveTo(Point point)
        {
            _Path.Add(point);
        }

        //inline
        public void PathLineTo(Point pos)
        {
            _Path.Add(pos);
        }

        private static readonly Point[] circle_vtx = InitCircleVtx();

        private static Point[] InitCircleVtx()
        {
            Point[] result = new Point[12];
            for (int i = 0; i < 12; i++)
            {
                var a = (float) i/12*2*Math.PI;
                result[i].X = Math.Cos(a);
                result[i].Y = Math.Sin(a);
            }
            return result;
        }

        public void PathArcToFast(Point center, double radius, int amin, int amax)
        {
            if (amin > amax) return;
            if (MathEx.AmostZero(radius))
            {
                _Path.Add(center);
            }
            else
            {
                _Path.Capacity = _Path.Count + amax - amin + 1;
                for (int a = amin; a <= amax; a++)
                {
                    Point c = circle_vtx[a % circle_vtx.Length];
                    _Path.Add(new Point(center.X + c.X* radius, center.Y + c.Y* radius));
                }
            }
        }

        public void PathClose()
        {
            _Path.Add(_Path[0]);
        }

        #region filled bezier curve

        public void PathAddBezier(Point start, Point control, Point end)
        {
            _Path.Add(start);

            _Path.Add(control);

            _Path.Add(end);
        }

        #endregion

        #endregion

        #region TODO data-based path api

#if false
        struct Path
        {
            PathData[] data;
        }

        struct PathData
        {
            public PathType type;
        }

        enum PathType
        {
            MoveTo,
            LineTo,
            Close,
            AddBezier,
            Clear,
        }
#endif

        #endregion

        /// <summary>
        /// Append a text mesh to this drawlist
        /// </summary>
        /// <param name="textMesh"></param>
        /// <param name="offset"></param>
        public void AppendTextMesh(TextMesh textMesh, Vector offset)
        {
            if (textMesh == null)
            {
                throw new ArgumentNullException(nameof(textMesh));
            }

            this.TextMesh.Append(textMesh, offset);
        }

        #region Extra
        public void RenderCollapseTriangle(Point p_min, bool is_open, double height, Color color, double scale = 1)
        {
            GUIContext g = Form.current.uiContext;
            Window window = Utility.GetCurrentWindow();

            double h = height;
            double r = h * 0.40f * scale;
            Point center = p_min + new Vector(h * 0.50f, h * 0.50f * scale);

            Point a, b, c;
            if (is_open)
            {
                center.Y -= r * 0.25f;
                a = center + new Vector(0, 1) * r;
                b = center + new Vector(-0.866f, -0.5f) * r;
                c = center + new Vector(0.866f, -0.5f) * r;
            }
            else
            {
                a = center + new Vector(1, 0) * r;
                b = center + new Vector(-0.500f, 0.866f) * r;
                c = center + new Vector(-0.500f, -0.866f) * r;
            }

            window.DrawList.AddTriangleFilled(a, b, c, color);
        }
        #endregion
    }
}
