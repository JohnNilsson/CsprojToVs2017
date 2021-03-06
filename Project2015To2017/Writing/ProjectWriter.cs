﻿using Project2015To2017.Definition;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Project2015To2017.Writing
{
    internal sealed class ProjectWriter
    {
        public void Write(Project project, FileInfo outputFile)
        {
            //TODO: Detemine if "Microsoft.NET.Test.Sdk should be used
            var projectNode = new XElement("Project", new XAttribute("Sdk", "Microsoft.NET.Sdk"));

            projectNode.Add(GetMainPropertyGroup(project, outputFile));

            if (project.ConditionalPropertyGroups != null)
            {
                projectNode.Add(project.ConditionalPropertyGroups.Select(RemoveAllNamespaces));
            }

            if (project.ProjectReferences?.Count > 0)
            {
                var itemGroup = new XElement("ItemGroup");
                foreach (var projectReference in project.ProjectReferences)
                {
					var projectReferenceElement = new XElement("ProjectReference",
							new XAttribute("Include", projectReference.Include));

					if (!string.IsNullOrWhiteSpace(projectReference.Aliases) && projectReference.Aliases != "global")
					{
						projectReferenceElement.Add(new XElement("Aliases", projectReference.Aliases));
					}

					itemGroup.Add(projectReferenceElement);
                }

                projectNode.Add(itemGroup);
            }

            if (project.PackageReferences?.Count > 0)
            {
                var nugetReferences = new XElement("ItemGroup");
                foreach (var packageReference in project.PackageReferences)
                {
                    var reference = new XElement("PackageReference", new XAttribute("Include", packageReference.Id), new XAttribute("Version", packageReference.Version));
                    if (packageReference.IsDevelopmentDependency)
                    {
                        reference.Add(new XElement("PrivateAssets", "all"));
                    }

                    nugetReferences.Add(reference);
                }

                projectNode.Add(nugetReferences);
            }

            if (project.AssemblyReferences?.Count > 0)
            {
              var assemblyReferences = new XElement("ItemGroup");
              foreach (var assemblyReference in project.AssemblyReferences.Where(x => !IsDefaultIncludedAssemblyReference(x)))
              {
                if (assemblyReference.Contains(Path.DirectorySeparatorChar))
                {
                  var hintPath = assemblyReference;
                  var assemblyName = Path.GetFileNameWithoutExtension(assemblyReference);
                  assemblyReferences.Add(new XElement("Reference", new XAttribute("Include", assemblyName), new XElement("HintPath", hintPath)));
                }
                else
                {
                  assemblyReferences.Add(new XElement("Reference", new XAttribute("Include", assemblyReference)));
                }
              }
              projectNode.Add(assemblyReferences);
            }

            // manual includes
            if (project.ItemsToInclude?.Count > 0)
            {
                var includeGroup = new XElement("ItemGroup");
                foreach (var include in project.ItemsToInclude.Select(RemoveAllNamespaces))
                {
                    includeGroup.Add(include);
                }

                projectNode.Add(includeGroup);
            }

            using (var filestream = File.Open(outputFile.FullName, FileMode.Create))
            using (var streamWriter = new StreamWriter(filestream, Encoding.UTF8))
            {
                streamWriter.Write(projectNode.ToString());
            }
		}

		private static XElement RemoveAllNamespaces(XElement e)
		{
			return new XElement(e.Name.LocalName,
			  (from n in e.Nodes()
			   select ((n is XElement) ? RemoveAllNamespaces(n as XElement) : n)),
				  (e.HasAttributes) ?
					(from a in e.Attributes()
					 where (!a.IsNamespaceDeclaration)
					 select new XAttribute(a.Name.LocalName, a.Value)) : null);
		}

		private bool IsDefaultIncludedAssemblyReference(string assemblyReference)
        {
            return new string[]
            {
                "System",
                "System.Core",
                "System.Data",
                "System.Drawing",
                "System.IO.Compression.FileSystem",
                "System.Numerics",
                "System.Runtime.Serialization",
                "System.Xml",
                "System.Xml.Linq"
            }.Contains(assemblyReference);
        }

        private XElement GetMainPropertyGroup(Project project, FileInfo outputFile)
        {
            var mainPropertyGroup = new XElement("PropertyGroup",
              ToTargetFrameworks(project.TargetFrameworks));
            // https://github.com/dotnet/sdk/issues/350
            mainPropertyGroup.Add(new XElement("Platforms", "x64"));

            // Regaring RuntimeIdentifier and Platforms
            // https://github.com/dotnet/sdk/issues/840
            // https://github.com/dotnet/sdk/issues/696
            // https://github.com/dotnet/standard/issues/193
            // https://github.com/dotnet/sdk/blob/master/src/Tasks/Microsoft.NET.Build.Tasks/build/Microsoft.NET.Sdk.props
            switch (project.Type)
            {
              case ApplicationType.ConsoleApplication:
                mainPropertyGroup.Add(new XElement("OutputType", "Exe"));
                mainPropertyGroup.Add(new XElement("RuntimeIdentifier", "win10-x64"));
                break;
              case ApplicationType.WindowsApplication:
                mainPropertyGroup.Add(new XElement("OutputType", "WinExe"));
                mainPropertyGroup.Add(new XElement("RuntimeIdentifier", "win10-x64"));
                break;

              // https://github.com/dotnet/sdk/issues/1351
              // https://github.com/dotnet/sdk/issues/1595
              // Or should https://github.com/Microsoft/vstest/blob/master/src/package/nuspec/Microsoft.NET.Test.Sdk.targets be used instead?
              case ApplicationType.ClassLibrary:
                mainPropertyGroup.Add(new XElement("AutoGenerateBindingRedirects", "true"));
                mainPropertyGroup.Add(new XElement("GenerateBindingRedirectsOutputType", "true"));
                break;
            }



            AddIfNotNull(mainPropertyGroup, "Optimize", project.Optimize ? "true" : null);
            AddIfNotNull(mainPropertyGroup, "TreatWarningsAsErrors", project.TreatWarningsAsErrors ? "true" : null);
            AddIfNotNull(mainPropertyGroup, "RootNamespace", project.RootNamespace != Path.GetFileNameWithoutExtension(outputFile.Name) ? project.RootNamespace : null);
            AddIfNotNull(mainPropertyGroup, "AssemblyName", project.AssemblyName != Path.GetFileNameWithoutExtension(outputFile.Name) ? project.AssemblyName : null);
            AddIfNotNull(mainPropertyGroup, "AllowUnsafeBlocks", project.AllowUnsafeBlocks ? "true" : null);


            AddAssemblyAttributeNodes(mainPropertyGroup, project.AssemblyAttributes);
            AddPackageNodes(mainPropertyGroup, project.PackageConfiguration);

            return mainPropertyGroup;
        }

        private void AddPackageNodes(XElement mainPropertyGroup, PackageConfiguration packageConfiguration)
        {
            if (packageConfiguration== null)
            {
                return;
            }

            AddIfNotNull(mainPropertyGroup, "Authors", packageConfiguration.Authors);
            AddIfNotNull(mainPropertyGroup, "Copyright", packageConfiguration.Copyright);
            AddIfNotNull(mainPropertyGroup, "Description", packageConfiguration.Description);
            AddIfNotNull(mainPropertyGroup, "PackageIconUrl", packageConfiguration.IconUrl);
            AddIfNotNull(mainPropertyGroup, "PackageId", packageConfiguration.Id);
            AddIfNotNull(mainPropertyGroup, "PackageLicenseUrl", packageConfiguration.LicenseUrl);
            AddIfNotNull(mainPropertyGroup, "PackageProjectUrl", packageConfiguration.ProjectUrl);
            AddIfNotNull(mainPropertyGroup, "PackageReleaseNotes", packageConfiguration.ReleaseNotes);
            AddIfNotNull(mainPropertyGroup, "PackageTags", packageConfiguration.Tags);
            AddIfNotNull(mainPropertyGroup, "PackageVersion", packageConfiguration.Version);

            if (packageConfiguration.RequiresLicenseAcceptance)
            {
                mainPropertyGroup.Add(new XElement("PackageRequireLicenseAcceptance", "true"));
            }
        }

        private void AddAssemblyAttributeNodes(XElement mainPropertyGroup, AssemblyAttributes assemblyAttributes)
        {
            if (assemblyAttributes == null)
            {
                return;
            }

            AddIfNotNull(mainPropertyGroup, "GenerateAssemblyTitleAttribute", "false");
            AddIfNotNull(mainPropertyGroup, "GenerateAssemblyCompanyAttribute", "false");
            AddIfNotNull(mainPropertyGroup, "GenerateAssemblyDescriptionAttribute", "false");
            AddIfNotNull(mainPropertyGroup, "GenerateAssemblyProductAttribute", "false");
            AddIfNotNull(mainPropertyGroup, "GenerateAssemblyCopyrightAttribute", "false");
            AddIfNotNull(mainPropertyGroup, "GenerateAssemblyInformationalVersionAttribute", "false");
            AddIfNotNull(mainPropertyGroup, "GenerateAssemblyVersionAttribute", "false");
            AddIfNotNull(mainPropertyGroup, "GenerateAssemblyFileVersionAttribute", "false");
            AddIfNotNull(mainPropertyGroup, "GenerateAssemblyConfigurationAttribute", "false");
        }

        private void AddIfNotNull(XElement node, string elementName, string value)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                node.Add(new XElement(elementName, value));
            }
        }

        private string ToOutputType(ApplicationType type)
        {
            

            return null;
        }

        private XElement ToTargetFrameworks(IReadOnlyList<string> targetFrameworks)
        {
            if (targetFrameworks.Count > 1)
            {
                return new XElement("TargetFrameworks", string.Join(";", targetFrameworks));
            }

            return new XElement("TargetFramework", targetFrameworks[0]);
        }
    }
}
