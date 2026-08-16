# GameTools UISystem

A lightweight, UI framework for Unity.

`GameTools.UISystem` separates UI into **Screens**, **ScreenContainers**, and **Layers**, while handling screen lifecycle, ordering, input blocking, transitions, and screen reuse.


## Features

- Open, close, pause, and resume screens with animation.
- Organize screens inside independent `ScreenContainer`.
- Place containers on global UI layers with `UIManager`.
- Automatically update sorting order.
- Block input to lower screens with `blockInput`.
- Pass strongly typed data with `Screen<TParam>`.


## Example 1 — Create a Screen

Create a screen by deriving from `Screen`:

```csharp
using GameTools.UISystem;
using UnityEngine;

public sealed class InventoryScreen : Screen
{
    [SerializeField] private Canvas canvas;
    [SerializeField] private CanvasGroup canvasGroup;

    protected override bool blockInput => true;

    protected override void OnOpen()
    {
        Debug.Log("Inventory opened");
    }

    protected override void OnClose()
    {
        Debug.Log("Inventory closed");
    }

    protected override void Animation(string animKey, float progress)
    {
        canvasGroup.alpha = progress;
    }

    protected override void SetInteractable(bool value)
    {
        canvasGroup.interactable = value;
    }

    protected override void SetOrder(int order)
    {
        canvas.sortingOrder = order;
    }
}
```

`Animation()` receives a normalized progress value from `0` to `1`. Opening moves toward `1`; closing and pausing move toward `0`.

When `blockInput` is `true`, screens below this screen will no longer remain interactable while it is active.

---

## Example 2 — Open and Close Screens

Create a container through `UIManager` and open a screen:

```csharp
using GameTools.UISystem;
using UnityEngine;

public sealed class UIExample : MonoBehaviour
{
    private ScreenContainer mainUI;
    private InventoryScreen inventory;

    private void Start()
    {
        mainUI = UIManager.AddContainer(
            layerOrder: 0,
            name: "MainUI"
        );

        inventory = mainUI.Open<InventoryScreen>(
            fadeTime: 0.2f,
            animKey: "fade"
        );
    }

    public void CloseInventory()
    {
        mainUI.Close(
            inventory,
            fadeTime: 0.2f,
            animKey: "fade"
        );
    }
}
```

Containers created by `UIManager.AddContainer()` use the default `ResourcesScreenLoader`.

For the example above, place the prefab at:

```text
Assets/Resources/UI/Screens/InventoryScreen.prefab
```

The loader resolves screens using:

```text
UI/Screens/{ScreenTypeName}
```

Closed screens are returned to the loader and can be reused by its object pool.

---

## Example 3 — Pass Data to a Screen

Use `Screen<TParam>` when a screen requires data when it opens.

```csharp
public readonly struct ConfirmDialogData
{
    public readonly string Message;

    public ConfirmDialogData(string message)
    {
        Message = message;
    }
}
```

```csharp
using GameTools.UISystem;
using UnityEngine;
using UnityEngine.UI;

public sealed class ConfirmDialog : Screen<ConfirmDialogData>
{
    [SerializeField] private Text messageLabel;
    [SerializeField] private Canvas canvas;
    [SerializeField] private CanvasGroup canvasGroup;

    protected override bool blockInput => true;

    protected override void OnOpen(ConfirmDialogData data)
    {
        messageLabel.text = data.Message;
    }

    protected override void Animation(string animKey, float progress)
    {
        canvasGroup.alpha = progress;
    }

    protected override void SetInteractable(bool value)
    {
        canvasGroup.interactable = value;
    }

    protected override void SetOrder(int order)
    {
        canvas.sortingOrder = order;
    }
}
```

Open it with strongly typed data:

```csharp
ConfirmDialog dialog = mainUI.Open<ConfirmDialog, ConfirmDialogData>(
    new ConfirmDialogData("Return to the main menu?"),
    fadeTime: 0.15f
);
```

This keeps screen input explicit and avoids temporary global state just for opening a UI view.

---

## Example 4 — Use Scene-Owned Screens

If your screens already exist in the scene, use `MonoScreenContainer` instead of loading them from `Resources`.

Example hierarchy:

```text
MainUI                    <- MonoScreenContainer
├── HUDScreen             <- Screen
├── PauseScreen           <- Screen
└── SettingsScreen        <- Screen
```

`MonoScreenContainer` registers direct child objects containing a `ScreenBase` component.

Then open them normally:

```csharp
using GameTools.UISystem;
using UnityEngine;

public sealed class PauseMenuController : MonoBehaviour
{
    [SerializeField] private MonoScreenContainer ui;

    public void OpenPauseMenu()
    {
        ui.screenContainer.Open<PauseScreen>(fadeTime: 0.15f);
    }
}
```
---

## Layers

Use different layer orders to separate UI domains:

```csharp
ScreenContainer gameplay = UIManager.AddContainer(0, "Gameplay");
ScreenContainer popup    = UIManager.AddContainer(100, "Popup");
ScreenContainer debug    = UIManager.AddContainer(1000, "Debug");
```

Higher layer orders are placed above lower ones.

You can reorder a layer at runtime:

```csharp
UIManager.ChangeOrder("Popup", 200);
```

Or retrieve a named container:

```csharp
ScreenContainer popup = UIManager.GetContainer("Popup");
```

`UIManager` recalculates screen sorting order and interactability across all containers.

---

## Navigation


```csharp
var screen = container.Open<MyScreen>();

container.Pause(screen);
container.Resume(screen);
container.ChangeOrder(screen);
container.Close(screen);

container.CloseAll();
container.CompleteAnimations();
```



```csharp
public sealed class MainMenuScreen : Screen
{
    public void OpenLoginScreen()
    {
        Close(fadeTime: 0.2f, animKey: "fade"); // close main menu
        OpenScreen<LoginScreen>(fadeTime: 0.2f);
    }
}
```

---

## Custom Transitions

`animKey` is forwarded to your `Animation()` implementation, so a screen can provide multiple transitions:

```csharp
protected override void Animation(string animKey, float progress)
{
    switch (animKey)
    {
        case "fade":
            canvasGroup.alpha = progress;
            break;

        case "slide":
            panel.anchoredPosition = Vector2.Lerp(
                new Vector2(600f, 0f),
                Vector2.zero,
                progress
            );
            break;
    }
}
```

```csharp
var settings = container.Open<SettingsScreen>(0.25f, "slide");
container.Close(settings, 0.15f, "fade");
```

Transitions use unscaled time, so UI animations continue to run when `Time.timeScale` is changed.

---

## Screen Lifecycle

Screens can override lifecycle hooks for local setup and cleanup:

```csharp
protected override void OnInit() { }
protected override void OnOpen() { }
protected override void OnPause() { }
protected override void OnResume() { }
protected override void OnClose() { }
protected override void OnDispose() { }
```

Typical state flow:

```text
Close -> Open -> Pause -> Resume -> Open -> Close
```

Navigation, ordering, loading, and transition scheduling remain outside the individual screen implementation.
