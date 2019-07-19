using UnityEngine;

public class SoundController : MonoBehaviour
{
    public static SoundController instance { get; set; }

    public AudioClip itemSelected;
    public AudioClip itemPlaced;
    public AudioClip invalidLocation;

    AudioSource source;

    private void Awake()
    {
        instance = this;
        source = Camera.main.GetComponent<AudioSource>();
    }

    void Start()
    {
        
        if (source == null)
        {
            Debug.LogError("Audio source is missing, please attach an Audio source component to the main camera");
        }

    }

    public void PlayClip(AudioClip clip)
    {
        if (clip != null)
        {
            source.clip = clip;
            source.Play();
        }
        else
        {
            Debug.LogError("Audio clip not set, please make sure all the clips are set in the Sound Controller");
        }

    }

}
