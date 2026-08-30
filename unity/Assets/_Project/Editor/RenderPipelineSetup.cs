using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace GridInfect.EditorTools
{
    // Creates and assigns the URP asset from code on first open, so the
    // project needs no template and no manual pipeline setup. Forward
    // renderer for the procedural baseline (the 2D Renderer needs a Light2D
    // to not render sprites black); switch renderers when the art pass lands.
    static class RenderPipelineSetup
    {
        const string SettingsDir = "Assets/Settings";
        const string PipelinePath = SettingsDir + "/UniversalRP.asset";
        const string RendererPath = SettingsDir + "/UniversalRenderer.asset";

        [InitializeOnLoadMethod]
        static void EnsureUrpAssigned()
        {
            EditorApplication.delayCall += () =>
            {
                if (GraphicsSettings.defaultRenderPipeline != null) return;

                var pipeline = AssetDatabase.LoadAssetAtPath<RenderPipelineAsset>(PipelinePath);
                if (pipeline == null)
                {
                    if (!AssetDatabase.IsValidFolder(SettingsDir))
                    {
                        AssetDatabase.CreateFolder("Assets", "Settings");
                    }
                    var rendererData = ScriptableObject.CreateInstance<UniversalRendererData>();
                    AssetDatabase.CreateAsset(rendererData, RendererPath);
                    pipeline = UniversalRenderPipelineAsset.Create(rendererData);
                    AssetDatabase.CreateAsset(pipeline, PipelinePath);
                    AssetDatabase.SaveAssets();
                }

                GraphicsSettings.defaultRenderPipeline = pipeline;
                QualitySettings.renderPipeline = pipeline;
                Debug.Log($"[setup] URP assigned ({PipelinePath})");
            };
        }
    }
}
