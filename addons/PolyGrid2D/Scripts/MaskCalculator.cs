using System;
using Godot;

namespace PolyGrid2D
{
	public static partial class MaskCalculator	
	{
		private static readonly Vector2I TL = new(-1, -1);
		private static readonly Vector2I TR = new(0, -1);
		private static readonly Vector2I BL = new(-1, 0);
		private static readonly Vector2I BR = new(0, 0);

		public static int CalculateMask(Vector2I pos, int layerId, TileMapLayer logicLayer, string layerIdName)
		{
			int mask = 0;
			mask += IsTileMatch(pos + TL, layerId, logicLayer, layerIdName) ? 1 : 0;
			mask += IsTileMatch(pos + TR, layerId, logicLayer, layerIdName) ? 2 : 0;
			mask += IsTileMatch(pos + BL, layerId, logicLayer, layerIdName) ? 4 : 0;
			mask += IsTileMatch(pos + BR, layerId, logicLayer, layerIdName) ? 8 : 0;
			return mask;
		}

		private static bool IsTileMatch(Vector2I pos, int layerId, TileMapLayer logicLayer, string layerIdName)
		{
			Vector2I coords = logicLayer.GetCellAtlasCoords(pos);
			bool validPos = coords != new Vector2I(-1, -1);
			if (validPos)
			{
				int sourceId = logicLayer.GetCellSourceId(pos);
				var source = logicLayer.TileSet.GetSource(sourceId) as TileSetAtlasSource;
				TileData tileData = source.GetTileData(coords, 0);
				
				if (!tileData.HasCustomData(layerIdName))
				{
					throw new Exception($"No custom data layer named {layerIdName}.");
				}
				return (int)tileData.GetCustomData(layerIdName) == layerId;
			}
			return validPos && coords.X == layerId;
		}
	}				
}
