using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(ScrollRect))]
public class AutoHideScrollbar : MonoBehaviour
{
    [Tooltip("The RectTransform of the Content inside your Scroll View.")]
    public RectTransform content;

    [Tooltip("The RectTransform of the Viewport (the masked area).")]
    public RectTransform viewport;

    private ScrollRect _scrollRect;
    private Scrollbar _vsb;

    void Awake()
    {
        _scrollRect = GetComponent<ScrollRect>();
        _vsb = _scrollRect.verticalScrollbar;
    }

    void LateUpdate()
    {
        // make sure layout has rebuilt before we measure
        LayoutRebuilder.ForceRebuildLayoutImmediate(content);

        bool needsScroll = content.rect.height > viewport.rect.height;
        
        // enable/disable vertical scrolling
        _scrollRect.vertical = needsScroll;
        
        // show/hide the scrollbar GameObject
        if (_vsb != null)
            _vsb.gameObject.SetActive(needsScroll);

        // Force horizontal off every frame:
        _scrollRect.horizontal = false;
        if (_scrollRect.horizontalScrollbar != null)
            _scrollRect.horizontalScrollbar.gameObject.SetActive(false);
    }
}
