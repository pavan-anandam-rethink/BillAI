using System.Reflection;

namespace ClearingHouse.SharedKernel.Domain;

public abstract class Enumeration : IComparable
{
    public int Id { get; }
    public string Name { get; }
    
    protected Enumeration(int id, string name) => (Id, Name) = (id, name);
    
    public override string ToString() => Name;
    
    public static IEnumerable<T> GetAll<T>() where T : Enumeration =>
        typeof(T).GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)
            .Select(f => f.GetValue(null))
            .Cast<T>();
    
    public static T FromId<T>(int id) where T : Enumeration =>
        GetAll<T>().FirstOrDefault(e => e.Id == id) ??
        throw new InvalidOperationException($"'{id}' is not a valid ID in {typeof(T)}");
    
    public static T FromName<T>(string name) where T : Enumeration =>
        GetAll<T>().FirstOrDefault(e => e.Name.Equals(name, StringComparison.OrdinalIgnoreCase)) ??
        throw new InvalidOperationException($"'{name}' is not a valid name in {typeof(T)}");
    
    public int CompareTo(object? other) => Id.CompareTo(((Enumeration)other!).Id);
    
    public override bool Equals(object? obj) =>
        obj is Enumeration other && GetType() == other.GetType() && Id == other.Id;
    
    public override int GetHashCode() => Id.GetHashCode();
}
