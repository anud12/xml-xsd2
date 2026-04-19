using Xunit;
using Xunit.Gherkin.Quick;

namespace NewGameProject;

class State
{
    
}

[FeatureFile(@".*Features/.*\.feature", FeatureFilePathType.Regex)]
public class InteropFeatures : Feature
{
    private State state = new();

    [Given(@"I load the runtime")]
    public void I_load_the_runtime()
    {
    }

    [Given(@"I have added {string} file as {string} to archive")]
    public void I_have_added_string_file_as_string_to_archive (string path, string name)
    {
        
        
    }
    
    [Then(@"assert that ""GetPanelNames"" returns {string}")]
    public void Assert_that_getPanelNames_returns (string expectedList)
    {
        var resultString = RuntimeInterop.GetPanelNames();
        var expectedListArray = expectedList.Split(",");
        Assert.Equal(expectedListArray, resultString);
        
    }
}

internal class Calculator
{
    public int Result { get; private set; }
    private int _first;
    private int _second;
    public void SetFirstNumber(int number) => _first = number;
    public void SetSecondNumber(int number) => _second = number;
    public void AddNumbers() => Result = _first + _second;
}