using System;
using System.Data.SQLite;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;

namespace Design_Develop_Deploy_Project.Tables
{
    public static class DatabaseSeeder
    {
        public static void Seed()
        {
            string dbpath = Path.GetFullPath(
                Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "Tables", "Project_database.db")
            );

            string connString = $"Data Source={dbpath};Version=3;";
            Console.WriteLine("[DEBUG] Seeder using DB: " + dbpath);

            using (var conn = new SQLiteConnection(connString))
            {
                conn.Open();

                using (var pragmaCommand = new SQLiteCommand("PRAGMA foreign_keys = ON;", conn))
                    pragmaCommand.ExecuteNonQuery();

                // Prevent duplicate seeding
                using (var checkCmd = new SQLiteCommand("SELECT COUNT(*) FROM Users;", conn))
                {
                    long count = (long)checkCmd.ExecuteScalar();
                    if (count > 0)
                    {
                        Console.WriteLine("Database already contains data — skipping seeding.");
                        return;
                    }
                }

                Console.WriteLine("🌱 Seeding database with realistic university data...");
                var random = new Random();

                static string GeneratePassword(Random random, int length = 10)
                {
                    const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
                    return new string(Enumerable.Repeat(chars, length)
                        .Select(s => s[random.Next(s.Length)]).ToArray());
                }

                string[] officeHoursSamples =
                {
            "Monday 09:00-11:00, Thursday 13:00-15:00",
            "Tuesday 10:00-12:00, Friday 09:00-11:00",
            "Wednesday 09:00-11:00, Friday 14:00-16:00",
            "Monday 14:00-16:00, Thursday 09:00-11:00",
            "Tuesday 13:00-15:00, Thursday 10:00-12:00"
        };

                // ----------------------
                // 1. Senior Tutor
                // ----------------------
                long seniorTutorUserId;
                string tutorPass = GeneratePassword(random);

                using (var cmd = new SQLiteCommand(
                    "INSERT INTO Users (first_name, last_name, email, password, role) " +
                    "VALUES (@f, @l, @e, @p, 'senior_tutor'); SELECT last_insert_rowid();", conn))
                {
                    cmd.Parameters.AddWithValue("@f", "Sarah");
                    cmd.Parameters.AddWithValue("@l", "Thompson");
                    cmd.Parameters.AddWithValue("@e", "sarah.thompson@hull.ac.uk");
                    cmd.Parameters.AddWithValue("@p", tutorPass);
                    seniorTutorUserId = (long)cmd.ExecuteScalar();
                }

                Console.WriteLine($"👩‍🏫 Senior Tutor created — Password: {tutorPass}");

                // ----------------------
                // 2. Supervisors
                // ----------------------
                var supervisorList = new List<(long supervisorId, string fullName, string officeHours)>();

                var supNames = new (string, string)[]
                {
            ("Emily", "Zhang"),
            ("David", "Patel"),
            ("Rachel", "Hughes"),
            ("Michael", "Carter"),
            ("Anna", "Nguyen")
                };

                foreach (var (first, last) in supNames)
                {
                    string email = $"{first.ToLower()}.{last.ToLower()}@hull.ac.uk";
                    string officeHours = officeHoursSamples[random.Next(officeHoursSamples.Length)];
                    string password = GeneratePassword(random);

                    long userId;
                    using (var userCmd = new SQLiteCommand(
                        "INSERT INTO Users (first_name, last_name, email, password, role) " +
                        "VALUES (@f, @l, @e, @p, 'supervisor'); SELECT last_insert_rowid();", conn))
                    {
                        userCmd.Parameters.AddWithValue("@f", first);
                        userCmd.Parameters.AddWithValue("@l", last);
                        userCmd.Parameters.AddWithValue("@e", email);
                        userCmd.Parameters.AddWithValue("@p", password);
                        userId = (long)userCmd.ExecuteScalar();
                    }

                    long supervisorId;
                    using (var supCmd = new SQLiteCommand(
                        @"INSERT INTO Supervisors (user_id, office_hours, last_office_hours_update, last_wellbeing_check) 
                  VALUES (@u, @o, date('now'), date('now')); 
                  SELECT last_insert_rowid();", conn))
                    {
                        supCmd.Parameters.AddWithValue("@u", userId);
                        supCmd.Parameters.AddWithValue("@o", officeHours);
                        supervisorId = (long)supCmd.ExecuteScalar();
                    }

                    supervisorList.Add((supervisorId, $"{first} {last}", officeHours));
                    Console.WriteLine($"🧑‍🏫 Supervisor created: {first} {last} | Office Hours: {officeHours} | Password: {password}");
                }

                // ----------------------
                // 3. Students
                // ----------------------
                var maleNames = new List<string> { "James", "Liam", "Noah", "Ethan", "Oliver", "Lucas", "Henry", "Mason", "Jack", "William" };
                var femaleNames = new List<string> { "Emma", "Olivia", "Ava", "Sophia", "Isabella", "Mia", "Charlotte", "Amelia", "Harper", "Ella" };
                string[] lastNames = { "Smith", "Johnson", "Brown", "Jones", "Taylor", "Wilson", "Clark", "Lee", "Harris", "Lopez" };

                var studentList = new List<(long studentId, long supervisorId)>();

                foreach (var sup in supervisorList)
                {
                    int numStudents = random.Next(5, 8);

                    for (int i = 0; i < numStudents; i++)
                    {
                        bool isFemale = random.NextDouble() < 0.5;

                        string first = isFemale && femaleNames.Count > 0
                            ? femaleNames[random.Next(femaleNames.Count)]
                            : maleNames[random.Next(maleNames.Count)];

                        string last = lastNames[random.Next(lastNames.Length)];
                        string email = $"{first.ToLower()}.{last.ToLower()}{random.Next(1000, 9999)}@hull.ac.uk";
                        string password = GeneratePassword(random);

                        int score = random.Next(2, 10);
                        string lastUpdate = DateTime.Now.AddDays(-random.Next(0, 30))
                            .ToString("yyyy-MM-dd HH:mm:ss");

                        long userId;
                        using (var userCmd = new SQLiteCommand(
                            "INSERT INTO Users (first_name, last_name, email, password, role) " +
                            "VALUES (@f, @l, @e, @p, 'student'); SELECT last_insert_rowid();", conn))
                        {
                            userCmd.Parameters.AddWithValue("@f", first);
                            userCmd.Parameters.AddWithValue("@l", last);
                            userCmd.Parameters.AddWithValue("@e", email);
                            userCmd.Parameters.AddWithValue("@p", password);
                            userId = (long)userCmd.ExecuteScalar();
                        }

                        long studentId;
                        using (var studCmd = new SQLiteCommand(
                            @"INSERT INTO Students (user_id, supervisor_id, wellbeing_score, last_status_update)
                      VALUES (@u, @s, @w, @d); SELECT last_insert_rowid();", conn))
                        {
                            studCmd.Parameters.AddWithValue("@u", userId);
                            studCmd.Parameters.AddWithValue("@s", sup.supervisorId);
                            studCmd.Parameters.AddWithValue("@w", score);
                            studCmd.Parameters.AddWithValue("@d", lastUpdate);
                            studentId = (long)studCmd.ExecuteScalar();
                        }

                        studentList.Add((studentId, sup.supervisorId));
                        Console.WriteLine($"🎓 Student created: {first} {last} under {sup.fullName}");
                    }
                }

                Console.WriteLine($"✅ Created {studentList.Count} students.");

                // ----------------------
                // 4. Meetings — FUTURE ONLY + RANDOM NOTES + NO DOUBLE BOOKING
                // ----------------------
                string[] meetingNotes =
                {
            "Progress check and wellbeing discussion.",
            "Want to discuss assignment.",
            "Just checking up.",
            "Need help with project work.",
            "Discuss timetable and workload.",
            "Clarifying assessment questions.",
            "Checking wellbeing and academic progress."
        };

                foreach (var (studentId, supervisorId) in studentList)
                {
                    // 50% chance to create a meeting
                    if (random.NextDouble() >= 0.5)
                        continue;

                    var supervisor = supervisorList.First(s => s.supervisorId == supervisorId);

                    var blocks = supervisor.officeHours
                        .Split(',', StringSplitOptions.RemoveEmptyEntries)
                        .Select(b => b.Trim())
                        .ToList();

                    // Choose one office-hour block at random: "Monday 09:00-11:00"
                    string chosenBlock = blocks[random.Next(blocks.Count)];
                    var parts = chosenBlock.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    string dayName = parts[0];
                    string timeRange = parts[1];

                    var times = timeRange.Split('-');
                    TimeSpan blockStart = TimeSpan.Parse(times[0]);
                    TimeSpan blockEnd = TimeSpan.Parse(times[1]);

                    // Build all 30-minute slots in that office block
                    var slots = new List<(TimeSpan start, TimeSpan end)>();
                    var slotStart = blockStart;

                    while (slotStart < blockEnd)
                    {
                        var slotEnd = slotStart.Add(TimeSpan.FromMinutes(30));
                        if (slotEnd > blockEnd)
                            break;

                        slots.Add((slotStart, slotEnd));
                        slotStart = slotStart.Add(TimeSpan.FromMinutes(30));
                    }

                    bool created = false;

                    // Randomize order of slots to avoid bias
                    foreach (var (slotS, slotE) in slots.OrderBy(_ => random.Next()))
                    {
                        // Pick a future date (1–14 days) that matches the office-hour day
                        DateTime date;
                        int safety = 0;

                        do
                        {
                            date = DateTime.Now.AddDays(random.Next(1, 15));
                            safety++;
                        }
                        while (!string.Equals(date.DayOfWeek.ToString(), dayName, StringComparison.OrdinalIgnoreCase)
                               && safety < 30);

                        if (safety >= 30)
                            continue; // Failed to find a matching day; try another slot

                        // Check if this exact slot is already taken for this supervisor & date
                        using (var checkCmd = new SQLiteCommand(
                            @"SELECT COUNT(*) FROM Meetings
                      WHERE supervisor_id = @sup
                        AND meeting_date = @d
                        AND start_time = @st
                        AND end_time = @et;", conn))
                        {
                            checkCmd.Parameters.AddWithValue("@sup", supervisorId);
                            checkCmd.Parameters.AddWithValue("@d", date.ToString("yyyy-MM-dd"));
                            checkCmd.Parameters.AddWithValue("@st", slotS.ToString(@"hh\:mm"));
                            checkCmd.Parameters.AddWithValue("@et", slotE.ToString(@"hh\:mm"));

                            long existing = (long)checkCmd.ExecuteScalar();
                            if (existing > 0)
                                continue; // slot already in use → try another
                        }

                        // Slot is free → insert meeting
                        using (var meetCmd = new SQLiteCommand(
                            @"INSERT INTO Meetings (student_id, supervisor_id, meeting_date, start_time, end_time, notes)
                      VALUES (@stu, @sup, @d, @st, @et, @n);", conn))
                        {
                            meetCmd.Parameters.AddWithValue("@stu", studentId);
                            meetCmd.Parameters.AddWithValue("@sup", supervisorId);
                            meetCmd.Parameters.AddWithValue("@d", date.ToString("yyyy-MM-dd"));
                            meetCmd.Parameters.AddWithValue("@st", slotS.ToString(@"hh\:mm"));
                            meetCmd.Parameters.AddWithValue("@et", slotE.ToString(@"hh\:mm"));
                            meetCmd.Parameters.AddWithValue("@n", meetingNotes[random.Next(meetingNotes.Length)]);
                            meetCmd.ExecuteNonQuery();
                        }

                        Console.WriteLine($"🗓️ Meeting seeded for Student {studentId} on {date:yyyy-MM-dd} {slotS}-{slotE}");
                        created = true;
                        break;
                    }

                    if (!created)
                    {
                        Console.WriteLine($"⚠ No available meeting slot for Student {studentId} (Supervisor {supervisorId})");
                    }
                }

                Console.WriteLine("🎉 Seeder Completed Successfully!");
            }
        }



        public static void WipeTable()
        {
            string dbpath = Path.GetFullPath(
                Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "Tables", "Project_database.db")
            );

            string connString = $"Data Source={dbpath};Version=3;";

            using (var conn = new SQLiteConnection(connString))
            {
                conn.Open();

                using (var pragmaCommand = new SQLiteCommand("PRAGMA foreign_keys = ON;", conn))
                    pragmaCommand.ExecuteNonQuery();

                // 1️⃣ Clear all data from tables
                using (var clearCmd = new SQLiteCommand(@"
            DELETE FROM Meetings;
            DELETE FROM Students;
            DELETE FROM Supervisors;
            DELETE FROM Users;
        ", conn))
                {
                    clearCmd.ExecuteNonQuery();
                    Console.WriteLine("🧹 Cleared all existing data from REAL database tables.");
                }

                // 2️⃣ Reset AUTOINCREMENT counters
                using (var resetSeqCmd = new SQLiteCommand(@"
            DELETE FROM sqlite_sequence WHERE name='Meetings';
            DELETE FROM sqlite_sequence WHERE name='Students';
            DELETE FROM sqlite_sequence WHERE name='Supervisors';
            DELETE FROM sqlite_sequence WHERE name='Users';
        ", conn))
                {
                    resetSeqCmd.ExecuteNonQuery();
                    Console.WriteLine("🔄 Reset AUTOINCREMENT counters for all tables.");
                }
            }
        }


        public static void PrintAllUsers()
        {
            string dbpath = Path.GetFullPath(
                Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "Tables", "Project_database.db")
            );
            string connString = $"Data Source={dbpath};Version=3;";

            using (var conn = new SQLiteConnection(connString))
            {
                conn.Open();

                string query = @"
        SELECT 
            u.user_id,
            u.first_name,
            u.last_name,
            u.email,
            u.password,
            u.role,
            s.supervisor_id AS supervisor_record_id,
            st.student_id AS student_record_id
        FROM Users u
        LEFT JOIN Supervisors s ON s.user_id = u.user_id
        LEFT JOIN Students st ON st.user_id = u.user_id
        ORDER BY u.user_id;
    ";

                using (var cmd = new SQLiteCommand(query, conn))
                using (var reader = cmd.ExecuteReader())
                {
                    Console.WriteLine("==============================================");
                    Console.WriteLine("USERS TABLE CONTENTS");
                    Console.WriteLine("==============================================");

                    if (!reader.HasRows)
                    {
                        Console.WriteLine("No users found in the database.");
                        return;
                    }

                    while (reader.Read())
                    {
                        int userId = reader.GetInt32(0);
                        string firstName = reader.GetString(1);
                        string lastName = reader.GetString(2);
                        string email = reader.GetString(3);
                        string password = reader.GetString(4);
                        string role = reader.GetString(5);

                        object supervisorIdObj = reader["supervisor_record_id"];
                        object studentIdObj = reader["student_record_id"];

                        Console.WriteLine("----------------------------------------------");
                        Console.WriteLine($"User ID: {userId}");
                        Console.WriteLine($"Name: {firstName} {lastName}");
                        Console.WriteLine($"Email: {email}");
                        Console.WriteLine($"Password: {password}");
                        Console.WriteLine($"Role: {role}");

                        if (role == "supervisor")
                        {
                            int supervisorId = supervisorIdObj != DBNull.Value ? Convert.ToInt32(supervisorIdObj) : -1;
                            Console.WriteLine($"Supervisor ID: {supervisorId}");
                        }

                        if (role == "student")
                        {
                            int studentId = studentIdObj != DBNull.Value ? Convert.ToInt32(studentIdObj) : -1;
                            Console.WriteLine($"Student ID: {studentId}");
                        }
                    }

                    Console.WriteLine("----------------------------------------------");
                    Console.WriteLine("All user records displayed successfully.");
                    Console.WriteLine("==============================================");
                }
            }
        }


    }
}
