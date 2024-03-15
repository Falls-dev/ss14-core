using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Content.Shared.Administration;

public class QDEntry
{
    public Type? type { get; private set; }
    public string description { get; private set; }
    public object? Value { get; set; }
    public object? info { get; private set; }

    public QDEntry(Type? type, string description, object? Value = null, object? info = null)
    {
        this.type = type;
        this.description = description;
        this.Value = Value;
        this.info = info;
    }
    public static implicit operator QDEntry((Type type, string desc, object? Value, object? info) tuple4)
    {
        return new QDEntry(tuple4.type, tuple4.desc, tuple4.Value, tuple4.info);
    }
    public static implicit operator QDEntry((Type type, string desc, object? Value) tuple3)
    {
        return new QDEntry(tuple3.type, tuple3.desc, tuple3.Value);
    }
    public static implicit operator QDEntry((Type? type, string desc) tuple)    // nullable type only here because it marks a bare label with no control
    {                                                                           // if you're making a label in a quickdialog, you don't need to specify a "default value".
        return new QDEntry(tuple.type, tuple.desc);
    }
    public void Deconstruct(out Type? type, out string description, out object? Value, out object? info)
    {
        type = this.type;
        description = this.description;
        Value = this.Value;
        info = this.info;
    }
}
