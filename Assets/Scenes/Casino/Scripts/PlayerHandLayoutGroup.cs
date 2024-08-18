using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.UI;

/// <summary>
/// Stripped version of Horizontal Layout Group made to specifically use center alignment and center anchors
/// </summary>
public class PlayerHandLayoutGroup : CasinoLayoutGroup
{
    /// <summary>
    /// Called by the layout system. Also see ILayoutElement
    /// </summary>
    public override void CalculateLayoutInputHorizontal()
    {
        rectChildren.Clear();
        var toIgnoreList = ListPool<Component>.Get();
        for (int i = 0; i < rectTransform.childCount; i++)
        {
            var rect = rectTransform.GetChild(i) as RectTransform;
            if (rect == null || !rect.gameObject.activeInHierarchy)
                continue;

            rect.GetComponents(typeof(ILayoutIgnorer), toIgnoreList);

            if (toIgnoreList.Count == 0)
            {
                rectChildren.Add(rect);
                continue;
            }

            for (int j = 0; j < toIgnoreList.Count; j++)
            {
                var ignorer = (ILayoutIgnorer)toIgnoreList[j];
                if (!ignorer.ignoreLayout)
                {
                    rectChildren.Add(rect);
                    break;
                }
            }
        }
        ListPool<Component>.Release(toIgnoreList);
        m_Tracker.Clear();

        CalcAlongAxis(0, false);
    }

    /// <summary>
    /// Called by the layout system. Also see ILayoutElement
    /// </summary>
    public override void CalculateLayoutInputVertical()
    {
        CalcAlongAxis(1, false);
    }

    #region ILayoutController Interface

    /// <summary>
    /// Called by the layout system. Also see ILayoutElement
    /// </summary>
    public override void SetLayoutHorizontal()
    {
        SetChildrenAlongAxis(0, false);
    }
    
    /// <summary>
    /// Called by the layout system. Also see ILayoutElement
    /// </summary>
    public override void SetLayoutVertical()
    {
        SetChildrenAlongAxis(1, false);
    }

    #endregion

    #region Implementation

    [SerializeField] protected float m_Spacing = 0;

    /// <summary>
    /// The spacing to use between layout elements in the layout group.
    /// </summary>
    public float spacing { get { return m_Spacing; } set { SetProperty(ref m_Spacing, value); } }

    /// <summary>
    /// Whether the order of children objects should be sorted in reverse.
    /// </summary>
    /// <remarks>
    /// If False the first child object will be positioned first.
    /// If True the last child object will be positioned first.
    /// </remarks>
    public bool reverseArrangement { get { return m_ReverseArrangement; } set { SetProperty(ref m_ReverseArrangement, value); } }

    [SerializeField] protected bool m_ReverseArrangement = false;

    /// <summary>
    /// Calculate the layout element properties for this layout element along the given axis.
    /// </summary>
    /// <param name="axis">The axis to calculate for. 0 is horizontal and 1 is vertical.</param>
    /// <param name="isVertical">Is this group a vertical group?</param>
    protected void CalcAlongAxis(int axis, bool isVertical)
    {
        float totalMin = 0;
        float totalPreferred = 0;

        bool alongOtherAxis = (isVertical ^ (axis == 1));
        var rectChildrenCount = rectChildren.Count;
        for (int i = 0; i < rectChildrenCount; i++)
        {
            RectTransform child = rectChildren[i];
            float min = child.sizeDelta[axis];
            float preferred = min;

            if (alongOtherAxis)
            {
                totalMin = Mathf.Max(min, totalMin);
                totalPreferred = Mathf.Max(preferred, totalPreferred);
            }
            else
            {
                totalMin += min + spacing;
                totalPreferred += preferred + spacing;
            }
        }

        if (!alongOtherAxis && rectChildren.Count > 0)
        {
            totalMin -= spacing;
            totalPreferred -= spacing;
        }
        totalPreferred = Mathf.Max(totalMin, totalPreferred);
        SetLayoutInputForAxis(totalMin, totalPreferred, axis);
    }

    /// <summary>
    /// Set the positions and sizes of the child layout elements for the given axis.
    /// </summary>
    /// <param name="axis">The axis to handle. 0 is horizontal and 1 is vertical.</param>
    /// <param name="isVertical">Is this group a vertical group?</param>
    protected void SetChildrenAlongAxis(int axis, bool isVertical)
    {
        float size = rectTransform.rect.size[axis];
        float alignmentOnAxis = 0.5f;

        bool alongOtherAxis = (isVertical ^ (axis == 1));
        int startIndex = m_ReverseArrangement ? rectChildren.Count - 1 : 0;
        int endIndex = m_ReverseArrangement ? 0 : rectChildren.Count;
        int increment = m_ReverseArrangement ? -1 : 1;
        if (alongOtherAxis)
        {
            for (int i = startIndex; m_ReverseArrangement ? i >= endIndex : i < endIndex; i += increment)
            {
                RectTransform child = rectChildren[i];
                float min = child.sizeDelta[axis];
                float preferred = min;

                float requiredSpace = Mathf.Clamp(size, min, preferred);
                float startOffset = -requiredSpace / 2;

                float offsetInCell = (requiredSpace - child.sizeDelta[axis]) * alignmentOnAxis;
                SetChildAlongAxis(child, axis, startOffset + offsetInCell);
            }
        }
        else
        {
            float pos = 0;
            float surplusSpace = size - GetTotalPreferredSize(axis);

            if (surplusSpace > 0)
                pos = GetStartOffset(axis);

            float minMaxLerp = 0;
            if (GetTotalMinSize(axis) != GetTotalPreferredSize(axis))
                minMaxLerp = Mathf.Clamp01((size - GetTotalMinSize(axis)) / (GetTotalPreferredSize(axis) - GetTotalMinSize(axis)));

            for (int i = startIndex; m_ReverseArrangement ? i >= endIndex : i < endIndex; i += increment)
            {
                RectTransform child = rectChildren[i];
                float min = child.sizeDelta[axis];
                float preferred = min;

                float childSize = Mathf.Lerp(min, preferred, minMaxLerp);

                float offsetInCell = (childSize - child.sizeDelta[axis]) * alignmentOnAxis;
                SetChildAlongAxis(child, axis, pos + offsetInCell);

                pos += childSize + spacing;
            }
        }
    }

    #endregion
}