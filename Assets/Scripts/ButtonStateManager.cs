using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class UIButtonStateManager : MonoBehaviour
{
    [System.Serializable]
    public class StatefulButton
    {
        public Button targetButton;
        public Image targetImage; // Button visual
        public Sprite inactiveSprite;
        public Sprite activeSprite;

        [HideInInspector] public bool isActive = false;

        public Sprite GetCurrentSprite()
        {
            return isActive ? activeSprite : inactiveSprite;
        }

        // Attach click event
        public void Initialize(System.Action<StatefulButton> onClickCallback)
        {
            if (targetButton != null)
            {
                targetButton.onClick.RemoveAllListeners();
                targetButton.onClick.AddListener(() =>
                {
                    onClickCallback?.Invoke(this);
                });
            }
        }
    }

    [Header("UI Buttons (mutually exclusive active state)")]
    public List<StatefulButton> buttons = new List<StatefulButton>();

    private void Start()
    {
        foreach (var btn in buttons)
            btn.Initialize(OnButtonClicked);

        ApplyButtonStates();
    }

    // Called when a button is clicked — makes only one active
    private void OnButtonClicked(StatefulButton clickedButton)
    {
        foreach (var btn in buttons)
            btn.isActive = (btn == clickedButton);

        ApplyButtonStates();
    }

    // Apply current active/inactive states visually
    private void ApplyButtonStates()
    {
        foreach (var btn in buttons)
        {
            if (btn.targetImage == null) continue;
            btn.targetImage.sprite = btn.GetCurrentSprite();
        }
    }

    // Optional: manually set a button's active state
    public void SetButtonActiveState(Button button, bool active)
    {
        var btn = buttons.Find(b => b.targetButton == button);
        if (btn != null)
        {
            btn.isActive = active;
            ApplyButtonStates();
        }
    }
}



