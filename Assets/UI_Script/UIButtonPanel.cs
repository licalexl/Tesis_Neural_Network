using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Ejemplo de uso del sistema simplificado de animación UI con botón toggle
/// </summary>
public class UIButtonPanel : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private Button toggleButton;
    [SerializeField] private GameObject panel;
    [SerializeField] private TextMeshProUGUI buttonText; // Opcional, para cambiar el texto del botón

    [Header("Configuración de Animación")]
    [SerializeField] private float animationDuration = 0.5f;
    [SerializeField] private bool useSlideAnimation = true;
    [SerializeField] private bool useScaleAnimation = true;
    [SerializeField] private Vector3 slideOffset = new Vector3(300, 0, 0);

    [Header("Textos del Botón (Opcional)")]
    [SerializeField] private string openText = "Abrir Panel";
    [SerializeField] private string closeText = "Cerrar Panel";

    private SimpleUIAnimator panelAnimator;
    private SimpleUIAnimator exitAnimator; // Para la animación de salida
    private SimpleUIAnimator buttonAnimator;
    private bool isPanelOpen = false;

    void Start()
    {
        // Asegurarse de que el panel esté inicialmente oculto
        if (panel != null)
        {
            panel.SetActive(false);
        }

        // Actualizar texto del botón si está disponible
        UpdateButtonText();

        // Configurar animaciones
        SetupAnimations();

        // Configurar evento del botón
        if (toggleButton != null)
        {
            toggleButton.onClick.AddListener(TogglePanel);
        }
    }

    private void SetupAnimations()
    {
        // Configurar animación de entrada del panel
        if (panel != null)
        {
            // Añadir animador al panel
            panelAnimator = panel.GetComponent<SimpleUIAnimator>();
            if (panelAnimator == null)
            {
                panelAnimator = panel.AddComponent<SimpleUIAnimator>();
            }

            // Configurar propiedades básicas
            panelAnimator.animationID = "PanelOpenAnimation";
            panelAnimator.duration = animationDuration;
            panelAnimator.animationCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

            // Añadir tipos de animación
            panelAnimator.activeAnimations.Add(SimpleUIAnimator.AnimationType.Fade);
            panelAnimator.fromAlpha = 0f;
            panelAnimator.toAlpha = 1f;

            if (useSlideAnimation)
            {
                panelAnimator.activeAnimations.Add(SimpleUIAnimator.AnimationType.Move);

                // Guardar posición final
                RectTransform rectTransform = panel.GetComponent<RectTransform>();
                Vector3 finalPosition = rectTransform.localPosition;

                // Configurar posiciones
                panelAnimator.fromPosition = finalPosition + slideOffset;
                panelAnimator.toPosition = finalPosition;
                panelAnimator.useLocalPosition = true;
            }

            if (useScaleAnimation)
            {
                panelAnimator.activeAnimations.Add(SimpleUIAnimator.AnimationType.Scale);
                panelAnimator.fromScale = new Vector3(0.8f, 0.8f, 1f);
                panelAnimator.toScale = Vector3.one;
                panelAnimator.useBounce = true;
                panelAnimator.bounceIntensity = 0.1f;
            }

            // Registrar animación en el manager
            SimpleUIAnimationManager.Instance.RegisterAnimation(panelAnimator.animationID, panelAnimator);

            // Crear la animación de salida (clonar y revertir la de entrada)
            exitAnimator = panel.AddComponent<SimpleUIAnimator>();
            exitAnimator.animationID = "PanelCloseAnimation";
            exitAnimator.duration = animationDuration;
            exitAnimator.animationCurve = panelAnimator.animationCurve;

            // Configurar animación de salida (invirtiendo la de entrada)
            exitAnimator.activeAnimations.Add(SimpleUIAnimator.AnimationType.Fade);
            exitAnimator.fromAlpha = 1f;
            exitAnimator.toAlpha = 0f;

            if (useSlideAnimation)
            {
                exitAnimator.activeAnimations.Add(SimpleUIAnimator.AnimationType.Move);
                exitAnimator.fromPosition = panelAnimator.toPosition;
                exitAnimator.toPosition = panelAnimator.fromPosition;
                exitAnimator.useLocalPosition = true;
            }

            if (useScaleAnimation)
            {
                exitAnimator.activeAnimations.Add(SimpleUIAnimator.AnimationType.Scale);
                exitAnimator.fromScale = Vector3.one;
                exitAnimator.toScale = new Vector3(0.8f, 0.8f, 1f);
            }

            // Conectar evento de finalización para ocultar el panel
            exitAnimator.onAnimationComplete.AddListener(() => {
                panel.SetActive(false);
            });

            // Registrar animación en el manager
            SimpleUIAnimationManager.Instance.RegisterAnimation(exitAnimator.animationID, exitAnimator);
        }

        // Configurar animación del botón
        if (toggleButton != null)
        {
            buttonAnimator = toggleButton.gameObject.AddComponent<SimpleUIAnimator>();
            buttonAnimator.animationID = "ButtonAnimation";
            buttonAnimator.duration = 0.2f;
            buttonAnimator.activeAnimations.Add(SimpleUIAnimator.AnimationType.Scale);
            buttonAnimator.fromScale = Vector3.one;
            buttonAnimator.toScale = new Vector3(0.9f, 0.9f, 1f);
            buttonAnimator.useBounce = true;

            // Registrar animación en el manager
            SimpleUIAnimationManager.Instance.RegisterAnimation(buttonAnimator.animationID, buttonAnimator);
        }
    }

    /// <summary>
    /// Alterna el estado del panel (abre si está cerrado, cierra si está abierto)
    /// </summary>
    public void TogglePanel()
    {
        if (panel == null) return;

        // Animar botón
        if (buttonAnimator != null)
        {
            buttonAnimator.Play();
        }

        // Alternar estado del panel
        if (isPanelOpen)
        {
            // Cerrar panel
            if (exitAnimator != null)
            {
                exitAnimator.Play();
            }
            isPanelOpen = false;
        }
        else
        {
            // Abrir panel
            panel.SetActive(true);
            if (panelAnimator != null)
            {
                panelAnimator.Play();
            }
            isPanelOpen = true;
        }

        // Actualizar texto del botón
        UpdateButtonText();
    }

    /// <summary>
    /// Actualiza el texto del botón según el estado del panel
    /// </summary>
    private void UpdateButtonText()
    {
        if (buttonText != null)
        {
            buttonText.text = isPanelOpen ? closeText : openText;
        }
    }
}