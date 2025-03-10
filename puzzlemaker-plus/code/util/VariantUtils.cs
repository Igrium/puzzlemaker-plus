using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Godot;
using Godot.Collections;

namespace PuzzlemakerPlus;

public static class VariantUtils
{
    /// <summary>
    /// Attempt to create a variant from a generic variable type.
    /// </summary>
    /// <typeparam name="T">Variable type.</typeparam>
    /// <param name="value">Value to put into variant.</param>
    /// <param name="variant">Variable to store created variant.</param>
    /// <returns>If the supplied type was variant-compatible and a variant was created.</returns>
    public static bool TryCreateVariant<T>(in T? value, out Variant variant)
    {
        if (value == null)
            variant = default;
        else if (value is bool boolv)
            variant = boolv;
        else if (value is char charv)
            variant = charv;
        else if (value is sbyte sbytev)
            variant = sbytev;
        else if (value is short shortv)
            variant = shortv;
        else if (value is int intv)
            variant = intv;
        else if (value is long longv)
            variant = longv;
        else if (value is byte bytev)
            variant = bytev;
        else if (value is ushort ushortv)
            variant = ushortv;
        else if (value is uint uintv)
            variant = uintv;
        else if (value is ulong ulongv)
            variant = ulongv;
        else if (value is float floatv)
            variant = floatv;
        else if (value is double doublev)
            variant = doublev;

        else if (value is Vector2 vec2)
            variant = vec2;
        else if (value is Vector2I vec2i)
            variant = vec2i;
        else if (value is Rect2 rect2)
            variant = rect2;
        else if (value is Rect2I rect2i)
            variant = rect2i;
        else if (value is Transform2D transform2)
            variant = transform2;
        else if (value is Projection proj)
            variant = proj;
        else if (value is Vector3 vec3)
            variant = vec3;
        else if (value is Vector3I vec3i)
            variant = vec3i;
        else if (value is Basis basis)
            variant = basis;
        else if (value is Quaternion quat)
            variant = quat;
        else if (value is Vector4 vec4)
            variant = vec4;
        else if (value is Vector4I vec4i)
            variant = vec4i;
        else if (value is Aabb aabb)
            variant = aabb;
        else if (value is Color color)
            variant = color;
        else if (value is Plane plane)
            variant = plane;
        else if (value is Callable callable)
            variant = callable;
        else if (value is Signal signal)
            variant = signal;

        else if (value is string str)
            variant = str;
        else if (value is byte[] bytea)
            variant = bytea;
        else if (value is int[] inta)
            variant = inta;
        else if (value is long[] longa)
            variant = longa;
        else if (value is float[] floata)
            variant = floata;
        else if (value is double[] doublea)
            variant = doublea;
        else if (value is string[] stringa)
            variant = stringa;
        else if (value is Vector2[] vec2a)
            variant = vec2a;
        else if (value is Vector3[] vec3a)
            variant = vec3a;
        else if (value is Vector4[] vec4a)
            variant = vec4a;
        else if (value is Color[] colora)
            variant = colora;
        else if (value is StringName[] strnamea)
            variant = strnamea;
        else if (value is NodePath[] nodepatha)
            variant = nodepatha;
        else if (value is Rid[] rida)
            variant = rida;

        else if (value is StringName strname)
            variant = strname;
        else if (value is NodePath nodepath)
            variant = nodepath;
        else if (value is Rid rid)
            variant = rid;
        else if (value is Dictionary dict)
            variant = dict;
        else if (value is Godot.Collections.Array array)
            variant = array;
        else if (value is Variant var)
            variant = var;
        else if (value is GodotObject obj)
            variant = obj;
        // TODO: support enums
        else
        {
            variant = default;
            return false;
        }

        return true;
    }
}
