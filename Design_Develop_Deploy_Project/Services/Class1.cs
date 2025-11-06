using Design_Develop_Deploy_Project.Repos;
using System;

namespace Design_Develop_Deploy_Project.Services
{
    public class SupervisorFunctionService
    {
        private readonly SupervisorRepository _supervisorRepo;

        public SupervisorFunctionService(SupervisorRepository supervisorRepo)
        {
            _supervisorRepo = supervisorRepo;
        }

        public bool NeedsOfficeHourUpdate(int supervisorId)
        {
            var last = _supervisorRepo.GetLastOfficeHourUpdate(supervisorId);
            return !last.HasValue || (DateTime.UtcNow - last.Value).TotalDays >= 7;
        }

        public bool NeedsWellbeingCheckUpdate(int supervisorId)
        {
            var last = _supervisorRepo.GetLastWellbeingCheck(supervisorId);
            return !last.HasValue || (DateTime.UtcNow - last.Value).TotalDays > 7;
        }

    }
}
