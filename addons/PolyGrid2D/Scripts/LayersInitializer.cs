using Godot;
using System;

namespace PolyGrid2D
{
    /// <summary>
    /// Initializes the layers of a PolyGridMap instance.
    /// </summary>
    public partial class LayersInitializer : Node
    {
        private readonly string _terrainName = "PolyGrid2D's Terrain";
        public void InitializeAllLayers(PolyGridMap polyGrid)
        {
            DoForAllLayers(polyGrid, InitializeLayer);
        }

        private void InitializeLayer(PolyGridMap polyGrid, TileMapLayer layer)
        {
            TileSet placeholderTileSet = polyGrid.TileSet;
            
			Vector2 offset = (Vector2)placeholderTileSet.TileSize / -2f;
			layer.Position = offset;
            layer.TileSet ??= new();
            layer.TileSet.TileShape = placeholderTileSet.TileShape;
            layer.TileSet.TileLayout = placeholderTileSet.TileLayout;
            layer.TileSet.TileOffsetAxis = placeholderTileSet.TileOffsetAxis;
            layer.TileSet.TileSize = placeholderTileSet.TileSize;

            int terrainSetIndex = GetTerrainSetIndex(layer);
            int terrainSetsCount = layer.TileSet.GetTerrainSetsCount();
            if (terrainSetIndex == terrainSetsCount)
            {
                layer.TileSet.AddTerrainSet();
                layer.TileSet.SetTerrainSetMode(terrainSetIndex, TileSet.TerrainMode.Corners);
            }

            int terrainIndex = GetTerrainIndex(layer, terrainSetIndex);
            int terrainsCount = layer.TileSet.GetTerrainsCount(terrainSetIndex);
            if (terrainIndex == terrainsCount)
            {
                layer.TileSet.AddTerrain(terrainSetIndex);
                layer.TileSet.SetTerrainName(terrainSetIndex, terrainIndex, _terrainName);
            }
        }

        private int GetTerrainSetIndex(TileMapLayer layer)
        {
            int terrainSetsCount = layer.TileSet.GetTerrainSetsCount();
            int terrainSetIndex = 0;
            while (terrainSetIndex < terrainSetsCount)
            {
                if (layer.TileSet.GetTerrainSetMode(terrainSetIndex) == TileSet.TerrainMode.Corners) break;

                terrainSetIndex++;
            }

            return terrainSetIndex;
        }

        private int GetTerrainIndex(TileMapLayer layer, int terrainSetIndex)
        {
            int terrainsCount = layer.TileSet.GetTerrainsCount(terrainSetIndex);
            int terrainIndex = 0;
            while (terrainIndex < terrainsCount)
            {
                if (layer.TileSet.GetTerrainName(terrainSetIndex, terrainIndex) == _terrainName) break;

                terrainIndex++;
            }

            return terrainIndex;
        }

        public void PaintTerrainForAllLayers(PolyGridMap polyGrid)
        {
            DoForAllLayers(polyGrid, PaintTerrain);
        }

        private void PaintTerrain(PolyGridMap polyGrid, TileMapLayer layer)
        {
            TileSet layerTileSet = layer.TileSet;

            int sourceId = layerTileSet.GetSourceId(0);
            if (!layerTileSet.HasSource(sourceId)) return;

            TileSetSource source = layerTileSet.GetSource(sourceId);
            
            int terrainSetIndex = GetTerrainSetIndex(layer);
            int terrainIndex = GetTerrainIndex(layer, terrainSetIndex);

            if (source is TileSetAtlasSource atlasSource)
            {
                Image image = atlasSource.Texture.GetImage();
                int tileCount = atlasSource.GetTilesCount();
                for (int i = 0; i < tileCount; i++)
                {
                    Vector2I coords = atlasSource.GetTileId(i);
                    Rect2I region = atlasSource.GetTileTextureRegion(coords);
                    TileData tileData = atlasSource.GetTileData(coords, 0);
                    tileData.TerrainSet = terrainSetIndex;
                    tileData.Terrain = terrainIndex;

                    ComputeTilePeeringBits(image, region, tileData);
                }
            }
        }

        private void ComputeTilePeeringBits(Image image, Rect2I region, TileData tileData)
        {
            const float pixelPercentageThreshold = 0.8f;
            const float alphaThreshold = 0.05f;
            int pixelCountPerCorner = region.Area / 4;

            var corners = new (Rect2I area, TileSet.CellNeighbor neighbor)[]
            {
                (new Rect2I(region.Position, region.GetCenter() - region.Position), TileSet.CellNeighbor.TopLeftCorner),
                (new Rect2I(new Vector2I(region.GetCenter().X, region.Position.Y), new Vector2I(region.End.X - region.GetCenter().X, region.GetCenter().Y - region.Position.Y)), TileSet.CellNeighbor.TopRightCorner),
                (new Rect2I(new Vector2I(region.Position.X, region.GetCenter().Y), new Vector2I(region.GetCenter().X - region.Position.X, region.End.Y - region.GetCenter().Y)), TileSet.CellNeighbor.BottomLeftCorner),
                (new Rect2I(region.GetCenter(), region.End - region.GetCenter()), TileSet.CellNeighbor.BottomRightCorner),
            };

            foreach (var (area, neighbor) in corners)
            {
                int opaquePixels = CountOpaquePixels(image, area, alphaThreshold);
                if (opaquePixels != 0 && (float)opaquePixels / pixelCountPerCorner > pixelPercentageThreshold)
                {
                    tileData.SetTerrainPeeringBit(neighbor, 0);
                }
            }
        }

        private int CountOpaquePixels(Image image, Rect2I region, float alphaThreshold)
        {
            int count = 0;
            for (int x = region.Position.X; x < region.End.X; x++)
            {
                for (int y = region.Position.Y; y < region.End.Y; y++)
                {
                    count += image.GetPixel(x, y).A > alphaThreshold ? 1 : 0;
                }
            }
            return count;
        }

        private void DoForAllLayers(PolyGridMap polyGrid, Action<PolyGridMap, TileMapLayer> action)
        {
            if (polyGrid == null)
            {
                throw new Exception("PolyGridMap is null.");
            }
            if (polyGrid.VisualLayers == null)
            {
                throw new Exception("PolyGridMap's VisualLayers is null.");
            }

            foreach (TileMapLayer layer in polyGrid.VisualLayers)
            {
                action(polyGrid, layer);
            }
        }
    }
}