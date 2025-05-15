using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Animación de cambio de color para elementos UI
/// </summary>
public class UIColorAnimation : UIAnimation
{
    [Header("Configuración de Color")]
    [Tooltip("Color inicial")]
    public Color fromColor = Color.white;

    [Tooltip("Color final")]
    public Color toColor = Color.red;

    [Tooltip("Afectar a los elementos hijos")]
    public bool affectChildren = true;

    [Tooltip("Restablecer color al completar")]
    public bool resetOnComplete = false;

    private Graphic[] _graphics;
    private Color[] _originalColors;

    protected override void InitializeAnimation()
    {
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

        // Establecer color inicial
        SetColor(fromColor);
    }

    protected override void UpdateAnimation(float progress)
    {
        // Interpolar entre colores
        Color currentColor = Color.Lerp(fromColor, toColor, progress);
        SetColor(currentColor);
    }

    private void SetColor(Color color)
    {
        if (_graphics != null)
        {
            for (int i = 0; i < _graphics.Length; i++)
            {
                if (_graphics[i] != null)
                {
                    _graphics[i].color = color;
                }
            }
        }
    }

    public override void Stop()
    {
        base.Stop();

        if (resetOnComplete && _graphics != null && _originalColors != null)
        {
            for (int i = 0; i < _graphics.Length; i++)
            {
                if (_graphics[i] != null)
                {
                    _graphics[i].color = _originalColors[i];
                }
            }
        }
    }
}