using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    // -------------------------
    // SFX
    // -------------------------
    public AudioSource sfxSource;

    public AudioClip jumpClip;
    public AudioClip tileClip;
    public AudioClip deathClip;

    public AudioClip enemyMoveClip;
    public AudioClip enemyBounceClip;
    public AudioClip enemyHitClip;

    public AudioClip uiClickClip;

    // -------------------------
    // MÚSICA
    // -------------------------
    [Header("Música")]
    public AudioSource musicSource;   // NUEVO: fuente de música
    public AudioClip menuMusic;       // NUEVO: música del menú
    public AudioClip world1Music;     // NUEVO: música del mundo 1
    public AudioClip world2Music;     // (más adelante)
    public AudioClip world3Music;
    public AudioClip world4Music;

    public AudioClip victoryMusic;    // NUEVO
    public AudioClip gameOverMusic;   // NUEVO

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // -------------------------
    // SFX
    // -------------------------
    public void PlaySFX(AudioClip clip)
    {
        if (clip != null)
            sfxSource.PlayOneShot(clip);
    }

    public void PlaySFX(AudioClip clip, float volume)
    {
        if (clip == null || sfxSource == null) return;

        float previousVolume = sfxSource.volume;
        sfxSource.volume = Mathf.Clamp01(volume);
        sfxSource.PlayOneShot(clip);
        sfxSource.volume = previousVolume;
    }

    public void PlayUIClick()
    {
        PlaySFX(uiClickClip);
    }

    // -------------------------
    // MÚSICA
    // -------------------------
    public void PlayMusic(AudioClip clip, bool loop = true)
    {
        if (clip == null || musicSource == null)
            return;

        // Si ya está sonando esta música, NO reiniciar
        if (musicSource.clip == clip && musicSource.isPlaying)
            return;

        musicSource.loop = loop;
        musicSource.clip = clip;
        musicSource.Play();
    }

    public void StopMusic()
    {
        if (musicSource != null)
            musicSource.Stop();
    }
}
