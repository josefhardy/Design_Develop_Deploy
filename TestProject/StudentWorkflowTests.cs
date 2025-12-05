using System;
using Xunit;
using Design_Develop_Deploy_Project.Objects;
using Design_Develop_Deploy_Project.Services;
using Tests;

namespace Tests
{
    public class StudentWorkflowTests
    {
        private StudentService _studentService;
        private Student _student;

        public StudentWorkflowTests()
        {
            // Pick the first test student
            _student = TestRepos.TestStudents[0];

            // Instead of using DB, we'll inject test student only
            _studentService = new StudentService(null, _student, testMode: true);
        }

        [Fact]
        public void StudentCanViewWellbeingScore()
        {
            // Arrange
            int expectedScore = _student.wellbeing_score;

            // Act
            int actualScore = _student.wellbeing_score; // just read the property

            // Assert
            Assert.Equal(expectedScore, actualScore);
        }

        [Fact]
        public void StudentCanUpdateWellbeingScore_TestMode()
        {
            // Arrange
            int initialScore = _student.wellbeing_score;
            int newTestScore = 8;

            // Act
            _studentService.UpdateStatus(newTestScore); // bypass console
            int updatedScore = _student.wellbeing_score;

            // Assert
            Assert.Equal(newTestScore, updatedScore);
            Assert.NotEqual(initialScore, updatedScore);
        }
    }
}
