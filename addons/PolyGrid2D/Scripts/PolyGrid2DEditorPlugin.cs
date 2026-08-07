using System;
using Godot;
using PolyGrid2D;

/// <summary>
/// Minimal Godot editor plugin entry point for the PolyGrid2D addon.
/// </summary>
[Tool]
public partial class PolyGrid2DEditorPlugin : EditorPlugin
{
    private EditorInspector inspector;

    public override void _EnterTree()
    {
        // The plugin is now active.
        // Try to listen to inspector property edits to react to VisualLayers changes.
        GD.Print("Plugin actif");
        inspector = EditorInterface.Singleton.GetInspector();
        if (inspector != null)
        {
            GD.Print("inpector != null");
            inspector.Connect("property_edited", new Callable(this, nameof(OnInspectorPropertyEdited)));
        }
    }

    public override void _ExitTree()
    {
        // The plugin is being disabled.
    }

    private void OnInspectorPropertyEdited(string property)
    {
        GD.Print("TEST");
        if (string.IsNullOrEmpty(property)) return;
        GodotObject editedObject = inspector.GetEditedObject();

        // Only react to VisualLayers edits on PolyGridMap
        if (editedObject is PolyGrid2D.PolyGridMap map && property == "VisualLayers")
        {
            GD.Print("TEST2");

            LayersInitializer layersInitializer = new();
            layersInitializer.InitializeAllLayers(map);
        }
    }
}
