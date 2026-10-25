# SaveSystem

A lightweight save system for Unity. 
## Save location

Paths are relative to:

```text
Application.persistentDataPath/PlayerData/
```

Use non-empty relative paths without leading or trailing separators, empty segments, or `.` / `..` segments. Both `/` and `\` are accepted as separators.

In the Windows Unity Editor, use **Tools > Open PlayerData Folder** to open the save directory.

## Save and load text

```csharp
using GameTools.SaveSystem;

SaveManager.SaveString("notes.txt", "Checkpoint reached");

string notes = SaveManager.LoadString( "notes.txt", defaultValue: null);
```

Saving replaces the existing file. Loading a missing file returns `defaultValue`, or `null` if no default is supplied.

## Save and load objects

Define a data type supported by Unity Serialization:

```csharp
using System;

[Serializable]
public class PlayerData
{
    public string playerName;
    public int level;
    public int coins;
}
```

Save the object and load it using the same type:

```csharp
using GameTools.SaveSystem;

var player = new PlayerData
{
    playerName = "Player",
    level = 3,
    coins = 120
};

SaveManager.Save("profiles/slot1.json", player);

PlayerData loaded = SaveManager.Load<PlayerData>("profiles/slot1.json", defaultValue: null);
```

`Save()` and `Load<T>()` handle JSON conversion. File operations are synchronous. The default value handles a missing file; it does not suppress file-access, deserialization, or decryption errors.

## Use a password

Pass the same password when saving and loading:

```csharp
string password = "example-password";

SaveManager.Save("profiles/slot1.sav", player, password: password);
PlayerData loaded = SaveManager.Load<PlayerData>("profiles/slot1.sav", password: password);

SaveManager.SaveString("notes.sav", "Checkpoint reached", password: password);
string notes = SaveManager.LoadString("notes.sav", password: password);
```

Omitting the password, or passing `null`, saves plain text. Password-protected files must be read with the same password; an incorrect password raises an error rather than returning the missing-file default.

## Check and delete files

```csharp
bool exists = SaveManager.HasFile("profiles/slot1.json");
bool deleted = SaveManager.DeleteFile("profiles/slot1.json");
```

`DeleteFile()` returns `false` if the file does not exist. To delete a directory and all its contents, or remove all saved files:

```csharp
SaveManager.DeleteDirectory("profiles");
SaveManager.DeleteAll();
```

`DeleteDirectory()` returns `false` if the directory does not exist. `DeleteAll()` removes the entire `PlayerData` directory.

## Store values in a container

Use `DataContainer.Get()` to access a named save file containing multiple keyed values:

```csharp
using GameTools.SaveSystem;

DataContainer settings = DataContainer.Get("settings.json");

settings.Set("musicVolume", 0.8f);
settings.Set("fullscreen", true);
settings.Set("language", "en");
settings.Save();

float volume = settings.Get<float>("musicVolume", defaultValue: 1f);
bool fullscreen = settings.Get<bool>("fullscreen", defaultValue: true);
string language = settings.Get<string>("language", defaultValue: "en");
```

`Set()` updates the container's cache. Call `Save()` to write changes to disk. Data is loaded on first access; call `LoadToCache()` if you want to load it explicitly beforehand.

Each normalized path identifies one shared container instance. Repeated calls to `DataContainer.Get("settings.json")` return that same instance. Use separate paths for separate profiles or slots.

For a shared default file, use:

```csharp
DataContainer data = DataContainer.defaultContainer;
data.Set("lastPlayedSlot", 1);
data.Save();
```

The default container uses the path `default`, without an extension or password.

## Read and update container data

```csharp
if (settings.TryGet<float>("musicVolume", out float volume))
{
    UnityEngine.Debug.Log(volume);
}

bool hasLanguage = settings.ContainsKey("language");
int entryCount = settings.Count;

foreach (string key in settings.Keys)
{
    UnityEngine.Debug.Log(key);
}
```

`Get<T>()` returns the supplied default when the key is missing. `TryGet<T>()` returns `false` for a missing key. Read each key using the type used to save it; keys are shared across all value types within a container.

Objects can also be stored by key. After changing a retrieved object, call `Set()` again before saving:

```csharp
DataContainer profile = DataContainer.Get("profiles/main.json");
PlayerData player = profile.Get<PlayerData>(
    "player", new PlayerData { playerName = "Player", level = 1 });

player.coins += 10;
profile.Set("player", player);
profile.Save();
```

To remove entries:

```csharp
settings.Remove("language");
settings.Save();

// Remove every entry and save the empty container.
settings.Clear();
settings.Save();
```

`Remove()` returns whether an entry was removed. `Clear()` empties the container; it does not delete its file.

## Save containers and release cached data

```csharp
settings.Save();                   // Save this container's pending changes.
DataContainer.SaveAll();           // Save all registered containers.
settings.Save(releaseCache: true); // Save changes, then release this data cache.
```

`Save()` writes only when the container has pending changes. `isDirty` reports pending changes, and `isLoaded` reports whether its data cache is loaded. After releasing the cache, the next data access loads it again; the shared container instance remains registered.

Once `DataContainer` has been initialized, it also calls `SaveAll()` when the application loses focus or quits. Explicitly save at important checkpoints. `SaveAll()` logs individual save failures and continues with the remaining containers.

## Password-protected containers

```csharp
DataContainer profile = DataContainer.Get("profiles/protected.sav", password: "example-password");

profile.Set("coins", 120);
profile.Save();

DataContainer existing = DataContainer.GetExist("profiles/protected.sav");
```

Supply the same password each time you call `Get()` for a registered path. A different password, including omitting a previously supplied password, throws an exception.

`GetExist()` returns an already registered container without requiring its password, or `null` if none is registered. It does not check for a file on disk or create a container.

Use a dedicated path for each container. Avoid modifying or deleting its file through `SaveManager` while using the cached container: file-level changes do not update its cache, and a later container save can overwrite or recreate the file.
