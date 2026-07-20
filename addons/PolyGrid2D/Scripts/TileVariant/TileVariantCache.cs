using Godot;
using System;
using System.Collections.Generic;

public class TileVariantCache
{
	private Dictionary<int, LayerCache> _cache = new Dictionary<int, LayerCache>();
	private int _layersHash;

	[Flags]
	private enum DualMask
	{
		None = 0,
		TL = 1,
		TR = 2,
		BL = 4,
		BR = 8
	}

	private int GetIndexFromNativePeeringBits(TileData data)
	{
		if (data.TerrainSet == -1) return -1;

		DualMask mask = 0;
		if (data.GetTerrainPeeringBit(TileSet.CellNeighbor.TopLeftCorner) != -1) mask |= DualMask.TL;
		if (data.GetTerrainPeeringBit(TileSet.CellNeighbor.TopRightCorner) != -1) mask |= DualMask.TR;
		if (data.GetTerrainPeeringBit(TileSet.CellNeighbor.BottomLeftCorner) != -1) mask |= DualMask.BL;
		if (data.GetTerrainPeeringBit(TileSet.CellNeighbor.BottomRightCorner) != -1) mask |= DualMask.BR;
		return (int)mask;
	}

	public void Build(IList<TileMapLayer> layers)
	{
		_layersHash = ComputeLayersHash(layers);
		_cache.Clear();

		LayerCache layerCache;
		TileSetSource source;
		Vector2I coords;
		TileData data;

		for (int i = 0; i < layers.Count; i++)
		{
			if (layers[i] == null) continue;
			layerCache = new LayerCache();

			int sourceId = layers[i].TileSet.GetSourceId(0);
			source = layers[i].TileSet.GetSource(sourceId);
			if (source is TileSetAtlasSource atlasSource)
			{
				int tileCount = atlasSource.GetTilesCount();
				for (int j = 0; j < tileCount; j++)
				{
					coords = atlasSource.GetTileId(j);
					data = atlasSource.GetTileData(coords, 0);

					int mask = GetIndexFromNativePeeringBits(data);
					if (mask >= 0 && mask < LayerCache.MASK_COUNT)
					{
						TileVariantSet variantSet = layerCache.GetVariantSetFromMask(mask);
						variantSet.Variants.Add(new TileVariant(coords, data.Probability, sourceId));
						variantSet.TotalProbability += data.Probability;
					}
				}
			}
			_cache[i] = layerCache;
		}
	}

	public TileVariantSet GetVariantSet(int layer, int mask)
{
	if (!_cache.TryGetValue(layer, out var layerCache))
	{
		throw new Exception($"Layer {layer} not cached");
	}

	TileVariantSet variantSet = layerCache.GetVariantSetFromMask(mask);
	if (variantSet.Variants.Count == 0)
	{
		throw new Exception($"Mask {mask} missing for layer {layer}");
	}

	return variantSet;
}

	public void EnsureCache(IList<TileMapLayer> layers)
	{
		if (_cache.Count == 0 || _layersHash != ComputeLayersHash(layers))
		{
			Build(layers);
		}
	}
	
	private int ComputeLayersHash(IList<TileMapLayer> layers)
	{
		var hc = new HashCode();

		foreach (var l in layers)
		{
			if (l == null || !GodotObject.IsInstanceValid(l)) continue;

			hc.Add(l.GetInstanceId());

			var ts = l.TileSet;
			if (ts != null)
			{
				hc.Add(ts.GetInstanceId());
				
				int sourceCount = ts.GetSourceCount();
				hc.Add(sourceCount);

				if (sourceCount > 0)
				{
					int firstId = ts.GetSourceId(0);
					hc.Add(firstId);

					if (ts.GetSource(firstId) is TileSetAtlasSource atlas)
					{
						hc.Add(atlas.GetTilesCount());
					}
				}
			}
		}

		return hc.ToHashCode();
	}
}
