using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class PlaySoundButton : MonoBehaviour
{
    public AudioClip sound;

    private void Awake()
    {
        GetComponent<Button>().onClick.AddListener(PlaySound);
    }


    private void PlaySound()
    {
        UISound.Instance.PlayUISound();
    }
}

