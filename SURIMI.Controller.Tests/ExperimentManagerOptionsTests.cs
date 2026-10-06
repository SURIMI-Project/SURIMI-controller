using FluentAssertions;
using Microsoft.Extensions.Options;
using SURIMI_controller.Services;

namespace SURIMI.Controller.Tests
{
    public class ExperimentManagerOptionsTests
    {
        [Fact]
        public void NrOfMseRuns_DefaultsToOne()
        {
            // Arrange / Act
            var options = Options.Create(new ExperimentManagerOptions());

            // Assert
            options.Value.NrOfMseRuns.Should().Be(1);
        }

        [Theory]
        [InlineData(null, 1)]
        [InlineData("", 1)]
        [InlineData("abc", 1)]
        [InlineData("5", 5)]
        public void NrOfMseRuns_ParsesEnvironmentValueWithFallback(string? envValue, int expected)
        {
            // Arrange — mirrors the binding logic in Program.cs
            var options = new ExperimentManagerOptions
            {
                NrOfMseRuns = int.TryParse(envValue, out var n) ? n : 1
            };

            // Assert
            options.NrOfMseRuns.Should().Be(expected);
        }
    }
}
