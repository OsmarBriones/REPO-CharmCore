using System.Globalization;
using CharmCore.Domain;
using CharmCore.Net;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CharmCore.Ui;

/// <summary>
/// Draws a charm's cooldown on its inventory slot: a dark veil over the picture that empties as the cooldown runs
/// out, with the seconds left. Attached to each <see cref="InventorySpot"/>; nothing shows when the charm in the slot has
/// no cooldown running. While a cooldown shows, the seconds (upper part) and Pockets' value label (lower part) are fitted
/// to the visible part of the slot with auto-sizing, so both use as much of the tiny slot as possible.
/// </summary>
internal sealed class CharmSlotView : MonoBehaviour
{
	private static Sprite? whiteSprite;

	private InventorySpot spot = null!;
	private Image? veil;
	private TextMeshProUGUI? label;

	private readonly Vector3[] corners = new Vector3[4];

	// Pockets' value label while this view lays it out (its original look is saved and restored when the cooldown ends).
	private TextMeshProUGUI? valueLabel;
	private LabelState valueOriginal;
	private bool valueLaidOut;

	private struct LabelState
	{
		internal Vector2 AnchorMin;
		internal Vector2 AnchorMax;
		internal Vector2 Pivot;
		internal Vector2 SizeDelta;
		internal Vector2 AnchoredPosition;
		internal float FontSize;
		internal bool AutoSizing;
		internal float FontSizeMin;
		internal float FontSizeMax;
	}

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
			RestoreValueLabel();
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
		LayoutTexts();
	}

	/// <summary>The value label Pockets draws on this slot, when it exists.</summary>
	private TextMeshProUGUI? FindValueLabel()
	{
		Transform? parent = spot.noItem != null ? spot.noItem.transform.parent : null;
		Transform? found = parent != null ? parent.Find("PocketValue") : null;
		return found != null ? found.GetComponent<TextMeshProUGUI>() : null;
	}

	/// <summary>
	/// Fits the seconds and the stored valuable's value to the visible part of the slot (the screen cuts the slots off at the bottom):
	/// the seconds in the upper part, the value in the lower part, each auto-sized to fill its box.
	/// </summary>
	private void LayoutTexts()
	{
		RectTransform? slotRect = spot.GetComponent<RectTransform>();
		Canvas? canvas = spot.GetComponentInParent<Canvas>();
		if (slotRect == null || label == null)
		{
			return;
		}

		Camera? camera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
		slotRect.GetWorldCorners(corners);
		Vector2 bottomLeft = RectTransformUtility.WorldToScreenPoint(camera, corners[0]);
		Vector2 topRight = RectTransformUtility.WorldToScreenPoint(camera, corners[2]);
		// The visible area: the slot clipped to the screen (or to the HUD camera's picture when the canvas renders through a camera).
		float screenWidth = camera != null ? camera.pixelWidth : Screen.width;
		float screenHeight = camera != null ? camera.pixelHeight : Screen.height;
		float left = Mathf.Max(bottomLeft.x, 0f);
		float right = Mathf.Min(topRight.x, screenWidth);
		float top = Mathf.Min(topRight.y, screenHeight);
		float bottom = Mathf.Max(bottomLeft.y, 0f);
		SlotLayout.Split(left, right, top, bottom, out LabelBox secondsBox, out LabelBox valueBox);
		if (secondsBox.Width <= 0f)
		{
			return;
		}

		Place(label, secondsBox, camera);

		TextMeshProUGUI? value = FindValueLabel();
		if (value == null)
		{
			return;
		}

		if (!valueLaidOut)
		{
			valueLabel = value;
			valueOriginal = Capture(value);
			valueLaidOut = true;
		}

		Place(value, valueBox, camera);
	}

	/// <summary>Puts a text in a screen-space box and lets it auto-size to the largest font that fits.</summary>
	private static void Place(TextMeshProUGUI text, LabelBox box, Camera? camera)
	{
		RectTransform rect = text.rectTransform;
		rect.anchorMin = new Vector2(0.5f, 0.5f);
		rect.anchorMax = new Vector2(0.5f, 0.5f);
		rect.pivot = new Vector2(0.5f, 0.5f);
		Vector3 scale = rect.lossyScale;
		float scaleX = scale.x > 0.0001f ? scale.x : 1f;
		float scaleY = scale.y > 0.0001f ? scale.y : 1f;
		rect.sizeDelta = new Vector2(box.Width / scaleX, box.Height / scaleY);
		if (rect.parent is RectTransform parent
			&& RectTransformUtility.ScreenPointToWorldPointInRectangle(parent, new Vector2(box.CenterX, box.CenterY), camera, out Vector3 world))
		{
			rect.position = world;
		}

		text.alignment = TextAlignmentOptions.Center;
		text.textWrappingMode = TextWrappingModes.NoWrap;
		text.enableAutoSizing = true;
		text.fontSizeMin = 1f;
		text.fontSizeMax = Mathf.Max(box.Height / scaleY, 2f);
	}

	private static LabelState Capture(TextMeshProUGUI text)
	{
		RectTransform rect = text.rectTransform;
		return new LabelState
		{
			AnchorMin = rect.anchorMin,
			AnchorMax = rect.anchorMax,
			Pivot = rect.pivot,
			SizeDelta = rect.sizeDelta,
			AnchoredPosition = rect.anchoredPosition,
			FontSize = text.fontSize,
			AutoSizing = text.enableAutoSizing,
			FontSizeMin = text.fontSizeMin,
			FontSizeMax = text.fontSizeMax,
		};
	}

	/// <summary>Gives Pockets' value label back its own size, position and font when the cooldown ends.</summary>
	private void RestoreValueLabel()
	{
		if (!valueLaidOut)
		{
			return;
		}

		valueLaidOut = false;
		TextMeshProUGUI? value = valueLabel;
		valueLabel = null;
		if (value == null)
		{
			return;
		}

		RectTransform rect = value.rectTransform;
		rect.anchorMin = valueOriginal.AnchorMin;
		rect.anchorMax = valueOriginal.AnchorMax;
		rect.pivot = valueOriginal.Pivot;
		rect.sizeDelta = valueOriginal.SizeDelta;
		rect.anchoredPosition = valueOriginal.AnchoredPosition;
		value.enableAutoSizing = valueOriginal.AutoSizing;
		value.fontSizeMin = valueOriginal.FontSizeMin;
		value.fontSizeMax = valueOriginal.FontSizeMax;
		value.fontSize = valueOriginal.FontSize;
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

		// Draw the veil right above the picture and below everything created later, in particular Pockets' value label,
		// so the veil darkens the picture but never covers the value.
		veilRect.SetSiblingIndex(iconRect.GetSiblingIndex() + 1);

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
		label.enableAutoSizing = true;
		label.textWrappingMode = TextWrappingModes.NoWrap;
		label.overflowMode = TextOverflowModes.Overflow;
		label.color = Color.white;
		label.raycastTarget = false;
		label.outlineWidth = 0.3f;
		label.outlineColor = new Color32(0, 0, 0, 255);
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
