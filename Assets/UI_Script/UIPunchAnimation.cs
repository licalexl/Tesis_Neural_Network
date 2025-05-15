using UnityEngine;

/// <summary>
/// Animación de "punch" (golpe rápido) para elementos UI
/// </summary>
public class UIPunchAnimation : UIAnimation
{
    [Header("Configuración de Punch")]
    [Tooltip("Dirección y magnitud del golpe")]
    public Vector3 punchDirection = new Vector3(0, 20, 0);

    [Tooltip("Número de oscilaciones")]
    [Range(1, 10)]
    public int oscillations = 2;

    [Tooltip("Elasticidad")]
    [Range(0.1f, 1f)]
    public float elasticity = 0.3f;

    [Tooltip("Afectar posición")]
    public bool affectPosition = true;

    [Tooltip("Afectar escala")]
    public bool affectScale = false;

    private RectTransform _rectTransform;
    private Vector3 _originalPosition;
    private Vector3 _originalScale;

    protected override void InitializeAnimation()
    {
        _rectTransform = GetComponent<RectTransform>();
        if (_rectTransform == null)
        {
            Debug.LogError("UIPunchAnimation requiere un RectTransform", this);
            enabled = false;
            return;
        }

        // Guardar valores originales
        _originalPosition = _rectTransform.localPosition;
        _originalScale = _rectTransform.localScale;
    }

    protected override void UpdateAnimation(float progress)
    {
        // Función de punch basada en seno amortiguado
        float decay = Mathf.Clamp01(progress * oscillations);
        float amplitude = Mathf.Sin(decay * Mathf.PI * 2) * Mathf.Pow(1 - progress, elasticity);

        if (affectPosition)
        {
            Vector3 punchOffset = punchDirection * amplitude;
            _rectTransform.localPosition = _originalPosition + punchOffset;
        }

        if (affectScale)
        {
            Vector3 scaleOffset = new Vector3(
                punchDirection.x * 0.01f,
                punchDirection.y * 0.01f,
                punchDirection.z * 0.01f
            ) * amplitude;

            _rectTransform.localScale = _originalScale + scaleOffset;
        }
    }

    public override void Stop()
    {
        base.Stop();

        // Restablecer valores originales
        _rectTransform.localPosition = _originalPosition;
        _rectTransform.localScale = _originalScale;
    }
}