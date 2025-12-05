using Xunit;
using System;
using System.IO;
using Design_Develop_Deploy_Project.Utilities;

namespace Tests
{
    public class InputValidationTests
    {
        // =========================
        // FR-20: Invalid input is rejected
        // =========================

        [Fact]
        public void AskForInput_WithEmptyInput_ReturnsEmptyString()
        {
            // Arrange
            var input = new StringReader("\n");
            Console.SetIn(input);

            // Act
            string result = ConsoleHelper.AskForInput("Enter something");

            // Assert
            Assert.Equal(string.Empty, result);
        }

        [Fact]
        public void AskForInput_WithValidInput_ReturnsTrimmedText()
        {
            // Arrange
            var input = new StringReader("  Hello  \n");
            Console.SetIn(input);

            // Act
            string result = ConsoleHelper.AskForInput("Enter text");

            // Assert
            Assert.Equal("Hello", result);
        }

        // =========================
        // FR-21: Error / message display
        // =========================

        [Fact]
        public void PrintSection_WithInvalidColour_DoesNotCrash()
        {
            // Arrange & Act
            var exception = Record.Exception(() =>
                ConsoleHelper.PrintSection("Error", "Something went wrong", "NotARealColour")
            );

            // Assert
            Assert.Null(exception); // No crash = PASS
        }

        [Fact]
        public void WriteInColour_WithInvalidColour_DoesNotCrash()
        {
            // Arrange & Act
            var exception = Record.Exception(() =>
                ConsoleHelper.WriteInColour("Test Message", "FakeColour")
            );

            // Assert
            Assert.Null(exception);
        }

        [Fact]
        public void WriteInColour_WithValidColour_DoesNotCrash()
        {
            // Arrange & Act
            var exception = Record.Exception(() =>
                ConsoleHelper.WriteInColour("Test Message", "Red")
            );

            // Assert
            Assert.Null(exception);
        }
    }
}
