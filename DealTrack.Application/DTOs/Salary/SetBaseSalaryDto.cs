namespace DealTrack.Application.DTOs.Salary
{
    public class SetBaseSalaryDto
    {
        public string UserId { get; set; } = string.Empty;
        public decimal BaseSalary { get; set; }
        public int Month { get; set; }
        public int Year { get; set; }
    }
}
