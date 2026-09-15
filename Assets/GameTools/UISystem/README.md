# GameTools UISystem

A lightweight UI framework for Unity. Use screens for individual views, containers to manage them, and layers to control their display order.

## Create a screen

Implement `blockInput`, `OnAnimation`, `SetInteractable`, and `SetOrder`. Lifecycle hooks are optional.

```csharp
using GameTools.UISystem;
using UnityEngine;
using UIScreen = GameTools.UISystem.Screen;

public sealed class InventoryScreen : UIScreen
{
    [SerializeField] private Canvas screenCanvas;
    [SerializeField] private CanvasGroup canvasGroup;

    public override bool blockInput => true;

    protected override void OnOpen()
    {
        Debug.Log("Inventory opened");
    }

    protected override void OnClose()
    {
        Debug.Log("Inventory closed");
    }

    protected override void OnAnimation(string animKey, float progress)
    {
        canvasGroup.alpha = progress;
    }

    protected override void SetInteractable(bool value)
    {
        canvasGroup.interactable = value;
    }

    protected override void SetOrder(int order)
    {
        screenCanvas.sortingOrder = order;
    }
}
```
Assign the Canvas and CanvasGroup in the Inspector. 
## Open and close screens

Save the screen prefab under `Assets/Resources/UI/Screens/`, using its screen class name:

```text
Assets/Resources/UI/Screens/InventoryScreen.prefab
```

```csharp
using GameTools.UISystem;
using UnityEngine;

public sealed class UIExample : MonoBehaviour
{
    private ScreenContainer mainUI;
    private InventoryScreen inventory;

    private void Start()
    {
        mainUI = LayerManager.AddContainer<ScreenContainer>(0, "MainUI");
        inventory = mainUI.Open<InventoryScreen>(0.2f, "fade");
    }

    public void CloseInventory()
    {
        if (inventory == null) return;
        mainUI.Close(inventory, 0.2f, "fade");
        inventory = null;
    }

    private void OnDestroy()
    {
        if (mainUI == null) return;
        LayerManager.RemoveContainer(mainUI);
        mainUI = null;
    }
}
```

`mainUI.CloseAll()` closes all screens while keeping the container available. Remove containers you create with `LayerManager.RemoveContainer()` when they are no longer needed. Stop using a screen reference after closing it.

## Pass data to a screen

Use `Screen<TParam>` and override `OnOpen(TParam)`:

```csharp
using GameTools.UISystem;
using UnityEngine;
using UnityEngine.UI;

public readonly struct ConfirmDialogData
{
    public readonly string Message;

    public ConfirmDialogData(string message)
    {
        Message = message;
    }
}

public sealed class ConfirmDialog : Screen<ConfirmDialogData>
{
    [SerializeField] private Text messageLabel;
    [SerializeField] private Canvas screenCanvas;
    [SerializeField] private CanvasGroup canvasGroup;

    public override bool blockInput => true;

    protected override void OnOpen(ConfirmDialogData data)
    {
        messageLabel.text = data.Message;
    }

    protected override void OnAnimation(string animKey, float progress)
    {
        canvasGroup.alpha = progress;
    }

    protected override void SetInteractable(bool value)
    {
        canvasGroup.interactable = value;
    }

    protected override void SetOrder(int order)
    {
        screenCanvas.sortingOrder = order;
    }
}
```

With an existing `mainUI` container:

```csharp
ConfirmDialog dialog = mainUI.Open<ConfirmDialog, ConfirmDialogData>(
    new ConfirmDialogData("Return to the main menu?"),
    fadeTime: 0.15f
);
```

Save `ConfirmDialog.prefab` in the same `Resources/UI/Screens` folder.

## Scene-owned screens

Use `MonoScreenContainer` for screens already present in a scene:

```text
MainUI                       <- MonoScreenContainer
├── InventoryScreen           <- InventoryScreen component
└── ConfirmDialog             <- ConfirmDialog component
```

Place screens as direct children, with one instance per screen type. Set `layerOrder` in the Inspector. `MonoScreenContainer` initializes automatically and removes its container when destroyed; screens are opened explicitly.

For a field declared as `[SerializeField] private MonoScreenContainer ui;`, open a registered screen with:

```csharp
var inventory = ui.screenContainer.Open<InventoryScreen>(fadeTime: 0.15f);
```

Wait for a screen's close transition to finish before opening that scene screen again.

## Layers and screen ordering

```csharp
ScreenContainer gameplay = LayerManager.AddContainer<ScreenContainer>(0, "Gameplay");
ScreenContainer popup = LayerManager.AddContainer<ScreenContainer>(100, "Popup");

LayerManager.ChangeOrder("Popup", 200);
LayerManager.ChangeOrder(gameplay, 10);

ScreenContainer found = LayerManager.GetContainer<ScreenContainer>("Popup");
```

Higher layer orders place containers above lower ones. Container names must be unique.

Within a container, `addAbove: true` places a screen at the top by default; `false` places it at the bottom. Supply `relative` to place it immediately above or below another screen in the same container:

```csharp
var inventory = gameplay.Open<InventoryScreen>();
var dialog = gameplay.Open<ConfirmDialog, ConfirmDialogData>(
    new ConfirmDialogData("Continue?"),
    addAbove: true,
    relative: inventory
);
gameplay.ChangeOrder(inventory, addAbove: true, relative: dialog);
```

A relative screen must belong to the same container and must not be closing.

## Pause and resume

With an open `inventory` in `mainUI`:

```csharp
mainUI.Pause(inventory, fadeTime: 0.15f);
mainUI.Resume(inventory, fadeTime: 0.15f);

mainUI.Inactive();
mainUI.Active();
```

`Pause()` hides a screen without closing it; `Resume()` shows it again and calls `OnResume()`.

`Inactive()` pauses the container's open screens. `Active()` restores them, leaving explicitly paused screens paused. Activate the container before opening new screens.

## Transitions and interaction

`OnAnimation()` receives progress from `0` to `1` when opening or resuming, and from `1` to `0` when closing or pausing. Use `animKey` to choose an animation in your screen implementation.

`fadeTime` is in seconds; omitting it completes the transition immediately. Animations continue when `Time.timeScale` is zero. Call `mainUI.CompleteAnimations()` to finish all current transitions.

Set `blockInput` to `true` for dialogs that should prevent interaction with lower screens while open or transitioning. Configure raycast blocking separately in your UI components.

