using Design.Develop.Deploy.Repos;
using Design_Develop_Deploy_Project.Objects;
using Design_Develop_Deploy_Project.Repos;
using Design_Develop_Deploy_Project.Utilities;
using Design_Develop_Deploy_Project.Services;
using System;

namespace Design_Develop_Deploy_Project.UI;

public class StudentMenu
{
	private readonly StudentRepository studentRepo;
	private readonly StatusRepository statusRepo;
	private readonly MeetingRepository meetingRepo;
	private readonly Student student;
	public StudentMenu(User user, string connectionString)
	{
        studentRepo = new StudentRepository(connectionString);
        statusRepo = new StatusRepository(connectionString);
        meetingRepo = new MeetingRepository(connectionString);
        student = studentRepo.GetStudentByEmail(user.email);
    }

	public void ShowStudentMenu() 
	{
        if ((DateTime.UtcNow - student.last_status_update.Value).TotalDays > 7) 
        {
            Console.WriteLine($"{student.first_name} it has been more than a week since your last status update, please let us know hopw you're feeling");
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

            switch (choice)
            {
                case 1:
                    StudentService.ViewStatus(student);
                    ConsoleHelper.Pause();
                    break;
                case 2:
                    StudentService.UpdateStatus(student);
                    ConsoleHelper.Pause();
                    break;
                case 3:
                    StudentService.BookMeeting(student);
                    ConsoleHelper.Pause();
                    break;
                case 4:
                    StudentService.ViewMeetings(student);
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
	}
}
