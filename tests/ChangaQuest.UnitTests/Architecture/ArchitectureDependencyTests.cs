using System.Xml.Linq;

namespace ChangaQuest.UnitTests.Architecture;

public class ArchitectureDependencyTests
{
    [Fact]
    public void Layer_projects_only_reference_the_layers_allowed_by_the_architecture()
    {
        var root = FindRepositoryRoot();

        AssertReferences(root, "ChangaQuest.Entities", []);
        AssertReferences(root, "ChangaQuest.DataAccess", ["ChangaQuest.Entities"]);
        AssertReferences(
            root,
            "ChangaQuest.BusinessLogic",
            ["ChangaQuest.DataAccess", "ChangaQuest.Entities"]);
        AssertReferences(root, "ChangaQuest.Presentation", ["ChangaQuest.BusinessLogic"]);
    }

    private static void AssertReferences(string root, string projectName, string[] expected)
    {
        var projectFile = Path.Combine(
            root,
            "src",
            "layers",
            projectName,
            $"{projectName}.csproj");
        var actual = XDocument.Load(projectFile)
            .Descendants("ProjectReference")
            .Select(reference => Path.GetFileNameWithoutExtension(
                (string?)reference.Attribute("Include") ?? string.Empty))
            .OrderBy(name => name, StringComparer.Ordinal);

        Assert.Equal(expected.OrderBy(name => name, StringComparer.Ordinal), actual);
    }

    private static string FindRepositoryRoot()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory);
             directory is not null;
             directory = directory.Parent)
        {
            if (Directory.Exists(Path.Combine(directory.FullName, ".git")) ||
                File.Exists(Path.Combine(directory.FullName, ".git")))
            {
                return directory.FullName;
            }
        }

        throw new DirectoryNotFoundException("Could not locate the repository root.");
    }
}
