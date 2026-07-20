namespace DealTrack.Domain.Enums
{
    public enum HRActionType
    {
        RemoveMember        = 1,
        AssignToTeamLead    = 2,
        UnassignFromTeamLead= 3,
        PromoteToTeamLead   = 4,
        DemoteToSales       = 5,
        SalaryDeduction     = 6,
        SalaryBonus         = 7,
        AcceptJoinRequest   = 8,
    }
}
