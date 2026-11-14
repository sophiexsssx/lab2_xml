using static XmlAnalyzer.Tests.Helpers;

namespace XmlAnalyzer.Tests;

public class ViewModelTests
{
    [Fact]
    public void LoadFile_FillsAttributeValues()
    {
        var vm = MainViewModel.CreateDefault();
        vm.LoadFile(SamplePath);

        Assert.Equal(SamplePath, vm.XmlPath);
        Assert.Contains("city", vm.AttributeValues.Keys);
        Assert.False(vm.HasSearched);
    }

    [Fact]
    public void LoadFile_BrokenFile_KeepsPreviousState()
    {
        var vm = MainViewModel.CreateDefault();
        vm.LoadFile(SamplePath);
        using var broken = new TempFile(".xml", "<oops");

        Assert.ThrowsAny<System.Xml.XmlException>(() => vm.LoadFile(broken.Path));
        Assert.Equal(SamplePath, vm.XmlPath);
    }

    [Fact]
    public void Search_BeforeOpening_Throws()
    {
        Assert.Throws<InvalidOperationException>(() => MainViewModel.CreateDefault().Search());
    }

    [Theory, MemberData(nameof(StrategyNames), MemberType = typeof(Helpers))]
    public void Search_UsesSelectedStrategyAndCriteria(string strategy)
    {
        var vm = MainViewModel.CreateDefault();
        vm.LoadFile(SamplePath);
        vm.SelectedStrategy = vm.Strategies.Single(s => s.Name == strategy);
        vm.Keyword = "  систем ";
        vm.SelectedFilters["format"] = "онлайн";

        vm.Search();

        Assert.True(vm.HasSearched);
        Assert.Equal("Розподілені системи та хмарні обчислення", Assert.Single(vm.Results).Title);
        Assert.Equal("систем", vm.LastCriteria!.Keyword);
    }

    [Fact]
    public void Clear_ResetsOutputAndParameters()
    {
        var vm = MainViewModel.CreateDefault();
        vm.LoadFile(SamplePath);
        vm.Keyword = "x";
        vm.SelectedFilters["city"] = "Київ";
        vm.Search();

        vm.Clear();

        Assert.Equal("", vm.Keyword);
        Assert.Empty(vm.SelectedFilters);
        Assert.Empty(vm.Results);
        Assert.False(vm.HasSearched);
        Assert.Equal(SamplePath, vm.XmlPath);
    }

    [Fact]
    public void SaveAndTransform_RequireSearchFirst()
    {
        var vm = MainViewModel.CreateDefault();
        vm.LoadFile(SamplePath);
        Assert.Throws<InvalidOperationException>(() => vm.SaveResults("x.xml"));
        Assert.Throws<InvalidOperationException>(() => vm.TransformToHtml(XslPath, "x.html"));
    }

    [Fact]
    public void SaveResults_WritesOnlyFilteredConferences()
    {
        var vm = MainViewModel.CreateDefault();
        vm.LoadFile(SamplePath);
        vm.SelectedFilters["topic"] = "Фізика";
        vm.Search();
        using var file = new TempFile();

        vm.SaveResults(file.Path);

        Assert.Equal(2, new Search.DomSearchStrategy().Search(file.Path, Criteria()).Count);
    }

    [Fact]
    public void TransformToHtml_UsesFilteredResults_AndCleansTempFile()
    {
        var vm = MainViewModel.CreateDefault();
        vm.LoadFile(SamplePath);
        vm.SelectedFilters["city"] = "Львів";
        vm.Search();
        using var html = new TempFile(".html");
        int tempBefore = Directory.GetFiles(Path.GetTempPath(), "conferences-*.xml").Length;

        vm.TransformToHtml(XslPath, html.Path);

        string text = File.ReadAllText(html.Path);
        Assert.Contains("Знайдено конференцій: 3", text);
        Assert.DoesNotContain("Київ", text);
        Assert.Equal(tempBefore, Directory.GetFiles(Path.GetTempPath(), "conferences-*.xml").Length);
    }
}
