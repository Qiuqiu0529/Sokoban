using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Animates pressure-controlled objects with a URP dissolve shader.</summary>
public sealed class PressureDissolveEffect : MonoBehaviour
{
    [SerializeField] private Shader dissolveShader;
    [SerializeField, Min(0f)] private float transitionDuration = 0.45f;
    [SerializeField, Range(0.005f, 0.2f)] private float edgeWidth = 0.06f;

    private readonly List<Renderer> renderers = new List<Renderer>();
    private readonly List<Collider> colliders = new List<Collider>();
    private readonly List<Material> generatedMaterials = new List<Material>();
    private Coroutine transition;
    private float dissolveAmount;
    private bool isVisible = true;

    private static readonly int DissolveAmountId = Shader.PropertyToID("_DissolveAmount");
    private static readonly int EdgeWidthId = Shader.PropertyToID("_EdgeWidth");

    private void Awake()
    {
        CacheRenderersAndColliders();
        PrepareMaterials();
        ApplyDissolveAmount(0f);
    }

    public void SetVisible(bool visible, bool animate = true)
    {
        if (isVisible == visible && (visible ? dissolveAmount <= 0f : dissolveAmount >= 1f)) return;
        isVisible = visible;
        if (transition != null) StopCoroutine(transition);

        foreach (Collider item in colliders)
            if (item != null) item.enabled = visible;
        foreach (Renderer item in renderers)
            if (item != null) item.enabled = true;

        float target = visible ? 0f : 1f;
        if (!animate || !isActiveAndEnabled)
        {
            dissolveAmount = target;
            ApplyDissolveAmount(target);
            return;
        }

        transition = StartCoroutine(AnimateDissolve(target));
    }

    private IEnumerator AnimateDissolve(float target)
    {
        float start = dissolveAmount;
        float duration = Mathf.Max(0.01f, transitionDuration);
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
            dissolveAmount = Mathf.Lerp(start, target, t);
            ApplyDissolveAmount(dissolveAmount);
            yield return null;
        }

        dissolveAmount = target;
        ApplyDissolveAmount(target);
        transition = null;
    }

    private void CacheRenderersAndColliders()
    {
        renderers.Clear();
        renderers.AddRange(GetComponentsInChildren<Renderer>(true));
        colliders.Clear();
        colliders.AddRange(GetComponentsInChildren<Collider>(true));
    }

    private void PrepareMaterials()
    {
        dissolveShader ??= Resources.Load<Material>("PressureDissolve")?.shader;
        dissolveShader ??= Shader.Find("Sokoban/Pressure Dissolve");
        if (dissolveShader == null) return;

        foreach (Renderer item in renderers)
        {
            if (item == null) continue;
            Material[] sourceMaterials = item.sharedMaterials;
            Material[] replacementMaterials = new Material[sourceMaterials.Length];
            for (int i = 0; i < sourceMaterials.Length; i++)
            {
                Material source = sourceMaterials[i];
                if (source == null) continue;
                Material replacement = new Material(source)
                {
                    name = source.name + " (Pressure Dissolve)",
                    shader = dissolveShader
                };
                string sourceMapProperty = source.HasProperty("_BaseMap") ? "_BaseMap" : "_MainTex";
                if (source.HasProperty(sourceMapProperty))
                {
                    replacement.SetTexture("_BaseMap", source.GetTexture(sourceMapProperty));
                    replacement.SetTextureScale("_BaseMap", source.GetTextureScale(sourceMapProperty));
                    replacement.SetTextureOffset("_BaseMap", source.GetTextureOffset(sourceMapProperty));
                }
                if (source.HasProperty("_BaseColor")) replacement.SetColor("_BaseColor", source.GetColor("_BaseColor"));
                else if (source.HasProperty("_Color")) replacement.SetColor("_BaseColor", source.GetColor("_Color"));
                if (source.HasProperty("_Metallic")) replacement.SetFloat("_Metallic", source.GetFloat("_Metallic"));
                if (source.HasProperty("_Smoothness")) replacement.SetFloat("_Smoothness", source.GetFloat("_Smoothness"));
                if (source.HasProperty("_EmissionMap"))
                {
                    replacement.SetTexture("_EmissionMap", source.GetTexture("_EmissionMap"));
                    replacement.SetTextureScale("_EmissionMap", source.GetTextureScale("_EmissionMap"));
                    replacement.SetTextureOffset("_EmissionMap", source.GetTextureOffset("_EmissionMap"));
                }
                if (source.HasProperty("_EmissionColor")) replacement.SetColor("_EmissionColor", source.GetColor("_EmissionColor"));
                replacementMaterials[i] = replacement;
                generatedMaterials.Add(replacement);
            }
            item.sharedMaterials = replacementMaterials;
        }
    }

    private void ApplyDissolveAmount(float value)
    {
        var block = new MaterialPropertyBlock();
        foreach (Renderer item in renderers)
        {
            if (item == null) continue;
            item.GetPropertyBlock(block);
            block.SetFloat(DissolveAmountId, value);
            block.SetFloat(EdgeWidthId, edgeWidth);
            item.SetPropertyBlock(block);
        }
    }

    private void OnDestroy()
    {
        foreach (Material material in generatedMaterials)
            if (material != null) Destroy(material);
    }
}
