using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class MusicPlayer : MonoBehaviour
{
    public AudioClip[] songs;
    private AudioSource audioSource;
    private int lastSong = -1;
    private float nextSongTime = 0;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        audioSource.volume = OptionsManager.GetCurrentOptions().musicVolume;
        PlayRandomSong();
        nextSongTime = Time.time + UnityEngine.Random.Range(8*60, 12*60 + 1); // Every 8-12 minutes
    }

    void Update()
    {
        audioSource.volume = OptionsManager.GetCurrentOptions().musicVolume;
        if (Time.time >= nextSongTime)
        {
            PlayRandomSong();
            nextSongTime = Time.time + UnityEngine.Random.Range(8*60, 12*60 + 1); // Every 8-12 minutes
        }
    }

    private void PlayRandomSong()
    {
        int song = UnityEngine.Random.Range(0, 4);
        if (song == lastSong)
            song = (song + 1) % 4;
        lastSong = song;
        audioSource.clip = songs[song];
        audioSource.Play();
    }
}
