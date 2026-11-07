using System.Globalization;
using CharmCore.Domain;
using CharmCore.Net;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CharmCore.Ui;

/// <summary>
/// Draws a charm's cooldown on its inventory slot: a dark veil over the picture that empties as the cooldown runs
/// out, with the seconds left in the middle. Attached to each <see cref="InventorySpot"/>; nothing shows when the
/// charm in the slot has no cooldown running.
/// </summary>
internal sealed class CharmSlotView : MonoBehaviour
{
	private static Sprite? whiteSprite;

	private InventorySpot spot = null!;
	private Image? veil;
	private TextMeshProUGUI? label;

	internal static CharmSlotView GetOrAdd(InventorySpot spot)
	{
		CharmSlotView? view = spot.GetComponent<CharmSlotView>();
		if (view == null)
		{
			view = spot.gameObject.AddComponent<CharmSlotView>();
		}

		return view;
	}

	private void Awake()
	{
		spot = GetComponent<InventorySpot>();
	}

	private void Update()
	{
		if (spot == null || !CharmTracker.TryGetCooldown(spot.inventorySpotIndex, out float remaining, out float total))
		{
			SetVisible(false);
			return;
		}

		if (!EnsureWidgets())
		{
			return;
		}

		veil!.fillAmount = WearMath.CooldownFraction(remaining, total);
		label!.text = remaining >= 10f
			? Mathf.CeilToInt(remaining).ToString(CultureInfo.InvariantCulture)
			: remaining.ToString("0.0", CultureInfo.InvariantCulture);
		SetVisible(true);
	}

	private void SetVisible(bool visible)
	{
		if (veil != null)
		{
			veil.enabled = visible;
		}

		if (label != null)
		{
			label.enabled = visible;
		}
	}

	private bool EnsureWidgets()
	{
		if (veil != null && label != null)
		{
			return true;
		}

		Image? icon = spot.inventoryIcon;
		TextMeshProUGUI? template = spot.noItem;
		if (icon == null || template == null)
		{
			return false;
		}

		RectTransform iconRect = icon.rectTransform;
		GameObject veilObject = new GameObject("CharmCooldown", typeof(RectTransform), typeof(Image));
		RectTransform veilRect = veilObject.GetComponent<RectTransform>();
		veilRect.SetParent(iconRect.parent, false);
		veilRect.anchorMin = iconRect.anchorMin;
		veilRect.anchorMax = iconRect.anchorMax;
		veilRect.pivot = iconRect.pivot;
		veilRect.anchoredPosition = iconRect.anchoredPosition;
		veilRect.sizeDelta = iconRect.sizeDelta;
		veilRect.localScale = iconRect.localScale;

		veil = veilObject.GetComponent<Image>();
		veil.sprite = WhiteSprite();
		veil.type = Image.Type.Filled;
		veil.fillMethod = Image.FillMethod.Vertical;
		veil.fillOrigin = (int)Image.OriginVertical.Top;
		veil.color = new Color(0f, 0f, 0f, 0.65f);
		veil.raycastTarget = false;

		label = Instantiate(template, iconRect.parent);
		label.name = "CharmCooldownSeconds";
		label.alignment = TextAlignmentOptions.Center;
		label.enableAutoSizing = false;
		label.textWrappingMode = TextWrappingModes.NoWrap;
		label.overflowMode = TextOverflowModes.Overflow;
		label.fontSize = template.fontSize * 0.8f;
		label.color = Color.white;
		label.raycastTarget = false;
		label.outlineWidth = 0.3f;
		label.outlineColor = new Color32(0, 0, 0, 255);
		label.rectTransform.anchoredPosition = iconRect.anchoredPosition;
		return true;
	}

	private static Sprite WhiteSprite()
	{
		if (whiteSprite == null)
		{
			Texture2D texture = new Texture2D(1, 1);
			texture.SetPixel(0, 0, Color.white);
			texture.Apply();
			whiteSprite = Sprite.Create(texture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f));
		}

		return whiteSprite;
	}
}
