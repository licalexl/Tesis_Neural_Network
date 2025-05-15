using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Gestor central para todas las animaciones de UI.
/// Permite controlar, secuenciar y sincronizar múltiples animaciones.
/// </summary>
public class UIAnimationManager : MonoBehaviour
{
    private static UIAnimationManager _instance;
    public static UIAnimationManager Instance
    {
        get
        {
            if (_instance == null)
            {
                GameObject go = new GameObject("UIAnimationManager");
                _instance = go.AddComponent<UIAnimationManager>();
                DontDestroyOnLoad(go);
            }
            return _instance;
        }
    }

    private Dictionary<string, UIAnimation> _registeredAnimations = new Dictionary<string, UIAnimation>();
    private List<UIAnimation> _activeAnimations = new List<UIAnimation>();

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

    void Update()
    {
        // Actualizar todas las animaciones activas
        for (int i = _activeAnimations.Count - 1; i >= 0; i--)
        {
            UIAnimation anim = _activeAnimations[i];
            if (anim == null || !anim.IsPlaying)
            {
                _activeAnimations.RemoveAt(i);
            }
        }
    }

    /// <summary>
    /// Registra una animación para futura referencia
    /// </summary>
    public void RegisterAnimation(string id, UIAnimation animation)
    {
        if (!_registeredAnimations.ContainsKey(id))
        {
            _registeredAnimations.Add(id, animation);
        }
        else
        {
            _registeredAnimations[id] = animation;
        }
    }

    /// <summary>
    /// Obtiene una animación registrada por su ID
    /// </summary>
    public UIAnimation GetAnimation(string id)
    {
        UIAnimation anim;
        _registeredAnimations.TryGetValue(id, out anim);
        return anim;
    }

    /// <summary>
    /// Reproduce una animación registrada por su ID
    /// </summary>
    public void PlayAnimation(string id)
    {
        UIAnimation anim = GetAnimation(id);
        if (anim != null)
        {
            anim.Play();
            if (!_activeAnimations.Contains(anim))
            {
                _activeAnimations.Add(anim);
            }
        }
    }

    /// <summary>
    /// Detiene una animación registrada por su ID
    /// </summary>
    public void StopAnimation(string id)
    {
        UIAnimation anim = GetAnimation(id);
        if (anim != null)
        {
            anim.Stop();
            if (_activeAnimations.Contains(anim))
            {
                _activeAnimations.Remove(anim);
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

    private IEnumerator PlaySequenceCoroutine(string[] animationIds)
    {
        foreach (string id in animationIds)
        {
            UIAnimation anim = GetAnimation(id);
            if (anim != null)
            {
                anim.Play();
                if (!_activeAnimations.Contains(anim))
                {
                    _activeAnimations.Add(anim);
                }

                // Esperar a que termine esta animación antes de continuar
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
        foreach (var anim in _activeAnimations)
        {
            if (anim != null)
            {
                anim.Stop();
            }
        }
        _activeAnimations.Clear();
    }
}