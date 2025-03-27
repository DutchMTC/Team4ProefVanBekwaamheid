using UnityEngine;

public class UIManager : MonoBehaviour
{
    [SerializeField]
    private RectTransform _topHalfContainer;
    
    [SerializeField]
    private RectTransform _bottomHalfContainer;
    
    [SerializeField]
    private float _splitRatio = 0.5f; // Ratio between top and bottom (0.5 = 50/50 split)

    private void Start()
    {
        SetupLayout();
    }

    private void SetupLayout()
    {
        if (_topHalfContainer != null)
        {
            // Position the top container
            _topHalfContainer.anchorMin = new Vector2(0, _splitRatio);
            _topHalfContainer.anchorMax = Vector2.one;
            _topHalfContainer.offsetMin = Vector2.zero;
            _topHalfContainer.offsetMax = Vector2.zero;
        }

        if (_bottomHalfContainer != null)
        {
            // Position the bottom container
            _bottomHalfContainer.anchorMin = Vector2.zero;
            _bottomHalfContainer.anchorMax = new Vector2(1, _splitRatio);
            _bottomHalfContainer.offsetMin = Vector2.zero;
            _bottomHalfContainer.offsetMax = Vector2.zero;
        }
    }

    // Call this when adding new UI elements to the top half
    public void AddToTopHalf(RectTransform element)
    {
        if (_topHalfContainer != null && element != null)
        {
            element.SetParent(_topHalfContainer, false);
        }
    }

    // Call this when adding new UI elements to the bottom half
    public void AddToBottomHalf(RectTransform element)
    {
        if (_bottomHalfContainer != null && element != null)
        {
            element.SetParent(_bottomHalfContainer, false);
        }
    }
}