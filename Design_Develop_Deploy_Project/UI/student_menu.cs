
using Design_Develop_Deploy_Project.Objects;
using Design_Develop_Deploy_Project.Repos;
using Design_Develop_Deploy_Project.Utilities;
using Design_Develop_Deploy_Project.Services;
using System;
using System.Runtime.CompilerServices;
using System.Linq.Expressions;

namespace Design_Develop_Deploy_Project.UI;

public class StudentMenu
{
    private readonly string _connectionString;
    private readonly StudentRepository studentRepo;
    private readonly Student student;
    public StudentMenu(User user, string connectionString)
    {
        _connectionString = connectionString;
        studentRepo = new StudentRepository(connectionString);
        student = studentRepo.GetStudentByEmail(user.email);
    }

    public void ShowStudentMenu()
    {
        var studentService = new StudentService(_connectionString, student);

        if ((DateTime.UtcNow - student.last_status_update.Value).TotalDays > 7)
        {
            Console.Clear();
            Console.WriteLine($"{student.first_name} it has been more than a week since your last status update, please let us know how you're feeling");
            Thread.Sleep(5000);
        }
        bool exit = false;

        while (!exit)
        {
            var menuItems = new List<string>
        {
            "View wellbeing status",
            "Update wellbeing status",
            "Book a meeting with supervisor",
            "View your meetings",
            "Logout"
        };

            Console.Clear();
            Console.WriteLine($"Welcome {student.first_name}!");
            int choice = ConsoleHelper.PromptForChoice(menuItems, "====== Student Menu ======");

            try
            {
                switch (choice)
                {
                    case 1:
                        studentService.ViewStatus();
                        ConsoleHelper.Pause();
                        break;
                    case 2:
                        studentService.UpdateStatus();
                        ConsoleHelper.Pause();
                        break;
                    case 3:
                        studentService.BookMeeting();
                        ConsoleHelper.Pause();
                        break;
                    case 4:
                        studentService.ViewMeetings();
                        ConsoleHelper.Pause();
                        break;
                    case 5:
                        exit = true;
                        break;
                    default:
                        Console.WriteLine("Invalid choice, please try again");
                        break;
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
                ConsoleHelper.Pause();
            }
        }
    }
}
