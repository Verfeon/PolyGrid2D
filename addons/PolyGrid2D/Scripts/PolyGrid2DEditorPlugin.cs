using Godot;

[Tool]
public partial class PolyGrid2DEditorPlugin : EditorPlugin
{
    public override void _EnterTree()
    {
        // The plugin is now active.
    }

    public override void _ExitTree()
    {
        // The plugin is being disabled.
    }
}
