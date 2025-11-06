using Design_Develop_Deploy_Project.Repos;
using Design_Develop_Deploy_Project.Utilities;
using System;
using System.ComponentModel.Design;

namespace Design_Develop_Deploy_Project.Services;

public class TutorService
{
	private readonly StudentRepository studentRepo;
	private readonly SupervisorRepository supervisorRepo;
    readonly InteractionRepository _interactionRepo;

    public TutorService(string connectionString)
	{
		studentRepo = new StudentRepository(connectionString);
		supervisorRepo = new SupervisorRepository(connectionString);
        _interactionRepo = new InteractionRepository(connectionString);
    }

    public void ViewAllStudents()
    {
        Console.Clear();
        Console.WriteLine("=========== All Students ===========\n");

        var interactions = _interactionRepo.GetAllStudentInteractions();

        if (interactions == null || interactions.Count == 0)
        {
            Console.WriteLine("No students found in the system.");
            ConsoleHelper.Pause();
            return;
        }

        foreach (var (student, total) in interactions)
        {
            Console.WriteLine($"ID: {student.student_id}");
            Console.WriteLine($"Name: {student.first_name} {student.last_name}");
            Console.WriteLine($"Supervisor ID: {student.supervisor_id}");
            Console.WriteLine($"Wellbeing: {student.wellbeing_score}/10");
            Console.WriteLine($"Last Updated: {(student.last_status_update.HasValue ? student.last_status_update.Value.ToString("dd MMM yyyy") : "No record")}");
            Console.WriteLine($"Total Interactions: {total}");
            Console.WriteLine("---------------------------------------------");
        }

        ConsoleHelper.Pause("Press any key to return to the menu...");
    }



    public void ViewAllSupervisors()
    {
        Console.Clear();
        Console.WriteLine("=========== All Supervisors ===========\n");

        var interactions = _interactionRepo.GetAllSupervisorInteractions();

        if (interactions == null || interactions.Count == 0)
        {
            Console.WriteLine("No supervisors found in the system.");
            ConsoleHelper.Pause();
            return;
        }

        foreach (var (supervisor, total) in interactions)
        {
            Console.WriteLine($"ID: {supervisor.supervisor_id}");
            Console.WriteLine($"Name: {supervisor.first_name} {supervisor.last_name}");
            Console.WriteLine($"Email: {supervisor.email}");
            Console.WriteLine($"Meetings Booked: {supervisor.meetings_booked_this_month}");
            Console.WriteLine($"Wellbeing Checks: {supervisor.wellbeing_checks_this_month}");
            Console.WriteLine($"Total Interactions: {total}");

            if (supervisor.meetings_booked_this_month == 0 && supervisor.wellbeing_checks_this_month == 0)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("⚠️  No activity recorded this month.");
                Console.ResetColor();
            }

            Console.WriteLine("---------------------------------------");
        }

        ConsoleHelper.Pause("Press any key to return to the menu...");
    }


    public void ViewStudentsByWellBeingScore()
    {
        Console.Clear();
        Console.WriteLine("=========== Students by Wellbeing Score ===========\n");

        int minScore = -1, maxScore = -1;
        bool validInput = false;

        while (!validInput)
        {
            try
            {
                string minInput = ConsoleHelper.AskForInput("Enter minimum wellbeing score (0–10)");
                string maxInput = ConsoleHelper.AskForInput("Enter maximum wellbeing score (0–10)");

                minScore = int.Parse(minInput);
                maxScore = int.Parse(maxInput);

                if (minScore < 0 || minScore > 10 || maxScore < 0 || maxScore > 10 || minScore > maxScore)
                {
                    Console.WriteLine("\nScores must be between 0 and 10, and the minimum cannot be greater than the maximum.");
                    continue;
                }

                validInput = true;
            }
            catch (FormatException)
            {
                Console.WriteLine("\nInvalid input — please enter numeric values between 0 and 10.");
            }
        }

        var students = studentRepo.GetAllStudentsByWellBeingScore(minScore, maxScore);

        Console.Clear();
        Console.WriteLine($"=========== Students with Wellbeing Score {minScore}–{maxScore} ===========\n");

        if (students == null || students.Count == 0)
        {
            Console.WriteLine("No students found in the specified wellbeing score range.");
            ConsoleHelper.Pause();
            return;
        }

        foreach (var student in students)
        {
            Console.WriteLine($"ID: {student.student_id}");
            Console.WriteLine($"Name: {student.first_name} {student.last_name}");
            Console.WriteLine($"Wellbeing: {student.wellbeing_score}/10");
            Console.WriteLine($"Last Updated: {(student.last_status_update.HasValue ? student.last_status_update.Value.ToString("dd MMM yyyy") : "No record")}");
            Console.WriteLine("---------------------------------------------");
        }

        ConsoleHelper.Pause("Press any key to return to the menu...");
    }

}
