namespace ServiceTrack;

// One completed service: what was done, when, at what mileage, and the cost.
public class ServiceRecord
{
    public DateTime Date { get; }
    public int MileageAtService { get; }
    public MaintenanceTask Task { get; }
    public decimal Cost { get; }

    public ServiceRecord(DateTime date, int mileageAtService, MaintenanceTask task, decimal cost)
    {
        Date = date;
        MileageAtService = mileageAtService;
        Task = task;
        Cost = cost;
    }
}
