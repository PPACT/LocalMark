namespace LocalMark.Repository;

/// <summary>
/// 泛型仓储接口
/// </summary>
public interface IBaseRepo<T> where T : class
{
    IEnumerable<T> GetAll();
    T? GetById(int id);
    int Insert(T entity);
    void Update(T entity);
    void Delete(int id);
}
