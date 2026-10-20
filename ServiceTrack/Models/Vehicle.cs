using System.Text.Json.Serialization;

namespace ServiceTrack;

public class Vehicle
{
    public string Registration { get; }
    public string Make { get; }
    public string Model { get; }
    public int Year { get; }

    // Encapsulation: the setter is private, so mileage can only change through the methods below.
    public int Mileage { get; private set; }

    [JsonInclude]   // lets System.Text.Json fill this list through its private setter when loading
    public List<ServiceRecord> History { get; private set; } = new();

    public Vehicle(string registration, string make, string model, int year, int mileage)
    {
        Registration = registration;
        Make = make;
        Model = model;
        Year = year;
        Mileage = mileage;
    }

    // Records a new odometer reading; the mileage may never move backwards.
    public void UpdateMileage(int newMileage)
    {
        if (newMileage < Mileage)
            throw new InvalidMileageException(
                $"New mileage ({newMileage:N0}) is lower than the current mileage ({Mileage:N0}).");
        Mileage = newMileage;
    }

    public void AddServiceRecord(ServiceRecord record)
    {
        UpdateMileage(record.MileageAtService);   // reuses the same validation rule
        History.Add(record);
    }

    // LINQ: one expression replaces an explicit accumulation loop.
    public decimal TotalSpend() => History.Sum(r => r.Cost);
}
