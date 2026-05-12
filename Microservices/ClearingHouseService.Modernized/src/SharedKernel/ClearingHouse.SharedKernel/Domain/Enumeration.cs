namespace ClearingHouse.SharedKernel.Domain;

public abstract class Enumeration : IComparable
{
    public int Id { get; }
    public string Name { get; }

    protected Enumeration(int id, string name) => (Id, Name) = (id, name);

    public override string ToString() => Name;
    public override bool Equals(object? obj)
    {
        if (obj is not Enumeration other) return false;
        return GetType() == other.GetType() && Id == other.Id;
    }
    public override int GetHashCode() => Id.GetHashCode();
    public int CompareTo(object? other) => Id.CompareTo(((Enumeration)other!).Id);

    public static IEnumerable<T> GetAll<T>() where T : Enumeration =>
        typeof(T).GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.DeclaredOnly)
            .Select(f => f.GetValue(null))
            .Cast<T>();
}
