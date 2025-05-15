using UnityEngine;

/// <summary>
/// Animaci�n de rotaci�n para elementos UI
/// </summary>
public class UIRotateAnimation : UIAnimation
{
    [Header("Configuraci�n de Rotaci�n")]
    [Tooltip("Rotaci�n inicial en grados")]
    public Vector3 fromRotation = Vector3.zero;

    [Tooltip("Rotaci�n final en grados")]
    public Vector3 toRotation = new Vector3(0, 0, 360);

    [Tooltip("Usar la ruta m�s corta para la rotaci�n")]
    public bool useShortestPath = true;

    [Tooltip("Restablecer rotaci�n al completar")]
    public bool resetOnComplete = false;

    private RectTransform _rectTransform;
    private Quaternion _originalRotation;

    protected override void InitializeAnimation()
    {
        _rectTransform = GetComponent<RectTransform>();
        if (_rectTransform == null)
        {
            Debug.LogError("UIRotateAnimation requiere un RectTransform", this);
            enabled = false;
            return;
        }

        // Guardar rotaci�n original
        _originalRotation = _rectTransform.localRotation;

        // Establecer rotaci�n inicial
        _rectTransform.localRotation = Quaternion.Euler(fromRotation);
    }

    protected override void UpdateAnimation(float progress)
    {
        Quaternion fromQuat = Quaternion.Euler(fromRotation);
        Quaternion toQuat = Quaternion.Euler(toRotation);

        Quaternion currentRotation;
        if (useShortestPath)
        {
            // Usar Quaternion.Slerp para la ruta m�s corta
            currentRotation = Quaternion.Slerp(fromQuat, toQuat, progress);
        }
        else
        {
            // Usar Quaternion.Lerp para una interpolaci�n lineal
            currentRotation = Quaternion.Lerp(fromQuat, toQuat, progress);
        }

        _rectTransform.localRotation = currentRotation;
    }

    public override void Stop()
    {
        base.Stop();

        if (resetOnComplete)
        {
            _rectTransform.localRotation = _originalRotation;
        }
    }
}