﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DotJEM.ObservableExtensions;
using DotJEM.ObservableExtensions.InfoStreams;
using Newtonsoft.Json.Linq;

namespace DotJEM.Json.Index2.Management;


public interface IObservableValue<T> : IObservable<T>
{
    T Value { get; set; }
}
public class ObservableValue<T> : BasicSubject<T>, IObservableValue<T>
{
    private T value;

    public T Value
    {
        get => value;
        set
        {
            if(EqualityComparer<T>.Default.Equals(this.value, value))
                return;

            Publish(this.value = value);
        }
    }
}

public interface IJsonDocumentSource
{
    IInfoStream InfoStream { get; }
    IObservable<IJsonDocumentChange> DocumentChanges { get; }
    IObservableValue<bool> Initialized { get; }
    Task RunAsync();
    void UpdateGeneration(string area, long generation);
}

public interface IJsonDocumentChange
{
    string Area { get; }
    GenerationInfo Generation { get; }
    JsonChangeType Type { get; }
    JObject Entity { get; }
    public int Size { get; }
}

public class ChangeStream : BasicSubject<IJsonDocumentChange> { }

public record JsonDocumentChange(string Area, JsonChangeType Type, JObject Entity, int Size, GenerationInfo Generation)
    : IJsonDocumentChange
{
    public GenerationInfo Generation { get; } = Generation;
    public JsonChangeType Type { get; } = Type;
    public string Area { get; } = Area;
    public JObject Entity { get; } = Entity;
    public int Size { get; } = Size;
}

public struct GenerationInfo
{
    public long Current { get; }
    public long Latest { get; }

    public GenerationInfo(long current, long latest)
    {
        Current = current;
        Latest = latest;
    }

    public static GenerationInfo operator +(GenerationInfo left, GenerationInfo right)
    {
        return new GenerationInfo(left.Current + right.Current, left.Latest + right.Latest);
    }
}


public enum JsonChangeType
{
    Create, Update, Delete
}