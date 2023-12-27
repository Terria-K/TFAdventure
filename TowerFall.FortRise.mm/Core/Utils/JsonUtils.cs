using System;
using Microsoft.Xna.Framework;
using TeuJson;

namespace FortRise;

public static class JsonUtils 
{
    public static Option<Hjson.JsonValue> GetJsonValueOrNone(this Hjson.JsonValue value, string key) 
    {
        if (value.ContainsKey(key))
            return Option<Hjson.JsonValue>.Some(value[key]);
        return Option<Hjson.JsonValue>.None();
    }

    public static Hjson.JsonValue GetJsonValueOrNull(this Hjson.JsonValue value, string key) 
    {
        if (value.ContainsKey(key))
            return value[key];
        return null;
    }

    public static bool TryParseEnum<T>(this Hjson.JsonValue value, string key, out T result) 
    where T : struct, Enum
    {
        if (value.ContainsKey(key)) 
        {
            var str = value[key];
            if (Enum.TryParse<T>(str, out result))
                return true;

            return false;
        }
        result = default;
        return false;
    }

    public static bool TryGetValue(this Hjson.JsonValue value, string key, out Hjson.JsonValue result) 
    {
        if (value.ContainsKey(key)) 
        {
            result = value[key];
            return true;
        }
        result = null;
        return false;
    }

    public static Vector2 Position(this Hjson.JsonValue value) 
    {
        return new Vector2(value["x"], value["y"]);
    }

    public static Option<JsonValue> GetJsonValueOrNone(this JsonValue value, string key) 
    {
        if (value.Contains(key))
            return Option<JsonValue>.Some(value[key]);
        return Option<JsonValue>.None();
    }

    public static JsonValue GetJsonValueOrNull(this JsonValue value, string key) 
    {
        if (value.Contains(key))
            return value[key];
        return null;
    }

    public static Vector2 Position(this JsonValue value) 
    {
        return new Vector2(value["x"], value["y"]);
    }
}

public struct Option<T> 
{
    public T Wrapped;

    private Option(T value) 
    {
        Wrapped = value;
    }

    public bool Unwrap(out T value) 
    {
        if (Wrapped != null) 
        {
            value = Wrapped;
            return true;
        }
        value = default;
        return false;
    }

    public static Option<T> None() 
    {
        return new Option<T>(default);
    }

    public static Option<T> Some(T value) 
    {
        return new Option<T>(value);
    }
}