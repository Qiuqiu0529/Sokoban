using UnityEngine;

/// <summary>Shared channel colors used by editor previews and spawned objects.</summary>
[CreateAssetMenu(fileName = "ChannelPalette", menuName = "Sokoban/Channel Palette")]
public sealed class ChannelPalette : ScriptableObject
{
    private static ChannelPalette cachedPalette;

    [SerializeField] private Color[] colors =
    {
        new Color(1f, 0.55f, 0.2f),
        new Color(0.58f, 0.82f, 1f),
        new Color(0.78f, 0.62f, 0.76f)
    };

    public Color GetColor(int channelID)
    {
        int index = channelID - 1;
        return colors != null && index >= 0 && index < colors.Length
            ? colors[index]
            : Color.white;
    }

    public static Color GetSharedColor(int channelID)
    {
        if (cachedPalette == null)
            cachedPalette = Resources.Load<ChannelPalette>("ChannelPalette");
        return cachedPalette != null ? cachedPalette.GetColor(channelID) : Color.white;
    }
}
