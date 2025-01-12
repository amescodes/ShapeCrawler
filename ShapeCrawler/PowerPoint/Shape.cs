﻿using System;
using System.Linq;
using System.Text.RegularExpressions;
using DocumentFormat.OpenXml;
using OneOf;
using ShapeCrawler.Constants;
using ShapeCrawler.Exceptions;
using ShapeCrawler.Extensions;
using ShapeCrawler.Placeholders;
using ShapeCrawler.Services;
using ShapeCrawler.Shapes;
using ShapeCrawler.SlideMasters;
using ShapeCrawler.Statics;
using A = DocumentFormat.OpenXml.Drawing;
using P = DocumentFormat.OpenXml.Presentation;

namespace ShapeCrawler;

internal abstract class Shape : IShape, IRemovable, IPresentationComponent
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="Shape"/> class for grouped shape.
    /// </summary>
    protected Shape(OpenXmlCompositeElement pShapeTreeChild, OneOf<SCSlide, SCSlideLayout, SCSlideMaster> slideOrLayout,
        Shape? groupShape)
        : this(pShapeTreeChild, slideOrLayout)
    {
        this.GroupShape = groupShape;
    }

    protected Shape(OpenXmlCompositeElement pShapeTreeChild, OneOf<SCSlide, SCSlideLayout, SCSlideMaster> slideOrLayout)
    {
        this.PShapeTreesChild = pShapeTreeChild;
        this.SlideBase = slideOrLayout.Match(slide => slide as SlideBase, layout => layout, master => master);
    }

    /// <summary>
    ///     Gets shape identifier.
    /// </summary>
    public int Id => (int)this.PShapeTreesChild.GetNonVisualDrawingProperties().Id!.Value;

    /// <summary>
    ///     Gets shape name.
    /// </summary>
    public string Name => this.PShapeTreesChild.GetNonVisualDrawingProperties().Name!;

    /// <summary>
    ///     Gets a value indicating whether shape is hidden.
    /// </summary>
    public bool Hidden =>
        this.DefineHidden(); // TODO: the Shape is inherited by LayoutShape, hence do we need this property?

    /// <summary>
    ///     Gets or sets custom data.
    /// </summary>
    public string? CustomData
    {
        get => this.GetCustomData();
        set => this.SetCustomData(value ?? throw new ArgumentNullException(nameof(value)));
    }

    public abstract SCShapeType ShapeType { get; }

    /// <summary>
    ///     Gets placeholder. Returns <c>NULL</c> if the shape is not a placeholder.
    /// </summary>
    public abstract IPlaceholder? Placeholder { get; }

    public abstract SCPresentation PresentationInternal { get; }

    /// <summary>
    ///     Gets geometry form type.
    /// </summary>
    public virtual SCGeometry GeometryType => this.GetGeometryType();

    /// <summary>
    ///     Gets or sets x-coordinate of the upper-left corner of the shape.
    /// </summary>
    public int X
    {
        get => this.GetXCoordinate();
        set => this.SetXCoordinate(value);
    }

    /// <summary>
    ///     Gets or sets y-coordinate of the upper-left corner of the shape.
    /// </summary>
    public int Y
    {
        get => this.GetYCoordinate();
        set => this.SetYCoordinate(value);
    }

    /// <summary>
    ///     Gets or sets height of the shape.
    /// </summary>
    public int Height
    {
        get => this.GetHeightPixels();
        set => this.SetHeight(value);
    }

    /// <summary>
    ///     Gets or sets width of the shape.
    /// </summary>
    public int Width
    {
        get => this.GetWidthPixels();
        set => this.SetWidth(value);
    }

    bool IRemovable.IsRemoved { get; set; }

    internal SCSlideMaster SlideMasterInternal
    {
        get
        {
            if (this.SlideBase is SCSlide slide)
            {
                return slide.SlideLayoutInternal.SlideMasterInternal;
            }

            if (this.SlideBase is SCSlideLayout layout)
            {
                return layout.SlideMasterInternal;
            }

            var master = (SCSlideMaster)this.SlideBase;
            return master;
        }
    }

    internal OpenXmlCompositeElement PShapeTreesChild { get; }

    internal SlideBase SlideBase { get; }

    internal P.ShapeProperties PShapeProperties => this.PShapeTreesChild.GetFirstChild<P.ShapeProperties>() !;

    private Shape? GroupShape { get; }

    public void ThrowIfRemoved()
    {
        if (((IRemovable)this).IsRemoved)
        {
            throw new ElementIsRemovedException("Shape was removed.");
        }

        this.SlideBase.ThrowIfRemoved();
    }

    private void SetCustomData(string value)
    {
        string customDataElement =
            $@"<{SCConstants.CustomDataElementName}>{value}</{SCConstants.CustomDataElementName}>";
        this.PShapeTreesChild.InnerXml += customDataElement;
    }

    private string? GetCustomData()
    {
        var pattern = @$"<{SCConstants.CustomDataElementName}>(.*)<\/{SCConstants.CustomDataElementName}>";
        var regex = new Regex(pattern);
        var elementText = regex.Match(this.PShapeTreesChild.InnerXml).Groups[1];
        if (elementText.Value.Length == 0)
        {
            return null;
        }

        return elementText.Value;
    }

    private bool DefineHidden()
    {
        var parsedHiddenValue = this.PShapeTreesChild.GetNonVisualDrawingProperties().Hidden?.Value;
        return parsedHiddenValue is true;
    }

    private void SetXCoordinate(int value)
    {
        if (this.GroupShape is not null)
        {
            throw new RuntimeDefinedPropertyException("X coordinate of grouped shape cannot be changed.");
        }

        var aOffset = this.PShapeTreesChild.Descendants<A.Offset>().FirstOrDefault();
        if (aOffset == null)
        {
            var placeholderShape = ((Placeholder)this.Placeholder!).ReferencedShape;
            placeholderShape.X = value;
        }
        else
        {
            aOffset.X = PixelConverter.HorizontalPixelToEmu(value);
        }
    }

    private int GetXCoordinate()
    {
        var aOffset = this.PShapeTreesChild.Descendants<A.Offset>().FirstOrDefault();
        if (aOffset == null)
        {
            return ((Placeholder)this.Placeholder!).ReferencedShape.X;
        }

        long xEmu = aOffset.X!;

        if (this.GroupShape is not null)
        {
            var aTransformGroup = ((P.GroupShape)this.GroupShape.PShapeTreesChild).GroupShapeProperties!.TransformGroup;
            xEmu = xEmu - aTransformGroup!.ChildOffset!.X! + aTransformGroup!.Offset!.X!;
        }

        return PixelConverter.HorizontalEmuToPixel(xEmu);
    }

    private void SetYCoordinate(long value)
    {
        if (this.GroupShape is not null)
        {
            throw new RuntimeDefinedPropertyException("Y coordinate of grouped shape cannot be changed.");
        }

        var aOffset = this.PShapeTreesChild.Descendants<A.Offset>().First();
        if (this.Placeholder is not null)
        {
            throw new PlaceholderCannotBeChangedException();
        }

        aOffset.Y = PixelConverter.VerticalPixelToEmu(value);
    }

    private int GetYCoordinate()
    {
        var aOffset = this.PShapeTreesChild.Descendants<A.Offset>().FirstOrDefault();
        if (aOffset == null)
        {
            return ((Placeholder)this.Placeholder!).ReferencedShape.Y;
        }

        var yEmu = aOffset.Y!;

        if (this.GroupShape is not null)
        {
            var aTransformGroup =
                ((P.GroupShape)this.GroupShape.PShapeTreesChild).GroupShapeProperties!.TransformGroup!;
            yEmu = yEmu - aTransformGroup.ChildOffset!.Y! + aTransformGroup!.Offset!.Y!;
        }

        return PixelConverter.VerticalEmuToPixel(yEmu);
    }

    private int GetWidthPixels()
    {
        var aExtents = this.PShapeTreesChild.Descendants<A.Extents>().FirstOrDefault();
        if (aExtents == null)
        {
            var placeholder = (Placeholder)this.Placeholder!;
            return placeholder.ReferencedShape.Width;
        }

        return PixelConverter.HorizontalEmuToPixel(aExtents.Cx!);
    }

    private void SetWidth(int pixels)
    {
        var aExtents = this.PShapeTreesChild.Descendants<A.Extents>().FirstOrDefault();
        if (aExtents == null)
        {
            throw new PlaceholderCannotBeChangedException();
        }

        aExtents.Cx = PixelConverter.HorizontalPixelToEmu(pixels);
    }

    private int GetHeightPixels()
    {
        var aExtents = this.PShapeTreesChild.Descendants<A.Extents>().FirstOrDefault();
        if (aExtents == null)
        {
            return ((Placeholder)this.Placeholder!).ReferencedShape.Height;
        }

        return PixelConverter.VerticalEmuToPixel(aExtents!.Cy!);
    }

    private void SetHeight(int pixels)
    {
        var aExtents = this.PShapeTreesChild.Descendants<A.Extents>().FirstOrDefault();
        if (aExtents == null)
        {
            throw new PlaceholderCannotBeChangedException();
        }

        aExtents.Cy = PixelConverter.VerticalPixelToEmu(pixels);
    }

    private SCGeometry GetGeometryType()
    {
        var spPr = this.PShapeTreesChild.Descendants<P.ShapeProperties>().First(); // TODO: optimize
        var aTransform2D = spPr.Transform2D;
        if (aTransform2D != null)
        {
            var aPresetGeometry = spPr.GetFirstChild<A.PresetGeometry>();

            // Placeholder can have transform on the slide, without having geometry
            if (aPresetGeometry == null)
            {
                if (spPr.OfType<A.CustomGeometry>().Any())
                {
                    return SCGeometry.Custom;
                }
            }
            else
            {
                var name = aPresetGeometry.Preset!.Value.ToString();
                Enum.TryParse(name, true, out SCGeometry geometryType);
                return geometryType;
            }
        }

        var placeholder = (Placeholder)this.Placeholder;
        if (placeholder?.ReferencedShape != null)
        {
            return placeholder.ReferencedShape.GeometryType;
        }

        return SCGeometry.Rectangle; // return default
    }
}