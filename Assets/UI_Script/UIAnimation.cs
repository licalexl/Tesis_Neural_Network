using System.Collections;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Clase base abstracta para todas las animaciones de UI
/// </summary>
public abstract class UIAnimation : MonoBehaviour
{
    [Header("Configuraci�n B�sica")]
    [Tooltip("ID �nico para la animaci�n")]
    public string animationID;

    [Tooltip("Duraci�n de la animaci�n en segundos")]
    public float duration = 0.5f;

    [Tooltip("Retraso antes de iniciar la animaci�n")]
    public float delay = 0f;

    [Tooltip("Tipo de curva de animaci�n")]
    public AnimationCurve animationCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Tooltip("Reproducir al iniciar")]
    public bool playOnAwake = false;

    [Tooltip("Reproducir autom�ticamente en loop")]
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
        // Registrar esta animaci�n en el manager
        if (!string.IsNullOrEmpty(animationID))
        {
            UIAnimationManager.Instance.RegisterAnimation(animationID, this);
        }

        // Configuraci�n inicial
        InitializeAnimation();

        // Reproducir al inicio si est� configurado
        if (playOnAwake)
        {
            Play();
        }
    }

    /// <summary>
    /// Inicializa los par�metros de la animaci�n.
    /// Implementada por las clases derivadas.
    /// </summary>
    protected abstract void InitializeAnimation();

    /// <summary>
    /// Actualiza el estado de la animaci�n basado en el tiempo y la curva.
    /// Implementada por las clases derivadas.
    /// </summary>
    protected abstract void UpdateAnimation(float progress);

    /// <summary>
    /// Inicia la animaci�n
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
    /// Detiene la animaci�n
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
    /// Corrutina principal de animaci�n
    /// </summary>
    protected IEnumerator AnimationCoroutine()
    {
        // Esperar el retraso inicial
        if (delay > 0)
        {
            yield return new WaitForSeconds(delay);
        }

        // Iniciar la animaci�n
        _isPlaying = true;
        _currentTime = 0f;
        onAnimationStart?.Invoke();

        // Bucle principal de animaci�n
        do
        {
            while (_currentTime < duration)
            {
                // Calcular progreso normalizado (0-1)
                float normalizedTime = _currentTime / duration;

                // Evaluar la curva de animaci�n
                float curveValue = animationCurve.Evaluate(normalizedTime);

                // Actualizar la animaci�n
                UpdateAnimation(curveValue);

                // Incrementar tiempo
                _currentTime += Time.deltaTime;
                yield return null;
            }

            // Asegurar que la animaci�n llegue al valor final
            UpdateAnimation(animationCurve.Evaluate(1f));

            // Reiniciar si est� en loop
            if (loop)
            {
                _currentTime = 0f;
            }

        } while (loop);

        // Finalizar la animaci�n
        _isPlaying = false;
        _animationCoroutine = null;
        onAnimationComplete?.Invoke();
    }
}