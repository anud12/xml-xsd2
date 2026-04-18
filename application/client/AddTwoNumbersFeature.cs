using Xunit;
using System.Linq;
using System.Reflection;
using Xunit.Gherkin.Quick;
using Xunit.Abstractions;

namespace NewGameProject;

[FeatureFile("Features/feature.feature")]
public class AddTwoNumbersFeature: Feature
{
    private readonly Calculator _calculator = new Calculator();

    [Given(@"I chose (\d+) as first number")]
    public void I_chose_first_number(int firstNumber)
    {
        _calculator.SetFirstNumber(firstNumber);
    }

    [And(@"I chose (\d+) as second number")]
    public void I_chose_second_number(int secondNumber)
    {
        _calculator.SetSecondNumber(secondNumber);
    }

    [When(@"I press add")]
    public void I_press_add()
    {
        _calculator.AddNumbers();
    }

    [Then(@"the result should be (\d+) on the screen")]
    public void The_result_should_be_z_on_the_screen(int expectedResult)
    {
        var actualResult = _calculator.Result;

        Assert.Equal(expectedResult, actualResult);
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