using UnityEngine;
using PixelCrushers;
using PixelCrushers.DialogueSystem;
using System;
using System.Runtime.CompilerServices;

public class TextMgr : Singleton<TextMgr> 
{
    public TextTable textTable;
    public string language;
    public event Action changeText;


    private void Start()
    {
        language=PlayerPrefs.GetString(GlobalString.languageSet,GlobalString.english);
        DialogueManager.SetLanguage( language);
        changeText?.Invoke();
    }

    public void SetChinese()
    {
        
        language=GlobalString.chinese;
        PlayerPrefs.SetString(GlobalString.languageSet,language);
        DialogueManager.SetLanguage( language);
        changeText?.Invoke();
        
    }
    public void SetEnglish()
    {
        language=GlobalString.english;
        PlayerPrefs.SetString(GlobalString.languageSet,language);
        DialogueManager.SetLanguage( language);
        changeText?.Invoke();
    }

    public void ToggleLanguage()
    {
        switch (language)
        {
            case "EN":
                SetChinese();
                break;
            case "ZH":
                SetEnglish();
                break;
            default:
                break;
        } 
    }

    public string GetText(string textID)
    {
        if (textTable.GetFieldTextForLanguage(textID, language) != null)
        {
            return textTable.GetFieldTextForLanguage(textID, language);
        }
        return "";
    }
}
