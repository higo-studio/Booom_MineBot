using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using DG.Tweening;

public class TitlePageBtnFx : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private RectTransform mask;
    [SerializeField] private float expandWidth = 400f;
    [SerializeField] private float animDuration = 0.2f;

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (mask == null) return;
        mask.DOSizeDelta(new Vector2(expandWidth, mask.sizeDelta.y), animDuration).SetEase(Ease.OutQuad);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (mask == null) return;
        mask.DOSizeDelta(new Vector2(0f, mask.sizeDelta.y), animDuration).SetEase(Ease.OutQuad);
    }
}