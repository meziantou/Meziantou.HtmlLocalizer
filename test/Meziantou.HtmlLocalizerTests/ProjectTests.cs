using System.Globalization;
using Meziantou.HtmlLocalizer;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Meziantou.HtmlLocalizerTests
{
    [TestClass]
    public class ProjectTests
    {
        [TestMethod]
        public void ExtractInnerText_Test()
        {
            // Arrange
            Project project = new Project();
            HtmlFile file = new HtmlFile();
            file.Project = project;
            file.Path = "sample.html";
            file.LoadHtml("<span loc:name='Sample'>Cancel</span>");
            project.Files.Add(file);

            // Act
            file.ExtractFields();
            string innerText;
            bool valueFound = file.Fields["Sample"].TryGetAttributeValue(CultureInfo.InvariantCulture, "innerText", out innerText);

            // Assert
            Assert.IsTrue(valueFound);
            Assert.AreEqual("Cancel", innerText);
        }

        [TestMethod]
        public void ExtractInnerHtml_Test()
        {
            // Arrange
            Project project = new Project();
            HtmlFile file = new HtmlFile();
            file.Project = project;
            file.Path = "sample.html";
            file.LoadHtml("<span loc:name='Sample'>Sample <strong>test</strong></span>");
            project.Files.Add(file);

            // Act
            file.ExtractFields();
            string innerHtml;
            bool valueFound = file.Fields["Sample"].TryGetAttributeValue(CultureInfo.InvariantCulture, "innerHtml", out innerHtml);

            // Assert
            Assert.IsTrue(valueFound);
            Assert.AreEqual("Sample <strong>test</strong>", innerHtml);
        }

        [TestMethod]
        public void ReferencedField_Test()
        {
            // Arrange
            Project project = new Project();
            HtmlFile resourceFile = new HtmlFile();
            resourceFile.Project = project;
            resourceFile.Path = "SR.html";
            resourceFile.LoadHtml("<span loc:name='Cancel'>Cancel</span>");
            resourceFile.ExtractFields();
            resourceFile.Fields["Cancel"].SetAttributeValue(new CultureInfo("fr"), "innerText", "Annuler");
            project.Files.Add(resourceFile);

            HtmlFile file = new HtmlFile();
            file.Project = project;
            file.Path = "sr.html";
            file.LoadHtml("<span loc:name='SR.html#Cancel'>Cancel</span>");
            file.ExtractFields();
            project.Files.Add(file);

            // Act
            var localized = file.Localize(new CultureInfo("fr"));

            // Assert
            Assert.AreEqual("<span>Annuler</span>", localized);
        }

        [TestMethod]
        public void ExtractOptions_Test()
        {
            // Arrange
            HtmlFile file = new HtmlFile();
            file.LoadHtml("<i class='fa fa-cog' loc:name='Icon' loc:attributes='class'></i>");

            // Act
            file.ExtractFields();

            // Assert
            Assert.AreEqual(1, file.Fields.Count);
            Assert.AreEqual(1, file.Fields["Icon"].Values.Count);
            Assert.AreEqual("fa fa-cog", file.Fields["Icon"].Values["class"][""]);
        }

        [TestMethod]
        public void VoidElement_Test()
        {
            // Arrange
            Project project = new Project();
            HtmlFile file = new HtmlFile();
            file.Project = project;
            file.LoadHtml("<html><head><meta loc:name='Meta - Description' name='Description' content='test'></head> <body></body></html>");

            // Act
            file.ExtractFields();

            // Assert
            Assert.AreEqual(1, file.Fields.Count);
            Assert.AreEqual(false, file.Fields["Meta - Description"].Values.ContainsKey("innerHtml"));
            Assert.AreEqual(false, file.Fields["Meta - Description"].Values.ContainsKey("innerText"));
            Assert.AreEqual("test", file.Fields["Meta - Description"].Values["content"][""]);
        }
    }
}
