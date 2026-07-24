using Godot;

namespace PolyGrid2D
{
    /// <summary>
    /// Represents a single tile variant selected for a given mask and layer.
    /// </summary>
    public partial class TileVariant
    {
        public Vector2I AtlasCoords;
        public float Probability;
        public int SourceId;

        public TileVariant(Vector2I coords, float probability, int sourceId)
        {
            AtlasCoords = coords;
            Probability = probability;
            SourceId = sourceId;
        }
    }
}