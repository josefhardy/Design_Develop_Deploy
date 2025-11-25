using Design_Develop_Deploy_Project.Repos;
using Design_Develop_Deploy_Project.Utilities;
using System;
using System.ComponentModel.Design;

namespace Design_Develop_Deploy_Project.Services;

public class TutorService
{
	private readonly StudentRepository studentRepo;
	private readonly SupervisorRepository supervisorRepo;
    private readonly InteractionRepository _interactionRepo;
    private readonly StatusRepository statusRepo;

    public TutorService(string connectionString)
	{
		studentRepo = new StudentRepository(connectionString);
		supervisorRepo = new SupervisorRepository(connectionString);
        _interactionRepo = new InteractionRepository(connectionString);
        statusRepo = new StatusRepository(connectionString);
    }

    public void ViewAllStudents()
    {
        Console.Clear();
        Console.WriteLine("=========== All Students ===========");
        Console.WriteLine("Showing all students & interaction totals");
        Console.WriteLine("=====================================\n");

        var interactions = _interactionRepo.GetAllStudentInteractions();

        if (interactions == null || interactions.Count == 0)
        {
            ConsoleHelper.WriteInColour("No students found in the system.", "Yellow");
            return;
        }

        // TABLE HEADER
        Console.WriteLine("ID   | Name                     | Supervisor | Wellbeing | Last Update     | Interactions");
        Console.WriteLine("--------------------------------------------------------------------------------------------");

        // TABLE ROWS
        foreach (var (student, total) in interactions)
        {
            string fullName = $"{student.first_name} {student.last_name}";
            string lastUpdate = student.last_status_update.HasValue
                ? student.last_status_update.Value.ToString("dd MMM yyyy")
                : "No record";

            Console.WriteLine(
                $"{student.student_id,-4} | " +
                $"{fullName,-25} | " +
                $"{student.supervisor_id,-10} | " +
                $"{student.wellbeing_score,-9}/10 | " +
                $"{lastUpdate,-15} | " +
                $"{total}"
            );
        }

        Console.WriteLine("\n--------------------------------------------------------------------------------------------");
    }


    public void ViewAllSupervisors()
    {
        Console.Clear();
        Console.WriteLine("=========== All Supervisors ===========");
        Console.WriteLine("Showing system-wide supervisor activity");
        Console.WriteLine("========================================\n");

        var interactions = _interactionRepo.GetAllSupervisorInteractions();

        if (interactions == null || interactions.Count == 0)
        {
            ConsoleHelper.WriteInColour("No supervisors found in the system.", "Yellow");
            return;
        }

        // CLEAN, COMPACT TABLE HEADER
        Console.WriteLine("ID  | Name                 | Meetings | Checks | Total | Status");
        Console.WriteLine("--------------------------------------------------------------------");

        foreach (var (supervisor, total) in interactions)
        {
            string fullName = $"{supervisor.first_name} {supervisor.last_name}";
            string status;

            if (supervisor.meetings_booked_this_month == 0 &&
                supervisor.wellbeing_checks_this_month == 0)
            {
                status = "Inactive";
            }
            else
            {
                status = "Active";
            }

            // Print supervisor row in a clean, readable format
            Console.WriteLine(
                $"{supervisor.supervisor_id,-3} | " +
                $"{fullName,-20} | " +
                $"{supervisor.meetings_booked_this_month,-8} | " +
                $"{supervisor.wellbeing_checks_this_month,-6} | " +
                $"{total,-5} | " +
                $"{status}"
            );
        }

        Console.WriteLine("\n--------------------------------------------------------------------");
    }




    public void ViewStudentsByWellBeingScore()
    {
        Console.Clear();
        Console.WriteLine("=========== Students by Wellbeing Score ===========");
        Console.WriteLine("Filter students based on wellbeing score range");
        Console.WriteLine("===================================================\n");

        int minScore = -1, maxScore = -1;
        bool validInput = false;

        // GET VALID SCORE RANGE
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
                    ConsoleHelper.WriteInColour(
                        "\nScores must be between 0 and 10, and the minimum cannot be greater than the maximum.\n",
                        "Yellow"
                    );
                    continue;
                }

                validInput = true;
            }
            catch (FormatException)
            {
                ConsoleHelper.WriteInColour("\nInvalid input — please enter numeric values between 0 and 10.\n", "Red");
            }
        }

        // FETCH STUDENTS
        var students = statusRepo.GetAllStudentsByWellBeingScore(minScore, maxScore);

        Console.Clear();
        Console.WriteLine($"=========== Students with Wellbeing Score {minScore}–{maxScore} ===========\n");

        if (students == null || students.Count == 0)
        {
            ConsoleHelper.WriteInColour("No students found in the specified wellbeing score range.", "Yellow");
            return;
        }

        // TABLE HEADER
        Console.WriteLine("ID   | Name                     | Wellbeing | Last Update");
        Console.WriteLine("------------------------------------------------------------");

        // TABLE ROWS
        foreach (var s in students)
        {
            string fullName = $"{s.first_name} {s.last_name}";
            string lastUpdate = s.last_status_update.HasValue
                ? s.last_status_update.Value.ToString("dd MMM yyyy")
                : "No record";

            Console.WriteLine(
                $"{s.student_id,-4} | " +
                $"{fullName,-25} | " +
                $"{s.wellbeing_score,-9}/10 | " +
                $"{lastUpdate}"
            );
        }

        Console.WriteLine("\n------------------------------------------------------------");
    }


}
