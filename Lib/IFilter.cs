using MongoDB.Driver;

namespace MongoTokenPaging.Lib;

public interface IFilter<TModel>
where TModel: class
{
    FilterDefinition<TModel> BuildFilter();
}