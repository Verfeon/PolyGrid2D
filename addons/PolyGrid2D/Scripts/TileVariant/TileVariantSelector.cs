using System.Collections.Generic;
using Godot;

namespace PolyGrid2D
{
	/// <summary>
	/// Selects a tile variant for a given position, layer, and mask using deterministic randomness.
	/// </summary>
	public static partial class TileVariantSelector
	{
		private static HashSet<string> _loggedErrors = new();
		
		public static TileVariant GetVariant(Vector2I pos, int layerId, int mask, TileVariantCache tileCache, int seed)
		{
			var variantSet = tileCache.GetVariantSet(layerId, mask);
			if (variantSet.Variants.Count == 0)
			{
				string key = $"{layerId}-{mask}";
				if (_loggedErrors.Add(key))
				{
					GD.PrintErr($"MultiGrid Error : No tile for mask {mask} in layer {layerId}");
				}
				return null; 
			}

			uint x = (uint)pos.X;
			uint y = (uint)pos.Y;
			
			uint hash = x * 0x1E35A7BD ^ y * 0x84720968 ^ (uint)seed;
			hash = (hash ^ (hash >> 16)) * 0x85ebca6b;
			hash = (hash ^ (hash >> 13)) * 0xc2b2ae35;
			hash ^= (hash >> 16);

			double target = (hash / (double)uint.MaxValue) * variantSet.TotalProbability;
			float cumulative = 0;
			for (int i = 0; i < variantSet.Variants.Count; i++)
			{
				cumulative += variantSet.Variants[i].Probability;
				if (target < cumulative)
				{
					return variantSet.Variants[i];
				}
			}

			return variantSet.Variants[0];
		}
	}	
}
