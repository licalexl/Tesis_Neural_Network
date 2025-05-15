using System.Collections;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Clase base abstracta para todas las animaciones de UI
/// </summary>
public abstract class UIAnimation : MonoBehaviour
{
    [Header("Configuración Básica")]
    [Tooltip("ID único para la animación")]
    public string animationID;

    [Tooltip("Duración de la animación en segundos")]
    public float duration = 0.5f;

    [Tooltip("Retraso antes de iniciar la animación")]
    public float delay = 0f;

    [Tooltip("Tipo de curva de animación")]
    public AnimationCurve animationCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Tooltip("Reproducir al iniciar")]
    public bool playOnAwake = false;

    [Tooltip("Reproducir automáticamente en loop")]
    public bool loop = false;

    [Header("Eventos")]
    public UnityEvent onAnimationStart;
    public UnityEvent onAnimationComplete;

    // Estado interno
    protected bool _isPlaying = false;
    protected float _currentTime = 0f;
    protected Coroutine _animationCoroutine = null;

    public bool IsPlaying { get { return _isPlaying; } }

    protected virtual void Awake()
    {
        // Registrar esta animación en el manager
        if (!string.IsNullOrEmpty(animationID))
        {
            UIAnimationManager.Instance.RegisterAnimation(animationID, this);
        }

        // Configuración inicial
        InitializeAnimation();

        // Reproducir al inicio si está configurado
        if (playOnAwake)
        {
            Play();
        }
    }

    /// <summary>
    /// Inicializa los parámetros de la animación.
    /// Implementada por las clases derivadas.
    /// </summary>
    protected abstract void InitializeAnimation();

    /// <summary>
    /// Actualiza el estado de la animación basado en el tiempo y la curva.
    /// Implementada por las clases derivadas.
    /// </summary>
    protected abstract void UpdateAnimation(float progress);

    /// <summary>
    /// Inicia la animación
    /// </summary>
    public virtual void Play()
    {
        if (_animationCoroutine != null)
        {
            StopCoroutine(_animationCoroutine);
        }
        _animationCoroutine = StartCoroutine(AnimationCoroutine());
    }

    /// <summary>
    /// Detiene la animación
    /// </summary>
    public virtual void Stop()
    {
        if (_animationCoroutine != null)
        {
            StopCoroutine(_animationCoroutine);
            _animationCoroutine = null;
        }
        _isPlaying = false;
    }

    /// <summary>
    /// Corrutina principal de animación
    /// </summary>
    protected IEnumerator AnimationCoroutine()
    {
        // Esperar el retraso inicial
        if (delay > 0)
        {
            yield return new WaitForSeconds(delay);
        }

        // Iniciar la animación
        _isPlaying = true;
        _currentTime = 0f;
        onAnimationStart?.Invoke();

        // Bucle principal de animación
        do
        {
            while (_currentTime < duration)
            {
                // Calcular progreso normalizado (0-1)
                float normalizedTime = _currentTime / duration;

                // Evaluar la curva de animación
                float curveValue = animationCurve.Evaluate(normalizedTime);

                // Actualizar la animación
                UpdateAnimation(curveValue);

                // Incrementar tiempo
                _currentTime += Time.deltaTime;
                yield return null;
            }

            // Asegurar que la animación llegue al valor final
            UpdateAnimation(animationCurve.Evaluate(1f));

            // Reiniciar si está en loop
            if (loop)
            {
                _currentTime = 0f;
            }

        } while (loop);

        // Finalizar la animación
        _isPlaying = false;
        _animationCoroutine = null;
        onAnimationComplete?.Invoke();
    }
}