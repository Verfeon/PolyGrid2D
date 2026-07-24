using System;

namespace PolyGrid2D
{
	/// <summary>
	/// Stores the available tile variants grouped by mask value for a single visual layer.
	/// </summary>
	public partial class LayerCache
	{
		public const int MASK_COUNT = 16;

		private TileVariantSet[] _masks = new TileVariantSet[MASK_COUNT];

		public LayerCache() {
			for (int j = 0; j < MASK_COUNT; j++)
			{ 
				_masks[j] = new TileVariantSet();
			}
		}

		public TileVariantSet GetVariantSetFromMask(int mask)
		{
			if (mask < 0 || mask >= MASK_COUNT)
			{
				throw new Exception($"Invalid mask {mask}.");
			}

			return _masks[mask];
		}
	}
}
