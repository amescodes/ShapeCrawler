﻿using DocumentFormat.OpenXml;
using OneOf;
using ShapeCrawler.Shapes;
using ShapeCrawler.SlideMasters;
using P = DocumentFormat.OpenXml.Presentation;

namespace ShapeCrawler.Factories;

internal class AutoShapeCreator : OpenXmlElementHandler
{
    internal override Shape? Create(OpenXmlCompositeElement pShapeTreeChild, OneOf<SCSlide, SCSlideLayout, SCSlideMaster> oneOfSlide, SCGroupShape groupShape)
    {
        if (pShapeTreeChild is P.Shape pShape)
        {
            var slideAutoShape = new SlideAutoShape(pShape, oneOfSlide, groupShape);
            return slideAutoShape;
        }

        return this.Successor?.Create(pShapeTreeChild, oneOfSlide, groupShape);
    }
}