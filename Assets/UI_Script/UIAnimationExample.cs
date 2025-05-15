using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Ejemplo de c�mo usar el sistema de animaciones
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
        // Animaci�n de entrada para el panel
        UIFadeAnimation fadeIn = menuPanel.AddComponent<UIFadeAnimation>();
        fadeIn.animationID = "MenuFadeIn";
        fadeIn.duration = 0.3f;
        fadeIn.fromAlpha = 0f;
        fadeIn.toAlpha = 1f;

        // Animaci�n de movimiento para el panel
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

        // Reproducir animaci�n de grupo
        UIAnimationManager.Instance.PlayAnimation("MenuOpen");
    }

    private void OnCloseButtonClick()
    {
        // Reproducir animaci�n de salida y ocultar al finalizar
        UIAnimationManager.Instance.PlayAnimation("MenuFadeIn");

        // Ocultar despu�s de un retraso (duraci�n de la animaci�n)
        Invoke("HideMenu", 0.3f);
    }

    private void HideMenu()
    {
        menuPanel.SetActive(false);
    }
}