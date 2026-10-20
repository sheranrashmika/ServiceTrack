namespace ServiceTrack;

// A service category managed by the Admin: its name, how often it recurs, and a default price.
// A record gives value-based equality and immutability with very little code.
public record ServiceCategory(string Name, int IntervalKm, decimal DefaultCost);
