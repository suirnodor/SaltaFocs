using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameFlowManager : MonoBehaviour
{
    public static GameFlowManager Instance;

    // Mundo actual (0 = Mundo1, 1 = Mundo2, etc.)
    public int currentWorldIndex = 0;

    // Nivel actual dentro del mundo (0 = Level01, 1 = Level02, etc.)
    public int currentLevelIndex = 0;

    // ⭐ Puntuación con la que entraste al nivel (checkpoint)
    public int scoreAtLevelStart = 0;

    // ⭐ Puntuación actual de la partida
    public int currentScore = 0;

    // ⭐ Mejor puntuación histórica
    public int bestScore = 0;

    // Claves PlayerPrefs
    private const string KEY_BEST_SCORE = "BestScore";
    private const string KEY_HIGHEST_WORLD = "HighestWorldUnlocked";
    private const string KEY_HIGHEST_LEVEL = "HighestLevelUnlocked";
    private const string KEY_LANGUAGE = "Language";

    private const string KEY_CURRENT_SCORE = "CurrentScore";
    private const string KEY_SCORE_AT_LEVEL_START = "ScoreAtLevelStart";


    // MATRIZ DE NIVELES
    public string[,] levelSceneNames = new string[,]
    {
        // Mundo 1
        { "M1_Level01", "M1_Level02", "M1_Level03", "M1_Level04", "M1_Level05",
          "M1_Level06", "M1_Level07", "M1_Level08", "M1_Level09", "M1_Level10" },

        // Mundo 2
        { "M2_Level01", "M2_Level02", "M2_Level03", "M2_Level04", "M2_Level05",
          "M2_Level06", "M2_Level07", "M2_Level08", "M2_Level09", "M2_Level10" },

        // Mundo 3
        { "M3_Level01", "M3_Level02", "M3_Level03", "M3_Level04", "M3_Level05",
          "M3_Level06", "M3_Level07", "M3_Level08", "M3_Level09", "M3_Level10" },

        // Mundo 4
        { "M4_Level01", "M4_Level02", "M4_Level03", "M4_Level04", "M4_Level05",
          "M4_Level06", "M4_Level07", "M4_Level08", "M4_Level09", "M4_Level10" }
    };


    // Escenas auxiliares
    public string mainMenuSceneName = "MainMenu";
    public string levelSelectSceneName = "LevelSelect_M1";
    public string gameOverSceneName = "GameOver";
    public string victorySceneName = "Victory";

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadProgress();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // ---------------------------------------------------------
    // CARGA / GUARDADO DE PROGRESO
    // ---------------------------------------------------------

    private void LoadProgress()
    {
        bestScore = PlayerPrefs.GetInt(KEY_BEST_SCORE, 0);

        int highestWorld = PlayerPrefs.GetInt(KEY_HIGHEST_WORLD, 0);
        int highestLevel = PlayerPrefs.GetInt(KEY_HIGHEST_LEVEL, 0);

        highestWorld = Mathf.Clamp(highestWorld, 0, levelSceneNames.GetLength(0) - 1);
        highestLevel = Mathf.Clamp(highestLevel, 0, levelSceneNames.GetLength(1) - 1);

        currentScore = PlayerPrefs.GetInt(KEY_CURRENT_SCORE, 0);
        scoreAtLevelStart = PlayerPrefs.GetInt(KEY_SCORE_AT_LEVEL_START, 0);

    }

    private void SaveProgress(int worldIndex, int levelIndex)
    {
        int highestWorld = PlayerPrefs.GetInt(KEY_HIGHEST_WORLD, 0);
        int highestLevel = PlayerPrefs.GetInt(KEY_HIGHEST_LEVEL, 0);

        if (worldIndex > highestWorld)
        {
            highestWorld = worldIndex;
            highestLevel = levelIndex;
        }
        else if (worldIndex == highestWorld && levelIndex > highestLevel)
        {
            highestLevel = levelIndex;
        }

        PlayerPrefs.SetInt(KEY_HIGHEST_WORLD, highestWorld);
        PlayerPrefs.SetInt(KEY_HIGHEST_LEVEL, highestLevel);
        PlayerPrefs.Save();
    }

    private void SaveBestScoreIfNeeded()
    {
        if (currentScore > bestScore)
        {
            bestScore = currentScore;
            PlayerPrefs.SetInt(KEY_BEST_SCORE, bestScore);
            PlayerPrefs.Save();
        }
    }

    public bool IsLevelUnlocked(int worldIndex, int levelIndex)
    {
        int highestWorld = PlayerPrefs.GetInt(KEY_HIGHEST_WORLD, 0);
        int highestLevel = PlayerPrefs.GetInt(KEY_HIGHEST_LEVEL, 0);

        if (worldIndex < highestWorld) return true;
        if (worldIndex > highestWorld) return false;

        return levelIndex <= highestLevel;
    }

    // ---------------------------------------------------------
    // SISTEMA DE PUNTUACIÓN
    // ---------------------------------------------------------

    public void AddScore(int amount)
    {
        currentScore += amount;

        HUDController hud = Object.FindFirstObjectByType<HUDController>();
        if (hud != null)
            hud.SetScore(currentScore);
    }

    public void ResetScore()
    {
        currentScore = 0;

        HUDController hud = Object.FindFirstObjectByType<HUDController>();
        if (hud != null)
            hud.SetScore(currentScore);
    }

    // ---------------------------------------------------------
    // CARGA DE NIVELES
    // ---------------------------------------------------------

    // ⭐ NUEVO: cargar un nivel concreto (para LevelSelect)
    public void LoadSpecificLevel(int worldIndex, int levelIndex)
    {
        currentWorldIndex = worldIndex;
        currentLevelIndex = levelIndex;

        Debug.Log("ENTRANDO A NIVEL DESDE LEVEL SELECT: " + worldIndex + " - " + levelIndex);


        // Guardar puntuación de entrada al nivel
        scoreAtLevelStart = currentScore;
        PlayerPrefs.SetInt(KEY_SCORE_AT_LEVEL_START, scoreAtLevelStart);
        PlayerPrefs.SetInt(KEY_CURRENT_SCORE, currentScore);
        PlayerPrefs.Save();


        string sceneName = levelSceneNames[worldIndex, levelIndex];
        SceneManager.LoadScene(sceneName);
    }

    public void LoadWorld(int worldIndex)
    {
        currentWorldIndex = worldIndex;
        currentLevelIndex = 0;

        Debug.Log("ENTRANDO A PRIMER NIVEL DEL MUNDO: " + currentWorldIndex + " - " + currentLevelIndex);


        ResetScore();

        scoreAtLevelStart = currentScore;

        string sceneName = levelSceneNames[currentWorldIndex, currentLevelIndex];
        SceneManager.LoadScene(sceneName);
    }

    public void ReloadCurrentLevel()
    {
        // ⭐ Volver a la puntuación de entrada al nivel
        currentScore = scoreAtLevelStart;

        HUDController hud = Object.FindFirstObjectByType<HUDController>();
        if (hud != null)
            hud.SetScore(currentScore);

        string sceneName = levelSceneNames[currentWorldIndex, currentLevelIndex];
        SceneManager.LoadScene(sceneName);
    }

    public void LoadNextLevel()
    {
        // Subimos el índice del nivel actual
        // Ejemplo: estábamos en Nivel 1 (índice 0),
        // ahora pasamos a Nivel 2 (índice 1)
        currentLevelIndex++;

        Debug.Log("ENTRANDO A SIGUIENTE NIVEL: " + currentWorldIndex + " - " + currentLevelIndex);


        // Comprobamos si todavía hay niveles dentro de este mundo
        if (currentLevelIndex < levelSceneNames.GetLength(1))
        {
            // Guardar puntuación
            scoreAtLevelStart = currentScore;
            PlayerPrefs.SetInt(KEY_SCORE_AT_LEVEL_START, scoreAtLevelStart);
            PlayerPrefs.SetInt(KEY_CURRENT_SCORE, currentScore);
            PlayerPrefs.Save();


            // ⭐ AHORA SÍ: guardamos el progreso
            // currentWorldIndex = mundo actual
            // currentLevelIndex = nivel al que acabamos de pasar (desbloqueado)
            SaveProgress(currentWorldIndex, currentLevelIndex);

            // Cargamos la escena del siguiente nivel
            string sceneName = levelSceneNames[currentWorldIndex, currentLevelIndex];
            SceneManager.LoadScene(sceneName);
        }
        else
        {
            // Si no hay más niveles en este mundo, pasamos al siguiente mundo
            LoadNextWorld();
        }
    }


    public void LoadNextWorld()
    {
        // Pasamos al siguiente mundo
        currentWorldIndex++;

        if (currentWorldIndex < levelSceneNames.GetLength(0))
        {
            // Empezamos SIEMPRE por el nivel 0 del nuevo mundo
            currentLevelIndex = 0;

            Debug.Log("ENTRANDO A PRIMER NIVEL DEL MUNDO (SIN RESETEAR PUNTOS): "
                      + currentWorldIndex + " - " + currentLevelIndex);

            // ❗ IMPORTANTE:
            // NO llamamos a ResetScore().
            // Mantenemos currentScore con los puntos acumulados.

            // Guardamos la puntuación de entrada a este nuevo nivel
            scoreAtLevelStart = currentScore;
            PlayerPrefs.SetInt(KEY_SCORE_AT_LEVEL_START, scoreAtLevelStart);
            PlayerPrefs.SetInt(KEY_CURRENT_SCORE, currentScore);
            PlayerPrefs.Save();

            // Cargamos la escena del primer nivel del nuevo mundo
            string sceneName = levelSceneNames[currentWorldIndex, currentLevelIndex];
            SceneManager.LoadScene(sceneName);
        }
        else
        {
            // Si no hay más mundos, volvemos al selector de niveles
            LoadLevelSelect();
        }
    }


    // ---------------------------------------------------------
    // EVENTOS DEL JUGADOR
    // ---------------------------------------------------------

    public void OnPlayerDied()
    {
        SaveBestScoreIfNeeded();

        // ⭐ Música de Game Over
        AudioManager.Instance.PlayMusic(AudioManager.Instance.gameOverMusic, false);

        SceneManager.LoadScene(gameOverSceneName);
    }

    public void OnLevelCompleted()
    {
        // 1️⃣ Sumar puntos por completar el nivel
        AddScore(10);

        // 2️⃣ Guardar mejor puntuación si hace falta
        SaveBestScoreIfNeeded();

        // 3️⃣ Guardar la puntuación ACTUAL (muy importante)
        PlayerPrefs.SetInt(KEY_CURRENT_SCORE, currentScore);
        PlayerPrefs.Save();

        // 4️⃣ Comprobar si estamos en el ÚLTIMO nivel de este mundo
        int lastLevelIndex = levelSceneNames.GetLength(1) - 1;

        if (currentLevelIndex < lastLevelIndex)
        {
            // ✅ Aún hay niveles en este mundo
            // Desbloqueamos el siguiente nivel del mismo mundo
            int nextLevelIndex = currentLevelIndex + 1;
            SaveProgress(currentWorldIndex, nextLevelIndex);
        }
        else
        {
            // ✅ Hemos completado el ÚLTIMO nivel de este mundo
            // Intentamos desbloquear el PRIMER nivel del siguiente mundo
            int nextWorldIndex = currentWorldIndex + 1;

            if (nextWorldIndex < levelSceneNames.GetLength(0))
            {
                // Desbloqueamos Mundo siguiente, Nivel 0
                SaveProgress(nextWorldIndex, 0);
            }
            else
            {
                // No hay más mundos (estamos en el último mundo del juego)
                // Guardamos el progreso tal cual
                SaveProgress(currentWorldIndex, currentLevelIndex);
            }
        }

        // ⭐ Música de Victoria
        AudioManager.Instance.PlayMusic(AudioManager.Instance.victoryMusic, false);

        // 5️⃣ Cargar la pantalla de victoria
        SceneManager.LoadScene(victorySceneName);
    }







    // ---------------------------------------------------------
    // MENÚS
    // ---------------------------------------------------------

    public void LoadMainMenu()
    {
        // No borramos la puntuación, la mantenemos
        currentScore = PlayerPrefs.GetInt(KEY_CURRENT_SCORE, 0);
        scoreAtLevelStart = PlayerPrefs.GetInt(KEY_SCORE_AT_LEVEL_START, 0);


        // ⭐ Volver al primer nivel desbloqueado
        int highestWorld = PlayerPrefs.GetInt(KEY_HIGHEST_WORLD, 0);
        int highestLevel = PlayerPrefs.GetInt(KEY_HIGHEST_LEVEL, 0);

        currentWorldIndex = highestWorld;
        currentLevelIndex = highestLevel;

        // Cargar menú principal
        SceneManager.LoadScene(mainMenuSceneName);
    }


    public void LoadLevelSelect()
    {
        // Recuperar el mundo y nivel desbloqueado
        int highestWorld = PlayerPrefs.GetInt(KEY_HIGHEST_WORLD, 0);
        int highestLevel = PlayerPrefs.GetInt(KEY_HIGHEST_LEVEL, 0);

        currentWorldIndex = highestWorld;
        currentLevelIndex = highestLevel;

        // Recuperar puntuación
        currentScore = PlayerPrefs.GetInt(KEY_CURRENT_SCORE, 0);
        scoreAtLevelStart = PlayerPrefs.GetInt(KEY_SCORE_AT_LEVEL_START, 0);

        // Cargar la escena de selección de niveles
        SceneManager.LoadScene(levelSelectSceneName);
    }


    public void QuitGame()
    {
        Application.Quit();
    }

    public void LoadWorldSelect()
    {
        SceneManager.LoadScene("LevelSelect");
    }

    public void ReloadProgress()
    {
        LoadProgress();
    }

    public void LoadLevelSelectForWorld(int worldIndex)
    {
        // Guardamos el mundo actual
        currentWorldIndex = worldIndex;

        // Calculamos el número de mundo para el nombre de la escena
        // Mundo 0 → LevelSelect_M1
        // Mundo 1 → LevelSelect_M2
        // Mundo 2 → LevelSelect_M3
        // Mundo 3 → LevelSelect_M4
        int worldNumber = worldIndex + 1;

        // Construimos el nombre de la escena
        string sceneName = "LevelSelect_M" + worldNumber;

        // Cargamos la escena de selección de niveles de ese mundo
        SceneManager.LoadScene(sceneName);
    }



}

