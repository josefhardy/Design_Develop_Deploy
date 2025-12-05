using Design_Develop_Deploy_Project.Objects;
using Design_Develop_Deploy_Project.Repos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Design_Develop_Deploy_Project.Utilities;

namespace Design_Develop_Deploy_Project.Services;
public class StudentService
{
    private readonly StudentRepository _studentRepo;
    private readonly StatusRepository _statusRepo;
    private readonly MeetingRepository _meetingRepo;
    private readonly SupervisorRepository _supervisorRepo;
    private readonly Student student;
    public bool _testMode;

    public StudentService(string connectionString, Student _student, bool testMode = false)
    {
        student = _student;
        _testMode = testMode;
        _studentRepo = new StudentRepository(connectionString);
        _statusRepo = new StatusRepository(connectionString);
        _meetingRepo = new MeetingRepository(connectionString);
        _supervisorRepo = new SupervisorRepository(connectionString);
    }

    public void ViewStatus()
    {
        if (!_testMode)
        {
            Console.Clear();
            Console.WriteLine("======= Student Wellbeing Status =======");
            Console.WriteLine($"Student: {student.first_name} {student.last_name}");
            Console.WriteLine($"Current wellbeing score: {student.wellbeing_score}/10");
            Console.WriteLine($"Last updated: {(student.last_status_update?.ToString("dd MMM yyyy") ?? "No record")}");
            Console.WriteLine("========================================\n");
        }
        bool choice = _testMode ? false : ConsoleHelper.GetYesOrNo("Would you like to update your wellbeing score now?");

        if (choice)
        {
            UpdateStatus();
        }
        else
        {
            Thread.Sleep(1500);
        }
    }

    public void UpdateStatus(int? testScore = null)
    {
        int new_score_int = testScore ?? student.wellbeing_score;

        // Only call repo if NOT in test mode
        if (!_testMode)
        {
            Console.Clear();
            Console.WriteLine("======= Update Wellbeing Status =======");
            Console.WriteLine($"Student: {student.first_name} {student.last_name}");
            Console.WriteLine($"Current wellbeing score: {student.wellbeing_score}/10");
            Console.WriteLine("=======================================\n");

            string input = testScore.HasValue ? testScore.Value.ToString() : ConsoleHelper.AskForInput("Please enter your new wellbeing score (0–10)");
            if (!int.TryParse(input, out new_score_int) || new_score_int < 0 || new_score_int > 10)
            {
                ConsoleHelper.WriteInColour("\nInvalid score — please enter a number between 0 and 10.", "Red");
                Thread.Sleep(1500);
                return;
            }

            _statusRepo.UpdateStudentWellbeing(student.student_id, new_score_int, student);
            ConsoleHelper.WriteInColour("\nWellbeing status updated successfully!", "Green");
            Thread.Sleep(1500);
        }

        // Update in-memory student in both test and normal mode
        student.wellbeing_score = new_score_int;
    }




    public void BookMeeting()
    {
        if (!_testMode) Console.Clear();

        var scheduler = new MeetingScheduler(_meetingRepo, _supervisorRepo);
        Supervisor supervisor = _supervisorRepo.GetSupervisorById(student.supervisor_id);

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
            if (!_testMode) ConsoleHelper.PrintSection("No Available Slots", "No open slots in the next 2 weeks.", "Yellow");
            return;
        }

        var days = slotsByDate.Keys.OrderBy(d => d).ToList();
        var dayOptions = days.Select(d => d.ToString("ddd dd MMM")).ToList();

        int dayChoice = _testMode ? 1 : ConsoleHelper.PromptForChoice(dayOptions, "Choose a day with office hours:");
        var chosenDate = days[dayChoice - 1];

        var daySlots = slotsByDate[chosenDate];
        var slotOptions = daySlots.Select(s => $"{s.start:HH:mm} – {s.end:HH:mm}").ToList();

        int slotChoice = _testMode ? 1 : ConsoleHelper.PromptForChoice(slotOptions, "Choose a meeting slot:");
        var chosenSlot = daySlots[slotChoice - 1];

        string notes = _testMode ? "" : ConsoleHelper.AskForInput("Add meeting notes (optional)");

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
            if (!_testMode) ConsoleHelper.PrintSection("Invalid Meeting", message, "Yellow");
            return;
        }

        bool success = _meetingRepo.AddMeeting(meeting);
        if (!_testMode)
        {
            if (success)
                ConsoleHelper.PrintSection("Success", $"Meeting booked for {chosenSlot.start:dddd dd MMM HH:mm} – {chosenSlot.end:HH:mm}", "Green");
            else
                ConsoleHelper.WriteInColour("Error, failed to book the meeting", "Red");
        }
    }

    public void ViewMeetings()
    {
        if (!_testMode) Console.Clear();

        Console.WriteLine("=========== View Meetings ===========");
        Console.WriteLine($"Student: {student.first_name} {student.last_name}");
        Console.WriteLine("=======================================\n");

        var meetings = _meetingRepo.GetMeetingsByStudentId(student.student_id);

        if (meetings.Count == 0)
        {
            if (!_testMode) ConsoleHelper.WriteInColour("You currently have no meeting booked.\n", "Yellow");
            bool bookNow = _testMode ? false : ConsoleHelper.GetYesOrNo("Would you like to book a meeting now?");
            if (bookNow) BookMeeting();
            return;
        }

        var supervisor = _supervisorRepo.GetSupervisorById(student.supervisor_id);

        Console.WriteLine("=========== Your Meetings ===========");
        var menuOptions = new List<string>();
        for (int i = 0; i < meetings.Count; i++)
        {
            var m = meetings[i];
            string option = $"{m.meeting_date:ddd dd MMM} | {m.start_time:hh\\:mm}-{m.end_time:hh\\:mm} | " +
                            $"Notes: {(string.IsNullOrWhiteSpace(m.notes) ? "None" : m.notes)}";
            menuOptions.Add(option);
            Console.WriteLine($"{i + 1}. {option}");
        }
        Console.WriteLine("\n=======================================");

        int meetingChoice = _testMode ? 0 : ConsoleHelper.PromptForChoice(menuOptions, "Select a meeting:");
        var selectedMeeting = meetings[meetingChoice];

        if (!_testMode) Console.Clear();

        int action = _testMode ? 2 : ConsoleHelper.PromptForChoice(
            new List<string> { "Reschedule meeting", "Cancel meeting", "Return to menu" },
            "What would you like to do?"
        );

        if (action == 1) // Reschedule
        {
            if (!_testMode) Console.Clear();
            if (!_testMode) Console.WriteLine("=========== Reschedule Meeting ===========");
            if (!_testMode) Console.WriteLine($"Your current meeting is on {selectedMeeting.meeting_date:dddd dd MMM} " +
                                             $"from {selectedMeeting.start_time:hh\\:mm} to {selectedMeeting.end_time:hh\\:mm}.");
            if (!_testMode) Console.WriteLine("==========================================\n");

            bool confirmReschedule = _testMode ? false : ConsoleHelper.GetYesOrNo("Would you like to find a new time?");
            if (confirmReschedule)
            {
                if (!_testMode) Console.WriteLine("\nLet's reschedule your meeting.");
                BookMeeting();
                _meetingRepo.DeleteMeeting(selectedMeeting.meeting_id);
            }
            else if (!_testMode)
            {
                ConsoleHelper.WriteInColour("\nReschedule cancelled.", "Green");
                Thread.Sleep(1500);
            }
        }
        else if (action == 2) // Cancel
        {
            bool confirm = _testMode ? true : ConsoleHelper.GetYesOrNo("Are you sure you want to cancel this meeting?");
            if (confirm) _meetingRepo.DeleteMeeting(selectedMeeting.meeting_id);
            if (!_testMode)
            {
                ConsoleHelper.WriteInColour(confirm ? "\nMeeting cancelled successfully." : "\nMeeting cancellation aborted.", confirm ? "Green" : "Yellow");
            }
        }
    }
}
