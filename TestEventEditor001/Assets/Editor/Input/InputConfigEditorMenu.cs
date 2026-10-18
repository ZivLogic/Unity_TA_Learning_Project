#if UNITY_EDITOR
using UnityEditor;

public static class InputConfigEditorMenu
{
    [MenuItem("Tools/Input/Export Default Config To Resources")]
    public static void ExportDefaultConfig()
    {
        InputConfigLoader.ExportDefaultToResources();
    }
}
#endif