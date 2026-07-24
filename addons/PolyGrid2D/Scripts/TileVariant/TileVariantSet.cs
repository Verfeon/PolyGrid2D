using System.Collections.Generic;

namespace PolyGrid2D
{
        /// <summary>
        /// Groups all tile variants and their total probability for a specific mask.
        /// </summary>
        public partial class TileVariantSet
        {
                public List<TileVariant> Variants = [];
                public float TotalProbability = 0;
        }
}
