using UnityEngine;
using MoreMountains.Feedbacks;
using System.Runtime.InteropServices;
public class DogAnimSound : MonoBehaviour
{
    public MMF_Player soundFB,possessSoundFB;

    public void PlaySound()
    {
        soundFB.PlayFeedbacks();
    }

    public void PlayPossessSound()
    {
        possessSoundFB.PlayFeedbacks();
    }
}
