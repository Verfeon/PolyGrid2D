using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;

namespace PolyGrid2D
{
	[GlobalClass, Tool]
	public partial class PolyGridMap : TileMapLayer
	{
		[Export] public Godot.Collections.Array<TileMapLayer> VisualLayers = [];
		private int _variantSeed = 0;
		[Export] public int VariantSeed
		{
			get => _variantSeed;
			set
			{
				_variantSeed = value;
				UpdateFullGrid();
			}
		}

		[Export] public bool ShowLogicGrid 
		{ 
			get => _showLogicGrid;
			set 
			{
				_showLogicGrid = value;
				UpdateVisibility();
			}
		}
		private bool _showLogicGrid = false;

		[ExportToolButton("Update")] 
		private Callable _update => Callable.From(() => {
			InitializeLayers();
			_tileCache.Build(VisualLayers);
			UpdateFullGrid();
		});

		private TileVariantCache _tileCache = new ();
		private readonly int MIN_PARALLEL_AREA = 10000;
		private readonly string _customLayerIdName = "layerId";

		public override void _Ready()
		{
			InitializeCustomDataLayer();
			InitializeLayers();
			_tileCache.Build(VisualLayers);
			if (!Engine.IsEditorHint()) _showLogicGrid = false;
			UpdateVisibility();
		}

		public override void _UpdateCells(Godot.Collections.Array<Vector2I> coords, bool forcedCleanup)
		{
			if (coords == null || coords.Count == 0) return;
			if (forcedCleanup) return;
			if (!IsValidConfiguration()) return;

			Rect2I areaToUpdate = new Rect2I(coords[0], new Vector2I(2, 2));

			foreach (Vector2I logicPos in coords)
			{
				areaToUpdate = areaToUpdate.Merge(new Rect2I(logicPos, new Vector2I(2, 2)));
			}

			if (areaToUpdate.Area >= MIN_PARALLEL_AREA)
			{
				UpdateAreaParallel(areaToUpdate);
			}
			else
			{
				UpdateArea(areaToUpdate);
			}
		}

		private void UpdateVisibility()
		{
			SelfModulate = _showLogicGrid ? new Color(1, 1, 1, 1) : new Color(1, 1, 1, 0);
		}

		private void InitializeCustomDataLayer()
		{
			if (TileSet == null)
			{
				TileSet = new TileSet();
			}
			if (!TileSet.HasCustomDataLayerByName(_customLayerIdName))
			{
				int customDataLayersCount = TileSet.GetCustomDataLayersCount();
				TileSet.AddCustomDataLayer(-1);
				TileSet.SetCustomDataLayerName(customDataLayersCount, _customLayerIdName);
				TileSet.SetCustomDataLayerType(customDataLayersCount, Variant.Type.Int);
			}

			for (int i = 0 ; i < VisualLayers.Count; i++)
			{
				if (!TryFindPlaceholderByLayerId(i, out Vector2I _))
				{
					GD.PushWarning("Don't forget to associate the layer ids with the placeholders in the PolyGridMap's TileSet.");
					return;
				}
			}
		}

		private void InitializeLayers()
		{
			if (!IsValidConfiguration()) return;

			Vector2 offset = (Vector2)TileSet.TileSize / -2f;
			TileMapLayer layer;

			for (int i = 0; i < VisualLayers.Count; i++)
			{
				layer = VisualLayers[i];

				if (layer != null && IsInstanceValid(layer))
				{
					if (layer.TileSet.TileSize != TileSet.TileSize)
					{
						GD.PushWarning($"The layer {layer.Name} has a different tile size than the MultiGridController's");
					}
					layer.Position = offset;
				}
			}
		}

		public void UpdateFullGrid()
		{
			Rect2I usedRect = GetUsedRect();
			Rect2I areaToUpdate = new Rect2I(usedRect.Position - Vector2I.One, usedRect.Size + new Vector2I(2, 2));
			
			if (areaToUpdate.Area >= MIN_PARALLEL_AREA)
			{
				UpdateAreaParallel(areaToUpdate);
			}
			else
			{
				UpdateArea(areaToUpdate);
			}
		}

		private void UpdateArea(Rect2I area)
		{
			if (!IsValidConfiguration()) return;

			_tileCache.EnsureCache(VisualLayers);
			
			Vector2I visualPos;
			TileMapLayer visualLayer;
			TileVariant tile;

			for (int x = area.Position.X; x < area.End.X; x++)
			{
				for (int y = area.Position.Y; y < area.End.Y; y++)
				{
					visualPos.X = x;
					visualPos.Y = y;

					for (int terrainId = 0; terrainId < VisualLayers.Count; terrainId++)
					{
						visualLayer = VisualLayers[terrainId];
						if (visualLayer == null) continue;

						int mask = MaskCalculator.CalculateMask(visualPos, terrainId, this, _customLayerIdName);

						if (mask > 0)
						{
							tile = TileVariantSelector.GetVariant(visualPos, terrainId, mask, _tileCache, _variantSeed);
							
							int currentSource = visualLayer.GetCellSourceId(visualPos);
							Vector2I currentCoords = visualLayer.GetCellAtlasCoords(visualPos);

							if (currentCoords == tile.AtlasCoords && currentSource == tile.SourceId) continue;
							
							visualLayer.SetCell(visualPos, tile.SourceId, tile.AtlasCoords);
						}
						else
						{
							if (visualLayer.GetCellSourceId(visualPos) != -1)
							{
								visualLayer.EraseCell(visualPos);
							}
						}
					}
				}
			}
		}

		private async void UpdateAreaParallel(Rect2I area)
		{
			if (!IsValidConfiguration()) return;
			_tileCache.EnsureCache(VisualLayers);

			int chunkSize = 100;
			List<Task<List<CellUpdate>>> tasks = new();

			for (int x = area.Position.X; x < area.End.X; x += chunkSize)
			{
				for (int y = area.Position.Y; y < area.End.Y; y += chunkSize)
				{
					Rect2I chunkRect = new Rect2I(x, y, chunkSize, chunkSize).Intersection(area);
					tasks.Add(Task.Run(() => ComputeChunkMasks(chunkRect)));
				}
			}

			var results = await Task.WhenAll(tasks);

			foreach (var chunkUpdates in results)
			{
				foreach (var update in chunkUpdates)
				{
					var layer = VisualLayers[update.LayerId];
					if (update.Tile == null)
					{
						layer.EraseCell(update.Position);
					}
					else
					{
						layer.SetCell(update.Position, update.Tile.SourceId, update.Tile.AtlasCoords);
					}
				}
			}
		}

		private struct CellUpdate
		{
			public Vector2I Position;
			public int LayerId;
			public TileVariant Tile;
		}

		private List<CellUpdate> ComputeChunkMasks(Rect2I chunk)
		{
			List<CellUpdate> updates = new();

			Vector2I visualPos;
			TileMapLayer visualLayer;
			
			for (int x = chunk.Position.X; x < chunk.End.X; x++)
			{
				for (int y = chunk.Position.Y; y < chunk.End.Y; y++)
				{
					visualPos = new Vector2I(x, y);
					
					for (int terrainId = 0; terrainId < VisualLayers.Count; terrainId++)
					{
						visualLayer = VisualLayers[terrainId];

						int mask = MaskCalculator.CalculateMask(visualPos, terrainId, this, _customLayerIdName);
						
						if (mask > 0)
						{
							TileVariant tile = TileVariantSelector.GetVariant(visualPos, terrainId, mask, _tileCache, _variantSeed);
							
							int currentSource = visualLayer.GetCellSourceId(visualPos);
							Vector2I currentCoords = visualLayer.GetCellAtlasCoords(visualPos);
							
							if (currentCoords == tile.AtlasCoords && currentSource == tile.SourceId) continue;

							updates.Add(new CellUpdate { Position = visualPos, LayerId = terrainId, Tile = tile });
						}
						else
						{
							updates.Add(new CellUpdate { Position = visualPos, LayerId = terrainId, Tile = null });
						}
					}
				}
			}
			return updates;
		}

		private bool IsValidConfiguration()
		{
			if (VisualLayers == null || VisualLayers.Count == 0 || TileSet == null)
			{
				return false;
			}

			for (int i = 0; i < VisualLayers.Count; i++)
			{
				var l = VisualLayers[i];
				if (l == null || !IsInstanceValid(l) || l.TileSet == null)
				{
					return false;
				}
			}

			return true;
		}

		public bool TryFindPlaceholderByLayerId(int layerId, out Vector2I placeholderCoords)
		{
			TileSetAtlasSource source = TileSet.GetSource(TileSet.GetSourceId(0)) as TileSetAtlasSource;
			placeholderCoords = new (-1, -1);
			for (int index = 0; index < source.GetTilesCount(); index++)
			{
				Vector2I coords = source.GetTileId(index);
				TileData data = source.GetTileData(coords, 0);
				if (data.HasCustomData(_customLayerIdName) && (int)data.GetCustomData(_customLayerIdName) == layerId)
				{
					placeholderCoords = coords;
					return true;
				}
			}
			return false;
		}
	}
}
