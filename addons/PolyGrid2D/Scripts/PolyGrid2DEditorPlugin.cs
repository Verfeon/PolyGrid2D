using Godot;
using PolyGrid2D;

[Tool]
public partial class PolyGrid2DEditorPlugin : EditorPlugin
{
    private EditorInspector inspector;

    public override void _EnterTree()
    {
        // The plugin is now active.
        
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
        if (string.IsNullOrEmpty(property)) return;
        GodotObject editedObject = inspector.GetEditedObject();

        if (editedObject is PolyGridMap map && property == "VisualLayers")
        {
            LayersInitializer layersInitializer = new();
            layersInitializer.InitializeAllLayers(map);
        }
    }
}
