﻿using DotJEM.Json.Index2.Documents.Meta;
using Lucene.Net.Documents;
using Lucene.Net.Index;

namespace DotJEM.Json.Index2.Documents.Data;

/// <summary>
/// A Json Document with a list of Indexable Fields.
/// </summary>
public interface IIndexableJsonDocument
{
    /// <summary>
    /// 
    /// </summary>
    Document Document { get; }
    
    /// <summary>
    /// 
    /// </summary>
    IContentTypeInfo Info { get; }
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="field"></param>
    void Add(IIndexableJsonField field);
}

public class IndexableJsonDocument : IIndexableJsonDocument
{
    public Document Document { get; } = new Document();

    public IContentTypeInfo Info { get; }

    public IndexableJsonDocument(string contentType)
    {
        Info = new ContentTypeInfo(contentType);
    }

    public void Add(IIndexableJsonField field)
    {
        foreach (IIndexableField x in field.LuceneFields)
            Document.Add(x);

        Info.Add(field.Info());
    }
}