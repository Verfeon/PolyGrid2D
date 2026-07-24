using System.Linq;
using Godot;

namespace PolyGrid2D
{
	[GlobalClass, Tool]
	public partial class TerrainGenerator : Node
	{
		private PolyGridMap _polyGridMap;
		[Export] public PolyGridMap PolyGridMap
		{
			get => _polyGridMap;
			set
			{
				_polyGridMap = value;
				NotifyPropertyListChanged();
			}
		}
		[Export] private Noise _noise = null;
		[Export] private Vector2I _position = new (0, 0);
		[Export] private Vector2I _size = new (0, 0);
		[Export] private bool _clearBeforeGeneration = true;
		
		[ExportToolButton("Generate")] public Callable GenerateCallable => Callable.From(() => {Generate();});

		[Export] private Godot.Collections.Dictionary<string, float> _terrainLayersWeights = [];

		public override void _ValidateProperty(Godot.Collections.Dictionary property)
		{
			if ((string)property["name"] == nameof(_terrainLayersWeights))
			{
				property["usage"] = (int)PropertyUsageFlags.NoEditor;
			}
		}

		public override Godot.Collections.Array<Godot.Collections.Dictionary> _GetPropertyList()
		{
			if (_polyGridMap == null) return [];
			
			Godot.Collections.Array<Godot.Collections.Dictionary> properties = [];


			properties.Add(new Godot.Collections.Dictionary()
			{
				{ "name", "Terrain Layers Weights" },
				{ "type", (int)Variant.Type.Nil },
				{ "usage", (int)PropertyUsageFlags.Group }
			});

			foreach (TileMapLayer layer in _polyGridMap.VisualLayers)
			{
				if (layer == null) continue;

				string name = layer.Name + "Weight";
				if (!_terrainLayersWeights.ContainsKey(name))
				{
					_terrainLayersWeights[name] = 1.0f;
				}

				properties.Add(new Godot.Collections.Dictionary()
				{
					{ "name", name },
					{ "type", (int)Variant.Type.Float }
				});
			}

			return properties;
		}
		
		public override Variant _Get(StringName property)
		{
			string propertyName = property.ToString();

			if (_terrainLayersWeights.TryGetValue(propertyName, out float value))
			{
				return value;
			}

			return default;
		}

		public override bool _Set(StringName property, Variant value)
		{
			string propertyName = property.ToString();

			if (_terrainLayersWeights.ContainsKey(propertyName))
			{
				_terrainLayersWeights[propertyName] = value.As<float>();
				return true;
			}
			return false;
		}

		public void Generate()
		{
			if (_polyGridMap == null)
			{
				GD.PushWarning("No PolyGridMap given for generation.");
				return;
			}
			if (_noise == null)
			{
				GD.PushWarning("No noise map given for generation.");
				return;
			}
			if (_clearBeforeGeneration) _polyGridMap.Clear();
			NotifyPropertyListChanged();

			int layerCount = _polyGridMap.VisualLayers?.Count ?? 0;
			if (layerCount == 0) return;

			float totalWeight = _terrainLayersWeights.Values.Sum();
			if (totalWeight == 0) return;

			float minValue = float.MaxValue;
			float maxValue = float.MinValue;
			for (int x = _position.X; x < _position.X + _size.X; x++)
			{
				for (int y = _position.Y; y < _position.Y + _size.Y; y++)
				{
					float value = _noise.GetNoise2D(x, y);
					minValue = Mathf.Min(minValue, value);
					maxValue = Mathf.Max(maxValue, value);
				}
			}

			for (int x = _position.X; x < _position.X + _size.X; x++)
			{
				for (int y = _position.Y; y < _position.Y + _size.Y; y++)
				{
					float value = (_noise.GetNoise2D(x, y) - minValue) / (maxValue - minValue);
					value *= float.BitDecrement(totalWeight);
					int terrainId = -1;
					float cumulatedWeight = 0;

					for (int i = 0; i < layerCount; i++)
					{
						var layer = _polyGridMap.VisualLayers[i];
						float weight = _terrainLayersWeights[layer.Name + "Weight"];

						if (weight <= 0) continue;

						cumulatedWeight += weight;

						if (value < cumulatedWeight)
						{
							terrainId = i;
							break;
						}
					}

					int sourceId = _polyGridMap.TileSet.GetSourceId(0);

					if (_polyGridMap.TryFindPlaceholderByLayerId(terrainId, out Vector2I atlasCoords))
					{
						_polyGridMap.SetCell(new Vector2I(x, y), sourceId, atlasCoords);
					}
					else
					{
						GD.PushError($"No placeholder for layer {terrainId}");
					}
				}
			}
		}
	}
}
