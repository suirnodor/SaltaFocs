using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    // Instancia única del AudioManager (patrón Singleton)
    // Esto permite llamar a AudioManager.Instance desde cualquier script
    public static AudioManager Instance;

    // Este AudioSource será el que reproduzca los sonidos (SFX)
    public AudioSource sfxSource;

    // Clips de audio que asignaremos desde el Inspector para el player
    public AudioClip jumpClip;   // Sonido al saltar
    public AudioClip tileClip;   // Sonido al pisar un tile
    public AudioClip deathClip;  // Sonido al morir

    // Clips de audio que asignaremos desde el Inspector para el enemigo
    public AudioClip enemyMoveClip;     // Sonido cuando el enemigo se mueve
    public AudioClip enemyBounceClip;   // Sonido cuando rebota en un borde
    public AudioClip enemyHitClip;      // Sonido cuando mata al jugador


    private void Awake()
    {
        // Si no existe un AudioManager en la escena, este será el primero
        if (Instance == null)
        {
            Instance = this;

            // Esto hace que el AudioManager NO se destruya al cambiar de escena
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            // Si ya existe uno, destruimos este para evitar duplicados
            Destroy(gameObject);
        }
    }

    // Función para reproducir un sonido
    // Se llama desde otros scripts: AudioManager.Instance.PlaySFX(clip);
    public void PlaySFX(AudioClip clip)
    {
        // Si el clip no es nulo, lo reproducimos una vez
        if (clip != null)
            sfxSource.PlayOneShot(clip);
    }
}

