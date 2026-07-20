using Godot;

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
