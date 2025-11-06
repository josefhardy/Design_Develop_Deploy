using System;
using System.Collections.Generic;
using System.Data.SQLite;
using Design_Develop_Deploy_Project.Objects;

namespace Design_Develop_Deploy_Project.Repos;

public class InteractionRepository
{
    private readonly string _connectionString;

    public InteractionRepository(string connectionString)
    {
        _connectionString = connectionString;
    }

    public void RecordSupervisorInteraction(int supervisor_id, int student_id, string interaction_type)
    {
        if (supervisor_id <= 0 || student_id <= 0 || string.IsNullOrWhiteSpace(interaction_type))
        {
            throw new ArgumentException("Invalid parameters provided to RecordSupervisorInteraction.");
        }

        try
        {
            using (var conn = new SQLiteConnection(_connectionString))
            {
                conn.Open();

                string columnToUpdate = interaction_type.ToLower() switch
                {
                    "meeting" => "meetings_booked_this_month",
                    "wellbeing_check" => "wellbeing_checks_this_month",
                    _ => null
                };

                if (columnToUpdate == null)
                {
                    throw new ArgumentException("Invalid interaction type.");
                }

                string updateQuery = $@"
                    UPDATE Supervisors
                    SET {columnToUpdate} = {columnToUpdate} + 1
                    WHERE supervisor_id = @SupervisorId";

                using (var cmd = new SQLiteCommand(updateQuery, conn))
                {
                    cmd.Parameters.AddWithValue("@SupervisorId", supervisor_id);
                    cmd.ExecuteNonQuery();
                }
            }
        }
        catch (Exception ex)
        {
            throw new Exception("Error recording supervisor interaction.", ex);
        }
    }

    public (int meetingsBooked, int wellbeingChecks) GetSupervisorActivity(int supervisor_id)
    {
        if (supervisor_id <= 0)
        {
            throw new ArgumentException("Invalid supervisor ID provided to GetSupervisorActivity.");
        }

        try
        {
            using (var conn = new SQLiteConnection(_connectionString))
            {
                conn.Open();
                string query = @"
                    SELECT meetings_booked_last_month, wellbeing_checks_last_month
                    FROM Supervisors
                    WHERE supervisor_id = @SupervisorId";

                using (var cmd = new SQLiteCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@SupervisorId", supervisor_id);
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            int meetings = Convert.ToInt32(reader["meetings_booked_last_month"]);
                            int checks = Convert.ToInt32(reader["wellbeing_checks_last_month"]);
                            return (meetings, checks);
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            throw new Exception("Error getting supervisor activity.", ex);
        }

        return (0, 0);
    }

    public List<(Supervisor supervisor, int totalInteractions)> GetAllSupervisorInteractions()
    {
        var results = new List<(Supervisor, int)>();

        try
        {
            using (var conn = new SQLiteConnection(_connectionString))
            {
                conn.Open();
                string query = @"
                    SELECT s.supervisor_id, s.meetings_booked_this_month, s.wellbeing_checks_this_month,
                           u.user_id, u.first_name, u.last_name, u.email, u.role
                    FROM Supervisors s
                    JOIN Users u ON s.user_id = u.user_id";

                using (var cmd = new SQLiteCommand(query, conn))
                {
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var supervisor = new Supervisor
                            {
                                supervisor_id = Convert.ToInt32(reader["supervisor_id"]),
                                user_id = Convert.ToInt32(reader["user_id"]),
                                first_name = reader["first_name"].ToString(),
                                last_name = reader["last_name"].ToString(),
                                email = reader["email"].ToString(),
                                role = reader["role"].ToString(),
                                meetings_booked_this_month = Convert.ToInt32(reader["meetings_booked_last_month"]),
                                wellbeing_checks_this_month = Convert.ToInt32(reader["wellbeing_checks_last_month"])
                            };

                            int total = supervisor.meetings_booked_this_month + supervisor.wellbeing_checks_this_month;
                            results.Add((supervisor, total));
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            throw new Exception("Error fetching all student interactions.", ex);
        }

        return results;
    }

    public List<(Student student, int totalInteractions)> GetAllStudentInteractions()
    {
        var results = new List<(Student, int)>();

        try
        {
            using (var conn = new SQLiteConnection(_connectionString))
            {
                conn.Open();

                string query = @"
                    SELECT st.student_id, st.supervisor_id, st.wellbeing_score, st.last_status_update,
                           u.user_id, u.first_name, u.last_name, u.email, u.password, u.role,
                           COUNT(m.meeting_id) AS meeting_count
                    FROM Students st
                    JOIN Users u ON st.user_id = u.user_id
                    LEFT JOIN Meetings m ON st.student_id = m.student_id
                    GROUP BY st.student_id";

                using (var cmd = new SQLiteCommand(query, conn))
                {
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var student = new Student
                            {
                                student_id = Convert.ToInt32(reader["student_id"]),
                                user_id = Convert.ToInt32(reader["user_id"]),
                                supervisor_id = Convert.ToInt32(reader["supervisor_id"]),
                                first_name = reader["first_name"].ToString(),
                                last_name = reader["last_name"].ToString(),
                                email = reader["email"].ToString(),
                                wellbeing_score = Convert.ToInt32(reader["wellbeing_score"]),
                                last_status_update = reader["last_status_update"] == DBNull.Value
                                                     ? (DateTime?)null
                                                     : Convert.ToDateTime(reader["last_status_update"]),
                                role = reader["role"].ToString()
                            };

                            int totalInteractions = Convert.ToInt32(reader["meeting_count"]);
                            results.Add((student, totalInteractions));
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            throw new Exception("Error retrieving student activity in range.", ex);
        }

        return results;
    }

    public (int meetings, int wellbeingChecks) GetSupervisorActivityInRange(int supervisorId, DateTime start, DateTime end)
    {
        int meetings = 0;
        int wellbeingChecks = 0;

        try
        {
            using (var conn = new SQLiteConnection(_connectionString))
            {
                conn.Open();

                // ✅ Count meetings within the given date range
                const string meetingQuery = @"
                SELECT COUNT(*) 
                FROM Meetings
                WHERE supervisor_id = @SupervisorId
                  AND meeting_date >= @StartDate
                  AND meeting_date < @EndDate;";

                using (var cmd = new SQLiteCommand(meetingQuery, conn))
                {
                    cmd.Parameters.AddWithValue("@SupervisorId", supervisorId);
                    cmd.Parameters.AddWithValue("@StartDate", start);
                    cmd.Parameters.AddWithValue("@EndDate", end);
                    meetings = Convert.ToInt32(cmd.ExecuteScalar());
                }

                // ✅ Count wellbeing checks indirectly from Supervisors table
                const string wellbeingQuery = @"
                SELECT COUNT(*) 
                FROM Supervisors
                WHERE supervisor_id = @SupervisorId
                  AND last_wellbeing_check >= @StartDate
                  AND last_wellbeing_check < @EndDate;";

                using (var cmd = new SQLiteCommand(wellbeingQuery, conn))
                {
                    cmd.Parameters.AddWithValue("@SupervisorId", supervisorId);
                    cmd.Parameters.AddWithValue("@StartDate", start);
                    cmd.Parameters.AddWithValue("@EndDate", end);
                    wellbeingChecks = Convert.ToInt32(cmd.ExecuteScalar());
                }
            }
        }
        catch (Exception ex)
        {
            throw new Exception("Error retrieving supervisor activity in range.", ex);
        }


        return (meetings, wellbeingChecks);
    }

}
