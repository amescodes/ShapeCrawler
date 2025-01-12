﻿using System.IO;

namespace ShapeCrawler;

/// <summary>
///     Represents a shape fill.
/// </summary>
public interface IShapeFill
{
    /// <summary>
    ///     Gets fill type.
    /// </summary>
    public SCFillType Type { get; }

    /// <summary>
    ///     Gets picture image. Returns <c>null</c> if fill type is not picture.
    /// </summary>
    public IImage? Picture { get; }
        
    /// <summary>
    ///     Gets solid color. Returns <c>null</c> if fill type is not solid.
    /// </summary>
    public string? HexSolidColor { get; }

    /// <summary>
    ///     Sets picture fill.
    /// </summary>
    void SetPicture(Stream image);
}