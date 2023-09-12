﻿using System;
using System.Collections.Generic;
using System.Linq;
using DotJEM.Json.Index2.Configuration;
using DotJEM.Json.Index2.Documents;
using DotJEM.Json.Index2.Documents.Fields;
using DotJEM.Json.Index2.Documents.Info;
using DotJEM.Json.Index2.IO;
using DotJEM.Json.Index2.Searching;
using DotJEM.Json.Index2.Serialization;
using DotJEM.Json.Index2.Storage;
using DotJEM.ObservableExtensions.InfoStreams;
using Lucene.Net.Analysis;
using Lucene.Net.Util;

namespace DotJEM.Json.Index2;

public interface IJsonIndexSearcherProvider
{
    IJsonIndexSearcher CreateSearcher();
}

public interface IJsonIndex : IJsonIndexSearcherProvider
{
    IInfoStream InfoStream { get; }
    IJsonIndexStorageManager Storage { get; }
    IJsonIndexConfiguration Configuration { get; }
    IIndexWriterManager WriterManager { get; }
    IIndexSearcherManager SearcherManager { get; }
    IJsonIndexWriter CreateWriter();

    void Close();
}

public class JsonIndex : IJsonIndex
{
    public IInfoStream InfoStream { get; } = new InfoStream<JsonIndex>();
    public IJsonIndexStorageManager Storage { get; }
    public IJsonIndexConfiguration Configuration { get; }
    public IIndexWriterManager WriterManager => Storage.WriterManager;
    public IIndexSearcherManager SearcherManager => Storage.SearcherManager;

    public JsonIndex(IJsonIndexStorage storage, IJsonIndexConfiguration configuration)
    {
        Configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        Storage = new JsonIndexStorageManager(this, storage);
    }

    public IJsonIndexSearcher CreateSearcher()
    {
        return new JsonIndexSearcher(this);
    }

    public IJsonIndexWriter CreateWriter()
    {
        return new JsonIndexWriter(this, Configuration.DocumentFactory, WriterManager);
        //TODO: We cant do a resolve each time we need the writer, we should have some lend/lease
        //IJsonIndexWriterProvider provider = Configuration.Get<IJsonIndexWriterProvider>();
        //return provider.Get();
    }

    public void Close()
    {
        WriterManager.Close();
        Storage.Close();
    }
}

public interface IJsonIndexBuilder
{
    IJsonIndexBuilder UsingStorage(IJsonIndexStorage storage);
    IJsonIndexBuilder WithService<TService>(bool replace, Func<IJsonIndexConfiguration, TService> factory);
    IJsonIndex Build();
}

public class JsonIndexBuilder : IJsonIndexBuilder
{
    private readonly string name = Guid.NewGuid().ToString("D");
    private IJsonIndexStorage storage = new RamJsonIndexStorage();
    private readonly Dictionary<Type, Func<IJsonIndexConfiguration, object>> factories = new();

    public JsonIndexBuilder(string name)
    {
        this.name = name;
    }

    public IJsonIndexBuilder UsingStorage(IJsonIndexStorage storage)
    {
        this.storage = storage;
        return this;
    }

    public IJsonIndexBuilder WithService<TService>(bool replace, Func<IJsonIndexConfiguration, TService> factory)
    {
        if (factory == null) throw new ArgumentNullException(nameof(factory));
        if (replace)
            factories[typeof(TService)] = cfg => factory(cfg);
        else
        {

#if NETSTANDARD2_0
            if(!factories.ContainsKey(typeof(TService)))
                factories.Add(typeof(TService), cfg => factory(cfg));
#else
            factories.TryAdd(typeof(TService), cfg => factory(cfg));
#endif
        }
        return this;
    }

    public IJsonIndex Build()
    {
        return new JsonIndex(storage, new JsonIndexConfiguration(LuceneVersion.LUCENE_48, factories
            .Select(pair => new ServiceDescriptor(pair.Key, pair.Value))));
    }
}

public static class JsonIndexBuilderExt
{
    public static IJsonIndexBuilder UsingSimpleFileStorage(this IJsonIndexBuilder self, string path)
      => self.UsingStorage(new SimpleFsJsonIndexStorage(path));
    public static IJsonIndexBuilder UsingMemmoryStorage(this IJsonIndexBuilder self)
        => self.UsingStorage(new RamJsonIndexStorage());

    public static IJsonIndexBuilder WithAnalyzer(this IJsonIndexBuilder self, Func<IJsonIndexConfiguration, Analyzer> analyzerProvider)
        => self.WithService(analyzerProvider);
    public static IJsonIndexBuilder WithFieldResolver(this IJsonIndexBuilder self, IFieldResolver resolver)
        => self.WithService(resolver);
    public static IJsonIndexBuilder WithFieldInformationManager(this IJsonIndexBuilder self, Func<IJsonIndexConfiguration, IFieldInformationManager> managerProvider)
        => self.WithService(managerProvider);
    public static IJsonIndexBuilder WithFieldInformationManager(this IJsonIndexBuilder self, Func<IJsonIndexConfiguration, ILuceneDocumentFactory> factoryProvider)
        => self.WithService(factoryProvider);
    public static IJsonIndexBuilder WithFieldInformationManager(this IJsonIndexBuilder self, IJsonDocumentSerializer serializer)
        => self.WithService(serializer);
    public static IJsonIndexBuilder WithService<TService>(this IJsonIndexBuilder self, TService impl)
        => self.WithService(_ => impl);
    public static IJsonIndexBuilder WithService<TService>(this IJsonIndexBuilder self, Func<IJsonIndexConfiguration, TService> factory)
        => self.WithService(true, factory);
    public static IJsonIndexBuilder TryWithService<TService>(this IJsonIndexBuilder self, TService impl)
        => self.TryWithService(_ => impl);
    public static IJsonIndexBuilder TryWithService<TService>(this IJsonIndexBuilder self, Func<IJsonIndexConfiguration, TService> factory)
        => self.WithService(false, factory);
}

//builder.Services.TryAddSingleton();
//builder.Services.AddSingleton();