# GameTools UISystem

A lightweight UI framework for Unity in the `GameTools.UISystem` namespace. It manages screen lifecycle, transitions, ordering, interaction, and reuse. Call its APIs on the Unity main thread.


Create containers through `LayerManager.AddContainer<ScreenContainer>()`. Constructing a container with `new` alone does not initialize or register it.

## Create a screen

Implement `blockInput`, `OnAnimation`, `SetInteractable`, and `SetOrder`. Lifecycle hooks are optional.

```csharp
using GameTools.UISystem;
using UnityEngine;

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

Assign the serialized references in the Inspector. Each screen needs a Canvas setup that honors its sorting order; for a nested Canvas, configure `overrideSorting` as appropriate. Configure a `GraphicRaycaster` and an EventSystem for uGUI input.

`SetInteractable` can run before `OnInit`, so references used there must already be available. The framework does not add Canvas components or configure raycast blocking automatically.

## Open, close, and release ownership

For the default Resources loader, save the screen prefab at:

```text
Assets/Resources/UI/Screens/InventoryScreen.prefab
```

The loader uses `UI/Screens/{ScreenTypeName}` and requires the prefab's screen component to have exactly the requested runtime type. Types with the same simple name resolve to the same path, even across namespaces.

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

This component owns the container and removes it once. A closed screen is returned to its loader after its close transition finishes. Do not continue using a returned screen reference: a later open may reuse that instance.

`CloseAll()` closes screens but keeps the container available. `LayerManager.RemoveContainer()` disposes the container, completes its screen cleanup, calls its loader's `Dispose()`, and then removes its layer entry. Screens remain under layer management during cleanup. The default disposal path closes immediately; it does not wait for a visible fade-out.

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

Create a corresponding `ConfirmDialog.prefab` for the Resources loader. Each open receives fresh data, including when the instance comes from the pool.

## Scene-owned screens

Use `MonoScreenContainer` for screens already present in a scene:

```text
MainUI                       <- MonoScreenContainer
├── InventoryScreen           <- InventoryScreen component
└── ConfirmDialog             <- ConfirmDialog component
```

- Only direct children with a `ScreenBase` component are registered.
- Each concrete screen type can be registered once and borrowed once at a time. Close and finish returning an instance before opening that type again.
- Registration initializes and deactivates the screens; it does not open them.
- `initOnAwake` defaults to `true`. Otherwise, call `Initialize()` or access `screenContainer` to initialize lazily.
- Set `layerOrder` in the Inspector. Runtime reordering goes through `LayerManager.ChangeOrder()`.
- The component removes its container in `OnDestroy()`. Let it own this cleanup instead of manually removing the same container.
- Disabling the component or its GameObject does not call the container's `Inactive()` method.

For a field declared as `[SerializeField] private MonoScreenContainer ui;`, open a registered screen with:

```csharp
var inventory = ui.screenContainer.Open<InventoryScreen>(fadeTime: 0.15f);
```

The internal `RegistryScreenLoader` reuses the registered scene objects. It does not instantiate extra copies or fall back to Resources for missing types.

## Layers and screen ordering

```csharp
ScreenContainer gameplay = LayerManager.AddContainer<ScreenContainer>(0, "Gameplay");
ScreenContainer popup = LayerManager.AddContainer<ScreenContainer>(100, "Popup");

LayerManager.ChangeOrder("Popup", 200);
LayerManager.ChangeOrder(gameplay, 10);

ScreenContainer found = LayerManager.GetContainer<ScreenContainer>("Popup");
```

Higher layer orders place containers above lower ones. For equal orders, newly inserted containers are placed above existing ones. Non-null names must be unique; unnamed containers must be managed by reference. A missing name or mismatched requested type is logged and returns `null`.

Layer order determines container placement, not the exact Canvas sorting value. The manager assigns consecutive screen orders starting at zero across the ordered containers.

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

A relative screen must belong to the container and must not be closing. `count` includes paused screens and closing screens until they are returned.

## Pause and resume

With an open `inventory` in `mainUI`:

```csharp
mainUI.Pause(inventory, fadeTime: 0.15f);
mainUI.Resume(inventory, fadeTime: 0.15f);

mainUI.Inactive();
mainUI.Active();
```

Pausing keeps a screen in the container and keeps its loader reservation. It deactivates the GameObject after the pause transition; resuming reactivates the same instance and invokes `OnResume`, not `OnOpen`.

`Inactive()` immediately pauses open screens and completes outstanding transitions. `Active()` resumes screens that were paused by the container, while preserving explicit per-screen pauses. While inactive, `Pause()` and `Resume()` change that per-screen pause preference; they do not show the screen until the container is active. Opening a screen requires an active container.

Outside a reentrant operation, repeated `Active()` or `Inactive()` calls in the corresponding state do nothing. Individual screen operations require the appropriate state: pause an open screen, resume a paused screen, and close an open or paused screen.

## Transitions and interaction

`OnAnimation(string animKey, float progress)` receives progress from `0` to `1` when opening or resuming, and from `1` to `0` when pausing or closing. `animKey` is passed through unchanged, including `null`; define its meaning in the screen implementation.

- `fadeTime` is in seconds. The default `-1` completes immediately; it does not select a configured default animation duration.
- Durations at or below `1 / 120f` complete immediately. Use finite values.
- Animations use `Time.unscaledDeltaTime`, so they continue when `Time.timeScale` is zero.
- Starting another state transition first completes the previous animation and its completion callbacks. It does not smoothly reverse from the current progress.
- `CompleteAnimation()` completes one screen's transition; `CompleteAnimations()` completes all transitions in a container.
- `SpeedUpAnimation(fadeTime)` and `SpeedUpAnimations(fadeTime)` reduce the animation duration used for subsequent progress updates. The argument is not a guaranteed remaining duration; zero completes immediately.
- `CloseAll(fadeTime, animKey)` closes all screens and speeds up screens already closing. Closing a paused screen returns it immediately.

A screen becomes locally interactable when its open/resume animation completes and stops being locally interactable as soon as pause/close begins. Global interaction also depends on screens above it.

`blockInput` prevents lower screens from being interactable while the screen is open or transitioning, including its closing and pausing fades. Once paused or closed with no running animation, it no longer blocks lower screens. If `blockInput` changes while open, call `LayerManager.UpdateInteractable()` to refresh lower screens.

This mechanism calls `SetInteractable`; it does not change `CanvasGroup.blocksRaycasts`, `Graphic.raycastTarget`, or gameplay input. Configure those separately for the intended input behavior.

## Lifecycle and navigation rules

| Hook | When it runs |
| --- | --- |
| `OnInit()` | Once per instance, after entering `Close` and deactivating the GameObject. |
| `OnOpen()` / `OnOpen(TParam)` | Each open, after entering `Open` and activating the GameObject, before its transition. |
| `OnPause()` | After entering `Pause`, before the pause transition. |
| `OnResume()` | After returning to `Open` and activating the GameObject, before its transition. |
| `OnClose()` | After entering `Close`, before deactivation/return finishes. |
| `OnDispose()` | When the loader disposes the closed instance, before `Destroy(gameObject)`. |

The screen states are `Init`, `Close`, `Open`, `Pause`, and `Dispose`. Resume is an operation that changes `Pause` back to `Open`, not a separate state. State changes happen before animations finish; use `isFade` to inspect whether a transition is still running.

Use `OnOpen`/`OnClose` for per-use setup and cleanup, and `OnInit`/`OnDispose` for instance-lifetime resources. Pooled screens do not receive `OnDispose` on every close. Unity's `Awake`, `OnEnable`, and `OnDisable` are separate from these framework hooks.

Container mutations share a per-container `ReentrancyGuard`. Do not synchronously open, close, pause, resume, reorder, activate, deactivate, or remove that same container from callbacks executing inside one of its operations. Such calls throw `InvalidOperationException`. Schedule follow-up navigation after the current operation returns. Lifecycle-hook exceptions are logged by the screen; they do not automatically roll back the operation.

The screen convenience methods `OpenScreen`, `ChangeOrder`, `Pause`, `Resume`, and `Close` delegate to its parent container. They require a screen still attached to that container. `OpenScreen` requires an explicit `fadeTime`; the screen-level `Close` requires both `fadeTime` and `animKey`.

For a button-driven transition, keep the container reference in the controller. The following fragment uses the earlier `InventoryScreen` and `ConfirmDialog` types:

```csharp
// Run after the current container operation has returned, e.g. from a button handler.
mainUI.Open<ConfirmDialog, ConfirmDialogData>(
    new ConfirmDialogData("Continue?"), fadeTime: 0.2f);
mainUI.Close(inventory, fadeTime: 0.2f);
```

Do not call `Close()` followed by `OpenScreen()` on the closed screen: closing clears its parent-container reference before the fade finishes.

## Loaders and pooling

`ResourcesScreenLoader.Instance` is shared by default containers and persists across scenes. Instances are parented under the loader and pooled by concrete type. Multiple simultaneous instances of a type are allowed. Its `Dispose()` is intentionally a no-op, so removing one container does not dispose the shared loader.

The current Resources pool configuration retains one idle instance per type and gradually destroys surplus idle instances. `ResourcesScreenLoader.Instance.ClearPools()` clears idle instances, leaving borrowed screens in use. Container removal returns its screens but does not automatically clear the shared pools.

`DynamicObjectPool<T>` exposes `CountActive` (borrowed), `CountInactive` (idle), and `CountAll` (their sum). `Clear()` removes idle objects; `ClearToRetainedCount()` trims only surplus idle objects. These operations do not close or reclaim borrowed screens.

To inject a loader, pass `screenLoader:` to `LayerManager.AddContainer<ScreenContainer>()`. The synchronous `IScreenLoader` contract is:

```csharp
T GetScreen<T>() where T : ScreenBase;
void ReleaseScreen(ScreenBase screen);
void Dispose();
```

A loader must return an initialized, closed instance of the requested type that is not already borrowed. The container returns it after closing, then calls the loader's `Dispose()` when disposing the container. A shared custom loader must explicitly manage shared ownership. Screen initialization and disposal helpers are `internal`, so a loader implementing those lifecycle calls must have access to the UISystem assembly.

Use the container APIs for normal screen management. Do not directly return, destroy, or toggle the GameObject of a managed screen: doing so bypasses its lifecycle and ownership bookkeeping.
