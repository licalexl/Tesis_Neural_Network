using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Events;

/// <summary>
/// Componente unificado para animaciones de UI que combina múltiples tipos de animación
/// </summary>
public class SimpleUIAnimator : MonoBehaviour
{
    [System.Serializable]
    public enum AnimationType
    {
        Fade,
        Move,
        Scale,
        Rotate,
        Color,
        Shake,
        Punch
    }

    [Header("Configuración General")]
    public string animationID;
    public float duration = 0.5f;
    public float delay = 0f;
    public bool playOnAwake = false;
    public bool loop = false;
    public AnimationCurve animationCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Tipos de Animación")]
    public List<AnimationType> activeAnimations = new List<AnimationType>();

    [Header("Fade")]
    public float fromAlpha = 0f;
    public float toAlpha = 1f;
    public bool affectChildren = true;

    [Header("Move")]
    public Vector3 fromPosition;
    public Vector3 toPosition;
    public bool useLocalPosition = true;

    [Header("Scale")]
    public Vector3 fromScale = new Vector3(0.8f, 0.8f, 1f);
    public Vector3 toScale = Vector3.one;
    public bool useBounce = false;
    public float bounceIntensity = 0.2f;

    [Header("Rotate")]
    public Vector3 fromRotation;
    public Vector3 toRotation;
    public bool useShortestPath = true;

    [Header("Color")]
    public Color fromColor = Color.white;
    public Color toColor = Color.white;

    [Header("Shake")]
    public float shakeIntensity = 10f;
    public float shakeSpeed = 10f;
    public bool shakePosition = true;
    public bool shakeRotation = false;

    [Header("Punch")]
    public Vector3 punchDirection = new Vector3(0, 20, 0);
    public int oscillations = 2;
    public float elasticity = 0.3f;

    [Header("Grupo de Animaciones")]
    public List<SimpleUIAnimator> childAnimations = new List<SimpleUIAnimator>();
    public bool sequential = false;

    [Header("Eventos")]
    public UnityEvent onAnimationStart;
    public UnityEvent onAnimationComplete;

    // Componentes y variables internas
    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;
    private Graphic[] graphics;
    private Color[] originalColors;
    private Vector3 originalPosition;
    private Vector3 originalScale;
    private Quaternion originalRotation;
    private float randomOffset;

    // Estado de la animación
    private Coroutine animationCoroutine;
    private bool isPlaying = false;
    private float currentTime = 0f;

    public bool IsPlaying { get { return isPlaying; } }

    private void Awake()
    {
        // Registrar animación en el manager
        if (!string.IsNullOrEmpty(animationID))
        {
            SimpleUIAnimationManager.Instance.RegisterAnimation(animationID, this);
        }

        Initialize();

        // Reproducir al iniciar si está activado
        if (playOnAwake)
        {
            Play();
        }
    }

    public void Initialize()
    {
        // Obtener componentes necesarios
        rectTransform = GetComponent<RectTransform>();

        // Para animaciones de fade
        if (activeAnimations.Contains(AnimationType.Fade))
        {
            // Intentar obtener canvas group
            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null && affectChildren)
            {
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }

            // Obtener elementos gráficos
            if (affectChildren)
            {
                graphics = GetComponentsInChildren<Graphic>(true);
            }
            else
            {
                graphics = GetComponents<Graphic>();
            }

            // Guardar colores originales
            if (graphics != null && graphics.Length > 0)
            {
                originalColors = new Color[graphics.Length];
                for (int i = 0; i < graphics.Length; i++)
                {
                    if (graphics[i] != null)
                    {
                        originalColors[i] = graphics[i].color;
                    }
                }
            }

            // Establecer valor alpha inicial
            SetAlpha(fromAlpha);
        }

        // Para animaciones de movimiento
        if (activeAnimations.Contains(AnimationType.Move) && rectTransform != null)
        {
            originalPosition = useLocalPosition ? rectTransform.localPosition : rectTransform.position;

            // Si no se especifica la posición inicial, usar la actual
            if (fromPosition == Vector3.zero)
                fromPosition = originalPosition;

            // Si no se especifica la posición final, usar la actual
            if (toPosition == Vector3.zero)
                toPosition = originalPosition;

            // Establecer posición inicial
            if (useLocalPosition)
                rectTransform.localPosition = fromPosition;
            else
                rectTransform.position = fromPosition;
        }

        // Para animaciones de escala
        if (activeAnimations.Contains(AnimationType.Scale) && rectTransform != null)
        {
            originalScale = rectTransform.localScale;

            // Si no se especifica la escala inicial, usar la actual
            if (fromScale == Vector3.zero)
                fromScale = originalScale;

            // Si no se especifica la escala final, usar la actual
            if (toScale == Vector3.zero)
                toScale = originalScale;

            // Establecer escala inicial
            rectTransform.localScale = fromScale;
        }

        // Para animaciones de rotación
        if (activeAnimations.Contains(AnimationType.Rotate) && rectTransform != null)
        {
            originalRotation = rectTransform.localRotation;

            // Establecer rotación inicial
            rectTransform.localRotation = Quaternion.Euler(fromRotation);
        }

        // Para animaciones de color
        if (activeAnimations.Contains(AnimationType.Color) && graphics != null)
        {
            // Los colores originales ya se guardan en la sección de fade
            SetColor(fromColor);
        }

        // Para animaciones de shake o punch
        if ((activeAnimations.Contains(AnimationType.Shake) ||
            activeAnimations.Contains(AnimationType.Punch)) &&
            rectTransform != null)
        {
            randomOffset = Random.Range(0f, 100f);
        }
    }

    /// <summary>
    /// Inicia la animación
    /// </summary>
    public void Play()
    {
        // Si hay animaciones hijas, reproducirlas también
        if (childAnimations.Count > 0)
        {
            if (sequential)
            {
                StartCoroutine(PlaySequentialAnimations());
            }
            else
            {
                foreach (var anim in childAnimations)
                {
                    if (anim != null)
                    {
                        anim.Play();
                    }
                }
            }
        }

        // Si no hay animaciones activas, salir
        if (activeAnimations.Count == 0)
            return;

        // Detener animación actual si existe
        if (animationCoroutine != null)
        {
            StopCoroutine(animationCoroutine);
        }

        // Iniciar nueva animación
        animationCoroutine = StartCoroutine(AnimationCoroutine());
    }

    /// <summary>
    /// Detiene la animación
    /// </summary>
    public void Stop()
    {
        // Detener animaciones hijas
        foreach (var anim in childAnimations)
        {
            if (anim != null)
            {
                anim.Stop();
            }
        }

        // Detener animación actual
        if (animationCoroutine != null)
        {
            StopCoroutine(animationCoroutine);
            animationCoroutine = null;
        }

        // Restablecer estado
        isPlaying = false;

        // Restablecer valores originales
        ResetToOriginalValues();
    }

    /// <summary>
    /// Restablece los valores originales
    /// </summary>
    private void ResetToOriginalValues()
    {
        if (rectTransform != null)
        {
            if (activeAnimations.Contains(AnimationType.Move))
            {
                if (useLocalPosition)
                    rectTransform.localPosition = originalPosition;
                else
                    rectTransform.position = originalPosition;
            }

            if (activeAnimations.Contains(AnimationType.Scale))
            {
                rectTransform.localScale = originalScale;
            }

            if (activeAnimations.Contains(AnimationType.Rotate))
            {
                rectTransform.localRotation = originalRotation;
            }
        }

        if (activeAnimations.Contains(AnimationType.Fade) || activeAnimations.Contains(AnimationType.Color))
        {
            RestoreOriginalColors();
        }
    }

    /// <summary>
    /// Restaura los colores originales de los elementos gráficos
    /// </summary>
    private void RestoreOriginalColors()
    {
        if (graphics != null && originalColors != null)
        {
            for (int i = 0; i < graphics.Length; i++)
            {
                if (graphics[i] != null && i < originalColors.Length)
                {
                    graphics[i].color = originalColors[i];
                }
            }
        }
    }

    /// <summary>
    /// Corrutina principal que maneja la animación
    /// </summary>
    private IEnumerator AnimationCoroutine()
    {
        // Esperar el retraso inicial
        if (delay > 0)
        {
            yield return new WaitForSeconds(delay);
        }

        // Iniciar animación
        isPlaying = true;
        currentTime = 0f;
        onAnimationStart?.Invoke();

        // Bucle principal de animación
        do
        {
            while (currentTime < duration)
            {
                // Calcular progreso normalizado (0-1)
                float normalizedTime = currentTime / duration;

                // Evaluar la curva de animación
                float curveValue = animationCurve.Evaluate(normalizedTime);

                // Actualizar cada tipo de animación activa
                UpdateAnimations(curveValue);

                // Incrementar tiempo
                currentTime += Time.deltaTime;
                yield return null;
            }

            // Asegurar que la animación llegue al valor final
            UpdateAnimations(animationCurve.Evaluate(1f));

            // Reiniciar si está en loop
            if (loop)
            {
                currentTime = 0f;
            }

        } while (loop);

        // Finalizar animación
        isPlaying = false;
        animationCoroutine = null;
        onAnimationComplete?.Invoke();
    }

    /// <summary>
    /// Actualiza todos los tipos de animación activos
    /// </summary>
    private void UpdateAnimations(float progress)
    {
        foreach (var type in activeAnimations)
        {
            switch (type)
            {
                case AnimationType.Fade:
                    UpdateFade(progress);
                    break;
                case AnimationType.Move:
                    UpdateMove(progress);
                    break;
                case AnimationType.Scale:
                    UpdateScale(progress);
                    break;
                case AnimationType.Rotate:
                    UpdateRotate(progress);
                    break;
                case AnimationType.Color:
                    UpdateColor(progress);
                    break;
                case AnimationType.Shake:
                    UpdateShake(progress);
                    break;
                case AnimationType.Punch:
                    UpdatePunch(progress);
                    break;
            }
        }
    }

    // Métodos de actualización para cada tipo de animación

    private void UpdateFade(float progress)
    {
        float currentAlpha = Mathf.Lerp(fromAlpha, toAlpha, progress);
        SetAlpha(currentAlpha);
    }

    private void UpdateMove(float progress)
    {
        if (rectTransform == null) return;

        Vector3 currentPosition = Vector3.Lerp(fromPosition, toPosition, progress);

        if (useLocalPosition)
            rectTransform.localPosition = currentPosition;
        else
            rectTransform.position = currentPosition;
    }

    private void UpdateScale(float progress)
    {
        if (rectTransform == null) return;

        Vector3 currentScale;

        if (useBounce && progress > 0.8f)
        {
            // Crear efecto de rebote al final
            float bounceProgress = (progress - 0.8f) / 0.2f;
            float bounceFactor = Mathf.Sin(bounceProgress * Mathf.PI) * bounceIntensity;

            currentScale = Vector3.Lerp(fromScale, toScale, progress);
            currentScale += new Vector3(bounceFactor, bounceFactor, bounceFactor);
        }
        else
        {
            currentScale = Vector3.Lerp(fromScale, toScale, progress);
        }

        rectTransform.localScale = currentScale;
    }

    private void UpdateRotate(float progress)
    {
        if (rectTransform == null) return;

        Quaternion fromQuat = Quaternion.Euler(fromRotation);
        Quaternion toQuat = Quaternion.Euler(toRotation);

        Quaternion currentRotation;
        if (useShortestPath)
        {
            currentRotation = Quaternion.Slerp(fromQuat, toQuat, progress);
        }
        else
        {
            currentRotation = Quaternion.Lerp(fromQuat, toQuat, progress);
        }

        rectTransform.localRotation = currentRotation;
    }

    private void UpdateColor(float progress)
    {
        Color currentColor = Color.Lerp(fromColor, toColor, progress);
        SetColor(currentColor);
    }

    private void UpdateShake(float progress)
    {
        if (rectTransform == null) return;

        // Factor de intensidad basado en el progreso
        float intensityFactor = Mathf.Sin(progress * Mathf.PI); // Aumenta y disminuye

        // Tiempo para el ruido Perlin
        float time = Time.time * shakeSpeed + randomOffset;

        if (shakePosition)
        {
            // Calcular desplazamiento aleatorio
            Vector3 shakeOffset = new Vector3(
                Mathf.PerlinNoise(time, 0) * 2 - 1,
                Mathf.PerlinNoise(0, time) * 2 - 1,
                0
            ) * shakeIntensity * intensityFactor;

            // Aplicar a la posición
            if (useLocalPosition)
                rectTransform.localPosition = originalPosition + shakeOffset;
            else
                rectTransform.position = originalPosition + shakeOffset;
        }

        if (shakeRotation)
        {
            // Calcular rotación aleatoria (solo en Z para UI 2D)
            float rotationShake = (Mathf.PerlinNoise(time, time) * 2 - 1) *
                                  shakeIntensity * 0.5f * intensityFactor;

            // Aplicar a la rotación
            rectTransform.localRotation = originalRotation * Quaternion.Euler(0, 0, rotationShake);
        }
    }

    private void UpdatePunch(float progress)
    {
        if (rectTransform == null) return;

        // Función de punch basada en seno amortiguado
        float decay = Mathf.Clamp01(progress * oscillations);
        float amplitude = Mathf.Sin(decay * Mathf.PI * 2) * Mathf.Pow(1 - progress, elasticity);

        // Aplicar a la posición si está habilitado
        Vector3 punchOffset = punchDirection * amplitude;

        if (useLocalPosition)
            rectTransform.localPosition = originalPosition + punchOffset;
        else
            rectTransform.position = originalPosition + punchOffset;
    }

    /// <summary>
    /// Establece el valor alpha para los elementos
    /// </summary>
    private void SetAlpha(float alpha)
    {
        // Usar CanvasGroup si está disponible
        if (canvasGroup != null)
        {
            canvasGroup.alpha = alpha;
        }
        // O afectar directamente a los elementos gráficos
        else if (graphics != null && originalColors != null)
        {
            for (int i = 0; i < graphics.Length; i++)
            {
                if (graphics[i] != null && i < originalColors.Length)
                {
                    Color newColor = originalColors[i];
                    newColor.a = alpha;
                    graphics[i].color = newColor;
                }
            }
        }
    }

    /// <summary>
    /// Establece el color para los elementos
    /// </summary>
    private void SetColor(Color color)
    {
        if (graphics != null)
        {
            for (int i = 0; i < graphics.Length; i++)
            {
                if (graphics[i] != null)
                {
                    graphics[i].color = color;
                }
            }
        }
    }

    /// <summary>
    /// Reproduce las animaciones hijas en secuencia
    /// </summary>
    private IEnumerator PlaySequentialAnimations()
    {
        foreach (var anim in childAnimations)
        {
            if (anim != null)
            {
                anim.Play();

                // Esperar a que termine esta animación
                while (anim.IsPlaying)
                {
                    yield return null;
                }
            }
        }
    }
}