using System;
using System.Data.SQLite;

namespace Design_Develop_Deploy_Project.Repos;

public class SupervisorRepository
{
    public string _connectionString;

    public SupervisorRepository(string connectionString)
    {
        _connectionString = connectionString;
    }

    public Supervisor GetSupervisorById(int Supervisor_id)
    {
        if (Supervisor_id <= 0) { return null; }

        using (var conn = new SQLiteConnection(_connectionString))
        {
            conn.Open();
            string query = @"SELECT s.supervisor_id,
							u.user_id, u.first_name, u.last_name, u.email, u.password, u.role
							FROM Supervisors s
							JOIN Users u on s.user_id =  u.user_id
							WHERE s.supervisor_id = @Supervisor_id";

            using (var cmd = new SQLiteCommand(query, conn))
            {
                cmd.Parameters.AddWithValue("@Supervisor_id", Supervisor_id);
                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        return MapReaderToSupervisor(reader);
                    }
                    else
                    {
                        return null;
                    }
                }
            }
        }
        return null;
    }

    public Supervisor GetSupervisorByEmail(string Email)
    {
        if (string.IsNullOrWhiteSpace(Email)) { return null; }
        using (var conn = new SQLiteConnection(_connectionString))
        {
            conn.Open();
            string query = @"SELECT s.supervisor_id,
							u.user_id, u.first_name, u.last_name, u.email, u.password, u.role
							FROM Supervisors s
							JOIN Users u on s.user_id =  u.user_id
							WHERE LOWER(u.email) = @Email";
            using (var cmd = new SQLiteCommand(query, conn))
            {
                cmd.Parameters.AddWithValue("@Email", Email.Trim().ToLower());
                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        return MapReaderToSupervisor(reader);
                    }
                }
            }
            return null;
        }
    }

    public List<Supervisor> GetAllSupervisors()
    {
        var supervisors = new List<Supervisor>();

        try
        {
            using (var conn = new SQLiteConnection(_connectionString))
            {
                conn.Open();

                string query = @"
                SELECT supervisor_id, user_id, last_office_hours_update, 
                       last_wellbeing_check, office_hours, 
                       meetings_booked_this_month, wellbeing_checks_this_month
                FROM Supervisors";

                using (var cmd = new SQLiteCommand(query, conn))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        supervisors.Add(MapReaderToSupervisor(reader));
                    }
                }
            }
        }
        catch
        {
            throw;
        }


        return supervisors;
    }

    public void UpdateOfficeHours(int supervisorId, string officeHours)
    {
        using (var conn = new SQLiteConnection(_connectionString))
        {
            conn.Open();
            string query = @"UPDATE Supervisors
                         SET office_hours = @OfficeHours,
                             last_office_hours_update = @UpdateDate
                         WHERE supervisor_id = @SupervisorId";

            using (var cmd = new SQLiteCommand(query, conn))
            {
                cmd.Parameters.AddWithValue("@OfficeHours", officeHours);
                cmd.Parameters.AddWithValue("@UpdateDate", DateTime.UtcNow);
                cmd.Parameters.AddWithValue("@SupervisorId", supervisorId);
                cmd.ExecuteNonQuery();
            }
        }
    }

    public DateTime? GetLastOfficeHourUpdate(int supervisorId)
    {
        using var conn = new SQLiteConnection(_connectionString);
        conn.Open();
        const string query = "SELECT last_office_hours_update FROM Supervisors WHERE supervisor_id = @SupervisorId";
        using var cmd = new SQLiteCommand(query, conn);
        cmd.Parameters.AddWithValue("@SupervisorId", supervisorId);
        var result = cmd.ExecuteScalar();
        return result == DBNull.Value ? null : Convert.ToDateTime(result);
    }

    public DateTime? GetLastWellbeingCheck(int supervisorId)
    {
        using var conn = new SQLiteConnection(_connectionString);
        conn.Open();
        const string query = "SELECT last_wellbeing_check FROM Supervisors WHERE supervisor_id = @SupervisorId";
        using var cmd = new SQLiteCommand(query, conn);
        cmd.Parameters.AddWithValue("@SupervisorId", supervisorId);
        var result = cmd.ExecuteScalar();
        return result == DBNull.Value ? null : Convert.ToDateTime(result);
    }

    public void ResetInteractionStats()
    {
        using (var conn = new SQLiteConnection(_connectionString))
        {
            conn.Open();
            const string query = @"
        UPDATE Supervisors
        SET 
            meetings_booked_last_month = 0,
            wellbeing_checks_last_month = 0,
            last_meeting_update_month = strftime('%m', 'now')
        WHERE last_meeting_update_month != strftime('%m', 'now');";

            using (var cmd = new SQLiteCommand(query, conn))
            {
                cmd.ExecuteNonQuery();
            }
        }
    }

    public void UpdateWellbeingCheckCount(int supervisorId, DateTime checkDate)
    {
        using (var conn = new SQLiteConnection(_connectionString))
        {
            conn.Open();
            const string query = @"
        UPDATE Supervisors
        SET wellbeing_checks_this_month = wellbeing_checks_this_month + 1,
            last_wellbeing_check = @CheckDate
        WHERE supervisor_id = @SupervisorId";

            using (var cmd = new SQLiteCommand(query, conn))
            {
                cmd.Parameters.AddWithValue("@SupervisorId", supervisorId);
                cmd.Parameters.AddWithValue("@CheckDate", checkDate);
                cmd.ExecuteNonQuery();
            }
        }
    }

    private Supervisor MapReaderToSupervisor(SQLiteDataReader reader)
    {
        return new Supervisor
        {
            user_id = Convert.ToInt32(reader["user_id"]),
            supervisor_id = Convert.ToInt32(reader["supervisor_id"]),
            first_name = reader["first_name"].ToString(),
            last_name = reader["last_name"].ToString(),
            email = reader["email"].ToString(),
            password = reader["password"].ToString(),
            role = reader["role"].ToString(),
        };
    }
}



