using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Pool;
using UnityEngine.UI;

[DisallowMultipleComponent]
[ExecuteAlways]
[RequireComponent(typeof(RectTransform))]
/// <summary>
/// Stripped version of Layout Group made to specifically use center alignment and center anchors
/// </summary>
public abstract class CasinoLayoutGroup : UIBehaviour, ILayoutElement, ILayoutGroup
{
    [System.NonSerialized] private RectTransform m_Rect;
    protected RectTransform rectTransform
    {
        get
        {
            if (m_Rect == null)
                m_Rect = GetComponent<RectTransform>();
            return m_Rect;
        }
    }

    protected DrivenRectTransformTracker m_Tracker;
    private Vector2 m_TotalMinSize = Vector2.zero;
    private Vector2 m_TotalPreferredSize = Vector2.zero;

    [System.NonSerialized] private List<RectTransform> m_RectChildren = new List<RectTransform>();
    protected List<RectTransform> rectChildren { get { return m_RectChildren; } }

    #region ILayoutElement Interface

    public virtual void CalculateLayoutInputHorizontal()
    {
        m_RectChildren.Clear();
        var toIgnoreList = ListPool<Component>.Get();
        for (int i = 0; i < rectTransform.childCount; i++)
        {
            var rect = rectTransform.GetChild(i) as RectTransform;
            if (rect == null || !rect.gameObject.activeInHierarchy)
                continue;

            rect.GetComponents(typeof(ILayoutIgnorer), toIgnoreList);

            if (toIgnoreList.Count == 0)
            {
                m_RectChildren.Add(rect);
                continue;
            }

            for (int j = 0; j < toIgnoreList.Count; j++)
            {
                var ignorer = (ILayoutIgnorer)toIgnoreList[j];
                if (!ignorer.ignoreLayout)
                {
                    m_RectChildren.Add(rect);
                    break;
                }
            }
        }
        ListPool<Component>.Release(toIgnoreList);
        m_Tracker.Clear();
    }
    
    public abstract void CalculateLayoutInputVertical();

    /// <summary>
    /// See LayoutElement.minWidth
    /// </summary>
    public virtual float minWidth { get { return GetTotalMinSize(0); } }

    /// <summary>
    /// See LayoutElement.preferredWidth
    /// </summary>
    public virtual float preferredWidth { get { return GetTotalPreferredSize(0); } }

    /// <summary>
    /// See LayoutElement.flexibleWidth
    /// </summary>
    public virtual float flexibleWidth { get { return 0; } }

    /// <summary>
    /// See LayoutElement.minHeight
    /// </summary>
    public virtual float minHeight { get { return GetTotalMinSize(1); } }

    /// <summary>
    /// See LayoutElement.preferredHeight
    /// </summary>
    public virtual float preferredHeight { get { return GetTotalPreferredSize(1); } }

    /// <summary>
    /// See LayoutElement.flexibleHeight
    /// </summary>
    public virtual float flexibleHeight { get { return 0; } }

    /// <summary>
    /// See LayoutElement.layoutPriority
    /// </summary>
    public virtual int layoutPriority { get { return 0; } }

    #endregion

    #region ILayoutController Interface

    public abstract void SetLayoutHorizontal();
    public abstract void SetLayoutVertical();

    #endregion

    #region Implementation

    protected CasinoLayoutGroup() { }

    protected override void OnEnable()
    {
        base.OnEnable();
        SetDirty();
    }

    protected override void OnDisable()
    {
        m_Tracker.Clear();
        LayoutRebuilder.MarkLayoutForRebuild(rectTransform);
        base.OnDisable();
    }

    /// <summary>
    /// Callback for when properties have been changed by animation.
    /// </summary>
    protected override void OnDidApplyAnimationProperties()
    {
        SetDirty();
    }

    /// <summary>
    /// The min size for the layout group on the given axis.
    /// </summary>
    /// <param name="axis">The axis index. 0 is horizontal and 1 is vertical.</param>
    /// <returns>The min size</returns>
    protected float GetTotalMinSize(int axis)
    {
        return m_TotalMinSize[axis];
    }

    /// <summary>
    /// The preferred size for the layout group on the given axis.
    /// </summary>
    /// <param name="axis">The axis index. 0 is horizontal and 1 is vertical.</param>
    /// <returns>The preferred size.</returns>
    protected float GetTotalPreferredSize(int axis)
    {
        return m_TotalPreferredSize[axis];
    }

    /// <summary>
    /// Returns the calculated position of the first child layout element along the given axis.
    /// </summary>
    /// <param name="axis">The axis index. 0 is horizontal and 1 is vertical.</param>
    /// <param name="requiredSpace">The total space required on the given axis for all the layout elements including spacing</param>
    /// <returns>The position of the first child along the given axis.</returns>
    protected float GetStartOffset(int axis)
    {
        return -GetTotalPreferredSize(axis) / 2;
    }

    /// <summary>
    /// Used to set the calculated layout properties for the given axis.
    /// </summary>
    /// <param name="totalMin">The min size for the layout group.</param>
    /// <param name="totalPreferred">The preferred size for the layout group.</param>
    /// <param name="axis">The axis to set sizes for. 0 is horizontal and 1 is vertical.</param>
    protected void SetLayoutInputForAxis(float totalMin, float totalPreferred, int axis)
    {
        m_TotalMinSize[axis] = totalMin;
        m_TotalPreferredSize[axis] = totalPreferred;
    }

    /// <summary>
    /// Set the position and size of a child layout element along the given axis.
    /// </summary>
    /// <param name="rect">The RectTransform of the child layout element.</param>
    /// <param name="axis">The axis to set the position and size along. 0 is horizontal and 1 is vertical.</param>
    /// <param name="pos">The position from the left side or top.</param>
    protected void SetChildAlongAxis(RectTransform rect, int axis, float pos)
    {
        if (rect == null)
            return;

        SetChildAlongAxisWithScale(rect, axis, pos, 1.0f);
    }

    /// <summary>
    /// Set the position and size of a child layout element along the given axis.
    /// </summary>
    /// <param name="rect">The RectTransform of the child layout element.</param>
    /// <param name="axis">The axis to set the position and size along. 0 is horizontal and 1 is vertical.</param>
    /// <param name="pos">The position from the left side or top.</param>
    protected void SetChildAlongAxisWithScale(RectTransform rect, int axis, float pos, float scaleFactor)
    {
        if (rect == null)
            return;

        m_Tracker.Add(this, rect,
            DrivenTransformProperties.Anchors |
            (axis == 0 ? DrivenTransformProperties.AnchoredPositionX : DrivenTransformProperties.AnchoredPositionY));

        // Inlined rect.SetInsetAndSizeFromParentEdge(...) and refactored code in order to multiply desired size by scaleFactor.
        // sizeDelta must stay the same but the size used in the calculation of the position must be scaled by the scaleFactor.

        rect.anchorMin = Vector2.one / 2;
        rect.anchorMax = Vector2.one / 2;

        Vector2 anchoredPosition = rect.anchoredPosition;
        anchoredPosition[axis] = (axis == 0) ? (pos + rect.sizeDelta[axis] * rect.pivot[axis] * scaleFactor) : (-pos - rect.sizeDelta[axis] * (1f - rect.pivot[axis]) * scaleFactor);
        rect.anchoredPosition = anchoredPosition;
    }

    /// <summary>
    /// Set the position and size of a child layout element along the given axis.
    /// </summary>
    /// <param name="rect">The RectTransform of the child layout element.</param>
    /// <param name="axis">The axis to set the position and size along. 0 is horizontal and 1 is vertical.</param>
    /// <param name="pos">The position from the left side or top.</param>
    /// <param name="size">The size.</param>
    protected void SetChildAlongAxis(RectTransform rect, int axis, float pos, float size)
    {
        if (rect == null)
            return;

        SetChildAlongAxisWithScale(rect, axis, pos, size, 1.0f);
    }

    /// <summary>
    /// Set the position and size of a child layout element along the given axis.
    /// </summary>
    /// <param name="rect">The RectTransform of the child layout element.</param>
    /// <param name="axis">The axis to set the position and size along. 0 is horizontal and 1 is vertical.</param>
    /// <param name="pos">The position from the left side or top.</param>
    /// <param name="size">The size.</param>
    protected void SetChildAlongAxisWithScale(RectTransform rect, int axis, float pos, float size, float scaleFactor)
    {
        if (rect == null)
            return;

        m_Tracker.Add(this, rect,
            DrivenTransformProperties.Anchors |
            (axis == 0 ?
                (DrivenTransformProperties.AnchoredPositionX | DrivenTransformProperties.SizeDeltaX) :
                (DrivenTransformProperties.AnchoredPositionY | DrivenTransformProperties.SizeDeltaY)
            )
        );

        // Inlined rect.SetInsetAndSizeFromParentEdge(...) and refactored code in order to multiply desired size by scaleFactor.
        // sizeDelta must stay the same but the size used in the calculation of the position must be scaled by the scaleFactor.

        rect.anchorMin = Vector2.one / 2;
        rect.anchorMax = Vector2.one / 2;

        Vector2 sizeDelta = rect.sizeDelta;
        sizeDelta[axis] = size;
        rect.sizeDelta = sizeDelta;

        Vector2 anchoredPosition = rect.anchoredPosition;
        anchoredPosition[axis] = (axis == 0) ? (pos + size * rect.pivot[axis] * scaleFactor) : (-pos - size * (1f - rect.pivot[axis]) * scaleFactor);
        rect.anchoredPosition = anchoredPosition;
    }

    private bool isRootLayoutGroup
    {
        get
        {
            Transform parent = transform.parent;
            if (parent == null)
                return true;
            return transform.parent.GetComponent(typeof(ILayoutGroup)) == null;
        }
    }

    protected override void OnRectTransformDimensionsChange()
    {
        base.OnRectTransformDimensionsChange();
        if (isRootLayoutGroup)
            SetDirty();
    }

    protected virtual void OnTransformChildrenChanged()
    {
        SetDirty();
    }

    /// <summary>
    /// Helper method used to set a given property if it has changed.
    /// </summary>
    /// <param name="currentValue">A reference to the member value.</param>
    /// <param name="newValue">The new value.</param>
    protected void SetProperty<T>(ref T currentValue, T newValue)
    {
        if ((currentValue == null && newValue == null) || (currentValue != null && currentValue.Equals(newValue)))
            return;
        currentValue = newValue;
        SetDirty();
    }

    /// <summary>
    /// Mark the LayoutGroup as dirty.
    /// </summary>
    protected void SetDirty()
    {
        if (!IsActive())
            return;

        if (!CanvasUpdateRegistry.IsRebuildingLayout())
            LayoutRebuilder.MarkLayoutForRebuild(rectTransform);
        else
            StartCoroutine(DelayedSetDirty(rectTransform));
    }

    IEnumerator DelayedSetDirty(RectTransform rectTransform)
    {
        yield return null;
        LayoutRebuilder.MarkLayoutForRebuild(rectTransform);
    }

    #endregion

#if UNITY_EDITOR
    protected override void OnValidate()
    {
        SetDirty();
    }

#endif
}