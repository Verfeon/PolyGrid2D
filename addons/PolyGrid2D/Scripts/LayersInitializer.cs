using Godot;
using System;

namespace PolyGrid2D
{
    /// <summary>
    /// Initializes the layers of a PolyGridMap instance.
    /// </summary>
    public partial class LayersInitializer : Node
    {
        private string _terrainName = "PolyGrid2D's Terrain";
        public void InitializeAllLayers(PolyGridMap polyGrid)
        {
            if (polyGrid == null)
            {
                throw new Exception("PolyGridMap is null.");
            }
            if (polyGrid.VisualLayers == null)
            {
                throw new Exception("PolyGridMap's VisualLayers is null.");
            }
            int layerCount = polyGrid.VisualLayers.Count;
            if (layerCount == 0)
            {
                GD.Print("No layer to initialize.");
            }


            foreach (TileMapLayer layer in polyGrid.VisualLayers)
            {
                InitializeLayer(polyGrid, layer);
            }
        }

        private void InitializeLayer(PolyGridMap polyGrid, TileMapLayer layer)
        {
            GD.Print($"Initializing layer {layer.Name}");
            
            TileSet placeHolderTileSet = polyGrid.TileSet;
            
            layer.TileSet ??= new();
            layer.TileSet.TileShape = placeHolderTileSet.TileShape;
            layer.TileSet.TileLayout = placeHolderTileSet.TileLayout;
            layer.TileSet.TileOffsetAxis = placeHolderTileSet.TileOffsetAxis;
            layer.TileSet.TileSize = placeHolderTileSet.TileSize;

            int terrainSetsCount = layer.TileSet.GetTerrainSetsCount();
            int terrainSetIndex = 0;
            while (terrainSetIndex < terrainSetsCount)
            {
                if (layer.TileSet.GetTerrainSetMode(terrainSetIndex) == TileSet.TerrainMode.Corners) break;

                terrainSetIndex++;
            }
            if (terrainSetIndex == terrainSetsCount)
            {
                layer.TileSet.AddTerrainSet();
                layer.TileSet.SetTerrainSetMode(terrainSetIndex, TileSet.TerrainMode.Corners);
            }
            
            int terrainsCount = layer.TileSet.GetTerrainsCount(terrainSetIndex);

            int terrainIndex = 0;
            while (terrainIndex < terrainsCount)
            {
                if (layer.TileSet.GetTerrainName(terrainSetIndex, terrainIndex) == _terrainName) break;

                terrainIndex++;
            }
            if (terrainIndex == terrainsCount)
            {
                layer.TileSet.AddTerrain(terrainSetIndex);
                layer.TileSet.SetTerrainName(terrainSetIndex, terrainIndex, _terrainName);
            }

            PaintTerrain(layer, terrainSetIndex, terrainIndex);
        }

        private void PaintTerrain(TileMapLayer layer, int terrainSetIndex, int terrainIndex)
        {
            TileSet layerTileSet = layer.TileSet;

            TileSetSource source = layerTileSet.GetSource(layerTileSet.GetSourceId(0));

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

                    GD.Print($"Computing peering bits at {coords}..."); 
                    ComputeTilePeeringBits(image, region, tileData);
                }
            }
        }

        private void ComputeTilePeeringBits(Image image, Rect2I region, TileData tileData)
        {
            float pixelPercentageThreshold = 0.8f;
            float alphaThreshold = 0.05f;
            int pixelCountPerCorner = region.Area/4;
            GD.Print($"Pixel count per corner : {pixelCountPerCorner}");

            int notEmptyPixelsCount = 0;
            for (int x = region.Position.X; x < region.GetCenter().X; x++)
            {  
                for (int y = region.Position.Y; y < region.GetCenter().Y; y++)
                {
                    notEmptyPixelsCount += image.GetPixel(x, y).A > alphaThreshold ? 1 : 0;
                }
            }
            GD.Print($"Not empty pixels count top left : {notEmptyPixelsCount}");
            if (notEmptyPixelsCount != 0 && (float)notEmptyPixelsCount/pixelCountPerCorner > pixelPercentageThreshold)
            {
                GD.Print($"Add peering bit top left");
                tileData.SetTerrainPeeringBit(TileSet.CellNeighbor.TopLeftCorner, 0);
            }
            
            notEmptyPixelsCount = 0;
            for (int x = region.GetCenter().X; x < region.End.X; x++)
            {  
                for (int y = region.Position.Y; y < region.GetCenter().Y; y++)
                {
                    notEmptyPixelsCount += image.GetPixel(x, y).A > alphaThreshold ? 1 : 0;
                }
            }
            GD.Print($"Not empty pixels count top right : {notEmptyPixelsCount}");
            if (notEmptyPixelsCount != 0 && (float)notEmptyPixelsCount/pixelCountPerCorner > pixelPercentageThreshold)
            {
                GD.Print($"Add peering bit top right");
                tileData.SetTerrainPeeringBit(TileSet.CellNeighbor.TopRightCorner, 0);
            }
            
            notEmptyPixelsCount = 0;
            for (int x = region.Position.X; x < region.GetCenter().X; x++)
            {  
                for (int y = region.GetCenter().Y; y < region.End.Y; y++)
                {
                    notEmptyPixelsCount += image.GetPixel(x, y).A > alphaThreshold ? 1 : 0;
                }
            }
            GD.Print($"Not empty pixels count bottom left : {notEmptyPixelsCount}");
            if (notEmptyPixelsCount != 0 && (float)notEmptyPixelsCount/pixelCountPerCorner > pixelPercentageThreshold)
            {
                GD.Print($"Add peering bit bottom left");
                tileData.SetTerrainPeeringBit(TileSet.CellNeighbor.BottomLeftCorner, 0);
            }
            
            notEmptyPixelsCount = 0;
            for (int x = region.GetCenter().X; x < region.End.X; x++)
            {  
                for (int y = region.GetCenter().Y; y < region.End.Y; y++)
                {
                    notEmptyPixelsCount += image.GetPixel(x, y).A > alphaThreshold ? 1 : 0;
                }
            }
            GD.Print($"Not empty pixels count bottom right : {notEmptyPixelsCount}");
            if (notEmptyPixelsCount != 0 && (float)notEmptyPixelsCount/pixelCountPerCorner > pixelPercentageThreshold)
            {
                GD.Print($"Add peering bit bottom right");
                tileData.SetTerrainPeeringBit(TileSet.CellNeighbor.BottomRightCorner, 0);
            }
        }
    }
}