using System;
using Design_Develop_Deploy_Project.Repos;

namespace Design_Develop_Deploy_Project.Services
{
    public class SupervisorFunctionService
    {
        private readonly SupervisorRepository _supervisorRepo;
        private readonly InteractionRepository _interactionRepo;

        public SupervisorFunctionService(
            SupervisorRepository supervisorRepo,
            InteractionRepository interactionRepo)
        {
            _supervisorRepo = supervisorRepo;
            _interactionRepo = interactionRepo;
        }

        public bool NeedsOfficeHourUpdate(int supervisorId)
        {
            var last = _supervisorRepo.GetLastOfficeHourUpdate(supervisorId);
            return !last.HasValue || (DateTime.UtcNow - last.Value).TotalDays >= 7;
        }

        public bool NeedsWellbeingCheckUpdate(int supervisorId)
        {
            var last = _supervisorRepo.GetLastWellbeingCheck(supervisorId);
            return !last.HasValue || (DateTime.UtcNow - last.Value).TotalDays >= 7;
        }

        public void ResetMonthlyInteractionStatsIfNeeded()
        {
            _supervisorRepo.ResetInteractionStats();
        }

        public void RecordWellbeingCheck(int supervisorId)
        {
            _supervisorRepo.UpdateWellbeingCheckCount(supervisorId, DateTime.UtcNow);
        }

        public (int meetings, int wellbeingChecks) GetSupervisorActivityThisMonth(int supervisorId)
        {
            // This assumes InteractionRepository has the range-based version
            var now = DateTime.UtcNow;
            var first = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
            var next = first.AddMonths(1);
            return _interactionRepo.GetSupervisorActivityInRange(supervisorId, first, next);
        }
    }
}
