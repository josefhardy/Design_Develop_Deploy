using System;
using System.Data.SQLite;
using System.Collections.Generic;
using System.Linq;

namespace Design_Develop_Deploy_Project.Tables
{
    public static class DatabaseSeeder
    {
        public static void Seed()
        {
            string dbpath = "Project_database.db";
            string connString = $"Data Source={dbpath};Version=3;";

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

                // ---- Data Sources ----
                string[] maleNames = { "James", "Liam", "Noah", "Ethan", "Oliver", "Lucas", "Henry", "Mason", "Jack", "William" };
                string[] femaleNames = { "Emma", "Olivia", "Ava", "Sophia", "Isabella", "Mia", "Charlotte", "Amelia", "Harper", "Ella" };
                string[] lastNames = { "Smith", "Johnson", "Brown", "Williams", "Jones", "Taylor", "Davis", "Clark", "Lee", "Harris", "Patel", "Nguyen", "Zhang", "Singh" };
                string[] wellbeingStatuses = { "Excellent", "Good", "Average", "Struggling", "Stressed", "Unwell" };
                var wellbeingMap = new Dictionary<string, int> {
                    { "Excellent", 9 }, { "Good", 8 }, { "Average", 6 }, { "Struggling", 4 }, { "Stressed", 3 }, { "Unwell", 2 }
                };

                string[] officeHours = {
                                         "Monday 10–12am",
                                         "Tuesday 2–4pm",
                                         "Wednesday 9–11am",
                                         "Thursday 1–3pm",
                                         "Friday 10–12am"
                                        };


                // ---- 1. Senior Tutor ----
                long seniorTutorUserId;
                using (var cmd = new SQLiteCommand(
                    "INSERT INTO Users (first_name, last_name, email, password, role) VALUES (@f, @l, @e, @p, 'senior_tutor'); SELECT last_insert_rowid();", conn))
                {
                    cmd.Parameters.AddWithValue("@f", "Sarah");
                    cmd.Parameters.AddWithValue("@l", "Thompson");
                    cmd.Parameters.AddWithValue("@e", "sarah.thompson@university.edu");
                    cmd.Parameters.AddWithValue("@p", "profsecure123");
                    seniorTutorUserId = (long)cmd.ExecuteScalar();
                }

                Console.WriteLine("👩‍🏫 Senior Tutor: Prof. Sarah Thompson created.");

                // ---- 2. Supervisors ----
                var supervisorList = new List<(long supervisorId, string fullName)>();
                var supNames = new (string, string)[] {
                    ("Emily", "Zhang"), ("David", "Patel"), ("Rachel", "Hughes"), ("Michael", "Carter"), ("Anna", "Nguyen")
                };

                for (int i = 0; i < supNames.Length; i++)
                {
                    string first = supNames[i].Item1;
                    string last = supNames[i].Item2;
                    string email = $"{first.ToLower()}.{last.ToLower()}@university.edu";
                    string office = officeHours[random.Next(officeHours.Length)];

                    long userId;
                    using (var userCmd = new SQLiteCommand(
                        "INSERT INTO Users (first_name, last_name, email, password, role) VALUES (@f, @l, @e, @p, 'supervisor'); SELECT last_insert_rowid();", conn))
                    {
                        userCmd.Parameters.AddWithValue("@f", first);
                        userCmd.Parameters.AddWithValue("@l", last);
                        userCmd.Parameters.AddWithValue("@e", email);
                        userCmd.Parameters.AddWithValue("@p", "teach123");
                        userId = (long)userCmd.ExecuteScalar();
                    }

                    long supervisorId;
                    using (var supCmd = new SQLiteCommand(
                        @"INSERT INTO Supervisors (user_id, last_office_hours_update, last_wellbeing_check, office_hours) 
                          VALUES (@u, date('now'), date('now'), @o); 
                          SELECT last_insert_rowid();", conn))
                    {
                        supCmd.Parameters.AddWithValue("@u", userId);
                        supCmd.Parameters.AddWithValue("@o", office);
                        supervisorId = (long)supCmd.ExecuteScalar();
                    }

                    supervisorList.Add((supervisorId, $"{first} {last}"));
                    Console.WriteLine($"🧑‍🏫 Supervisor created: Dr. {first} {last}, office hours {office}");
                }

                // ---- 3. Students ----
                var studentList = new List<(long studentId, long supervisorId, string name)>();
                foreach (var (supervisorId, supervisorName) in supervisorList)
                {
                    int numStudents = random.Next(5, 8);
                    for (int i = 0; i < numStudents; i++)
                    {
                        bool isFemale = random.NextDouble() < 0.5;
                        string first = isFemale ? femaleNames[random.Next(femaleNames.Length)] : maleNames[random.Next(maleNames.Length)];
                        string last = lastNames[random.Next(lastNames.Length)];
                        string email = $"{first.ToLower()}.{last.ToLower()}@student.university.edu";

                        string wellbeing = wellbeingStatuses[random.Next(wellbeingStatuses.Length)];
                        int score = wellbeingMap[wellbeing];

                        long userId;
                        using (var userCmd = new SQLiteCommand(
                            "INSERT INTO Users (first_name, last_name, email, password, role) VALUES (@f, @l, @e, @p, 'student'); SELECT last_insert_rowid();", conn))
                        {
                            userCmd.Parameters.AddWithValue("@f", first);
                            userCmd.Parameters.AddWithValue("@l", last);
                            userCmd.Parameters.AddWithValue("@e", email);
                            userCmd.Parameters.AddWithValue("@p", "student123");
                            userId = (long)userCmd.ExecuteScalar();
                        }

                        long studentId;
                        using (var studCmd = new SQLiteCommand(
                            @"INSERT INTO Students (user_id, supervisor_id, wellbeing_score, last_status_update)
                              VALUES (@u, @s, @w, @status); SELECT last_insert_rowid();", conn))
                        {
                            studCmd.Parameters.AddWithValue("@u", userId);
                            studCmd.Parameters.AddWithValue("@s", supervisorId);
                            studCmd.Parameters.AddWithValue("@w", score);
                            studCmd.Parameters.AddWithValue("@status", wellbeing);
                            studentId = (long)studCmd.ExecuteScalar();
                        }

                        studentList.Add((studentId, supervisorId, $"{first} {last}"));
                        Console.WriteLine($"🎓 Student created: {first} {last} ({wellbeing}, score {score}) under {supervisorName}");
                    }
                }

                Console.WriteLine($"✅ Created {studentList.Count} students total.");

                // ---- 4. Meetings (0–1 per student) ----
                foreach (var (studentId, supervisorId, name) in studentList)
                {
                    if (random.NextDouble() < 0.5)
                    {
                        DateTime date = DateTime.Now.AddDays(-random.Next(0, 30));
                        string start = "10:00";
                        string end = "11:00";
                        string note = "Progress check and wellbeing discussion.";

                        using (var meetCmd = new SQLiteCommand(
                            @"INSERT INTO Meetings (student_id, supervisor_id, meeting_date, start_time, end_time, notes)
                              VALUES (@stu, @sup, @d, @st, @et, @n);", conn))
                        {
                            meetCmd.Parameters.AddWithValue("@stu", studentId);
                            meetCmd.Parameters.AddWithValue("@sup", supervisorId);
                            meetCmd.Parameters.AddWithValue("@d", date.ToString("yyyy-MM-dd"));
                            meetCmd.Parameters.AddWithValue("@st", start);
                            meetCmd.Parameters.AddWithValue("@et", end);
                            meetCmd.Parameters.AddWithValue("@n", note);
                            meetCmd.ExecuteNonQuery();
                        }

                        Console.WriteLine($"🗓️  Meeting logged for {name} on {date:yyyy-MM-dd}");
                    }
                }

                Console.WriteLine("🎉 Realistic database seeding completed!");
            }
        }
    }
}
