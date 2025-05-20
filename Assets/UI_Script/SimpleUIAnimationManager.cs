using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Gestor simplificado para las animaciones de UI
/// </summary>
public class SimpleUIAnimationManager : MonoBehaviour
{
    // Singleton
    private static SimpleUIAnimationManager _instance;
    public static SimpleUIAnimationManager Instance
    {
        get
        {
            if (_instance == null)
            {
                GameObject go = new GameObject("SimpleUIAnimationManager");
                _instance = go.AddComponent<SimpleUIAnimationManager>();
                DontDestroyOnLoad(go);
            }
            return _instance;
        }
    }

    // Diccionario de animaciones registradas
    private Dictionary<string, SimpleUIAnimator> registeredAnimations = new Dictionary<string, SimpleUIAnimator>();

    // Lista de animaciones activas
    private List<SimpleUIAnimator> activeAnimations = new List<SimpleUIAnimator>();

    // Asegurar que solo haya una instancia
    void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (_instance != this)
        {
            Destroy(gameObject);
        }
    }

    // Limpiar lista de animaciones activas
    void Update()
    {
        for (int i = activeAnimations.Count - 1; i >= 0; i--)
        {
            SimpleUIAnimator anim = activeAnimations[i];
            if (anim == null || !anim.IsPlaying)
            {
                activeAnimations.RemoveAt(i);
            }
        }
    }

    /// <summary>
    /// Registra una animación para su uso posterior
    /// </summary>
    public void RegisterAnimation(string id, SimpleUIAnimator animation)
    {
        if (!registeredAnimations.ContainsKey(id))
        {
            registeredAnimations.Add(id, animation);
        }
        else
        {
            registeredAnimations[id] = animation;
        }
    }

    /// <summary>
    /// Obtiene una animación por su ID
    /// </summary>
    public SimpleUIAnimator GetAnimation(string id)
    {
        SimpleUIAnimator anim;
        registeredAnimations.TryGetValue(id, out anim);
        return anim;
    }

    /// <summary>
    /// Reproduce una animación por su ID
    /// </summary>
    public void PlayAnimation(string id)
    {
        SimpleUIAnimator anim = GetAnimation(id);
        if (anim != null)
        {
            anim.Play();
            if (!activeAnimations.Contains(anim))
            {
                activeAnimations.Add(anim);
            }
        }
        else
        {
            Debug.LogWarning($"Animación no encontrada: {id}");
        }
    }

    /// <summary>
    /// Detiene una animación por su ID
    /// </summary>
    public void StopAnimation(string id)
    {
        SimpleUIAnimator anim = GetAnimation(id);
        if (anim != null)
        {
            anim.Stop();
            if (activeAnimations.Contains(anim))
            {
                activeAnimations.Remove(anim);
            }
        }
    }

    /// <summary>
    /// Reproduce una secuencia de animaciones en orden
    /// </summary>
    public void PlaySequence(params string[] animationIds)
    {
        StartCoroutine(PlaySequenceCoroutine(animationIds));
    }

    /// <summary>
    /// Corrutina para reproducir animaciones en secuencia
    /// </summary>
    private IEnumerator PlaySequenceCoroutine(string[] animationIds)
    {
        foreach (string id in animationIds)
        {
            SimpleUIAnimator anim = GetAnimation(id);
            if (anim != null)
            {
                anim.Play();
                if (!activeAnimations.Contains(anim))
                {
                    activeAnimations.Add(anim);
                }

                // Esperar a que termine esta animación
                while (anim.IsPlaying)
                {
                    yield return null;
                }
            }
        }
    }

    /// <summary>
    /// Detiene todas las animaciones activas
    /// </summary>
    public void StopAllAnimations()
    {
        foreach (var anim in activeAnimations)
        {
            if (anim != null)
            {
                anim.Stop();
            }
        }
        activeAnimations.Clear();
    }

    /// <summary>
    /// Añade animaciones predeterminadas a elementos UI comunes
    /// </summary>
    public void SetupButtonAnimation(GameObject button)
    {
        if (button == null) return;

        SimpleUIAnimator buttonAnim = button.AddComponent<SimpleUIAnimator>();
        buttonAnim.activeAnimations.Add(SimpleUIAnimator.AnimationType.Scale);
        buttonAnim.fromScale = Vector3.one;
        buttonAnim.toScale = new Vector3(0.95f, 0.95f, 0.95f);
        buttonAnim.duration = 0.1f;
        buttonAnim.animationID = "Button_" + button.name + "_Click";

        // Añadir UnityEvents para manejar clicks (requiere configuración manual en el inspector)
    }

    /// <summary>
    /// Configura una animación de fade in para un panel
    /// </summary>
    public void SetupPanelFadeIn(GameObject panel, string id = "", float duration = 0.3f, float delay = 0f)
    {
        if (panel == null) return;

        SimpleUIAnimator fadeAnim = panel.AddComponent<SimpleUIAnimator>();
        fadeAnim.activeAnimations.Add(SimpleUIAnimator.AnimationType.Fade);
        fadeAnim.fromAlpha = 0f;
        fadeAnim.toAlpha = 1f;
        fadeAnim.duration = duration;
        fadeAnim.delay = delay;

        if (string.IsNullOrEmpty(id))
            fadeAnim.animationID = "Panel_" + panel.name + "_FadeIn";
        else
            fadeAnim.animationID = id;
    }

    /// <summary>
    /// Configura una animación de entrada completa para un panel
    /// </summary>
    public void SetupPanelEntrance(GameObject panel, string id = "", Vector3 startOffset = default, float duration = 0.5f)
    {
        if (panel == null) return;

        // Si no se especifica un offset, usar uno predeterminado
        if (startOffset == default)
            startOffset = new Vector3(100, 0, 0);

        // Crear animador
        SimpleUIAnimator panelAnim = panel.AddComponent<SimpleUIAnimator>();

        // Configurar tipos de animación
        panelAnim.activeAnimations.Add(SimpleUIAnimator.AnimationType.Fade);
        panelAnim.activeAnimations.Add(SimpleUIAnimator.AnimationType.Move);
        panelAnim.activeAnimations.Add(SimpleUIAnimator.AnimationType.Scale);

        // Configurar fade
        panelAnim.fromAlpha = 0f;
        panelAnim.toAlpha = 1f;

        // Configurar movimiento
        RectTransform rect = panel.GetComponent<RectTransform>();
        if (rect != null)
        {
            panelAnim.fromPosition = rect.localPosition + startOffset;
            panelAnim.toPosition = rect.localPosition;
            panelAnim.useLocalPosition = true;
        }

        // Configurar escala
        panelAnim.fromScale = new Vector3(0.8f, 0.8f, 1f);
        panelAnim.toScale = Vector3.one;
        panelAnim.useBounce = true;
        panelAnim.bounceIntensity = 0.05f;

        // Configurar timing
        panelAnim.duration = duration;
        panelAnim.animationCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        // Configurar ID
        if (string.IsNullOrEmpty(id))
            panelAnim.animationID = "Panel_" + panel.name + "_Entrance";
        else
            panelAnim.animationID = id;
    }
}