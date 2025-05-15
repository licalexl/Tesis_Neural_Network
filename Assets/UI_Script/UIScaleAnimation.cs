using UnityEngine;

/// <summary>
/// Animación de escala para elementos UI
/// </summary>
public class UIScaleAnimation : UIAnimation
{
    [Header("Configuración de Escala")]
    [Tooltip("Escala inicial")]
    public Vector3 fromScale = Vector3.zero;

    [Tooltip("Escala final")]
    public Vector3 toScale = Vector3.one;

    [Tooltip("Restablecer escala al completar")]
    public bool resetOnComplete = false;

    [Tooltip("Usar una animación de rebote (bounce)")]
    public bool useBounce = false;

    [Tooltip("Intensidad del rebote (si está activado)")]
    [Range(0.1f, 0.5f)]
    public float bounceIntensity = 0.2f;

    private RectTransform _rectTransform;
    private Vector3 _originalScale;

    protected override void InitializeAnimation()
    {
        _rectTransform = GetComponent<RectTransform>();
        if (_rectTransform == null)
        {
            Debug.LogError("UIScaleAnimation requiere un RectTransform", this);
            enabled = false;
            return;
        }

        // Guardar escala original
        _originalScale = _rectTransform.localScale;

        // Si no se ha establecido una escala inicial, usar la actual
        if (fromScale == Vector3.zero)
        {
            fromScale = _originalScale;
        }

        // Si no se ha establecido una escala final, usar la original
        if (toScale == Vector3.zero)
        {
            toScale = _originalScale;
        }

        // Establecer escala inicial
        _rectTransform.localScale = fromScale;
    }

    protected override void UpdateAnimation(float progress)
    {
        Vector3 currentScale;

        if (useBounce && progress > 0.8f)
        {
            // Crear un pequeño efecto de rebote al final
            float bounceProgress = (progress - 0.8f) / 0.2f;
            float bounceFactor = Mathf.Sin(bounceProgress * Mathf.PI) * bounceIntensity;

            currentScale = Vector3.Lerp(fromScale, toScale, progress);
            currentScale += new Vector3(bounceFactor, bounceFactor, bounceFactor);
        }
        else
        {
            currentScale = Vector3.Lerp(fromScale, toScale, progress);
        }

        _rectTransform.localScale = currentScale;
    }

    public override void Stop()
    {
        base.Stop();

        if (resetOnComplete)
        {
            _rectTransform.localScale = _originalScale;
        }
    }
}