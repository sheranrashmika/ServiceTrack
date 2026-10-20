using System.Text.Json.Serialization;

namespace ServiceTrack;

// Abstract base for everyone who logs in. Admin and Customer specialise it.
public abstract class User
{
    public int Id { get; }
    public string Username { get; }
    public string PasswordHash { get; }   // salted PBKDF2 hash, never the plain password

    protected User(int id, string username, string passwordHash)
    {
        Id = id;
        Username = username;
        PasswordHash = passwordHash;
    }
}

public class Admin : User
{
    public Admin(int id, string username, string passwordHash) : base(id, username, passwordHash) { }
}

public class Customer : User
{
    [JsonInclude]
    public List<Vehicle> Vehicles { get; private set; } = new();

    public Customer(int id, string username, string passwordHash) : base(id, username, passwordHash) { }
}
