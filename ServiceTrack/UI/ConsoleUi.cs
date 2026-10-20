namespace ServiceTrack;

// Menu-driven console interface. Input helpers use TryParse so bad input never crashes the program.
public class ConsoleUi
{
    private readonly ServiceManager _manager;
    private readonly DataStore _store;

    public ConsoleUi(ServiceManager manager, DataStore store)
    {
        _manager = manager;
        _store = store;
        _manager.ReminderDue += (sender, r) =>
            Console.WriteLine($"  >> Reminder: {r.Registration} {r.TaskName} - {r.Status} ({r.RemainingKm:N0} km)");
    }

    // ---------- input helpers ----------
    private static string Ask(string prompt)
    {
        Console.Write(prompt);
        string? line = Console.ReadLine();
        if (line == null) throw new OperationCanceledException();   // end of input
        return line.Trim();
    }

    private static int AskInt(string prompt, int min, int max)
    {
        while (true)
        {
            if (int.TryParse(Ask(prompt), out int value) && value >= min && value <= max) return value;
            Console.WriteLine($"  Please enter a whole number from {min} to {max}.");
        }
    }

    private static decimal AskDecimal(string prompt, decimal fallback)
    {
        while (true)
        {
            string text = Ask(prompt);
            if (text == "") return fallback;
            if (decimal.TryParse(text, out decimal value) && value >= 0) return value;
            Console.WriteLine("  Please enter a positive amount, or press Enter for the default.");
        }
    }

    private static Vehicle? ChooseVehicle(Customer customer)
    {
        if (customer.Vehicles.Count == 0) { Console.WriteLine("  You have no vehicles yet."); return null; }
        for (int i = 0; i < customer.Vehicles.Count; i++)
            Console.WriteLine($"  {i + 1}. {customer.Vehicles[i].Registration} ({customer.Vehicles[i].Make} {customer.Vehicles[i].Model})");
        int choice = AskInt("Vehicle number: ", 1, customer.Vehicles.Count);
        return customer.Vehicles[choice - 1];
    }

    // ---------- main menu ----------
    public async Task RunAsync()
    {
        while (true)
        {
            Console.WriteLine();
            Console.WriteLine("=== ServiceTrack: Vehicle Service & Maintenance Management ===");
            Console.WriteLine("1. Log in    2. Register    3. Language demonstrations    0. Exit");
            switch (AskInt("Choose: ", 0, 3))
            {
                case 1: await LoginAsync(); break;
                case 2: await RegisterAsync(); break;
                case 3: LanguageIssues.Run(); break;
                case 0: return;
            }
        }
    }

    private async Task RegisterAsync()
    {
        string username = Ask("New username: ");
        string password = Ask("Password: ");
        if (username.Length < 3 || password.Length < 6)
        {
            Console.WriteLine("  Username needs 3+ characters and password 6+ characters.");
            return;
        }
        try
        {
            _manager.Register(username, password);
            await _store.SaveAsync();
            Console.WriteLine("  Account created. You can now log in.");
        }
        catch (InvalidOperationException ex)
        {
            Console.WriteLine($"  {ex.Message}");
        }
    }

    private async Task LoginAsync()
    {
        string username = Ask("Username: ");
        string password = Ask("Password: ");
        User? user = _manager.Authenticate(username, password);
        switch (user)                                   // pattern matching on the runtime type
        {
            case Admin admin: await AdminMenuAsync(admin); break;
            case Customer customer: await CustomerMenuAsync(customer); break;
            default: Console.WriteLine("  Invalid username or password."); break;
        }
    }

    // ---------- customer ----------
    private async Task CustomerMenuAsync(Customer customer)
    {
        Console.WriteLine($"\nWelcome, {customer.Username}.");
        while (true)
        {
            Console.WriteLine("\n[Customer] 1 Add vehicle  2 My vehicles  3 Add service record  4 Update mileage");
            Console.WriteLine("           5 Maintenance history  6 Upcoming reminders  7 Spending  0 Log out");
            switch (AskInt("Choose: ", 0, 7))
            {
                case 1: await AddVehicleAsync(customer); break;
                case 2: ListVehicles(customer); break;
                case 3: await AddServiceRecordAsync(customer); break;
                case 4: await UpdateMileageAsync(customer); break;
                case 5: ShowHistory(customer); break;
                case 6: ShowReminders(customer); break;
                case 7: ShowSpending(customer); break;
                case 0: return;
            }
        }
    }

    private async Task AddVehicleAsync(Customer customer)
    {
        string registration = Ask("Registration: ").ToUpperInvariant();
        if (registration == "") { Console.WriteLine("  Registration is required."); return; }
        if (customer.Vehicles.Any(v => v.Registration == registration))
        {
            Console.WriteLine("  That vehicle is already registered.");
            return;
        }
        string make = Ask("Make: ");
        string model = Ask("Model: ");
        int year = AskInt("Year: ", 1950, DateTime.Today.Year + 1);
        int mileage = AskInt("Current mileage (km): ", 0, 2_000_000);
        customer.Vehicles.Add(new Vehicle(registration, make, model, year, mileage));
        await _store.SaveAsync();
        Console.WriteLine("  Vehicle added.");
    }

    private static void ListVehicles(Customer customer)
    {
        if (customer.Vehicles.Count == 0) { Console.WriteLine("  No vehicles yet."); return; }
        Console.WriteLine($"  {"Reg",-9}{"Vehicle",-22}{"Year",-6}{"Mileage",10}");
        foreach (Vehicle v in customer.Vehicles)
            Console.WriteLine($"  {v.Registration,-9}{v.Make + " " + v.Model,-22}{v.Year,-6}{v.Mileage,10:N0}");
    }

    private async Task AddServiceRecordAsync(Customer customer)
    {
        Vehicle? vehicle = ChooseVehicle(customer);
        if (vehicle == null) return;

        for (int i = 0; i < _manager.Categories.Count; i++)
            Console.WriteLine($"  {i + 1}. {_manager.Categories[i].Name} (default {_manager.Categories[i].DefaultCost:C})");
        ServiceCategory category = _manager.Categories[AskInt("Service type: ", 1, _manager.Categories.Count) - 1];

        int mileage = AskInt($"Mileage at service (current {vehicle.Mileage:N0}): ", 0, 2_000_000);
        decimal cost = AskDecimal($"Cost (Enter for {category.DefaultCost:C}): ", category.DefaultCost);

        try
        {
            vehicle.AddServiceRecord(new ServiceRecord(DateTime.Today, mileage, TaskFactory.Create(category), cost));
            await _store.SaveAsync();
            Console.WriteLine("  Service record saved.");
        }
        catch (InvalidMileageException ex)
        {
            Console.WriteLine($"  Not saved: {ex.Message}");
        }
    }

    private async Task UpdateMileageAsync(Customer customer)
    {
        Vehicle? vehicle = ChooseVehicle(customer);
        if (vehicle == null) return;
        int newMileage = AskInt($"New mileage (current {vehicle.Mileage:N0}): ", 0, 2_000_000);
        try
        {
            vehicle.UpdateMileage(newMileage);
            await _store.SaveAsync();
            Console.WriteLine("  Mileage updated.");
        }
        catch (InvalidMileageException ex)
        {
            Console.WriteLine($"  Not updated: {ex.Message}");
        }
    }

    private static void ShowHistory(Customer customer)
    {
        Vehicle? vehicle = ChooseVehicle(customer);
        if (vehicle == null) return;
        Console.WriteLine($"\n  Maintenance history for {vehicle.Registration}");
        Console.WriteLine($"  {"Date",-13}{"Service",-18}{"Mileage",10}{"Cost",11}");
        foreach (ServiceRecord r in vehicle.History.OrderBy(r => r.Date))
            Console.WriteLine($"  {r.Date,-13:dd MMM yyyy}{r.Task.TaskName,-18}{r.MileageAtService,10:N0}{r.Cost,11:C}");
        Console.WriteLine($"  {"Total spend",-31}{"",10}{vehicle.TotalSpend(),11:C}");
    }

    private void ShowReminders(Customer customer)
    {
        List<Reminder> reminders = _manager.GetReminders(customer);
        if (reminders.Count == 0) { Console.WriteLine("  No recurring services recorded yet."); return; }
        Console.WriteLine($"\n  {"Vehicle",-9}{"Service",-16}{"Due at",10}{"Remaining",11}  Status");
        foreach (Reminder r in reminders)
            Console.WriteLine($"  {r.Registration,-9}{r.TaskName,-16}{r.DueMileage,10:N0}{r.RemainingKm,11:N0}  {r.Status}");
    }

    private void ShowSpending(Customer customer)
    {
        Console.WriteLine("\n  Total spend per vehicle");
        foreach (Vehicle v in customer.Vehicles)
            Console.WriteLine($"  {v.Registration,-9}{v.TotalSpend(),11:C}");
        Console.WriteLine("  Monthly spending");
        foreach (var (month, total) in _manager.MonthlySpend(customer))
            Console.WriteLine($"  {month,-9}{total,11:C}");
    }

    // ---------- admin ----------
    private async Task AdminMenuAsync(Admin admin)
    {
        Console.WriteLine($"\nWelcome, administrator {admin.Username}.");
        while (true)
        {
            Console.WriteLine("\n[Admin] 1 Users  2 All service records  3 Summary report");
            Console.WriteLine("        4 Manage service categories  5 Delete a customer  0 Log out");
            switch (AskInt("Choose: ", 0, 5))
            {
                case 1: ShowUsers(); break;
                case 2: ShowAllRecords(); break;
                case 3: ShowSummary(); break;
                case 4: await ManageCategoriesAsync(); break;
                case 5: await DeleteCustomerAsync(); break;
                case 0: return;
            }
        }
    }

    private void ShowUsers()
    {
        Console.WriteLine($"\n  {"Username",-14}{"Vehicles",9}");
        foreach (Customer c in _manager.Customers)
            Console.WriteLine($"  {c.Username,-14}{c.Vehicles.Count,9}");
    }

    private void ShowAllRecords()
    {
        Console.WriteLine($"\n  {"Date",-13}{"Owner",-10}{"Vehicle",-9}{"Service",-16}{"Cost",10}");
        foreach (var (owner, vehicle, record) in _manager.AllRecords())
            Console.WriteLine($"  {record.Date,-13:dd MMM yyyy}{owner,-10}{vehicle.Registration,-9}{record.Task.TaskName,-16}{record.Cost,10:C}");
    }

    private void ShowSummary()
    {
        Console.WriteLine("\n  Spend per customer");
        foreach (var (customer, vehicles, spend) in _manager.SpendPerCustomer())
            Console.WriteLine($"  {customer,-14}{vehicles,3} vehicle(s){spend,12:C}");
        Console.WriteLine("  Spend per service type");
        foreach (var (task, count, total) in _manager.SpendPerTaskType())
            Console.WriteLine($"  {task,-16}{count,3} job(s){total,12:C}");
    }

    private async Task ManageCategoriesAsync()
    {
        Console.WriteLine($"\n  {"#",-3}{"Category",-18}{"Interval (km)",14}{"Default cost",14}");
        for (int i = 0; i < _manager.Categories.Count; i++)
        {
            ServiceCategory c = _manager.Categories[i];
            Console.WriteLine($"  {i + 1,-3}{c.Name,-18}{c.IntervalKm,14:N0}{c.DefaultCost,14:C}");
        }
        int choice = AskInt("Category to edit (0 to cancel): ", 0, _manager.Categories.Count);
        if (choice == 0) return;
        ServiceCategory selected = _manager.Categories[choice - 1];
        int interval = AskInt($"New interval in km (0 = one-off, now {selected.IntervalKm:N0}): ", 0, 200_000);
        decimal cost = AskDecimal($"New default cost (Enter to keep {selected.DefaultCost:C}): ", selected.DefaultCost);
        _manager.UpdateCategory(selected.Name, interval, cost);
        await _store.SaveAsync();
        Console.WriteLine("  Category updated.");
    }

    private async Task DeleteCustomerAsync()
    {
        string username = Ask("Customer username to delete: ");
        if (_manager.DeleteCustomer(username))
        {
            await _store.SaveAsync();
            Console.WriteLine("  Customer deleted.");
        }
        else Console.WriteLine("  No such customer.");
    }
}
