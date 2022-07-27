using System;
using System.IO;
using System.Linq;
using FluentAssertions;
using ShapeCrawler.Charts;
using ShapeCrawler.Exceptions;
using ShapeCrawler.Extensions;
using ShapeCrawler.Statics;
using ShapeCrawler.Tests.Helpers;
using Xunit;

namespace ShapeCrawler.Tests
{
    public class PresentationTests : ShapeCrawlerTest, IClassFixture<PresentationFixture>
    {
        private readonly PresentationFixture _fixture;

        public PresentationTests(PresentationFixture fixture)
        {
            _fixture = fixture;
        }

        [Fact]
        public void Close_ClosesPresentationAndReleasesResources()
        {
            // Arrange
            string originFilePath = Path.GetTempFileName();
            string savedAsFilePath = Path.GetTempFileName();
            File.WriteAllBytes(originFilePath, TestFiles.Presentations.pre001);
            IPresentation presentation = SCPresentation.Open(originFilePath, true);
            presentation.SaveAs(savedAsFilePath);

            // Act
            presentation.Close();

            // Assert
            Action act = () => presentation = SCPresentation.Open(originFilePath, true);
            act.Should().NotThrow<IOException>();
            presentation.Close();

            // Clean up
            File.Delete(originFilePath);
            File.Delete(savedAsFilePath);
        }

        [Fact]
        public void Close_ShouldNotThrowObjectDisposedException()
        {
            // Arrange
            IPresentation presentation = SCPresentation.Open(TestFiles.Presentations.pre025_byteArray, true);
            MemoryStream mStream = new();
            IPieChart chart = (IPieChart)presentation.Slides[0].Shapes.First(sp => sp.Id == 7);
            chart.Categories[0].Name = "new name";
            presentation.SaveAs(mStream);

            // Act
            Action act = () => presentation.Close();

            // Assert
            act.Should().NotThrow<ObjectDisposedException>();
        }

        [Fact]
        public void Open_ThrowsPresentationIsLargeException_WhenThePresentationContentSizeIsBeyondThePermitted()
        {
            // Arrange
            var bytes = new byte[Limitations.MaxPresentationSize + 1];

            // Act
            Action act = () => SCPresentation.Open(bytes, false);

            // Assert
            act.Should().Throw<PresentationIsLargeException>();
        }

        [Fact]
        public void Slide_Width_returns_presentation_slides_width_in_pixels()
        {
            // Arrange
            var presentation = _fixture.Pre009;

            // Act
            var slideWidth = presentation.SlideWidth;

            // Assert
            slideWidth.Should().Be(960);
        }
        
        [Fact]
        public void Slide_Height_returns_presentation_slides_height_in_pixels()
        {
            // Arrange
            var presentation = _fixture.Pre009;

            // Act
            var slideHeight = presentation.SlideHeight;

            // Assert
            slideHeight.Should().Be(540);
        }

        [Fact]
        public void SlideMastersCount_ReturnsNumberOfMasterSlidesInThePresentation()
        {
            // Arrange
            IPresentation presentationCase1 = _fixture.Pre001;
            IPresentation presentationCase2 = _fixture.Pre002;

            // Act
            int slideMastersCountCase1 = presentationCase1.SlideMasters.Count;
            int slideMastersCountCase2 = presentationCase2.SlideMasters.Count;

            // Assert
            slideMastersCountCase1.Should().Be(1);
            slideMastersCountCase2.Should().Be(2);
        }

        [Fact]
        public void SlideMasterShapesCount_ReturnsNumberOfShapesOnTheMasterSlide()
        {
            // Arrange
            IPresentation presentation = _fixture.Pre001;

            // Act
            int slideMasterShapesCount = presentation.SlideMasters[0].Shapes.Count;

            // Assert
            slideMasterShapesCount.Should().Be(7);
        }

        [Fact]
        public void Sections_Remove_removes_specified_section()
        {
            // Arrange
            var pptxStream = GetTestPptxStream("030.pptx");
            var pres = SCPresentation.Open(pptxStream, true);
            var removingSection = pres.Sections[0];

            // Act
            pres.Sections.Remove(removingSection);

            // Assert
            pres.Sections.Count.Should().Be(0);
        }
        
        [Fact]
        public void Sections_Remove_should_remove_section_after_Removing_Slide_from_section()
        {
            // Arrange
            var pptxStream = GetTestPptxStream("030.pptx");
            var pres = SCPresentation.Open(pptxStream, true);
            var removingSection = pres.Sections[0];

            // Act
            pres.Slides.Remove(pres.Slides[0]);
            pres.Sections.Remove(removingSection);

            // Assert
            pres.Sections.Count.Should().Be(0);
        }
        
        [Fact]
        public void Sections_Section_Slides_Count_returns_Zero_When_section_is_Empty()
        {
            // Arrange
            var pptxStream = GetTestPptxStream("008.pptx");
            var pres = SCPresentation.Open(pptxStream, false);
            var section = pres.Sections.GetByName("Section 2");

            // Act
            var slidesCount = section.Slides.Count;

            // Assert
            slidesCount.Should().Be(0);
        }
                
        [Fact]
        public void Sections_Section_Slides_Count_returns_number_of_slides_in_section()
        {
            var pptxStream = GetTestPptxStream("030.pptx");
            var pres = SCPresentation.Open(pptxStream, false);
            var section = pres.Sections.GetByName("Section 1");

            // Act
            var slidesCount = section.Slides.Count;

            // Assert
            slidesCount.Should().Be(1);
        }
        
        [Fact]
        public void SaveAs_should_not_change_the_Original_Stream_when_it_is_saved_to_New_Stream()
        {
            // Arrange
            var originalStream = GetTestPptxStream("001.pptx");
            var pres = SCPresentation.Open(originalStream, true);
            var textBox = pres.Slides[0].Shapes.GetByName<IAutoShape>("TextBox 3").TextBox;
            var originalText = textBox!.Text;
            var newStream = new MemoryStream();

            // Act
            textBox.Text = originalText + "modified";
            pres.SaveAs(newStream);
            
            pres.Close();
            pres = SCPresentation.Open(originalStream, false);
            textBox = pres.Slides[0].Shapes.GetByName<IAutoShape>("TextBox 3").TextBox;
            var autoShapeText = textBox!.Text; 

            // Assert
            autoShapeText.Should().BeEquivalentTo(originalText);
        }
        
        [Fact]
        public void SaveAs_should_not_change_the_Original_Stream_when_it_is_saved_to_New_Path()
        {
            // Arrange
            var originalStream = GetTestPptxStream("001.pptx");
            var originalFile = Path.GetTempFileName();
            originalStream.SaveToFile(originalFile);
            var pres = SCPresentation.Open(originalFile, true);
            var textBox = pres.Slides[0].Shapes.GetByName<IAutoShape>("TextBox 3").TextBox;
            var originalText = textBox!.Text;
            var newPath = Path.GetTempFileName();

            // Act
            textBox.Text = originalText + "modified";
            pres.SaveAs(newPath);
            
            pres.Close();
            pres = SCPresentation.Open(originalFile, false);
            textBox = pres.Slides[0].Shapes.GetByName<IAutoShape>("TextBox 3").TextBox;
            var autoShapeText = textBox!.Text; 

            // Assert
            autoShapeText.Should().BeEquivalentTo(originalText);
            
            // Clean
            pres.Close();
            File.Delete(newPath);
        }
        
        [Fact]
        public void SaveAs_should_not_change_the_Original_Path_when_it_is_saved_to_New_Stream()
        {
            // Arrange
            var originalPath = GetTestPptxPath("001.pptx");
            var pres = SCPresentation.Open(originalPath, true);
            var textBox = pres.Slides[0].Shapes.GetByName<IAutoShape>("TextBox 3").TextBox;
            var originalText = textBox!.Text;
            var newStream = new MemoryStream();

            // Act
            textBox.Text = originalText + "modified";
            pres.SaveAs(newStream);
            
            pres.Close();
            pres = SCPresentation.Open(originalPath, false);
            textBox = pres.Slides[0].Shapes.GetByName<IAutoShape>("TextBox 3").TextBox;
            var autoShapeText = textBox!.Text; 

            // Assert
            autoShapeText.Should().BeEquivalentTo(originalText);
            
            // Clean
            pres.Close();
            File.Delete(originalPath);
        }
        
        [Fact]
        public void SaveAs_should_not_change_the_Original_Path_when_it_is_saved_to_New_Path()
        {
            // Arrange
            var originalPath = GetTestPptxPath("001.pptx");
            var pres = SCPresentation.Open(originalPath, true);
            var textBox = pres.Slides[0].Shapes.GetByName<IAutoShape>("TextBox 3").TextBox;
            var originalText = textBox!.Text;
            var newPath = Path.GetTempFileName();

            // Act
            textBox.Text = originalText + "modified";
            pres.SaveAs(newPath);
            
            pres.Close();
            pres = SCPresentation.Open(originalPath, false);
            textBox = pres.Slides[0].Shapes.GetByName<IAutoShape>("TextBox 3").TextBox;
            var autoShapeText = textBox!.Text; 

            // Assert
            autoShapeText.Should().BeEquivalentTo(originalText);
            
            // Clean
            pres.Close();
            File.Delete(originalPath);
            File.Delete(newPath);
        }
    }
}
