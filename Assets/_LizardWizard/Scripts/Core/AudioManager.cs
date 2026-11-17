using System.Collections;
using UnityEngine;

//-- WIP audio manager, just music + sfx for now
//-- usage: AudioManager.instance.PlaySFX("WaterShot");
public class AudioManager : MonoBehaviour
{
    [System.Serializable]
    public class Sound
    {
        public string name;
        public AudioClip clip;
    }

    public static AudioManager instance;

    public AudioSource musicSource;
    public AudioSource sfxSource;

    public Sound[] music;
    public Sound[] sfx;

    [Range(0f, 1f)] public float musicVolume = 0.5f;
    [Range(0f, 1f)] public float sfxVolume = 1f;

    public string startMusic;
    public float fadeTime = 1.5f;

    private float switchFade = 1f; //-- goes to 0 when changing songs

    void Awake()
    {
        //-- keep it between scenes
        //-- https://docs.unity3d.com/ScriptReference/Object.DontDestroyOnLoad.html
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        if (musicSource == null) musicSource = gameObject.AddComponent<AudioSource>();
        if (sfxSource == null) sfxSource = gameObject.AddComponent<AudioSource>();
        musicSource.loop = true;
    }

    void Start()
    {
        if (startMusic != "") PlayMusic(startMusic);
    }

    void Update()
    {
        if (musicSource.clip == null) return;

        //-- fade in at the start of the song and out at the end so the loop isnt a hard cut
        float t = musicSource.time;
        float len = musicSource.clip.length;
        float loopFade = Mathf.Clamp01(Mathf.Min(t, len - t) / fadeTime);

        musicSource.volume = musicVolume * loopFade * switchFade;
    }

    public void PlayMusic(string name)
    {
        Sound s = System.Array.Find(music, x => x.name == name);
        if (s == null)
        {
            Debug.Log("music not found: " + name);
            return;
        }
        if (musicSource.clip == s.clip && musicSource.isPlaying) return;

        StopAllCoroutines();
        StartCoroutine(SwitchSong(s.clip));
    }

    public void StopMusic()
    {
        StopAllCoroutines();
        StartCoroutine(SwitchSong(null));
    }

    //-- fade the old song out then start the new one (it fades in by itself in Update)
    //-- https://docs.unity3d.com/Manual/Coroutines.html
    IEnumerator SwitchSong(AudioClip next)
    {
        while (musicSource.isPlaying && switchFade > 0f)
        {
            switchFade -= Time.unscaledDeltaTime / fadeTime;
            yield return null;
        }

        musicSource.Stop();
        switchFade = 1f;
        if (next == null) yield break;

        //-- https://docs.unity3d.com/ScriptReference/AudioSource.Play.html
        musicSource.clip = next;
        musicSource.Play();
    }

    public void PlaySFX(string name)
    {
        Sound s = System.Array.Find(sfx, x => x.name == name);
        if (s == null)
        {
            Debug.Log("sfx not found: " + name);
            return;
        }

        //-- https://docs.unity3d.com/ScriptReference/AudioSource.PlayOneShot.html
        sfxSource.PlayOneShot(s.clip, sfxVolume);
    }

    //-- TODO 3d sounds (lizard noise), hook volume up to the pause menu
}
