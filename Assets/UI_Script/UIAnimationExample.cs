using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Ejemplo de cómo usar el sistema de animaciones
/// </summary>
public class UIAnimationExample : MonoBehaviour
{
    [SerializeField] private Button menuButton;
    [SerializeField] private GameObject menuPanel;
    [SerializeField] private Button closeButton;

    void Start()
    {
        // Preparar animaciones
        SetupAnimations();

        // Configurar botones
        menuButton.onClick.AddListener(OnMenuButtonClick);
        closeButton.onClick.AddListener(OnCloseButtonClick);
    }

    private void SetupAnimations()
    {
        // Animación de entrada para el panel
        UIFadeAnimation fadeIn = menuPanel.AddComponent<UIFadeAnimation>();
        fadeIn.animationID = "MenuFadeIn";
        fadeIn.duration = 0.3f;
        fadeIn.fromAlpha = 0f;
        fadeIn.toAlpha = 1f;

        // Animación de movimiento para el panel
        UIMoveAnimation moveIn = menuPanel.AddComponent<UIMoveAnimation>();
        moveIn.animationID = "MenuSlideIn";
        moveIn.duration = 0.5f;
        moveIn.fromPosition = new Vector3(500, 0, 0);
        moveIn.toPosition = Vector3.zero;
        moveIn.useLocalPosition = true;

        // Crear grupo de animaciones
        UIAnimationGroup groupIn = menuPanel.AddComponent<UIAnimationGroup>();
        groupIn.groupID = "MenuOpen";
        groupIn.animations.Add(fadeIn);
        groupIn.animations.Add(moveIn);

        // Ocultar panel inicialmente
        menuPanel.SetActive(false);
    }

    private void OnMenuButtonClick()
    {
        // Mostrar panel
        menuPanel.SetActive(true);

        // Reproducir animación de grupo
        UIAnimationManager.Instance.PlayAnimation("MenuOpen");
    }

    private void OnCloseButtonClick()
    {
        // Reproducir animación de salida y ocultar al finalizar
        UIAnimationManager.Instance.PlayAnimation("MenuFadeIn");

        // Ocultar después de un retraso (duración de la animación)
        Invoke("HideMenu", 0.3f);
    }

    private void HideMenu()
    {
        menuPanel.SetActive(false);
    }
}