﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace PuzzlemakerPlus.Item;

[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public class ItemProp : Attribute
{
    public string? DisplayName { get; }

    public ItemProp(string? displayName = null)
    {
        DisplayName = displayName;
    }
}
