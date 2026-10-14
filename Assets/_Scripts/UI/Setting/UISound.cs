using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MoreMountains.Feedbacks;
public class UISound : Singleton<UISound>
{
    public bool useUIsound = true;
    public MMF_Player mMF_Player;
    public MMF_Player mMF_Player1;
    public MMF_MMSoundManagerSound sound1;
    private void Start()
    {
        sound1 = mMF_Player1.GetFeedbackOfType<MMF_MMSoundManagerSound>();
    }

    public void PlayUISound()
    {
        mMF_Player.PlayFeedbacks();
    }

    public void PlayUISound(AudioClip sound)
    {

        if (!useUIsound)
        {
            return;
        }
        if (sound == null)
        {
            PlayUISound();
            return;
        }

        sound1.Sfx = sound;
        mMF_Player1.PlayFeedbacks();
    }
}
