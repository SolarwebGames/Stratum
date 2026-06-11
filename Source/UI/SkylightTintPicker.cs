using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using RimWorld;

namespace SolarWeb.Stratum.UI;

public class SkylightTintPicker : Dialog_ColorPickerBase
{
  private readonly Action<Color> callback;
  private readonly Color defaultColor;

  private static readonly List<Color> Palette =
  [
    new Color(0.7f, 0.85f, 1.0f), // Default Clear
    new Color(0.5f, 1.0f, 1.0f), // Cyan
    new Color(0.3f, 0.6f, 0.9f), // Sky Blue
    new Color(0.2f, 0.4f, 0.8f), // Deep Blue
    new Color(0.5f, 0.5f, 1.0f), // Blue
    new Color(0.0f, 0.5f, 0.5f), // Teal
    new Color(0.2f, 0.8f, 0.4f), // Emerald
    new Color(0.5f, 1.0f, 0.5f), // Green
    new Color(0.8f, 1.0f, 0.2f), // Lime
    new Color(1.0f, 1.0f, 0.5f), // Yellow
    new Color(1.0f, 0.8f, 0.2f), // Amber
    new Color(1.0f, 0.7f, 0.3f), // Orange
    new Color(1.0f, 0.5f, 0.2f), // Burnt Orange
    new Color(1.0f, 0.5f, 0.5f), // Red
    new Color(0.9f, 0.3f, 0.5f), // Rose
    new Color(1.0f, 0.4f, 0.7f), // Pink
    new Color(0.8f, 0.5f, 1.0f), // Purple
    new Color(0.6f, 0.2f, 0.8f), // Deep Purple
    new Color(1.0f, 1.0f, 1.0f), // White
    new Color(0.6f, 0.6f, 0.6f), // Grey
    new Color(0.2f, 0.2f, 0.2f)  // Dark
  ];

  protected override bool ShowDarklight => false;
  protected override Color DefaultColor => defaultColor;
  protected override List<Color> PickableColors => Palette;
  protected override float ForcedColorValue => 1f;
  protected override bool ShowColorTemperatureBar => false;

  public SkylightTintPicker(Color initialColor, Color defaultColor, Action<Color> callback)
    : base(Widgets.ColorComponents.All, Widgets.ColorComponents.All)
  {
    this.color = initialColor;
    this.oldColor = initialColor;
    this.defaultColor = defaultColor;
    this.callback = callback;
  }

  protected override void SaveColor(Color color)
  {
    callback?.Invoke(color);
  }
}
