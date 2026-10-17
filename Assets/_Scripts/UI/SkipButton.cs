using PixelCrushers.DialogueSystem;
using UnityEngine;

public class DialogSkip : MonoBehaviour
{

    public void SkipDialog()
    {
        DialogueManager.StopConversation();
    }
}
