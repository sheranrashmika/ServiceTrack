namespace ServiceTrack;

// A reminder for a recurring service (a record type: short, immutable, value-based).
public record Reminder(string Registration, string TaskName, int DueMileage, int RemainingKm)
{
    // Relational patterns in a switch expression.
    public string Status => RemainingKm switch
    {
        < 0 => "OVERDUE",
        <= 500 => "Due soon",
        _ => "OK"
    };
}

// Business logic: accounts, reminders and reports. Reports are written with LINQ.
public class ServiceManager
{
    private readonly AppData _data;

    // Event: the UI subscribes to be told when a reminder is overdue or nearly due.
    public event EventHandler<Reminder>? ReminderDue;

    public ServiceManager(AppData data) => _data = data;

    public IReadOnlyList<ServiceCategory> Categories => _data.Categories;
    public IReadOnlyList<Customer> Customers => _data.Customers;

    // ---------- accounts ----------
    public User? Authenticate(string username, string password)
    {
        User? user = _data.Admins.Cast<User>().Concat(_data.Customers)
            .FirstOrDefault(u => u.Username.Equals(username, StringComparison.OrdinalIgnoreCase));
        return user != null && AuthService.Verify(password, user.PasswordHash) ? user : null;
    }

    public Customer Register(string username, string password)
    {
        bool taken = _data.Admins.Cast<User>().Concat(_data.Customers)
            .Any(u => u.Username.Equals(username, StringComparison.OrdinalIgnoreCase));
        if (taken) throw new InvalidOperationException($"The username '{username}' is already taken.");

        int nextId = _data.Admins.Cast<User>().Concat(_data.Customers).Max(u => u.Id) + 1;
        var customer = new Customer(nextId, username, AuthService.Hash(password));
        _data.Customers.Add(customer);
        return customer;
    }

    public bool DeleteCustomer(string username) =>
        _data.Customers.RemoveAll(c => c.Username.Equals(username, StringComparison.OrdinalIgnoreCase)) > 0;

    public void UpdateCategory(string name, int intervalKm, decimal defaultCost)
    {
        int index = _data.Categories.FindIndex(c => c.Name == name);
        if (index >= 0) _data.Categories[index] = _data.Categories[index] with
        {
            IntervalKm = intervalKm,
            DefaultCost = defaultCost
        };
    }

    // ---------- reminders (LINQ) ----------
    // For each recurring task, take the latest record and work out when it is next due.
    public IEnumerable<Reminder> GetReminders(Vehicle vehicle) =>
        vehicle.History
            .Where(r => r.Task.IntervalKm > 0)
            .GroupBy(r => r.Task.TaskName)
            .Select(g => g.OrderByDescending(r => r.MileageAtService).First())
            .Select(last => (Last: last, Due: last.Task.NextDueMileage(last.MileageAtService)))
            .Where(x => x.Due.HasValue)
            .Select(x => new Reminder(vehicle.Registration, x.Last.Task.TaskName,
                                      x.Due!.Value, x.Due.Value - vehicle.Mileage))
            .OrderBy(r => r.RemainingKm);

    public List<Reminder> GetReminders(Customer customer)
    {
        List<Reminder> all = customer.Vehicles.SelectMany(GetReminders).OrderBy(r => r.RemainingKm).ToList();
        foreach (Reminder r in all.Where(r => r.RemainingKm <= 500))
            ReminderDue?.Invoke(this, r);                       // raise the event
        return all;
    }

    // ---------- reports (LINQ) ----------
    public IEnumerable<(string Month, decimal Total)> MonthlySpend(Customer customer) =>
        customer.Vehicles.SelectMany(v => v.History)
            .GroupBy(r => new DateTime(r.Date.Year, r.Date.Month, 1))
            .OrderBy(g => g.Key)
            .Select(g => (g.Key.ToString("MMM yyyy"), g.Sum(r => r.Cost)));

    public IEnumerable<(string Customer, int Vehicles, decimal Spend)> SpendPerCustomer() =>
        _data.Customers.Select(c => (c.Username, c.Vehicles.Count, c.Vehicles.Sum(v => v.TotalSpend())));

    public IEnumerable<(string Task, int Count, decimal Total)> SpendPerTaskType() =>
        _data.Customers.SelectMany(c => c.Vehicles).SelectMany(v => v.History)
            .GroupBy(r => r.Task.TaskName)
            .Select(g => (g.Key, g.Count(), g.Sum(r => r.Cost)))
            .OrderByDescending(x => x.Item3);

    public IEnumerable<(string Owner, Vehicle Vehicle, ServiceRecord Record)> AllRecords() =>
        _data.Customers.SelectMany(c => c.Vehicles.SelectMany(v => v.History.Select(r => (c.Username, v, r))))
            .OrderBy(x => x.Item3.Date);
}
