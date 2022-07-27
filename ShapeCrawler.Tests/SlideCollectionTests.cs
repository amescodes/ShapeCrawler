﻿using System;
using System.Collections.Generic;
using System.IO;
using FluentAssertions;
using ShapeCrawler.Tests.Helpers;
using Xunit;

namespace ShapeCrawler.Tests
{
    public class SlideCollectionTests : ShapeCrawlerTest, IClassFixture<PresentationFixture>
    {
        private readonly PresentationFixture _fixture;

        public SlideCollectionTests(PresentationFixture fixture)
        {
            _fixture = fixture;
        }

        [Fact]
        public void Slides_Count_returns_one_When_presentation_contains_one_slide()
        {
            // Act
            var numberSlidesCase1 = _fixture.Pre017.Slides.Count;
            var numberSlidesCase2 = _fixture.Pre016.Slides.Count;

            // Assert
            numberSlidesCase1.Should().Be(1);
            numberSlidesCase2.Should().Be(1);
        }

        [Fact]
        public void Slides_Add_adds_slide_from_External_presentation_at_the_end_of_the_slide_collection()
        {
            // Arrange
            var sourceSlide = _fixture.Pre001.Slides[0];
            var destPre = SCPresentation.Open(Properties.Resources._002, true);
            var originSlidesCount = destPre.Slides.Count;
            var expectedSlidesCount = ++originSlidesCount;
            MemoryStream savedPre = new ();

            // Act
            destPre.Slides.Add(sourceSlide);

            // Assert
            destPre.Slides.Count.Should().Be(expectedSlidesCount, "because the new slide has been added");

            destPre.SaveAs(savedPre);
            destPre = SCPresentation.Open(savedPre, false);
            destPre.Slides.Count.Should().Be(expectedSlidesCount, "because the new slide has been added");
        }
        
        [Fact]
        public void Slides_Add_copies_slide_from_the_Same_presentation_at_the_end_of_the_slide_collection()
        {
            // Arrange
            var pptxStream = GetTestPptxStream("charts-case003.pptx");
            // var pres = SCPresentation.Open(pptxStream, true);
            var pres = SCPresentation.Open(@"c:\temp\with-chart.pptx", true);
            var originalSlidesCount = pres.Slides.Count;
            var copyingSlide = pres.Slides[0];

            // Act
            pres.Slides.Add(copyingSlide);

            // Assert
            pres.Slides.Count.Should().Be(originalSlidesCount + 1);
            
            pres.SaveAs(@"c:\temp\result.pptx");
        }

        [Fact]
        public void Slides_Add_should_not_Break_presentation()
        {
            // Arrange
            var sourceSlide = _fixture.Pre001.Slides[0];
            var destPres = SCPresentation.Open(Properties.Resources._002, true);
            var newStream = new MemoryStream();

            // Act
            destPres.Slides.Add(sourceSlide);

            // Assert
            destPres.SaveAs(newStream);
            var validateResponse = PptxValidator.Validate(newStream);
            validateResponse.IsValid.Should().BeTrue();
        }

        [Fact]
        public void Slides_Insert_inserts_specified_slide_at_the_specified_position()
        {
            // Arrange
            ISlide sourceSlide = SCPresentation.Open(TestFiles.Presentations.pre001, true).Slides[0];
            string sourceSlideId = Guid.NewGuid().ToString();
            sourceSlide.CustomData = sourceSlideId;
            IPresentation destPre = SCPresentation.Open(Properties.Resources._002, true);

            // Act
            destPre.Slides.Insert(2, sourceSlide);

            // Assert
            destPre.Slides[1].CustomData.Should().Be(sourceSlideId);
        }

        [Theory]
        [MemberData(nameof(TestCasesSlidesRemove))]
        public void Slides_Remove_removes_slide(byte[] pptxBytes, int expectedSlidesCount)
        {
            // Arrange
            var pres = SCPresentation.Open(pptxBytes, true);
            var removingSlide = pres.Slides[0];
            var mStream = new MemoryStream();

            // Act
            pres.Slides.Remove(removingSlide);

            // Assert
            pres.Slides.Should().HaveCount(expectedSlidesCount);

            pres.SaveAs(mStream);
            pres = SCPresentation.Open(mStream, false);
            pres.Slides.Should().HaveCount(expectedSlidesCount);
        }
        
        public static IEnumerable<object[]> TestCasesSlidesRemove()
        {
            yield return new object[] {Properties.Resources._007_2_slides, 1};
            yield return new object[] {Properties.Resources._006_1_slides, 0};
        }
        
        [Fact]
        public void Slides_Remove_removes_slide_from_section()
        {
            // Arrange
            var pptxStream = GetTestPptxStream("030.pptx");
            var pres = SCPresentation.Open(pptxStream, true);
            var sectionSlides = pres.Sections[0].Slides;
            var removingSlide = sectionSlides[0];
            var mStream = new MemoryStream();

            // Act
            pres.Slides.Remove(removingSlide);

            // Assert
            sectionSlides.Count.Should().Be(0);

            pres.SaveAs(mStream);
            pres = SCPresentation.Open(mStream, false);
            sectionSlides = pres.Sections[0].Slides;
            sectionSlides.Count.Should().Be(0);
        }
    }
}