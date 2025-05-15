using UnityEngine;

/// <summary>
/// Animación de vibración para elementos UI
/// </summary>
public class UIShakeAnimation : UIAnimation
{
    [Header("Configuración de Vibración")]
    [Tooltip("Intensidad de la vibración")]
    public float intensity = 10f;

    [Tooltip("Velocidad de la vibración")]
    public float shakeSpeed = 10f;

    [Tooltip("Vibración gradual (aumenta y disminuye)")]
    public bool fadeShake = true;

    [Tooltip("Afecta a la posición")]
    public bool shakePosition = true;

    [Tooltip("Afecta a la rotación")]
    public bool shakeRotation = true;

    private RectTransform _rectTransform;
    private Vector3 _originalPosition;
    private Quaternion _originalRotation;
    private float _randomOffset;

    protected override void InitializeAnimation()
    {
        _rectTransform = GetComponent<RectTransform>();
        if (_rectTransform == null)
        {
            Debug.LogError("UIShakeAnimation requiere un RectTransform", this);
            enabled = false;
            return;
        }

        // Guardar valores originales
        _originalPosition = _rectTransform.localPosition;
        _originalRotation = _rectTransform.localRotation;

        // Offset aleatorio para variación entre elementos
        _randomOffset = Random.Range(0f, 100f);
    }

    protected override void UpdateAnimation(float progress)
    {
        // Calcular un factor de intensidad basado en el progreso
        float intensityFactor = fadeShake ?
            Mathf.Sin(progress * Mathf.PI) : // Aumenta y disminuye
            1f; // Constante

        // Tiempo para el ruido Perlin
        float time = Time.time * shakeSpeed + _randomOffset;

        if (shakePosition)
        {
            // Calcular desplazamiento aleatorio
            Vector3 shakeOffset = new Vector3(
                Mathf.PerlinNoise(time, 0) * 2 - 1,
                Mathf.PerlinNoise(0, time) * 2 - 1,
                0
            ) * intensity * intensityFactor;

            // Aplicar a la posición
            _rectTransform.localPosition = _originalPosition + shakeOffset;
        }

        if (shakeRotation)
        {
            // Calcular rotación aleatoria (solo en Z para UI 2D)
            float rotationShake = (Mathf.PerlinNoise(time, time) * 2 - 1) * intensity * 0.5f * intensityFactor;

            // Aplicar a la rotación
            _rectTransform.localRotation = _originalRotation * Quaternion.Euler(0, 0, rotationShake);
        }
    }

    public override void Stop()
    {
        base.Stop();

        // Restablecer valores originales
        _rectTransform.localPosition = _originalPosition;
        _rectTransform.localRotation = _originalRotation;
    }
}