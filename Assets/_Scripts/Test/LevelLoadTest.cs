using UnityEngine;
using Nova;
public class LevelLoadTest : MonoBehaviour
{
    public TextBlock textBlock;

    public void TryLoad()
    {
        LevelMgr.Instance.TryLoadLevel(textBlock.Text);
    }
}
