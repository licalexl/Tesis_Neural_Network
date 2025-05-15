using UnityEngine;

/// <summary>
/// Animaci�n de movimiento para elementos UI
/// </summary>
public class UIMoveAnimation : UIAnimation
{
    [Header("Configuraci�n de Movimiento")]
    [Tooltip("Posici�n inicial")]
    public Vector3 fromPosition;

    [Tooltip("Posici�n final")]
    public Vector3 toPosition;

    [Tooltip("Usar posici�n relativa al padre")]
    public bool useLocalPosition = true;

    [Tooltip("Restablecer posici�n al completar")]
    public bool resetOnComplete = false;

    private RectTransform _rectTransform;
    private Vector3 _originalPosition;

    protected override void InitializeAnimation()
    {
        _rectTransform = GetComponent<RectTransform>();
        if (_rectTransform == null)
        {
            Debug.LogError("UIMoveAnimation requiere un RectTransform", this);
            enabled = false;
            return;
        }

        // Guardar posici�n original
        _originalPosition = useLocalPosition ? _rectTransform.localPosition : _rectTransform.position;

        // Si no se ha establecido una posici�n inicial, usar la posici�n actual
        if (fromPosition == Vector3.zero)
        {
            fromPosition = _originalPosition;
        }

        // Si no se ha establecido una posici�n final, usar la posici�n original
        if (toPosition == Vector3.zero)
        {
            toPosition = _originalPosition;
        }

        // Establecer posici�n inicial
        if (useLocalPosition)
        {
            _rectTransform.localPosition = fromPosition;
        }
        else
        {
            _rectTransform.position = fromPosition;
        }
    }

    protected override void UpdateAnimation(float progress)
    {
        Vector3 currentPosition = Vector3.Lerp(fromPosition, toPosition, progress);

        if (useLocalPosition)
        {
            _rectTransform.localPosition = currentPosition;
        }
        else
        {
            _rectTransform.position = currentPosition;
        }
    }

    public override void Stop()
    {
        base.Stop();

        if (resetOnComplete)
        {
            if (useLocalPosition)
            {
                _rectTransform.localPosition = _originalPosition;
            }
            else
            {
                _rectTransform.position = _originalPosition;
            }
        }
    }
}