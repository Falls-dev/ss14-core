using Robust.Shared.Audio;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Content.Shared.Administration;

namespace Content.Shared._White.MechComp;
public abstract class SharedMechCompDeviceSystem : EntitySystem
{
}


[RegisterComponent]
public sealed partial class MechCompButtonComponent : Component
{
    [DataField("clickSound")]
    public SoundSpecifier ClickSound = new SoundPathSpecifier("/Audio/Machines/lightswitch.ogg");
    public object pressedAnimation = default!;
}
[RegisterComponent]
public sealed partial class MechCompSpeakerComponent : Component
{
    public object speakAnimation = default!;
}

[RegisterComponent]
public sealed partial class MechCompTeleportComponent : Component
{
    public object firingAnimation = default!; // i genuinely cannot believe people over at wizden think this is okay
    public object glowAnimation = default!;

    [DataField("maxDistance", serverOnly: true)]
    public float MaxDistance = 25f;
}

[Serializable, NetSerializable]
public enum MechCompDeviceVisualLayers : byte
{
    Base = 0, // base sprite, changed by anchoring visualiser
    Effect1,
    Effect2,
    Effect3,
    Effect4,
    Effect5,
    Effect6
}
[Serializable, NetSerializable]
public enum MechCompDeviceVisuals : byte
{
    Mode = 0,
    Anchored
}


[RegisterComponent]
public sealed partial class MechCompMathComponent : Component
{
    //public float A = 0;
    //public float B = 0;
}


[RegisterComponent]
public sealed partial class MechCompPressurePadComponent : Component
{

}

[RegisterComponent]
public sealed partial class MechCompComparerComponent : Component
{

}



// todo: replace this with proper prototypes? maybe?
//
//public class MechCompConfigEntry<T>
//{
//
//    public MechCompConfigEntry(T val, string desc)
//    {
//        value = val;
//        description = desc;
//    }
//    T value = default!;
//    string description;
//}
//
//
///// <summary>
///// Prototype for mechcomp config entry.
///// </summary>
//[Prototype("mechCompConfig")]
//[Serializable, NetSerializable]
//public abstract class DevicePortPrototype
//{
//    [IdDataField]
//    public string ID { get; private set; } = default!;
//
//    /// <summary>
//    ///     Localization string for the config entry name. Displayed in the config dialog.
//    /// </summary>
//    [DataField("name", required: true)]
//    public string Name = default!;
//
//    // ???
//    //[DataField("type", required: true)]
//    //public Type Type = default!;
//}


/// <summary>
/// key is, well, key
/// (object, string) is the config entry, where
///      object is the value
///      string is the config entry short description, which shows up in the config dialog
/// if config entry tuple is null, then no control will be created for the key (see quickdialogsystem)
/// </summary>

public class MechCompConfig
{
    List<string> entryOrder = new(); // OrderedDictionary is non generic, and i don't feel like using non generic dictionaries

    Dictionary<string, QDEntry> dict = new();

    public void Build(params (string, QDEntry)[] entries) // Yes, it's coupled to QuickDialogSystem. I *really* do not want to write a new window from scratch for EVERY component.
    {
        if(dict.Count != 0) { throw new InvalidOperationException("Trying to build already built mechcomp config."); }
        if(entries.Length > 10) { throw new ArgumentException("Mechcomp config does not support more than 10 config entries."); }
        foreach(var ( key, entry ) in entries)
        {
            dict.Add(key, entry);
            entryOrder.Add(key);
        }
    }
    /// <summary>
    /// Does NOT check if the entry is not null. It will only be null when building config entry with a null as a defaultValue. Make sure to not do a stupid.
    /// </summary>
    public int GetInt(string key) // Also Hex16
    {
        return (int) dict[key].Value!;
    }
    /// <summary>
    /// Does NOT check if the entry is not null. It will only be null when building config entry with a null as a defaultValue. Make sure to not do a stupid.
    /// </summary>
    public float GetFloat(string key)
    {
        return (float) dict[key].Value!;
    }
    /// <summary>
    /// Does NOT check if the entry is not null. It will only be null when building config entry with a null as a defaultValue. Make sure to not do a stupid.
    /// </summary>
    public string GetString(string key) // also LongString
    {
        return (string) dict[key].Value!;
    }
    /// <summary>
    /// Does NOT check if the entry is not null. It will only be null when building config entry with a null as a defaultValue. Make sure to not do a stupid.
    /// </summary>
    public bool GetBool(string key)
    {
        return (bool) dict[key].Value!;
    }
    public void SetInt(string key, int value) // Also Hex16
    {
        dict[key].Value = value;
    }
    public void SetFloat(string key, float value)
    {
        dict[key].Value = value;
    }
    public void SetString(string key, string value) // also LongString
    {
        dict[key].Value = value;
    }
    public void SetBool(string key, bool value)
    {
        dict[key].Value = value;
    }
    /// <summary>
    /// Specifically for use with QuickDialogSystem
    /// </summary>
    public List<QDEntry> GetOrdered()
    {
        List<QDEntry> ret = new();
        foreach(string key in entryOrder)
        {
            ret.Add(dict[key]);
        }
        return ret;
    }

    public void SetFromObjectArray(object[] o)
    {
        if(o.Length != dict.Count) // dict and entryOrder length should be the same
        {
            throw new ArgumentException("Argument array length does not match config length");
        }
        for(int i = 0; i < o.Length; i++)
        {
            dict[entryOrder[i]].Value = o[i];
        }
    }
}


