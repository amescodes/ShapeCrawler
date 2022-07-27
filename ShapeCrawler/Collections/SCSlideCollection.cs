﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Drawing.Charts;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Presentation;
using ShapeCrawler.Charts;
using ShapeCrawler.Shared;
using P = DocumentFormat.OpenXml.Presentation;
using C = DocumentFormat.OpenXml.Drawing.Charts;

namespace ShapeCrawler.Collections
{
    internal class SCSlideCollection : ISlideCollection
    {
        private readonly SCPresentation parentPresentation;
        private readonly ResettableLazy<List<SCSlide>> slides;
        private PresentationPart presentationPart;

        internal EventHandler CollectionChanged;

        internal SCSlideCollection(SCPresentation presentation)
        {
            this.presentationPart = presentation.PresentationDocument.PresentationPart ??
                                    throw new ArgumentNullException("PresentationPart");
            this.parentPresentation = presentation;
            this.slides = new ResettableLazy<List<SCSlide>>(this.GetSlides);
        }

        public int Count => this.slides.Value.Count;

        public ISlide this[int index] => this.slides.Value[index];

        public IEnumerator<ISlide> GetEnumerator()
        {
            return this.slides.Value.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        public void Remove(ISlide removingSlide)
        {
            // TODO: slide layout and master of removed slide also should be deleted if they are unused
            var sdkPresentation = this.presentationPart.Presentation;
            var slideIdList = sdkPresentation.SlideIdList!;
            var removingSlideIndex = removingSlide.Number - 1;
            var removingSlideId = (P.SlideId)slideIdList.ChildElements[removingSlideIndex];
            var removingSlideRelId = removingSlideId.RelationshipId!;

            this.parentPresentation.SectionsInternal.RemoveSldId(removingSlideId.Id);

            slideIdList.RemoveChild(removingSlideId);
            RemoveFromCustomShow(sdkPresentation, removingSlideRelId);

            var removingSlidePart = (SlidePart)this.presentationPart.GetPartById(removingSlideRelId!);
            this.presentationPart.DeletePart(removingSlidePart);

            this.presentationPart.Presentation.Save();
            removingSlide.IsRemoved = true;

            this.slides.Reset();

            this.OnCollectionChanged();
        }

        public void Add(ISlide addingSlide)
        {
            var addingSlideInner = (SCSlide)addingSlide;
            if (addingSlideInner.ParentPresentation != this.parentPresentation)
            {
                this.AddExternal(addingSlide, addingSlideInner);
            }
            else
            {
                this.Duplicate(addingSlideInner);
            }

            this.OnCollectionChanged();
        }

        private void Duplicate(SCSlide addingSlideInner)
        {
            var slidePart = addingSlideInner.SDKSlidePart;

            var clonedSlide = (Slide)slidePart.Slide.CloneNode(true);
            var newSlidePart = this.presentationPart.AddNewPart<SlidePart>();
            clonedSlide.Save(newSlidePart);
            newSlidePart.AddPart(slidePart.SlideLayoutPart);

            var slideIdList = this.presentationPart.Presentation.SlideIdList;
            var maxSlideId = slideIdList.ChildElements
                .Cast<SlideId>()
                .Max(x => x.Id.Value);
            var id = maxSlideId + 1;
            var newSlideId = new SlideId();
            slideIdList.Append(newSlideId);
            newSlideId.Id = id;
            newSlideId.RelationshipId = this.presentationPart.GetIdOfPart(newSlidePart);

            // ADD CHARTS
            foreach (var chart in addingSlideInner.Shapes.OfType<SCChart>())
            {
                var rId = chart.pGraphicFrame.Graphic.GraphicData.GetFirstChild<ChartReference>().Id;
                var chartPart = (ChartPart) slidePart.GetPartById(rId);
                var clonedChartSpace = (ChartSpace)chartPart.ChartSpace.CloneNode(true);
                var newChartPart = newSlidePart.AddNewPart<ChartPart>();
                var ddd = 1;
                clonedChartSpace.Save(newChartPart);
            }

            var d = 1;
        }

        private void AddExternal(ISlide outerSlide, SCSlide outerInnerSlide)
        {
            this.parentPresentation.ThrowIfClosed();

            var presentation = (SCPresentation)outerInnerSlide.ParentPresentation;
            PresentationDocument addingSlideDoc = presentation.PresentationDocument;
            PresentationDocument destDoc = this.parentPresentation.PresentationDocument;
            PresentationPart addingPresentationPart = addingSlideDoc.PresentationPart;
            PresentationPart destPresentationPart = destDoc.PresentationPart;
            Presentation destPresentation = destPresentationPart.Presentation;
            int addingSlideIndex = outerSlide.Number - 1;
            SlideId addingSlideId =
                (SlideId)addingPresentationPart.Presentation.SlideIdList.ChildElements[addingSlideIndex];
            SlidePart addingSlidePart = (SlidePart)addingPresentationPart.GetPartById(addingSlideId.RelationshipId);

            SlidePart addedSlidePart = destPresentationPart.AddPart(addingSlidePart);
            NotesSlidePart noticePart = addedSlidePart.GetPartsOfType<NotesSlidePart>().FirstOrDefault();
            if (noticePart != null)
            {
                addedSlidePart.DeletePart(noticePart);
            }

            SlideMasterPart addedSlideMasterPart =
                destPresentationPart.AddPart(addedSlidePart.SlideLayoutPart.SlideMasterPart);

            // Create new slide ID
            SlideId slideId = new()
            {
                Id = CreateId(destPresentation.SlideIdList),
                RelationshipId = destDoc.PresentationPart.GetIdOfPart(addedSlidePart)
            };
            destPresentation.SlideIdList.Append(slideId);

            // Create new master slide ID
            uint masterId = CreateId(destPresentation.SlideMasterIdList);
            SlideMasterId slideMaterId = new()
            {
                Id = masterId,
                RelationshipId = destDoc.PresentationPart.GetIdOfPart(addedSlideMasterPart)
            };
            destDoc.PresentationPart.Presentation.SlideMasterIdList.Append(slideMaterId);

            destDoc.PresentationPart.Presentation.Save();

            // Make sure that all slide layouts have unique ids.
            foreach (SlideMasterPart slideMasterPart in destDoc.PresentationPart.SlideMasterParts)
            {
                foreach (SlideLayoutId slideLayoutId in slideMasterPart.SlideMaster.SlideLayoutIdList)
                {
                    masterId++;
                    slideLayoutId.Id = masterId;
                }

                slideMasterPart.SlideMaster.Save();
            }
        }

        public void Insert(int position, ISlide outerSlide)
        {
            if (position < 1 || position > this.slides.Value.Count + 1)
            {
                throw new ArgumentOutOfRangeException(nameof(position));
            }

            this.Add(outerSlide);
            int addedSlideIndex = this.slides.Value.Count - 1;
            this.slides.Value[addedSlideIndex].Number = position;

            this.slides.Reset();
            this.parentPresentation.SlideMastersValue.Reset();
            this.OnCollectionChanged();
        }

        internal SCSlide GetBySlideId(string slideId)
        {
            return this.slides.Value.First(scSlide => scSlide.SlideId.Id == slideId);
        }

        private static uint CreateId(SlideIdList slideIdList)
        {
            uint currentId = 0;
            foreach (SlideId slideId in slideIdList)
            {
                if (slideId.Id > currentId)
                {
                    currentId = slideId.Id;
                }
            }

            return ++currentId;
        }

        private static uint CreateId(SlideMasterIdList slideMasterIdList)
        {
            uint currentId = 0;
            foreach (SlideMasterId masterId in slideMasterIdList)
            {
                if (masterId.Id > currentId)
                {
                    currentId = masterId.Id;
                }
            }

            return ++currentId;
        }

        private List<SCSlide> GetSlides()
        {
            this.presentationPart = this.parentPresentation.PresentationDocument.PresentationPart!;
            int slidesCount = this.presentationPart.SlideParts.Count();
            var slides = new List<SCSlide>(slidesCount);
            var slideIds = this.presentationPart.Presentation.SlideIdList.ChildElements.OfType<SlideId>().ToList();
            for (var slideIndex = 0; slideIndex < slidesCount; slideIndex++)
            {
                var slideId = slideIds[slideIndex];
                var slidePart = (SlidePart)this.presentationPart.GetPartById(slideId.RelationshipId);
                var newSlide = new SCSlide(this.parentPresentation, slidePart, slideId);
                slides.Add(newSlide);
            }

            return slides;
        }

        private void OnCollectionChanged()
        {
            this.slides.Reset();
            this.parentPresentation.SlideMastersValue.Reset();
            this.CollectionChanged?.Invoke(this, null);
        }

        private static void RemoveFromCustomShow(Presentation sdkPresentation, StringValue? removingSlideRelId)
        {
            if (sdkPresentation.CustomShowList == null)
            {
                return;
            }

            // Iterate through the list of custom shows
            foreach (var customShow in sdkPresentation.CustomShowList.Elements<P.CustomShow>())
            {
                if (customShow.SlideList == null)
                {
                    continue;
                }

                // declares a link list of slide list entries
                var slideListEntries = new LinkedList<P.SlideListEntry>();
                foreach (P.SlideListEntry slideListEntry in customShow.SlideList.Elements())
                {
                    // finds the slide reference to remove from the custom show
                    if (slideListEntry.Id != null && slideListEntry.Id == removingSlideRelId)
                    {
                        slideListEntries.AddLast(slideListEntry);
                    }
                }

                // Removes all references to the slide from the custom show
                foreach (P.SlideListEntry slideListEntry in slideListEntries)
                {
                    customShow.SlideList.RemoveChild(slideListEntry);
                }
            }
        }
    }
}