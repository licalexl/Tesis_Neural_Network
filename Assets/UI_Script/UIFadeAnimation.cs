using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Animación de fade (transparencia) para elementos UI
/// </summary>
public class UIFadeAnimation : UIAnimation
{
    [Header("Configuración de Fade")]
    [Tooltip("Valor alpha inicial (0-1)")]
    public float fromAlpha = 0f;

    [Tooltip("Valor alpha final (0-1)")]
    public float toAlpha = 1f;

    [Tooltip("Afectar también a los elementos hijos")]
    public bool affectChildren = true;

    private CanvasGroup _canvasGroup;
    private Graphic[] _graphics;
    private Color[] _originalColors;

    protected override void InitializeAnimation()
    {
        // Intentar obtener un CanvasGroup
        _canvasGroup = GetComponent<CanvasGroup>();
        if (_canvasGroup == null && affectChildren)
        {
            _canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        // Obtener todos los elementos gráficos
        if (affectChildren)
        {
            _graphics = GetComponentsInChildren<Graphic>(true);
        }
        else
        {
            _graphics = GetComponents<Graphic>();
        }

        // Guardar colores originales
        if (_graphics.Length > 0)
        {
            _originalColors = new Color[_graphics.Length];
            for (int i = 0; i < _graphics.Length; i++)
            {
                _originalColors[i] = _graphics[i].color;
            }
        }

        // Establecer valor inicial
        SetAlpha(fromAlpha);
    }

    protected override void UpdateAnimation(float progress)
    {
        // Interpolar entre valores inicial y final
        float currentAlpha = Mathf.Lerp(fromAlpha, toAlpha, progress);
        SetAlpha(currentAlpha);
    }

    private void SetAlpha(float alpha)
    {
        // Usar CanvasGroup si está disponible
        if (_canvasGroup != null)
        {
            _canvasGroup.alpha = alpha;
        }

        // O afectar directamente a los elementos gráficos
        else if (_graphics != null && _originalColors != null)
        {
            for (int i = 0; i < _graphics.Length; i++)
            {
                if (_graphics[i] != null)
                {
                    Color newColor = _originalColors[i];
                    newColor.a = alpha;
                    _graphics[i].color = newColor;
                }
            }
        }
    }
}