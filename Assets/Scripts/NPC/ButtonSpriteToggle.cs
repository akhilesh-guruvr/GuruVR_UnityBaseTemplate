using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

public class ImageActiveStateToggle : MonoBehaviour
{
    public Sprite activeSprite;
    public Sprite inActiveSprite;
    public Image buttonImage;

    public bool isActiveState;

    // Public events
    public UnityEvent OnActivated;
    public UnityEvent OnDeactivated;

    public void ToggleState()
    {
        if (buttonImage == null)
            return;

        isActiveState = !isActiveState;

        if (isActiveState)
        {
            buttonImage.sprite = activeSprite;
            OnActivated?.Invoke();
        }
        else
        {
            buttonImage.sprite = inActiveSprite;
            OnDeactivated?.Invoke();
        }
    }
}
