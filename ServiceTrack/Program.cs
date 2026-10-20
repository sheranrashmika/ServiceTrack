using ServiceTrack;

string dataFile = Path.Combine(AppContext.BaseDirectory, "servicetrack-data.json");
var store = new DataStore(dataFile);
await store.LoadAsync();

var ui = new ConsoleUi(new ServiceManager(store.Data), store);
try
{
    await ui.RunAsync();
}
catch (OperationCanceledException)
{
    // input stream closed; fall through and save
}
await store.SaveAsync();
Console.WriteLine("Goodbye.");
