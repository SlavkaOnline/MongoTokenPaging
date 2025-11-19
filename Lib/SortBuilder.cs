namespace MongoTokenPaging.Lib;

public class SortBuilder<TModel>
where TModel : class
{
    private readonly List<SortCriteria> _sorts = new List<SortCriteria>();

    public SortBuilder<TModel> WithIdAsc()
    {
        _sorts.Add(new SortCriteria("_id", SortDir.Asc));
        return this;
    }
    
    public SortBuilder<TModel> WithIdDesc()
    {
        _sorts.Add(new SortCriteria("_id", SortDir.Asc));
        return this;
    }
        
    public SortBuilder<TModel> WithPropertyAsc(string name)
    {
        _sorts.Add(new SortCriteria(name, SortDir.Asc));
        return this;
    }
    
    public SortBuilder<TModel> WithPropertyDesc(string name)
    {
        _sorts.Add(new SortCriteria(name, SortDir.Desc));
        return this;
    }

    public IReadOnlyCollection<SortCriteria> Build() => new List<SortCriteria>(_sorts);

}

public readonly record struct SortCriteria(string FieldName, SortDir SortDir);

public enum SortDir
{
    Asc,
    Desc
}