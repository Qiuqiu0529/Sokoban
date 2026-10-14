using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
public class GeneralText : MonoBehaviour
{
    //use for fixed 
    public TextMgr textMgr;
    public TextMeshPro textBlock;
    public string textkey;
    void OnEnable()
    {
        textMgr=TextMgr.Instance;
        ChangeText();
        textMgr.changeText += ChangeText;
    }

    void OnDisable()
    {
        textMgr.changeText -= ChangeText;
    }
    public void ChangeText()
    {
        textBlock.text = textMgr.GetText(textkey);
    }

}
