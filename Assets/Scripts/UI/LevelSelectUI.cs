using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


public class LevelSelectUI : MonoBehaviour
{

    [Header("Botones de mundos")]
    public Button world1Button;
    public Button world2Button;
    public Button world3Button;
    public Button world4Button;

    [Header("Sprites Beix (mundo disponible)")]
    public Sprite blueNormal;
    public Sprite blueHover;
    public Sprite bluePressed;

    [Header("Sprites Dorado (mundo completado)")]
    public Sprite goldNormal;
    //public Sprite goldHover;
    //public Sprite goldPressed;

    [Header("Sprites Gris (mundo bloqueado)")]
    public Sprite greyNormal;
    //public Sprite greyHover;
    //public Sprite greyPressed;


    private void Start()
    {
        UpdateWorldButtons();
    }


    private void UpdateWorldButtons()
    {
        int highestWorld = PlayerPrefs.GetInt("HighestWorldUnlocked", 0);

        SetWorldButtonVisual(world1Button, 0, highestWorld);
        SetWorldButtonVisual(world2Button, 1, highestWorld);
        SetWorldButtonVisual(world3Button, 2, highestWorld);
        SetWorldButtonVisual(world4Button, 3, highestWorld);
    }


    private void SetWorldButtonVisual(Button btn, int worldIndex, int highestWorld)
    {
        Image img = btn.GetComponent<Image>();
        var state = btn.spriteState;

        if (worldIndex > highestWorld)
        {
            // 🔒 Mundo bloqueado → gris → NO pulsable
            btn.interactable = false;
            img.sprite = greyNormal;
            //state.highlightedSprite = greyHover;
            //state.pressedSprite = greyPressed;
        }
        else if (worldIndex < highestWorld)
        {
            // ⭐ Mundo completado → dorado → NO pulsable
            btn.interactable = false;
            img.sprite = goldNormal;
            //state.highlightedSprite = goldHover;
            //state.pressedSprite = goldPressed;
        }
        else
        {
            // 🔵 Mundo actual → Beix → SÍ pulsable
            btn.interactable = true;
            img.sprite = blueNormal;
            state.highlightedSprite = blueHover;
            state.pressedSprite = bluePressed;
        }

        btn.spriteState = state;
    }




    // Estos métodos los llamarán los botones desde el OnClick()

    public void OnSelectWorld1()
    {
        if (GameFlowManager.Instance != null)
        {
            // 1️⃣ Decimos que el mundo actual es el 0 (Mundo 1)
            GameFlowManager.Instance.currentWorldIndex = 0;

            // 2️⃣ Opcional: empezamos desde el nivel 0 (Nivel 1)
            GameFlowManager.Instance.currentLevelIndex = 0;

            // Cargar selector de niveles del Mundo 1
            GameFlowManager.Instance.LoadLevelSelectForWorld(0);
        }
        else
        {
            Debug.LogError("GameFlowManager no encontrado");
        }
    }

    public void OnSelectWorld2()
    {
        if (GameFlowManager.Instance != null)
        {
            GameFlowManager.Instance.currentWorldIndex = 1;
            GameFlowManager.Instance.currentLevelIndex = 0;
            // Cargar selector de niveles del Mundo 2
            GameFlowManager.Instance.LoadLevelSelectForWorld(1);
        }
    }

    public void OnSelectWorld3()
    {
        if (GameFlowManager.Instance != null)
        {
            GameFlowManager.Instance.currentWorldIndex = 2;
            GameFlowManager.Instance.currentLevelIndex = 0;
            // Cargar selector de niveles del Mundo 3
            GameFlowManager.Instance.LoadLevelSelectForWorld(2);
        }
    }

    public void OnSelectWorld4()
    {
        if (GameFlowManager.Instance != null)
        {
            GameFlowManager.Instance.currentWorldIndex = 3;
            GameFlowManager.Instance.currentLevelIndex = 0;
            // Cargar selector de niveles del Mundo 4
            GameFlowManager.Instance.LoadLevelSelectForWorld(3);
        }
    }

    public void PlayClickSound()
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayUIClick();
        else
            Debug.LogError("AudioManager no encontrado");
    }

    public void OnBackToMainMenu()
    {
        if (GameFlowManager.Instance != null)
        {
            GameFlowManager.Instance.LoadMainMenu();
        }
        else
        {
            Debug.LogError("GameFlowManager no encontrado");
        }
    }

}
