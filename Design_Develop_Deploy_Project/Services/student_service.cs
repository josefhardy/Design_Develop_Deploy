
using Design_Develop_Deploy_Project.Objects;
using Design_Develop_Deploy_Project.Repos;
using System;
using System.Collections.Generic;
using Design_Develop_Deploy_Project.Utilities;
using System.Data.SQLite;

namespace Design_Develop_Deploy_Project.Services;
public class StudentService
{
    private readonly StudentRepository _studentRepo;
    private readonly StatusRepository _statusRepo;
    private readonly MeetingRepository _meetingRepo;
    private readonly SupervisorRepository _supervisorRepo;
    private readonly Student student;

    public StudentService(string connectionString, Student _student)
    {
        student = _student;
        _studentRepo = new StudentRepository(connectionString);
        _statusRepo = new StatusRepository(connectionString);
        _meetingRepo = new MeetingRepository(connectionString);
        _supervisorRepo = new SupervisorRepository(connectionString);
    }

    public void ViewStatus()
    {
        Console.Clear();
        Console.WriteLine("======= Student Wellbeing Status =======");
        Console.WriteLine($"Student: {student.first_name} {student.last_name}");
        Console.WriteLine($"Current wellbeing score: {student.wellbeing_score}/10");
        Console.WriteLine($"Last updated: {(student.last_status_update?.ToString("dd MMM yyyy") ?? "No record")}");
        Console.WriteLine("========================================\n");

        bool choice = ConsoleHelper.GetYesOrNo("Would you like to update your wellbeing score now?");

        if (choice)
        {
            UpdateStatus();
        }
        else
        {
            Thread.Sleep(1500);
        }
    }

    public void UpdateStatus()
    {
        Console.Clear();
        Console.WriteLine("======= Update Wellbeing Status =======");
        Console.WriteLine($"Student: {student.first_name} {student.last_name}");
        Console.WriteLine($"Current wellbeing score: {student.wellbeing_score}/10");
        Console.WriteLine("=======================================\n");

        string new_score = ConsoleHelper.AskForInput("Please enter your new wellbeing score (0–10)");

        int new_score_int;
        try
        {
            new_score_int = int.Parse(new_score);
        }
        catch (FormatException)
        {
            Console.WriteLine("\nInvalid input — score must be a number between 0 and 10.");
            Thread.Sleep(1500);
            return;
        }

        if (new_score_int < 0 || new_score_int > 10)
        {
            Console.WriteLine("\nInvalid score — please enter a number between 0 and 10.");
            Thread.Sleep(1500);
            return;
        }

        _statusRepo.UpdateStudentWellbeing(student.student_id, new_score_int, student);
        student.wellbeing_score = new_score_int;

        Console.WriteLine("\nWellbeing status updated successfully!");
        Thread.Sleep(1500);
    }

    public void BookMeeting()
    {
        Console.Clear();
        var scheduler = new MeetingScheduler(_meetingRepo, _supervisorRepo);

        Supervisor supervisor = _supervisorRepo.GetSupervisorById(student.supervisor_id);

        // 1) Build a map: date -> list of slots
        var slotsByDate = new Dictionary<DateTime, List<(DateTime start, DateTime end)>>();

        for (int i = 0; i < 14; i++)
        {
            DateTime date = DateTime.Today.AddDays(i);
            if (date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
                continue;

            var slotsForDay = scheduler.FetchAvailableSlots(supervisor.supervisor_id, date);
            if (slotsForDay.Count > 0)
            {
                slotsByDate[date.Date] = slotsForDay;
            }
        }

        if (slotsByDate.Count == 0)
        {
            ConsoleHelper.PrintSection("No Available Slots", "No open slots in the next 2 weeks.");
            return;
        }

        // 2) Let student choose a day
        var days = slotsByDate.Keys.OrderBy(d => d).ToList();
        var dayOptions = days
            .Select(d => d.ToString("ddd dd MMM"))
            .ToList();

        int dayChoice = ConsoleHelper.PromptForChoice(dayOptions, "Choose a day with office hours:");
        var chosenDate = days[dayChoice - 1];

        // 3) Let student choose a 30-min slot on that day
        var daySlots = slotsByDate[chosenDate];
        var slotOptions = daySlots
            .Select(s => $"{s.start:HH:mm} – {s.end:HH:mm}")
            .ToList();

        int slotChoice = ConsoleHelper.PromptForChoice(slotOptions, "Choose a meeting slot:");
        var chosenSlot = daySlots[slotChoice - 1];

        string notes = ConsoleHelper.AskForInput("Add meeting notes (optional)");

        var meeting = new Meeting
        {
            student_id = student.student_id,
            supervisor_id = supervisor.supervisor_id,
            meeting_date = chosenSlot.start.Date,
            start_time = chosenSlot.start.TimeOfDay,
            end_time = chosenSlot.end.TimeOfDay,
            notes = notes
        };

        if (!scheduler.ValidateMeeting(meeting, out string message))
        {
            ConsoleHelper.PrintSection("Invalid Meeting", message);
            return;
        }

        bool success = _meetingRepo.AddMeeting(meeting);
        if (success)
        {
            ConsoleHelper.PrintSection("Success",
                $"Meeting booked for {chosenSlot.start:dddd dd MMM HH:mm} – {chosenSlot.end:HH:mm}");
        }
        else
        {
            ConsoleHelper.PrintSection("Error", "Failed to book the meeting.");
        }

    }

    public void ViewMeetings()
    {
        Console.Clear();
        Console.WriteLine("=========== View Meetings ===========");
        Console.WriteLine($"Student: {student.first_name} {student.last_name}");
        Console.WriteLine("=======================================\n");

        // 1️⃣ Fetch all meetings for the student
        var meetings = _meetingRepo.GetMeetingsByStudentId(student.student_id);

        // 2️⃣ No meetings?
        if (meetings.Count == 0)
        {
            Console.WriteLine("You currently have no meeting booked.\n");
            bool bookNow = ConsoleHelper.GetYesOrNo("Would you like to book a meeting now?");
            if (bookNow)
            {
                BookMeeting();
            }
            return;
        }

        var supervisor = _supervisorRepo.GetSupervisorById(student.supervisor_id);

        // 3️⃣ Show list of meetings
        Console.WriteLine("=========== Your Meetings ===========");

        var menuOptions = new List<string>();
        for (int i = 0; i < meetings.Count; i++)
        {
            var m = meetings[i];
            string option =
                $"{m.meeting_date:ddd dd MMM} | {m.start_time:hh\\:mm}-{m.end_time:hh\\:mm} | " +
                $"Notes: {(string.IsNullOrWhiteSpace(m.notes) ? "None" : m.notes)}";

            menuOptions.Add(option);
            Console.WriteLine($"{i + 1}. {option}");
        }

        Console.WriteLine("\n=======================================");

        // 4️⃣ Ask user which meeting to manage
        int meetingChoice = ConsoleHelper.PromptForChoice(menuOptions, "Select a meeting:");

        var selectedMeeting = meetings[meetingChoice - 1];
        Console.Clear();

        // 5️⃣ Meeting actions
        int action = ConsoleHelper.PromptForChoice(
            new List<string> { "Reschedule meeting", "Cancel meeting", "Return to menu" },
            "What would you like to do?"
        );

        // 6️⃣ Handle actions
        if (action == 1) // Reschedule
        {
            Console.Clear();
            Console.WriteLine("=========== Reschedule Meeting ===========");
            Console.WriteLine($"Your current meeting is on {selectedMeeting.meeting_date:dddd dd MMM} " +
                              $"from {selectedMeeting.start_time:hh\\:mm} to {selectedMeeting.end_time:hh\\:mm}.");
            Console.WriteLine("==========================================\n");

            bool confirmReschedule = ConsoleHelper.GetYesOrNo("Would you like to find a new time?");
            if (confirmReschedule)
            {
                Console.WriteLine("\nLet's reschedule your meeting.");
                BookMeeting();

                // Delete OLD meeting after the new one is booked
                _meetingRepo.DeleteMeeting(selectedMeeting.meeting_id);
            }
            else
            {
                Console.WriteLine("\nReschedule cancelled.");
                Thread.Sleep(1500);
            }
        }
        else if (action == 2) // Cancel
        {
            bool confirm = ConsoleHelper.GetYesOrNo("Are you sure you want to cancel this meeting?");
            if (confirm)
            {
                _meetingRepo.DeleteMeeting(selectedMeeting.meeting_id);
                Console.WriteLine("\nMeeting cancelled successfully.");
            }
            else
            {
                Console.WriteLine("\nMeeting cancellation aborted.");
            }
        }
    }

}