using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Content.Shared.Administration;


/// <summary>
/// DO NOT fucking pass <see langword="null"/> to Value if your type is not <see langword="null"/> or typeof(void) or typeof(VoidOption). You have been warned.
/// </summary>
public class QDEntry
{
    /// <summary>
    /// Option type for quick dialog system. typeof(bool) will show a checkmark,
    /// typeof(string) will show a text field, typeof(int) will show a number field, etc.
    /// 
    /// Passing null here is the same as passing typeof(VoidOption) and is used to show
    /// a text label without any corresponding dialog options.
    /// 
    /// See TypeToEntryType() in QuickDialogSystem for accepted types.
    /// </summary>
    public Type? type { get; private set; }
    /// <summary>
    /// Option description. It's the text label that is shown to the left of the option.
    /// </summary>
    public string description { get; private set; }
    /// <summary>
    /// Current value of the option. It's only expected to be null if the type is null or typeof(VoidOption).
    /// In other cases it will lead to errors.
    /// </summary>
    public object? Value { get; set; }

    /// <summary>
    /// Any additional arguments for the dialog. Currently only used for typeof(List<string>) for available options.
    /// </summary>
    public object? info { get; private set; }

    public QDEntry(Type? type, string description, object? Value = null, object? info = null)
    {
        this.type = type;
        this.description = description;
        this.Value = Value;
        this.info = info;
    }

    public static implicit operator QDEntry((Type type, string desc, object? Value, object? info) tuple)
    {
        return new QDEntry(tuple.type, tuple.desc, tuple.Value, tuple.info);
    }
    public static implicit operator QDEntry((Type type, string desc, object? Value) tuple)
    {
        return new QDEntry(tuple.type, tuple.desc, tuple.Value);
    }
    public static implicit operator QDEntry((Type? type, string desc) tuple)    // nullable type only here because it marks a bare label with no control
    {                                                                           // if you're making a label in a quickdialog, you don't need to specify a "default value" as it's never used.
        return new QDEntry(tuple.type, tuple.desc);
    }
    // deconstruct is used to get relevant info for QuickDialogSystem. deleg is only used in mechcompdevicesystem, so it's not returned here.
    public void Deconstruct(out Type? type, out string description, out object? Value, out object? info) 
    {
        type = this.type;
        description = this.description;
        Value = this.Value;
        info = this.info;
    }
}
