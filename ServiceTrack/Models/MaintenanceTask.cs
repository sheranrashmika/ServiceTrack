using System.Text.Json.Serialization;

namespace ServiceTrack;

// Abstract base class: each subclass decides when its next service is due (polymorphism).
// The attributes below are needed only so System.Text.Json can save and load the subclasses.
[JsonPolymorphic(TypeDiscriminatorPropertyName = "kind")]
[JsonDerivedType(typeof(OilChange), "oil")]
[JsonDerivedType(typeof(TireRotation), "tire")]
[JsonDerivedType(typeof(GeneralRepair), "repair")]
public abstract class MaintenanceTask
{
    public string TaskName { get; }
    public int IntervalKm { get; }

    protected MaintenanceTask(string taskName, int intervalKm)
    {
        TaskName = taskName;
        IntervalKm = intervalKm;
    }

    // Returns the mileage at which this task is next due, or null for one-off jobs.
    public abstract int? NextDueMileage(int serviceMileage);
}

public class OilChange : MaintenanceTask
{
    public OilChange(int intervalKm) : base("Oil Change", intervalKm) { }
    public override int? NextDueMileage(int serviceMileage) => serviceMileage + IntervalKm;
}

public class TireRotation : MaintenanceTask
{
    public TireRotation(int intervalKm) : base("Tire Rotation", intervalKm) { }
    public override int? NextDueMileage(int serviceMileage) => serviceMileage + IntervalKm;
}

public class GeneralRepair : MaintenanceTask
{
    public GeneralRepair(string taskName) : base(taskName, 0) { }
    public override int? NextDueMileage(int serviceMileage) => null;   // one-off repair
}

public static class TaskFactory
{
    // A switch expression chooses the right subclass for a category.
    public static MaintenanceTask Create(ServiceCategory category) => category.Name switch
    {
        "Oil Change" => new OilChange(category.IntervalKm),
        "Tire Rotation" => new TireRotation(category.IntervalKm),
        _ => new GeneralRepair(category.Name)
    };
}
