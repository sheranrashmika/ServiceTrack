using System.Text.Json;

namespace ServiceTrack;

// Everything that is saved to disk.
public class AppData
{
    public List<Admin> Admins { get; set; } = new();
    public List<Customer> Customers { get; set; } = new();
    public List<ServiceCategory> Categories { get; set; } = new();

    // Non-nullable annotations are not enforced when JSON is loaded, so check the data explicitly.
    public void Validate()
    {
        foreach (Customer c in Customers)
        {
            if (string.IsNullOrWhiteSpace(c.Username))
                throw new InvalidDataException("A customer in the data file has no username.");
            foreach (Vehicle v in c.Vehicles)
                if (string.IsNullOrWhiteSpace(v.Registration))
                    throw new InvalidDataException($"A vehicle of '{c.Username}' has no registration.");
        }
    }
}

// Loads and saves AppData as JSON. File I/O is asynchronous (async/await).
public class DataStore
{
    private static readonly JsonSerializerOptions Options = new() { WriteIndented = true };
    private readonly string _path;

    public AppData Data { get; private set; } = new();

    public DataStore(string path) => _path = path;

    public async Task LoadAsync()
    {
        if (!File.Exists(_path))
        {
            Data = SeedData.Create();
            await SaveAsync();
            return;
        }
        await using FileStream stream = File.OpenRead(_path);
        Data = await JsonSerializer.DeserializeAsync<AppData>(stream, Options) ?? new AppData();
        Data.Validate();
    }

    public async Task SaveAsync()
    {
        await using FileStream stream = File.Create(_path);
        await JsonSerializer.SerializeAsync(stream, Data, Options);
    }
}

// Sample data created the first time the program runs, so there is something to look at.
public static class SeedData
{
    public static AppData Create()
    {
        var data = new AppData();
        data.Categories.AddRange(new[]
        {
            new ServiceCategory("Oil Change", 5000, 60m),
            new ServiceCategory("Tire Rotation", 10000, 40m),
            new ServiceCategory("General Repair", 0, 0m)
        });

        data.Admins.Add(new Admin(1, "admin", AuthService.Hash("admin123")));

        var demo = new Customer(2, "demo", AuthService.Hash("demo123"));
        var car = new Vehicle("1ABC234", "Toyota", "Corolla", 2018, 35000);
        car.AddServiceRecord(new ServiceRecord(DateTime.Today.AddMonths(-5), 36500, TaskFactory.Create(data.Categories[0]), 65m));
        car.AddServiceRecord(new ServiceRecord(DateTime.Today.AddMonths(-3), 38000, TaskFactory.Create(data.Categories[1]), 45m));
        car.AddServiceRecord(new ServiceRecord(DateTime.Today.AddMonths(-1), 41200, new GeneralRepair("Brake Pads"), 220m));
        car.UpdateMileage(42000);   // current odometer reading
        demo.Vehicles.Add(car);
        data.Customers.Add(demo);
        return data;
    }
}
