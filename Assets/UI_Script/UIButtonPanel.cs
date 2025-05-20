using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Ejemplo de uso del sistema simplificado de animaci�n UI con bot�n toggle
/// </summary>
public class UIButtonPanel : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private Button toggleButton;
    [SerializeField] private GameObject panel;
    [SerializeField] private TextMeshProUGUI buttonText; // Opcional, para cambiar el texto del bot�n

    [Header("Configuraci�n de Animaci�n")]
    [SerializeField] private float animationDuration = 0.5f;
    [SerializeField] private bool useSlideAnimation = true;
    [SerializeField] private bool useScaleAnimation = true;
    [SerializeField] private Vector3 slideOffset = new Vector3(300, 0, 0);

    [Header("Textos del Bot�n (Opcional)")]
    [SerializeField] private string openText = "Abrir Panel";
    [SerializeField] private string closeText = "Cerrar Panel";

    private SimpleUIAnimator panelAnimator;
    private SimpleUIAnimator exitAnimator; // Para la animaci�n de salida
    private SimpleUIAnimator buttonAnimator;
    private bool isPanelOpen = false;

    void Start()
    {
        // Asegurarse de que el panel est� inicialmente oculto
        if (panel != null)
        {
            panel.SetActive(false);
        }

        // Actualizar texto del bot�n si est� disponible
        UpdateButtonText();

        // Configurar animaciones
        SetupAnimations();

        // Configurar evento del bot�n
        if (toggleButton != null)
        {
            toggleButton.onClick.AddListener(TogglePanel);
        }
    }

    private void SetupAnimations()
    {
        // Configurar animaci�n de entrada del panel
        if (panel != null)
        {
            // A�adir animador al panel
            panelAnimator = panel.GetComponent<SimpleUIAnimator>();
            if (panelAnimator == null)
            {
                panelAnimator = panel.AddComponent<SimpleUIAnimator>();
            }

            // Configurar propiedades b�sicas
            panelAnimator.animationID = "PanelOpenAnimation";
            panelAnimator.duration = animationDuration;
            panelAnimator.animationCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

            // A�adir tipos de animaci�n
            panelAnimator.activeAnimations.Add(SimpleUIAnimator.AnimationType.Fade);
            panelAnimator.fromAlpha = 0f;
            panelAnimator.toAlpha = 1f;

            if (useSlideAnimation)
            {
                panelAnimator.activeAnimations.Add(SimpleUIAnimator.AnimationType.Move);

                // Guardar posici�n final
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

            // Registrar animaci�n en el manager
            SimpleUIAnimationManager.Instance.RegisterAnimation(panelAnimator.animationID, panelAnimator);

            // Crear la animaci�n de salida (clonar y revertir la de entrada)
            exitAnimator = panel.AddComponent<SimpleUIAnimator>();
            exitAnimator.animationID = "PanelCloseAnimation";
            exitAnimator.duration = animationDuration;
            exitAnimator.animationCurve = panelAnimator.animationCurve;

            // Configurar animaci�n de salida (invirtiendo la de entrada)
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

            // Conectar evento de finalizaci�n para ocultar el panel
            exitAnimator.onAnimationComplete.AddListener(() => {
                panel.SetActive(false);
            });

            // Registrar animaci�n en el manager
            SimpleUIAnimationManager.Instance.RegisterAnimation(exitAnimator.animationID, exitAnimator);
        }

        // Configurar animaci�n del bot�n
        if (toggleButton != null)
        {
            buttonAnimator = toggleButton.gameObject.AddComponent<SimpleUIAnimator>();
            buttonAnimator.animationID = "ButtonAnimation";
            buttonAnimator.duration = 0.2f;
            buttonAnimator.activeAnimations.Add(SimpleUIAnimator.AnimationType.Scale);
            buttonAnimator.fromScale = Vector3.one;
            buttonAnimator.toScale = new Vector3(0.9f, 0.9f, 1f);
            buttonAnimator.useBounce = true;

            // Registrar animaci�n en el manager
            SimpleUIAnimationManager.Instance.RegisterAnimation(buttonAnimator.animationID, buttonAnimator);
        }
    }

    /// <summary>
    /// Alterna el estado del panel (abre si est� cerrado, cierra si est� abierto)
    /// </summary>
    public void TogglePanel()
    {
        if (panel == null) return;

        // Animar bot�n
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

        // Actualizar texto del bot�n
        UpdateButtonText();
    }

    /// <summary>
    /// Actualiza el texto del bot�n seg�n el estado del panel
    /// </summary>
    private void UpdateButtonText()
    {
        if (buttonText != null)
        {
            buttonText.text = isPanelOpen ? closeText : openText;
        }
    }
}