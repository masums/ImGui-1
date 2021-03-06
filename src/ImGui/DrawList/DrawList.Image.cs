﻿using ImGui.Common;
using ImGui.Common.Primitive;
using ImGui.OSAbstraction.Graphics;

namespace ImGui
{
    internal partial class DrawList
    {
        /// <summary>
        /// Add an image.
        /// </summary>
        /// <param name="texture">the texture that are used</param>
        /// <param name="a">top-left point</param>
        /// <param name="b">bottom-right point</param>
        /// <param name="uvA">texture coordinate of point a</param>
        /// <param name="uvB">texture coordinate of point b</param>
        /// <param name="color">tint color</param>
        public void AddImage(ITexture texture, Point a, Point b, Point uvA, Point uvB, Color color)
        {
            if (GetCurrentClipRect().IsEmpty)
            {
                return;
            }
            if (MathEx.AmostZero(color.A))
                return;
            AddImageDrawCommand(texture);
            this.ImageMesh.PrimReserve(6, 4);
            AddImageRect(a, b, uvA, uvB, color);
        }

        /// <summary>
        /// Add textured rect, used for rendering images parts.
        /// </summary>
        /// <param name="a">top-left point</param>
        /// <param name="c">bottom-right point</param>
        /// <param name="uvA">texture coordinate of point a</param>
        /// <param name="uvC">texture coordinate of point c</param>
        /// <param name="color">tint color</param>
        private void AddImageRect(Point a, Point c, Point uvA, Point uvC, Color color)
        {
            Point b = new Point(c.X, a.Y);
            Point d = new Point(a.X, c.Y);
            Point uvB = new Point(uvC.X, uvA.Y);
            Point uvD = new Point(uvA.X, uvC.Y);

            this.ImageMesh.AppendVertex(new DrawVertex { pos = a, uv = uvA, color = color });
            this.ImageMesh.AppendVertex(new DrawVertex { pos = b, uv = uvB, color = color });
            this.ImageMesh.AppendVertex(new DrawVertex { pos = c, uv = uvC, color = color });
            this.ImageMesh.AppendVertex(new DrawVertex { pos = d, uv = uvD, color = color });
            this.ImageMesh.AppendIndex(0);
            this.ImageMesh.AppendIndex(1);
            this.ImageMesh.AppendIndex(2);
            this.ImageMesh.AppendIndex(0);
            this.ImageMesh.AppendIndex(2);
            this.ImageMesh.AppendIndex(3);
            this.ImageMesh.currentIdx += 4;
        }

    }
}