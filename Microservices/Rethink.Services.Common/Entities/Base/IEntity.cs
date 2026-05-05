namespace Rethink.Services.Common.Entities.Base
{
    public interface IEntity<TKey>
    {
        TKey Id { get; set; }
    }

    public interface IEntity : IEntity<int>
    {

    }
}
