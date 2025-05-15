using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Agrupa múltiples animaciones para controlarlas juntas
/// </summary>
public class UIAnimationGroup : MonoBehaviour
{
    [Tooltip("ID del grupo de animación")]
    public string groupID;

    [Tooltip("Animaciones en este grupo")]
    public List<UIAnimation> animations = new List<UIAnimation>();

    [Tooltip("Reproducir secuencialmente (en orden) o simultáneamente")]
    public bool sequential = false;

    void Start()
    {
        // Registrar este grupo en el manager
        if (!string.IsNullOrEmpty(groupID))
        {
            UIAnimationManager.Instance.RegisterAnimation(groupID, GetComponent<UIAnimation>());
        }

        // Buscar animaciones si no se han asignado
        if (animations.Count == 0)
        {
            animations.AddRange(GetComponentsInChildren<UIAnimation>());
        }
    }

    public void PlayGroup()
    {
        if (sequential)
        {
            PlaySequential();
        }
        else
        {
            PlaySimultaneous();
        }
    }

    public void StopGroup()
    {
        foreach (var anim in animations)
        {
            if (anim != null)
            {
                anim.Stop();
            }
        }
    }

    private void PlaySimultaneous()
    {
        foreach (var anim in animations)
        {
            if (anim != null)
            {
                anim.Play();
            }
        }
    }

    private void PlaySequential()
    {
        StartCoroutine(PlaySequentialCoroutine());
    }

    private System.Collections.IEnumerator PlaySequentialCoroutine()
    {
        foreach (var anim in animations)
        {
            if (anim != null)
            {
                anim.Play();

                // Esperar a que termine esta animación antes de continuar
                while (anim.IsPlaying)
                {
                    yield return null;
                }
            }
        }
    }
}