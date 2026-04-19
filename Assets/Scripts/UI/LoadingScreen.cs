using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LoadingScreen : MonoBehaviour
{
    [Header("Escena a cargar")]
    public string sceneToLoad = "MainMenu";

    [Header("Tiempo mínimo de pantalla de carga")]
    public float minimumLoadingTime = 3f; // ← Aquí decides cuánto tarda la barra

    [Header("UI")]
    public Image loadingBar;

    private void Start()
    {
        // ⭐ Iniciar música del menú SOLO una vez
        AudioManager.Instance.PlayMusic(AudioManager.Instance.menuMusic);

        StartCoroutine(LoadSceneSimulated());
    }

    private IEnumerator LoadSceneSimulated()
    {
        // Empezamos a cargar la escena en segundo plano
        AsyncOperation op = SceneManager.LoadSceneAsync(sceneToLoad);
        op.allowSceneActivation = false;

        float elapsedTime = 0f;

        // Mientras no haya pasado el tiempo mínimo...
        while (elapsedTime < minimumLoadingTime)
        {
            elapsedTime += Time.deltaTime;

            // Progreso simulado entre 0 y 1
            float progress = elapsedTime / minimumLoadingTime;

            // Actualizamos la barra
            if (loadingBar != null)
                loadingBar.fillAmount = progress;

            yield return null;
        }

        // Cuando el tiempo se cumple, activamos la escena
        op.allowSceneActivation = true;
    }
}
