namespace ServiceTrack;

// Custom exception: thrown when a mileage value would make the odometer go backwards.
public class InvalidMileageException : Exception
{
    public InvalidMileageException(string message) : base(message) { }
}
