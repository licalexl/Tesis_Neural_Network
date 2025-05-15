using UnityEngine;

/// <summary>
/// Animación de rotación para elementos UI
/// </summary>
public class UIRotateAnimation : UIAnimation
{
    [Header("Configuración de Rotación")]
    [Tooltip("Rotación inicial en grados")]
    public Vector3 fromRotation = Vector3.zero;

    [Tooltip("Rotación final en grados")]
    public Vector3 toRotation = new Vector3(0, 0, 360);

    [Tooltip("Usar la ruta más corta para la rotación")]
    public bool useShortestPath = true;

    [Tooltip("Restablecer rotación al completar")]
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

        // Guardar rotación original
        _originalRotation = _rectTransform.localRotation;

        // Establecer rotación inicial
        _rectTransform.localRotation = Quaternion.Euler(fromRotation);
    }

    protected override void UpdateAnimation(float progress)
    {
        Quaternion fromQuat = Quaternion.Euler(fromRotation);
        Quaternion toQuat = Quaternion.Euler(toRotation);

        Quaternion currentRotation;
        if (useShortestPath)
        {
            // Usar Quaternion.Slerp para la ruta más corta
            currentRotation = Quaternion.Slerp(fromQuat, toQuat, progress);
        }
        else
        {
            // Usar Quaternion.Lerp para una interpolación lineal
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