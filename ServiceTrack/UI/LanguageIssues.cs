using System.Text.Json;

namespace ServiceTrack;

// Demonstrations used in Part B of the report: two weaknesses of C# met while building ServiceTrack.
public static class LanguageIssues
{
    public static void Run()
    {
        Console.WriteLine("\n--- Poor aspect 1: exceptions are unchecked ---");
        UncheckedExceptions();
        Console.WriteLine("\n--- Poor aspect 2: nullable annotations are not enforced at run time ---");
        NullAnnotations();
    }

    // Nothing in this signature tells a caller that it can throw. The compiler never asks
    // the caller to catch or declare FormatException (Java would, for its checked exceptions).
    private static int ParseMileageUnsafe(string text) => int.Parse(text);

    private static void UncheckedExceptions()
    {
        try
        {
            int km = ParseMileageUnsafe("12,500 km");   // compiles with no warning
            Console.WriteLine(km);
        }
        catch (FormatException ex)
        {
            Console.WriteLine($"Compiled cleanly, but at run time: {ex.GetType().Name}");
            Console.WriteLine("Fix used in ServiceTrack: int.TryParse inside AskInt() in ConsoleUi.cs");
        }
    }

    private static void NullAnnotations()
    {
        // Registration is declared as a non-nullable string, so the compiler treats it as never null.
        string json = "{\"Registration\":null,\"Make\":\"Toyota\",\"Model\":\"Corolla\",\"Year\":2018,\"Mileage\":1000}";
        Vehicle vehicle = JsonSerializer.Deserialize<Vehicle>(json)!;   // the ! operator silences the compiler

        try
        {
            Console.WriteLine($"Registration length: {vehicle.Registration.Length}");   // no compiler warning here
        }
        catch (NullReferenceException)
        {
            Console.WriteLine("No compiler warning, but a null got through: NullReferenceException at run time");
            Console.WriteLine("Fix used in ServiceTrack: validate loaded data (see DataStore.LoadAsync)");
        }
    }
}
