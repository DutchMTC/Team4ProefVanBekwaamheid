using UnityEngine;

public class UIManager : MonoBehaviour
{
    [SerializeField]
    private RectTransform topHalfContainer;
    
    [SerializeField]
    private RectTransform bottomHalfContainer;
    
    [SerializeField]
    private float splitRatio = 0.5f; // Ratio between top and bottom (0.5 = 50/50 split)

    private void Start()
    {
        SetupLayout();
    }

    private void SetupLayout()
    {
        if (topHalfContainer != null)
        {
            // Position the top container
            topHalfContainer.anchorMin = new Vector2(0, splitRatio);
            topHalfContainer.anchorMax = Vector2.one;
            topHalfContainer.offsetMin = Vector2.zero;
            topHalfContainer.offsetMax = Vector2.zero;
        }

        if (bottomHalfContainer != null)
        {
            // Position the bottom container
            bottomHalfContainer.anchorMin = Vector2.zero;
            bottomHalfContainer.anchorMax = new Vector2(1, splitRatio);
            bottomHalfContainer.offsetMin = Vector2.zero;
            bottomHalfContainer.offsetMax = Vector2.zero;
        }
    }

    // Call this when adding new UI elements to the top half
    public void AddToTopHalf(RectTransform element)
    {
        if (topHalfContainer != null && element != null)
        {
            element.SetParent(topHalfContainer, false);
        }
    }

    // Call this when adding new UI elements to the bottom half
    public void AddToBottomHalf(RectTransform element)
    {
        if (bottomHalfContainer != null && element != null)
        {
            element.SetParent(bottomHalfContainer, false);
        }
    }
}