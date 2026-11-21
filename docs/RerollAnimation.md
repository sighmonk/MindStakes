# Slot-machine reroll animation

This guide describes how to make the category reroll on the category pick screen feel like a slot machine. It is an editor task: build the nodes and the animation in Godot, then add the small C# hook described below. Do not edit `.tscn` files by hand.

## What the reroll does today

- Scene: `Source/Screens/CategoryPick/CategoryPickScreen.tscn`.
- Script: `Source/Screens/CategoryPick/CategoryPickScreen.cs`.
- The `RerollButton` (`Panel/PanelContainer/VBoxContainer/ButtonsContainer/HBoxContainer/MarginContainer/HBoxContainer2/RerollButton`) already has a `pressed` connection to `OnRerollButtonPressed()`.
- That method does the real work: it calls `CategoriesManager.Instance.SetRandomCategory()`, decrements `GameManager.Instance.RerollCount`, frees the current `CategoryButton` inside `CategoryButtonContainer` and instantiates a new one for the new category.

The animation is visual only. It must not change which category is drawn, in which round, or how many rerolls are left; it only decides **when** the new `CategoryButton` becomes visible.

## Node tree to add

Open `Source/Screens/CategoryPick/CategoryPickScreen.tscn` and add these nodes.

1. Select `Panel/PanelContainer/VBoxContainer/PanelContainer2/MarginContainer/HBoxContainer` and add:

| Node | Type | Properties |
|------|------|------------|
| `ReelArea` | `Control` | `anchors_preset = Full Rect`, `mouse_filter = Ignore`, `visible = false` |
| `ReelArea/ReelMask` | `Control` | `custom_minimum_size = Vector2(520, 120)`, `clip_contents = true`, centered in `ReelArea` (`anchors_preset = Center`, `grow_horizontal = Both`, `grow_vertical = Both`) |
| `ReelArea/ReelMask/ReelStrip` | `VBoxContainer` | `theme_override_constants/separation = 0` |
| `ReelArea/ReelMask/ReelFrame` | `Panel` | optional frame drawn over the mask; `mouse_filter = Ignore` |

2. Select `ReelArea/ReelMask/ReelStrip` and add six `Label` children named `Slot1` … `Slot6`:

- `custom_minimum_size = Vector2(520, 120)` (the same height as the mask, so exactly one slot is visible).
- `theme_override_font_sizes/font_size = 64`, `horizontal_alignment = Center`, `vertical_alignment = Center`, `autowrap_mode = Off`.
- Placeholder text in the editor: any six category names. Runtime fills them in.

3. Select `CategoryPickScreen` (root) and add `RerollAnimation` (`AnimationPlayer`), `playback_default_blend_time = 0`.

4. Select the root, open the Inspector for the script and assign the new exports:

- `RerollAnimation` → `NodePath("RerollAnimation")`
- `ReelArea` → `NodePath("Panel/PanelContainer/VBoxContainer/PanelContainer2/MarginContainer/HBoxContainer/ReelArea")`
- `ReelStrip` → `NodePath("Panel/PanelContainer/VBoxContainer/PanelContainer2/MarginContainer/HBoxContainer/ReelArea/ReelMask/ReelStrip")`

5. Keep `CategoryButtonContainer` and `RerollButton` as they are; the reel only hides the `CategoryButton` while it spins.

## Animation tracks

Create one animation named `spin` in `RerollAnimation` with length `1.4` s.

| Track | Keys |
|-------|------|
| `ReelStrip:position:y` | `0 s → 0` (linear), `0.5 s → -180` (Cubic, ease In), `1.4 s → -600` (Cubic, ease Out) |
| `ReelArea:visible` | `0 s → true`, `1.4 s → true` |
| `ReelStrip:material:shader_parameter/blur_strength` | only with the optional blur shader: `0 s → 0`, `0.4 s → 6`, `1.2 s → 2`, `1.4 s → 0` |

Key points:

- Slot height is `120` px and the strip starts with `Slot1` centered, so `-600` is exactly `Slot6`. The animation therefore always ends on the sixth slot, which the code fills with the freshly drawn category.
- `0.5 s → -180` starts the strip fast so it looks like it is being pulled, and the long Cubic Out keyframe from `0.5 s` to `1.4 s` makes it land with a deceleration.
- Do not loop `spin`. Play it once per reroll and let `animation_finished` restore the normal screen.

## Reel labels at runtime

`ReelStrip` is filled before the spin:

- `Slot1` … `Slot5` get random names from `CategoriesManager.Instance.Categories`.
- `Slot6` gets `CategoriesManager.Instance.RandomCategory.CategoryName`, the category already drawn by `SetRandomCategory()`.

Because the last slot holds the real result, the strip animation only needs its final keyframe, and the outcome matches the existing logic exactly.

## Signals and C# hooks

1. In the editor, select `RerollAnimation`, open **Node → Signals**, and connect `animation_finished(anim_name: StringName)` to `CategoryPickScreen` → `OnRerollAnimationFinished`.
2. `RerollButton.pressed` already goes to `OnRerollButtonPressed()`; keep that connection.

`CategoryPickScreen.cs` additions (sketch; the existing reroll code stays in one place):

```csharp
[Export] public AnimationPlayer RerollAnimation { get; set; }
[Export] public Control ReelArea { get; set; }
[Export] public VBoxContainer ReelStrip { get; set; }

private const string SpinAnimation = "spin";
private const string AnimationsSetting = "ui/animations_enabled";

public void OnRerollButtonPressed()
{
    if (GameManager.Instance.RerollCount <= 0)
        return;

    CategoriesManager.Instance.SetRandomCategory();
    GameManager.Instance.RerollCount--;

    if (CanPlayRerollAnimation())
        PlayRerollAnimation();
    else
        RefreshCategoryButton();

    if (RerollButton != null)
        RerollButton.Text = LocalizationManager.Format("REROLL_BUTTON", GameManager.Instance.RerollCount);
}

private bool CanPlayRerollAnimation()
{
    return RerollAnimation != null
        && ReelArea != null
        && ReelStrip != null
        && ProjectSettings.GetSetting(AnimationsSetting, true).AsBool();
}

private void PlayRerollAnimation()
{
    FillReelSlots();
    ReelArea.Visible = true;
    RerollAnimation.Play(SpinAnimation);
}

private void FillReelSlots()
{
    var slots = ReelStrip.GetChildren();
    var categories = CategoriesManager.Instance.Categories;
    var random = new Random();
    for (int i = 0; i < slots.Count - 1 && categories.Count > 0; i++)
    {
        if (slots[i] is Label slot)
            slot.Text = categories[random.Next(categories.Count)].CategoryName;
    }
    if (slots.Count > 0 && slots[^1] is Label lastSlot)
        lastSlot.Text = CategoriesManager.Instance.RandomCategory?.CategoryName ?? string.Empty;
}

private void OnRerollAnimationFinished(StringName animationName)
{
    if (animationName != SpinAnimation)
        return;

    ReelArea.Visible = false;
    RefreshCategoryButton();
}

private void RefreshCategoryButton()
{
    // The existing body of OnRerollButtonPressed: free the old button,
    // instantiate CategoryButtonScene, assign the category and call UpdateButton().
}
```

The current body of `OnRerollButtonPressed()` (free + instantiate + `UpdateButton()`) moves into `RefreshCategoryButton()` unchanged. Nothing else in the reroll logic changes: the category is still drawn by `SetRandomCategory()`, the reroll counter still drops by one and the question flow stays the same.

## Fallback when animations are disabled

- Skip the reel whenever `RerollAnimation`, `ReelArea` or `ReelStrip` is `null` (for example an older copy of the scene) or when `ui/animations_enabled` is `false`; then `RefreshCategoryButton()` runs immediately, exactly like the current build.
- Expose the switch to the host: add a `CheckBox` named `AnimationsCheckBox` to the title or settings screen, label it with a new `SETTINGS_ANIMATIONS` key in `Resources/Locale/pl.po` and `en.po`, and store the value in `user://settings.cfg` under `[ui] animations_enabled`. `LocalizationManager` already writes that file; reuse the same pattern.
- Do not gate the game logic on the animation: the reroll must work identically with and without the reel.

## Optional motion blur

The project keeps shaders in `Source/Shaders/`. Create `ReelBlur.gdshader` there (FileSystem dock → Create New → Shader) and assign it as a `ShaderMaterial` on `ReelStrip`:

```glsl
shader_type canvas_item;

uniform float blur_strength : hint_range(0.0, 8.0) = 0.0;

void fragment() {
    vec4 color = vec4(0.0);
    for (int i = 0; i < 5; i++) {
        float offset = (float(i) - 2.0) * blur_strength * TEXTURE_PIXEL_SIZE.y;
        color += texture(TEXTURE, UV + vec2(0.0, offset));
    }
    COLOR = color / 5.0;
}
```

The `blur_strength` track in the `spin` animation drives it. Without the shader, remove that track; the reel still reads as a slot machine.

## Sound (optional)

Add an `AudioStreamPlayer` named `ReelStopSound` to `CategoryPickScreen` and play it from `OnRerollAnimationFinished` after `ReelArea.Visible = false`. Keep the clip short (under 300 ms) so it does not overlap the next round.

## Checklist before committing

- Reroll with 1 and with 0 rerolls left behaves as before.
- The reel never shows a different category than the one the `CategoryButton` receives.
- The animation is skipped with `ui/animations_enabled = false` and when the reel nodes are missing.
- Both languages still work (`REROLL_BUTTON`, plus the optional `SETTINGS_ANIMATIONS`).
- The scene is saved from the editor and the project still builds with `dotnet build MindStakes.sln`.
