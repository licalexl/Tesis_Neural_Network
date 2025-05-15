using UnityEngine;
using System.Collections;
using UnityEngine.Events;

/// <summary>
/// Maneja transiciones entre diferentes pantallas o paneles de UI
/// </summary>
public class UIScreenTransition : MonoBehaviour
{
    [System.Serializable]
    public class ScreenData
    {
        public GameObject screen;
        public string screenID;
        public UIAnimation inAnimation;
        public UIAnimation outAnimation;
    }

    [Header("Configuraci�n de Pantallas")]
    [Tooltip("Lista de pantallas a gestionar")]
    public ScreenData[] screens;

    [Tooltip("Pantalla activa al inicio")]
    public string initialScreenID;

    [Tooltip("Permitir transiciones superpuestas")]
    public bool allowOverlappingTransitions = false;

    [Header("Eventos")]
    public UnityEvent<string> onScreenChanged;

    private string _currentScreenID;
    private bool _isTransitioning = false;

    void Start()
    {
        // Ocultar todas las pantallas inicialmente
        foreach (var screen in screens)
        {
            if (screen.screen != null)
            {
                screen.screen.SetActive(false);
            }
        }

        // Mostrar la pantalla inicial
        if (!string.IsNullOrEmpty(initialScreenID))
        {
            ShowScreen(initialScreenID, false);
        }
    }

    /// <summary>
    /// Muestra una pantalla por su ID
    /// </summary>
    public void ShowScreen(string screenID, bool useTransition = true)
    {
        if (_isTransitioning && !allowOverlappingTransitions)
        {
            Debug.LogWarning("Ya hay una transici�n en curso. Petici�n ignorada.");
            return;
        }

        StartCoroutine(TransitionToScreen(screenID, useTransition));
    }

    private IEnumerator TransitionToScreen(string newScreenID, bool useTransition)
    {
        _isTransitioning = true;

        // Buscar las pantallas
        ScreenData currentScreen = null;
        ScreenData newScreen = null;

        // Encontrar datos de pantalla actual
        if (!string.IsNullOrEmpty(_currentScreenID))
        {
            foreach (var screen in screens)
            {
                if (screen.screenID == _currentScreenID)
                {
                    currentScreen = screen;
                    break;
                }
            }
        }

        // Encontrar datos de nueva pantalla
        foreach (var screen in screens)
        {
            if (screen.screenID == newScreenID)
            {
                newScreen = screen;
                break;
            }
        }

        // Verificar si se encontr� la nueva pantalla
        if (newScreen == null || newScreen.screen == null)
        {
            Debug.LogError($"Pantalla no encontrada: {newScreenID}");
            _isTransitioning = false;
            yield break;
        }

        // Realizar transici�n de salida para la pantalla actual
        if (currentScreen != null && currentScreen.screen != null && useTransition)
        {
            if (currentScreen.outAnimation != null)
            {
                currentScreen.outAnimation.Play();

                // Esperar a que termine la animaci�n
                while (currentScreen.outAnimation.IsPlaying)
                {
                    yield return null;
                }
            }

            // Ocultar la pantalla actual
            currentScreen.screen.SetActive(false);
        }

        // Mostrar la nueva pantalla
        newScreen.screen.SetActive(true);

        // Realizar transici�n de entrada para la nueva pantalla
        if (useTransition && newScreen.inAnimation != null)
        {
            newScreen.inAnimation.Play();

            // Esperar a que termine la animaci�n
            while (newScreen.inAnimation.IsPlaying)
            {
                yield return null;
            }
        }

        // Actualizar pantalla actual
        _currentScreenID = newScreenID;
        _isTransitioning = false;

        // Invocar evento de cambio de pantalla
        onScreenChanged?.Invoke(newScreenID);
    }
}