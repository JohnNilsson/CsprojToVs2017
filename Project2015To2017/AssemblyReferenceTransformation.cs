using System.IO;
using System.Threading.Tasks;
using System.Xml.Linq;
using Project2015To2017.Definition;
using System.Linq;

namespace Project2015To2017
{
    internal sealed class AssemblyReferenceTransformation : ITransformation
    {
        public Task TransformAsync(XDocument projectFile, DirectoryInfo projectFolder, Project definition)
        {
            XNamespace nsSys = "http://schemas.microsoft.com/developer/msbuild/2003";

            definition.AssemblyReferences = (
              from itemGroup in projectFile.Element(nsSys + "Project").Elements(nsSys + "ItemGroup")
              from reference in itemGroup.Elements(nsSys + "Reference")
              let include = reference.Attribute("Include").Value
              let hintPath = reference.Elements(nsSys + "HintPath").SingleOrDefault()?.Value
              let isNugetRef = hintPath != null && hintPath.Contains("packages\\")
              where ! isNugetRef
              select hintPath ?? include
            ).ToArray();

            return Task.CompletedTask;
        }
    }
}
