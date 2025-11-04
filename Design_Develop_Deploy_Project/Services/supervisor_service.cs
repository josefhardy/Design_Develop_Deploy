
using Design.Develop.Deploy.Repos;
using Design_Develop_Deploy_Project.Objects;
using Design_Develop_Deploy_Project.Repos;
using Design_Develop_Deploy_Project.Utilities;
using System;

namespace Design_Develop_Deploy_Project.Services;
public class SupervisorService
{
    private readonly StudentRepository _studentRepo;
    private readonly StatusRepository _statusRepo;
    private readonly MeetingRepository _meetingRepo;
    private readonly SupervisorRepository _supervisorRepo;
    private readonly Supervisor supervisor;
    public SupervisorService(string connectionString, Supervisor _supervisor)
	{
        _studentRepo = new StudentRepository(connectionString);
        _statusRepo = new StatusRepository(connectionString);
        _meetingRepo = new MeetingRepository(connectionString);
        _supervisorRepo = new SupervisorRepository(connectionString);
        supervisor = _supervisor;
    }

    public void ViewAllStudents()
    {
        Console.Clear();
        Console.WriteLine("=========== My Students ===========");
        Console.WriteLine($"Supervisor: {supervisor.first_name} {supervisor.last_name}");
        Console.WriteLine("===================================\n");

        var students = _studentRepo.GetAllStudentsUnderSpecificSupervisor(supervisor.supervisor_id);

        if (students == null || students.Count == 0)
        {
            Console.WriteLine("You currently have no assigned students.");
            Console.WriteLine("\nReturning to menu...");
            Thread.Sleep(1500);
            return;
        }

        // Sort students by wellbeing score (lowest first)
        students = students.OrderBy(s => s.wellbeing_score).ToList();

        Console.WriteLine("Students (sorted by wellbeing score):\n");
        foreach (var student in students)
        {
            Console.WriteLine($"ID: {student.student_id} | Name: {student.first_name} {student.last_name} | Wellbeing: {student.wellbeing_score}/10");
        }

        Console.WriteLine("\n===================================");
        Console.WriteLine("Press any key to return to the main menu...");
        Console.ReadKey();
    }

    public void ViewStudentDetails()//needs to be checked 
    {
        Console.Clear();
        Console.WriteLine("=========== Student Details ===========");
        Console.Write("Enter Student ID: ");
        if (!int.TryParse(Console.ReadLine(), out int studentId))
        {
            Console.WriteLine("Invalid Student ID. Returning to menu...");
            Thread.Sleep(1500);
            return;
        }

        var student = _studentRepo.GetStudentById(studentId);

        if (student == null) 
        {
            Console.WriteLine("Student not found. Returning to menu...");
            Thread.Sleep(1500);
            return;
        }

        Console.WriteLine($"\nName: {student.first_name} {student.last_name}");
        Console.WriteLine($"Wellbeing Score: {student.wellbeing_score}/10");
        Console.WriteLine($"Last Status Update: {(student.last_status_update.HasValue ? student.last_status_update.Value.ToString("g") : "N/A")}");
        Console.WriteLine("======================================");
        
        bool answer = ConsoleHelper.GetYesOrNo("Would you like to book a meeting with this student?");
        if(answer)
        {
            BookMeeting(student);
        }
        else
        {
            Console.WriteLine("\nReturning to the main menu...");
            Thread.Sleep(1500);
        }
    }

    public void BookMeeting(Student student = null)//needs to be checked
    {
        Console.Clear();
        if (student == null) 
        {
            var students = _studentRepo.GetAllStudentsUnderSpecificSupervisor(supervisor.supervisor_id);
            List<string> student_details = students
                .Select(s => $"ID: {s.student_id} | Name: {s.first_name} {s.last_name}")
                .ToList();
            int choice = ConsoleHelper.PromptForChoice(student_details, "Select a student to book a meeting with:");
  
            student = _studentRepo.GetStudentById(students[choice -1 ].student_id);
        }

        Console.WriteLine($"\nBooking meeting with {student.first_name} {student.last_name} (ID: {student.student_id})");
        Console.WriteLine("======================================");

        var availableSlots = new List<(DateTime start, DateTime end)>();

        for (int i = 0; i < 14; i++)
        {
            DateTime date = DateTime.Today.AddDays(i);
            if (date.DayOfWeek == DayOfWeek.Saturday || date.DayOfWeek == DayOfWeek.Sunday)
                continue;

            var slotsForThatDay = _meetingRepo.FetchAvailableSlots(supervisor.supervisor_id, date);
            availableSlots.AddRange(slotsForThatDay);
        }

        availableSlots = availableSlots.OrderBy(s => s.start).ToList();
        if (availableSlots.Count == 0)
        {
            Console.WriteLine("No available meeting slots in the next 2 weeks.");
            Console.WriteLine("\nReturning to main menu...");
            Thread.Sleep(1500);
            return;
        }

        var slotStrings = new List<string>();
        foreach (var slot in availableSlots)
        {
            slotStrings.Add($"{slot.start:ddd dd MMM HH:mm} – {slot.end:HH:mm}");
        }

        int slot_choice = ConsoleHelper.PromptForChoice(slotStrings, "Available Meeting Slots:");
        slot_choice -= 1;

        var selectedSlot = availableSlots[slot_choice];

        Console.WriteLine("Do you have any notes for this meeting? (Leave blank for none)");
        string notes = Console.ReadLine()?.Trim();

        Console.Clear();
        Console.WriteLine("========== Meeting Confirmation ==========");
        Console.WriteLine($"Date: {selectedSlot.start:dddd dd MMM}");
        Console.WriteLine($"Time: {selectedSlot.start:HH:mm} – {selectedSlot.end:HH:mm}");
        Console.WriteLine($"Supervisor: {supervisor.first_name} {supervisor.last_name}");
        Console.WriteLine("==========================================\n");

        bool confirm = ConsoleHelper.GetYesOrNo("Confirm booking this meeting?");

        if (!confirm)
        {
            Console.WriteLine("\nMeeting booking was cancelled.");
            Console.WriteLine("Returning to menu...");
            Thread.Sleep(1500);
            return;
        }

        var meeting = new Meeting
        {
            student_id = student.student_id,
            supervisor_id = supervisor.supervisor_id,
            meeting_date = selectedSlot.start.Date,
            start_time = selectedSlot.start.TimeOfDay,
            end_time = selectedSlot.end.TimeOfDay,
            notes = notes
        };

        bool success = _meetingRepo.AddMeeting(meeting);

        if (success)
        {
            Console.WriteLine($"\nMeeting booked for {selectedSlot.start:dddd dd MMM HH:mm} – {selectedSlot.end:HH:mm}");
        }
        else
        {
            Console.WriteLine("\nCould not book the meeting. It may have been taken or an error occurred.");
        }

        Console.WriteLine("Returning to menu...");
        Thread.Sleep(1500);
    }

    public void ViewMeetings()
    {
        Console.Clear();
        Console.WriteLine("=========== My Meetings ===========");
        Console.WriteLine($"Supervisor: {supervisor.first_name} {supervisor.last_name}");
        Console.WriteLine("===================================\n");

        var meetings = _meetingRepo.GetMeetingsBySupervisorId(supervisor.supervisor_id);

        if (meetings == null || meetings.Count == 0)
        {
            Console.WriteLine("You have no upcoming meetings.");
            Console.WriteLine("Returning to menu...");
            Thread.Sleep(1500);
            return;
        }

        List<string> meetings_details = meetings
            .Select(m => {
                var student = _studentRepo.GetStudentById(m.student_id);
                string studentName = student != null ? $"{student.first_name} {student.last_name}" : "Unknown";
                return $"Date: {m.meeting_date:ddd dd MMM HH:mm} | Student: {studentName} | Notes: {(string.IsNullOrWhiteSpace(m.notes) ? "None" : m.notes)}";
            }).ToList();

        for (int i = 0; i < meetings_details.Count; i++)
        {
            Console.WriteLine($"{i + 1}. {meetings_details[i]}");
        }

        Console.WriteLine("\n===================================");

        int meeting_choice = ConsoleHelper.PromptForChoice(meetings_details, "Select a meeting to manage:");
        var selected_meeting = meetings[meeting_choice - 1];
        Console.Clear();

        var manageOptions = new List<string>
    {
        "Cancel Meeting",
        "Reschedule Meeting",
        "Return to Main Menu"
    };

        int manage_choice = ConsoleHelper.PromptForChoice(manageOptions, "Manage Meeting:");

        switch (manage_choice)
        {
            case 1:
                bool confirmCancel = ConsoleHelper.GetYesOrNo("Are you sure you want to cancel this meeting?");
                if (confirmCancel)
                {
                    _meetingRepo.DeleteMeeting(selected_meeting.meeting_id);
                    Console.WriteLine("Meeting cancelled successfully.");
                }
                else
                {
                    Console.WriteLine("Meeting cancellation aborted.");
                }
                ConsoleHelper.Pause();
                break;

            case 2:
                Console.Clear();
                Console.WriteLine("=========== Reschedule Meeting ===========");
                Console.WriteLine($"Your current meeting is on {selected_meeting.meeting_date:dddd dd MMM} " +
                                  $"from {selected_meeting.start_time:hh\\:mm} to {selected_meeting.end_time:hh\\:mm}.");
                Console.WriteLine("==========================================\n");

                bool confirmReschedule = ConsoleHelper.GetYesOrNo("Would you like to find a new time?");
                if (confirmReschedule)
                {
                    Console.WriteLine("\nLet's reschedule your meeting.");
                    BookMeeting();

                    // Delete the old meeting after successful booking
                    _meetingRepo.DeleteMeeting(selected_meeting.meeting_id);
                }
                else
                {
                    Console.WriteLine("\nReschedule cancelled. Returning to menu...");
                }
                ConsoleHelper.Pause();
                break;

            case 3:
                Console.WriteLine("Returning to main menu...");
                Thread.Sleep(1500);
                return;
        }

        Console.WriteLine("Returning to menu...");
        Thread.Sleep(1500);
    }


    public void UpdateOfficeHours()
    {
        
    }

    public void ViewPerformanceMetrics()
    {
        
    }

    public void ViewInactiveStudents()
    {

    }
}
