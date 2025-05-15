using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// Componente que a�ade efectos de animaci�n a los botones
/// </summary>
[RequireComponent(typeof(Button))]
public class UIButtonAnimation : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    [Header("Configuraci�n de Efectos")]
    [Tooltip("Animaci�n al pasar el cursor por encima")]
    public UIAnimation hoverAnimation;

    [Tooltip("Animaci�n al hacer clic")]
    public UIAnimation clickAnimation;

    [Tooltip("Escala normal del bot�n")]
    public Vector3 normalScale = Vector3.one;

    [Tooltip("Escala al pasar por encima")]
    public Vector3 hoverScale = new Vector3(1.05f, 1.05f, 1.05f);

    [Tooltip("Escala al hacer clic")]
    public Vector3 clickScale = new Vector3(0.95f, 0.95f, 0.95f);

    [Tooltip("Velocidad de transici�n entre estados")]
    public float transitionSpeed = 10f;

    [Tooltip("Usar animaciones personalizadas en lugar de escalas")]
    public bool useCustomAnimations = false;

    private Button _button;
    private RectTransform _rectTransform;
    private Vector3 _targetScale;
    private bool _isTransitioning = false;

    void Awake()
    {
        _button = GetComponent<Button>();
        _rectTransform = GetComponent<RectTransform>();
        _targetScale = normalScale;

        // Establecer escala inicial
        _rectTransform.localScale = normalScale;
    }

    void Update()
    {
        if (!useCustomAnimations && _isTransitioning)
        {
            // Transici�n suave hacia la escala objetivo
            _rectTransform.localScale = Vector3.Lerp(
                _rectTransform.localScale,
                _targetScale,
                Time.deltaTime * transitionSpeed);

            // Verificar si hemos llegado lo suficientemente cerca del objetivo
            if (Vector3.Distance(_rectTransform.localScale, _targetScale) < 0.01f)
            {
                _rectTransform.localScale = _targetScale;
                _isTransitioning = false;
            }
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!_button.interactable) return;

        if (useCustomAnimations)
        {
            if (hoverAnimation != null)
            {
                hoverAnimation.Play();
            }
        }
        else
        {
            _targetScale = hoverScale;
            _isTransitioning = true;
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (!_button.interactable) return;

        if (useCustomAnimations)
        {
            if (hoverAnimation != null)
            {
                hoverAnimation.Stop();
            }
        }
        else
        {
            _targetScale = normalScale;
            _isTransitioning = true;
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (!_button.interactable) return;

        if (useCustomAnimations)
        {
            if (clickAnimation != null)
            {
                clickAnimation.Play();
            }
        }
        else
        {
            _targetScale = clickScale;
            _isTransitioning = true;
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (!_button.interactable) return;

        if (useCustomAnimations)
        {
            if (clickAnimation != null)
            {
                clickAnimation.Stop();
            }

            // Si estamos sobre el bot�n, volver a activar la animaci�n hover
            if (eventData.pointerEnter == gameObject && hoverAnimation != null)
            {
                hoverAnimation.Play();
            }
        }
        else
        {
            // Si seguimos sobre el bot�n, ir a hoverScale, si no a normalScale
            _targetScale = (eventData.pointerEnter == gameObject) ? hoverScale : normalScale;
            _isTransitioning = true;
        }
    }
}