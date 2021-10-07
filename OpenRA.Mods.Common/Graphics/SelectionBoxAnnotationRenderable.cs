#region Copyright & License Information
/*
 * Copyright 2007-2021 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using OpenRA.Graphics;
using OpenRA.Primitives;

namespace OpenRA.Mods.Common.Graphics
{
	public class SelectionBoxAnnotationRenderable : IRenderable, IFinalizedRenderable
	{
		readonly WPos pos;
		readonly Rectangle decorationBounds;
		readonly Color color;
		readonly int layer;
		public SelectionBoxAnnotationRenderable(Actor actor, Rectangle decorationBounds, Color color, int layer = 0)
			: this(actor.CenterPosition, decorationBounds, color, layer) { }

		public SelectionBoxAnnotationRenderable(WPos pos, Rectangle decorationBounds, Color color, int layer = 0)
		{
			this.pos = pos;
			this.decorationBounds = decorationBounds;
			this.color = color;
			this.layer = layer;
		}

		public WPos Pos => pos;

		public int ZOffset => 0;
		public int Layer => layer;
		public bool IsDecoration => true;

		public IRenderable WithZOffset(int newOffset) { return this; }
		public IRenderable OffsetBy(in WVec vec) { return new SelectionBoxAnnotationRenderable(pos + vec, decorationBounds, color, layer); }
		public IRenderable AsDecoration() { return this; }

		public IFinalizedRenderable PrepareRender(WorldRenderer wr) { return this; }
		public void Render(WorldRenderer wr)
		{
			var tl = wr.Viewport.WorldToViewPx(new float2(decorationBounds.Left, decorationBounds.Top)).ToFloat2();
			var br = wr.Viewport.WorldToViewPx(new float2(decorationBounds.Right, decorationBounds.Bottom)).ToFloat2();
			var tr = new float2(br.X, tl.Y);
			var bl = new float2(tl.X, br.Y);
			var u = new float2(4, 0);
			var v = new float2(0, 4);

			var cr = Game.Renderer.RgbaColorRenderer;
			cr.DrawLine(new float3[] { tl + u, tl, tl + v }, 1, color, true);
			cr.DrawLine(new float3[] { tr - u, tr, tr + v }, 1, color, true);
			cr.DrawLine(new float3[] { br - u, br, br - v }, 1, color, true);
			cr.DrawLine(new float3[] { bl + u, bl, bl - v }, 1, color, true);
		}

		public void RenderDebugGeometry(WorldRenderer wr) { }
		public Rectangle ScreenBounds(WorldRenderer wr) { return Rectangle.Empty; }
	}
}
