using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Microsoft.Unity.VisualStudio.Editor;
using KToolkit;

public class TutorialUI : KObserver
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public int tutorialIndex;
    public List<string> tutorialTexts = new List<string>();
    public TMP_Text textComponent;
    public float typingSpeed = 0.05f;
    private Coroutine typingCoroutine;

    void Awake()
    {
        AddEventListener(EventName.PlayerPossess, (args) =>
        {
            Debug.Log("Listen Player Possess");
            if (tutorialIndex == 1)
            {
                StartTyping(0);
            }
        });
        AddEventListener(EventName.PlayerStandingOnClothIndicator, (args)=>
        {
            Debug.Log("Listen Player StandingOnCloth");
            if (tutorialIndex == 0)
            {
                StartTyping(0);
            }
        });
    }

    void Start()
    {

    }

    // Update is called once per frame
    public void StartTyping(int index)
    {
        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
        }
        typingCoroutine = StartCoroutine(TypeText(index));
    }

    IEnumerator TypeText(int index)
    {
        textComponent.text = "";
        foreach (char letter in tutorialTexts[index])
        {
            textComponent.text += letter;
            yield return new WaitForSeconds(typingSpeed);
        }
    }
}
