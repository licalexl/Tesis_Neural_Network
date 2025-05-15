using UnityEngine;

/// <summary>
/// Define presets de animación reutilizables
/// </summary>
[CreateAssetMenu(fileName = "New Animation Preset", menuName = "UI/Animation Preset")]
public class UIAnimationPreset : ScriptableObject
{
    [Header("Configuración General")]
    public float duration = 0.5f;
    public float delay = 0f;
    public AnimationCurve animationCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Configuración de Movimiento")]
    public bool useMovement = false;
    public Vector3 fromPosition;
    public Vector3 toPosition;

    [Header("Configuración de Escala")]
    public bool useScale = false;
    public Vector3 fromScale = Vector3.zero;
    public Vector3 toScale = Vector3.one;

    [Header("Configuración de Rotación")]
    public bool useRotation = false;
    public Vector3 fromRotation;
    public Vector3 toRotation;

    [Header("Configuración de Transparencia")]
    public bool useFade = false;
    public float fromAlpha = 0f;
    public float toAlpha = 1f;

    /// <summary>
    /// Aplica este preset a un objeto UI
    /// </summary>
    public void ApplyToGameObject(GameObject target)
    {
        if (target == null) return;

        // Aplicar movimiento
        if (useMovement)
        {
            UIMoveAnimation moveAnim = target.GetComponent<UIMoveAnimation>();
            if (moveAnim == null) moveAnim = target.AddComponent<UIMoveAnimation>();

            moveAnim.duration = duration;
            moveAnim.delay = delay;
            moveAnim.animationCurve = new AnimationCurve(animationCurve.keys);
            moveAnim.fromPosition = fromPosition;
            moveAnim.toPosition = toPosition;
        }

        // Aplicar escala
        if (useScale)
        {
            UIScaleAnimation scaleAnim = target.GetComponent<UIScaleAnimation>();
            if (scaleAnim == null) scaleAnim = target.AddComponent<UIScaleAnimation>();

            scaleAnim.duration = duration;
            scaleAnim.delay = delay;
            scaleAnim.animationCurve = new AnimationCurve(animationCurve.keys);
            scaleAnim.fromScale = fromScale;
            scaleAnim.toScale = toScale;
        }

        // Aplicar rotación
        if (useRotation)
        {
            UIRotateAnimation rotateAnim = target.GetComponent<UIRotateAnimation>();
            if (rotateAnim == null) rotateAnim = target.AddComponent<UIRotateAnimation>();

            rotateAnim.duration = duration;
            rotateAnim.delay = delay;
            rotateAnim.animationCurve = new AnimationCurve(animationCurve.keys);
            rotateAnim.fromRotation = fromRotation;
            rotateAnim.toRotation = toRotation;
        }

        // Aplicar transparencia
        if (useFade)
        {
            UIFadeAnimation fadeAnim = target.GetComponent<UIFadeAnimation>();
            if (fadeAnim == null) fadeAnim = target.AddComponent<UIFadeAnimation>();

            fadeAnim.duration = duration;
            fadeAnim.delay = delay;
            fadeAnim.animationCurve = new AnimationCurve(animationCurve.keys);
            fadeAnim.fromAlpha = fromAlpha;
            fadeAnim.toAlpha = toAlpha;
        }
    }
}