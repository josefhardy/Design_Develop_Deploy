using System;
using Design_Develop_Deploy_Project.Objects;

public class Supervisor : User
{
    // Table-specific fields
    public int supervisor_id { get; set; }
    public int user_id { get; set; }   // FK reference to Users table

    // Supervisor-specific data
    public string office_hours { get; set; }
    public DateTime? last_office_hours_update { get; set; }
    public DateTime? last_wellbeing_check { get; set; }
    public int meetings_booked_this_month { get; set; }
    public int wellbeing_checks_this_month { get; set; }

    // Constructors
    public Supervisor() { }

    public Supervisor(
        int supervisorId,
        int userId,
        string firstName,
        string lastName,
        string email,
        string password,
        string role,
        string officeHours,
        DateTime? lastOfficeHoursUpdate,
        DateTime? lastWellbeingCheck,
        int meetingsBookedThisMonth,
        int wellbeingChecksThisMonth
    ) : base(firstName, lastName, email, password, role)
    {
        supervisor_id = supervisorId;
        user_id = userId;
        office_hours = officeHours;
        last_office_hours_update = lastOfficeHoursUpdate;
        last_wellbeing_check = lastWellbeingCheck;
        meetings_booked_this_month = meetingsBookedThisMonth;
        wellbeing_checks_this_month = wellbeingChecksThisMonth;
    }
}
