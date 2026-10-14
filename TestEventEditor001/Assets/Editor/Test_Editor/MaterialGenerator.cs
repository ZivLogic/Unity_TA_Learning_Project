// Assets/Editor/MaterialGenerator.cs
using UnityEngine;
using UnityEditor;

public class MaterialGenerator
{
    [MenuItem("Tools/Generate Black Material")]
    public static void GenerateBlackMaterial()
    {
        // 改为 URP Lit Shader
        var material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        material.color = new Color(0.1f, 0.1f, 0.1f, 1.0f);

        // URP Lit 的属性名不同
        material.SetFloat("_Metallic", 0.0f);
        material.SetFloat("_Smoothness", 0.3f);  // URP 用 _Smoothness，不是 _Glossiness

        AssetDatabase.CreateAsset(material, "Assets/Resources/Materials/pawn_black.mat");
        AssetDatabase.Refresh();

        Debug.Log("黑色材质已创建 (URP Lit)");
    }
}