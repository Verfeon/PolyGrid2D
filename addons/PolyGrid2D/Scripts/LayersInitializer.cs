using Godot;
using System;

namespace PolyGrid2D
{
    /// <summary>
    /// Initializes the layers of a PolyGridMap instance.
    /// </summary>
    public partial class LayersInitializer : Node
    {
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
            
            TileSet layerTileSet = new();
            TileSet placeHolderTileSet = polyGrid.TileSet;

            layer.TileSet = layerTileSet;
            layerTileSet.TileShape = placeHolderTileSet.TileShape;
            layerTileSet.TileLayout = placeHolderTileSet.TileLayout;
            layerTileSet.TileOffsetAxis = placeHolderTileSet.TileOffsetAxis;
            layerTileSet.TileSize = placeHolderTileSet.TileSize;

            layerTileSet.AddTerrainSet();
            int terrainSetsCount = layerTileSet.GetTerrainSetsCount();
            layerTileSet.AddTerrain(terrainSetsCount-1);
            layerTileSet.SetTerrainName(terrainSetsCount-1, 0, "PolyGrid2d's terrain");
        }
    }
}