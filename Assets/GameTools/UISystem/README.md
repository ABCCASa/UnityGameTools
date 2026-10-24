# GameTools UISystem

A lightweight uGUI framework for Unity. Use screens for individual views, containers to manage them, and layers to control their display order. Call the framework's APIs on the Unity main thread.

`UGUIScreen` and `UGUIScreen<TParam>` inherit from `UGUIScreenBase`, which handles Canvas sorting, CanvasGroup interaction, and method binding. `ScreenBase` provides the shared lifecycle and transition logic.

## Create a screen

Derive from `UGUIScreen` and implement `blockInput` and `OnAnimation`. Lifecycle hooks are optional. Canvas sorting and interaction are already implemented by the base class.

```csharp
using GameTools.UISystem;
using UnityEngine;

public sealed class InventoryScreen : UGUIScreen
{
    protected override bool blockInput => true;

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
        alpha = progress;
    }
}
```

Assign the inherited Canvas and CanvasGroup fields in the Inspector. The protected `alpha` property reads and writes that CanvasGroup's alpha. 

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

The default `ResourcesScreenLoader` loads `UI/Screens/{ScreenTypeName}` and requires the prefab's screen component to match the requested concrete type. Closed instances are returned to a shared pool after their transitions finish and may be reused on a later open.

`mainUI.CloseAll()` closes all screens while keeping the container available. Remove containers you create with `LayerManager.RemoveContainer()` when they are no longer needed. Removal closes screens immediately and keeps the container registered with the layer manager until disposal finishes. Stop using a screen reference after closing it.

## Pass data to a screen

Use `UGUIScreen<TParam>` and implement its required `OnOpen(TParam)` method:

```csharp
using GameTools.UISystem;
using UnityEngine;
using TMPro;

public readonly struct ConfirmDialogData
{
    public readonly string Message;

    public ConfirmDialogData(string message)
    {
        Message = message;
    }
}

public sealed class ConfirmDialog : UGUIScreen<ConfirmDialogData>
{
    [SerializeField] private TMP_Text messageLabel;

    protected override bool blockInput => true;

    protected override void OnOpen(ConfirmDialogData data)
    {
        messageLabel.text = data.Message;
    }

    protected override void OnAnimation(string animKey, float progress)
    {
        alpha = progress;
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

## Bind UI methods

Add `[AutoBind("path/to/control")]` to an instance method to connect it to a UI event. Paths are relative to the screen's Transform. The target object must contain the corresponding component.

For example, add this method inside `InventoryScreen` and place a `UnityEngine.UI.Button` at `Panel/CloseButton`:

```csharp
[AutoBind("Panel/CloseButton")]
private void OnCloseClicked()
{
    Close(fadeTime: 0.2f, animKey: "fade");
}
```

Supported method signatures and events:

| Method signature | Component at the path | Event |
| --- | --- | --- |
| `void Handler()` | `UnityEngine.UI.Button` | `onClick` |
| `void Handler(float value)` | `UnityEngine.UI.Slider` | `onValueChanged` |
| `void Handler(bool value)` | `UnityEngine.UI.Toggle` | `onValueChanged` |
| `void Handler(string value)` | `TMPro.TMP_InputField` | `onValueChanged` |
| `void Handler(int value)` | `TMPro.TMP_Dropdown` | `onValueChanged` |

Methods must return `void` and take either no parameters or one supported parameter, without `ref`, `in`, or `out`. Automatic binding is enabled by default. Override `protected override bool useAutoBind => false;` if you want to disable it.

Listeners are attached during screen initialization, before `OnInit()`, and removed during disposal, before `OnDispose()`. Closing or pausing a screen does not remove these listeners; they remain attached when a pooled screen is reused.

## Scene-owned screens

Use `MonoScreenContainer` for screens already present in a scene:

```text
MainUI                       <- MonoScreenContainer
├── InventoryScreen           <- InventoryScreen component
└── ConfirmDialog             <- ConfirmDialog component
```

Place screens as direct children, with one instance per concrete screen type. Set `layerOrder` in the Inspector. `MonoScreenContainer` initializes in `Awake()` when `initOnAwake` is enabled (the default); otherwise, accessing `screenContainer` initializes it lazily. Initialization deactivates the registered screens; open them explicitly.

The component removes its container when destroyed. Let it own this cleanup instead of manually removing the same container. Disabling the GameObject does not call the container's `Inactive()` method.

For a field declared as `[SerializeField] private MonoScreenContainer ui;`, open a registered screen with:

```csharp
var inventory = ui.screenContainer.Open<InventoryScreen>(fadeTime: 0.15f);
```

The internal `RegistryScreenLoader` reuses the registered objects and allows only one open instance per type. Wait for a screen's close transition to finish before opening that scene screen again. Missing types are not loaded from Resources.

## Layers and screen ordering

```csharp
ScreenContainer gameplay = LayerManager.AddContainer<ScreenContainer>(0, "Gameplay");
ScreenContainer popup = LayerManager.AddContainer<ScreenContainer>(100, "Popup");

LayerManager.ChangeOrder("Popup", 200);
LayerManager.ChangeOrder(gameplay, 10);

ScreenContainer found = LayerManager.GetContainer<ScreenContainer>("Popup");
```

Higher layer orders place containers above lower ones. Non-null container names must be unique; unnamed containers are managed by reference. A missing name or mismatched type in `GetContainer<TContainer>()` is logged and returns `null`.

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

`Pause()` hides a screen after its transition without closing or returning it; `Resume()` shows the same instance again and calls `OnResume()`, not `OnOpen()`.

`Inactive()` pauses the container's open screens. `Active()` restores them, leaving explicitly paused screens paused. Activate the container before opening new screens.

While the container is inactive, `Pause()` and `Resume()` update whether an individual screen should remain paused when the container becomes active again.

## Transitions and interaction

`OnAnimation()` receives progress from `0` to `1` when opening or resuming, and from `1` to `0` when closing or pausing. Use `animKey` to choose an animation in your screen implementation.

`fadeTime` is in seconds; its default value of `-1` completes the transition immediately. Animations continue when `Time.timeScale` is zero. Call `mainUI.CompleteAnimations()` to finish all current transitions, or `screen.CompleteAnimation()` for one screen. Starting a new state transition completes the previous animation first; it does not reverse smoothly from its current progress.

Set `blockInput` to `true` for dialogs that should prevent interaction with lower screens while open or transitioning. The screen itself becomes interactable after opening or resuming finishes and stops being interactable when closing or pausing starts. The base class sets `CanvasGroup.interactable`; configure raycast blocking separately in your UI components.

## Lifecycle and navigation

| Hook | Purpose |
| --- | --- |
| `OnInit()` | Set up the instance once, after automatic binding. |
| `OnOpen()` / `OnOpen(TParam)` | Prepare the screen on every open, including pooled reuse. |
| `OnPause()` | Handle entering the paused state. |
| `OnResume()` | Handle returning from paused to open. |
| `OnClose()` | Clean up per-open state before the close transition finishes. |
| `OnDispose()` | Clean up instance resources before the GameObject is destroyed. |

States change before transitions finish. A screen can already be in `ScreenState.Close` while still fading out; `isFade` indicates whether an animation is running. Returning a screen to the Resources pool does not call `OnDispose()`.

Container operations reject synchronous reentry into the same container. Do not call its mutation methods from lifecycle callbacks executing inside an operation; schedule navigation after that operation returns. Button handlers can call screen convenience methods such as `Close()`, `Pause()`, and `Resume()`.

`OpenScreen<T>(fadeTime, ...)` and `OpenScreen<TScreen, TParam>(param, fadeTime, ...)` open another screen in the same container. Their `relative: true` option places the new screen relative to the calling screen. They require an explicit `fadeTime` and a screen still attached to its parent container.

For controller-driven navigation, keep the container reference:

```csharp
// Run from a button handler, outside a container lifecycle operation.
mainUI.Open<ConfirmDialog, ConfirmDialogData>(
    new ConfirmDialogData("Continue?"), fadeTime: 0.2f);
mainUI.Close(inventory, fadeTime: 0.2f);
```

Do not call `Close()` followed by `OpenScreen()` on the closed screen: closing clears its parent-container reference before the animation finishes.

