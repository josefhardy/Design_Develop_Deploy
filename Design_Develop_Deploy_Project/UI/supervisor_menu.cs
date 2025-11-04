using System;                               
using Design_Develop_Deploy_Project.Repos;
using Design_Develop_Deploy_Project.Objects;
using Design_Develop_Deploy_Project.Utilities;
using Design_Develop_Deploy_Project.Services;
using System.Runtime.CompilerServices;

namespace Design_Develop_Deploy_Project.UI;

public class SupervisorMenu
{
    private readonly string _connectionString;

    private readonly SupervisorRepository supervisorRepo;
	private readonly UserRepository userRepo;
	private readonly Supervisor supervisor;
	private readonly MeetingRepository meetingRepo;

	public SupervisorMenu(User user, string connectionstring)
	{
        _connectionString = connectionstring;
        supervisorRepo = new SupervisorRepository(connectionstring);
		userRepo = new UserRepository(connectionstring);
		meetingRepo = new MeetingRepository(connectionstring);
		supervisor = supervisorRepo.GetSupervisorByEmail(user.email);
	}

	public void ShowSupervisorMenu()
	{
        var supervisorService = new SupervisorService(_connectionString, supervisor);

        bool update_office_hours, update_well_being_check;

        bool updateOfficeHours = false;
        bool updateWellbeingCheck = false;
        List<string> reminders = new();

        if (supervisor.last_office_hours_update == null ||
            (DateTime.UtcNow - supervisor.last_office_hours_update.Value).TotalDays > 7)
        {
            updateOfficeHours = true;
            reminders.Add("your office hours");
        }

        if (supervisor.last_wellbeing_check == null ||
            (DateTime.UtcNow - supervisor.last_wellbeing_check.Value).TotalDays > 7)
        {
            updateWellbeingCheck = true;
            reminders.Add("your wellbeing check");
        }

        if (reminders.Count > 0)
        {
            string joined = string.Join(" and ", reminders);
            Console.WriteLine($"{supervisor.first_name}, it has been more than a week since your last {joined}. Please update them soon.");
        }

        bool exit = false;
        while (!exit) 
        {
            var menuItems = new List<string>
            {
                "View all your students",
                "View details of specific student",
                "View inactive students", 
                "Book a meeting with a student",
                "View your meetings",
                "Update office hours",
                "View your performance metrics",
                "Logout"
            };

            Console.Clear();
            Console.WriteLine($"Welcome {supervisor.first_name}!");
            int choice = ConsoleHelper.PromptForChoice(menuItems, "====== Supervisor Menu ======");

            switch(choice)
            {
                case 1:
                    supervisorService.ViewAllStudents();
                    ConsoleHelper.Pause();
                    break;
                case 2:
                    supervisorService.ViewStudentDetails();
                    ConsoleHelper.Pause();
                    break;
                case 3:
                    supervisorService.ViewInactiveStudents();
                    ConsoleHelper.Pause();
                    break;
                case 4:
                    supervisorService.BookMeeting();
                    ConsoleHelper.Pause();
                    break;
                case 5:
                    supervisorService.ViewMeetings();
                    ConsoleHelper.Pause();
                    break;
                case 6:
                    supervisorService.UpdateOfficeHours();
                    ConsoleHelper.Pause();
                    break;
                case 7:
                    supervisorService.ViewPerformanceMetrics();
                    ConsoleHelper.Pause();
                    break;
                case 8:
                    exit = true;
                    break;
                default:
                    Console.WriteLine("Invalid choice, please try again");
                    break;
            }
        }

    }
}
