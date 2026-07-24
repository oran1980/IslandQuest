using System;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>A world-space clickable element for the Task 16 menus. Sits on a
/// GameObject that also has a <see cref="SpriteRenderer"/> + <see cref="BoxCollider2D"/>
/// so the scene's <c>Physics2DRaycaster</c> can hit it (the same input path the
/// board tiles use). Fires <see cref="Clicked"/> on a pointer click. Kept
/// deliberately generic — level buttons and results-panel buttons both use it.</summary>
public sealed class ClickableView : MonoBehaviour, IPointerClickHandler
{
    /// <summary>Invoked when the element is clicked. Assigned in code by whoever
    /// builds the button.</summary>
    public Action? Clicked;

    void IPointerClickHandler.OnPointerClick(PointerEventData eventData)
    {
        Clicked?.Invoke();
    }
}
