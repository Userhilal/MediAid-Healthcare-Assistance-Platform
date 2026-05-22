namespace MediAid.Models;

public class PlatformStats
{
    public int TotalUsers { get; set; }
    public int TotalPatients { get; set; }
    public int TotalAidants { get; set; }
    public int AvailableAidants { get; set; }
    public int OpenRequests { get; set; }
    public int CompletedRequests { get; set; }
    public int PatientsHelped { get; set; }
    public int TotalVolunteerHours { get; set; }
    public double AverageRating { get; set; }
    public bool IsDatabaseAvailable { get; set; } = true;
}
