﻿using System.Collections.Generic;
using ImGui.Common;
using System.Runtime.CompilerServices;

namespace ImGui
{
    internal class Mesh
    {
        private int vtxWritePosition;
        private int idxWritePosition;
        public int currentIdx;

        /// <summary>
        /// Commands. Typically 1 command = 1 gpu draw call.
        /// </summary>
        /// <remarks>Every command corresponds to 1 sub-mesh.</remarks>
        public List<DrawCommand> CommandBuffer { get; } = new List<DrawCommand>();

        /// <summary>
        /// Index buffer. Each command consume DrawCommand.ElemCount of those
        /// </summary>
        public IndexBuffer IndexBuffer { get; } = new IndexBuffer(10000);

        /// <summary>
        /// Vertex buffer
        /// </summary>
        public VertexBuffer VertexBuffer { get; } = new VertexBuffer(10000);

        /// <summary>
        /// Append a vertex to the VertexBuffer
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AppendVertex(DrawVertex vertex)
        {
            this.VertexBuffer[this.vtxWritePosition] = vertex;
            this.vtxWritePosition++;
        }

        /// <summary>
        /// Append an index to the IndexBuffer
        /// </summary>
        /// <remarks>The value to insert is `_currentIdx + offsetToCurrentIndex`.</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AppendIndex(int offsetToCurrentIndex)
        {
            this.IndexBuffer[this.idxWritePosition] = new DrawIndex { Index = this.currentIdx + offsetToCurrentIndex };
            this.idxWritePosition++;
        }

        /// <summary>
        /// Pre-allocate space for a number of indexes and vertexes.
        /// </summary>
        /// <param name="idxCount">the number of indexes to add</param>
        /// <param name="vtxCount">the number of vertexes to add</param>
        public void PrimReserve(int idxCount, int vtxCount)
        {
            if (idxCount == 0)
            {
                return;
            }

            DrawCommand drawCommand = this.CommandBuffer[this.CommandBuffer.Count - 1];
            drawCommand.ElemCount += idxCount;
            this.CommandBuffer[this.CommandBuffer.Count - 1] = drawCommand;

            int vtxBufferSize = this.VertexBuffer.Count;
            this.vtxWritePosition = vtxBufferSize;
            this.VertexBuffer.Resize(vtxBufferSize + vtxCount);

            int idxBufferSize = this.IndexBuffer.Count;
            this.idxWritePosition = idxBufferSize;
            this.IndexBuffer.Resize(idxBufferSize + idxCount);
        }

        /// <summary>
        /// Clear the buffers and reset states of vertex and index writer.
        /// </summary>
        /// <remarks>
        /// The capacity of buffers is not changed.
        /// So no OS-level memory allocation will happen if the buffers don't get bigger than their capacity.
        /// </remarks>
        public void Clear()
        {
            this.CommandBuffer.Clear();
            this.IndexBuffer.Clear();
            this.VertexBuffer.Clear();
            this.vtxWritePosition = 0;
            this.idxWritePosition = 0;
            this.currentIdx = 0;
        }
    }
}