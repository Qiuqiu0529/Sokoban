using UnityEngine;
using UnityEngine.UI;
using TMPro;
using MoreMountains.Feedbacks;
using MoreMountains.Tools;
using static MoreMountains.Tools.MMSoundManager;

public class SoundManager : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    [SerializeField]
    private Slider volumeSlider;
    [SerializeField]
    private Slider musicVolumeSlider;
    [SerializeField]
    private Slider sfxVolumeSlider;
    [SerializeField]
    private TextMeshProUGUI valueText,musicValueText,sfxValueText;

    public MMF_Player musicFB, sfxFB;
    public MMF_MMSoundManagerTrackControl musicVolume, soundVolume;


    void Start()
    {
        musicVolume = musicFB.GetFeedbackOfType<MMF_MMSoundManagerTrackControl>();
        soundVolume = sfxFB.GetFeedbackOfType<MMF_MMSoundManagerTrackControl>();

        if (!PlayerPrefs.HasKey("musicTrackVolume"))
        {
            PlayerPrefs.SetFloat("musicTrackVolume", 1.0f);

        }

        if (!PlayerPrefs.HasKey("sfxTrackVolume"))
        {
            PlayerPrefs.SetFloat("sfxTrackVolume", 1.0f);

        }



        if (!PlayerPrefs.HasKey("musicVolume"))
        {
            PlayerPrefs.SetFloat("musicVolume", 1.0f);

        }
        else
        {
            Load();
        }
    }

    // Update is called once per frame
    public void ChangeVolume()
    {
        AudioListener.volume = volumeSlider.value / 100.0f;
        valueText.text = volumeSlider.value.ToString();
        Save();
    }

    public void ChangeMusicVolume()
    {
        musicVolume.Volume = musicVolumeSlider.value / 100.0f;
        
        musicValueText.text = musicVolumeSlider.value.ToString();
        musicFB.PlayFeedbacks();
    }

    public void ChangeSoundVolume()
    {
        soundVolume.Volume = sfxVolumeSlider.value / 100.0f;
        
        sfxValueText.text= sfxVolumeSlider.value.ToString();
        sfxFB.PlayFeedbacks();
    }

    private void Load()
    {
        volumeSlider.value = PlayerPrefs.GetFloat("musicVolume");
        valueText.text = volumeSlider.value.ToString();
        musicVolumeSlider.value = MMSoundManager.Instance.GetTrackVolume(MMSoundManagerTracks.Music, false) * 100;
        musicValueText.text = musicVolumeSlider.value.ToString();
        sfxVolumeSlider.value = MMSoundManager.Instance.GetTrackVolume(MMSoundManagerTracks.Sfx, false) * 100;
        sfxValueText.text= sfxVolumeSlider.value.ToString();
    }


    private void Save()//volume 1-100, set 0-1
    {
        PlayerPrefs.SetFloat("musicVolume", volumeSlider.value);
        PlayerPrefs.SetFloat("musicTrackVolume", musicVolumeSlider.value);
        PlayerPrefs.SetFloat("sfxTrackVolume", sfxVolumeSlider.value);

    }
}
