using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Applies the material for a ghost/grave channel to one explicitly chosen
/// renderer. The target can be assigned directly or found by hierarchy path.
/// </summary>
public class ChannelAppearance : MonoBehaviour
{
    [SerializeField] private Renderer[] targetRenderers;
    [SerializeField] private string[] targetRendererPaths;
    [SerializeField] private Material[] channelMaterials;
    [SerializeField] private Shader channelMappingShader;

    private readonly List<Material> generatedMaterials = new List<Material>();
    private bool channelMaterialsPrepared;
    private static readonly int ChannelColorId = Shader.PropertyToID("_ChannelColor");
    private static readonly int EdgeColorId = Shader.PropertyToID("_EdgeColor");

    private static readonly Color[] DefaultChannelColors =
    {
        new Color(1f, 0.55f, 0.2f),
        new Color(0.25f, 0.55f, 1f),
        new Color(0.35f, 0.9f, 1f)
    };

    public void ApplyChannel(int channelID)
    {
        int materialIndex = channelID - 1;
        if (targetRenderers == null || targetRenderers.Length == 0)
        {
            var foundRenderers = new System.Collections.Generic.List<Renderer>();
            if (targetRendererPaths != null)
            {
                foreach (string path in targetRendererPaths)
                {
                    if (string.IsNullOrEmpty(path)) continue;
                    Transform target = transform.Find(path);
                    if (target != null) foundRenderers.AddRange(target.GetComponentsInChildren<Renderer>(true));
                    else Debug.LogWarning($"[{nameof(ChannelAppearance)}] Renderer target '{path}' was not found on {name}.", this);
                }
            }

            // Channel props such as pressure plates and pumpkins are assigned
            // dynamically at runtime and don't have explicit renderer paths.
            if (foundRenderers.Count == 0)
                foundRenderers.AddRange(GetComponentsInChildren<Renderer>(true));

            targetRenderers = foundRenderers.ToArray();
        }

        if (targetRenderers == null || targetRenderers.Length == 0)
        {
            Debug.LogWarning($"[{nameof(ChannelAppearance)}] No target renderers configured on {name}.", this);
            return;
        }

        if (channelMaterials != null && materialIndex >= 0 && materialIndex < channelMaterials.Length &&
            channelMaterials[materialIndex] != null)
        {
            foreach (Renderer renderer in targetRenderers)
            {
                if (renderer != null) renderer.sharedMaterial = channelMaterials[materialIndex];
            }
            return;
        }

        if (materialIndex < 0 || materialIndex >= DefaultChannelColors.Length) return;
        Color color = DefaultChannelColors[materialIndex];
        PrepareChannelMappingMaterials();
        var propertyBlock = new MaterialPropertyBlock();
        foreach (Renderer renderer in targetRenderers)
        {
            if (renderer == null) continue;
            renderer.GetPropertyBlock(propertyBlock);
            propertyBlock.SetColor(ChannelColorId, color);
            propertyBlock.SetColor(EdgeColorId, color);
            renderer.SetPropertyBlock(propertyBlock);
        }
    }

    private void PrepareChannelMappingMaterials()
    {
        if (channelMaterialsPrepared) return;
        channelMaterialsPrepared = true;

        channelMappingShader ??= Resources.Load<Material>("ChannelColorMapping")?.shader;
        channelMappingShader ??= Shader.Find("Sokoban/Channel Color Mapping");
        if (channelMappingShader == null)
        {
            Debug.LogWarning($"[{nameof(ChannelAppearance)}] Channel color mapping shader was not found on {name}.", this);
            return;
        }

        foreach (Renderer renderer in targetRenderers)
        {
            if (renderer == null) continue;
            Material[] sourceMaterials = renderer.sharedMaterials;
            Material[] mappedMaterials = new Material[sourceMaterials.Length];
            for (int i = 0; i < sourceMaterials.Length; i++)
            {
                Material source = sourceMaterials[i];
                if (source == null) continue;
                if (source.shader == channelMappingShader || source.shader.name == "Sokoban/Pressure Dissolve")
                {
                    mappedMaterials[i] = source;
                    continue;
                }

                Material mapped = new Material(source)
                {
                    name = source.name + " (Channel Color Mapping)",
                    shader = channelMappingShader
                };
                CopyMaterialProperty(source, mapped, "_BaseMap", "_MainTex");
                CopyColorProperty(source, mapped, "_BaseColor", "_Color");
                CopyFloatProperty(source, mapped, "_Metallic", 0f);
                CopyFloatProperty(source, mapped, "_Smoothness", 0.25f);
                CopyMaterialProperty(source, mapped, "_EmissionMap", null);
                CopyColorProperty(source, mapped, "_EmissionColor", null);
                mappedMaterials[i] = mapped;
                generatedMaterials.Add(mapped);
            }
            renderer.sharedMaterials = mappedMaterials;
        }
    }

    private static void CopyMaterialProperty(Material source, Material destination, string destinationProperty, string alternateSourceProperty)
    {
        string sourceProperty = source.HasProperty(destinationProperty)
            ? destinationProperty
            : alternateSourceProperty;
        if (string.IsNullOrEmpty(sourceProperty) || !source.HasProperty(sourceProperty)) return;
        destination.SetTexture(destinationProperty, source.GetTexture(sourceProperty));
        destination.SetTextureScale(destinationProperty, source.GetTextureScale(sourceProperty));
        destination.SetTextureOffset(destinationProperty, source.GetTextureOffset(sourceProperty));
    }

    private static void CopyColorProperty(Material source, Material destination, string destinationProperty, string alternateSourceProperty)
    {
        string sourceProperty = source.HasProperty(destinationProperty)
            ? destinationProperty
            : alternateSourceProperty;
        if (!string.IsNullOrEmpty(sourceProperty) && source.HasProperty(sourceProperty))
            destination.SetColor(destinationProperty, source.GetColor(sourceProperty));
    }

    private static void CopyFloatProperty(Material source, Material destination, string property, float fallback)
    {
        destination.SetFloat(property, source.HasProperty(property) ? source.GetFloat(property) : fallback);
    }

    private void OnDestroy()
    {
        foreach (Material material in generatedMaterials)
            if (material != null) Destroy(material);
    }
}
