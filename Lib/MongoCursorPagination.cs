using System.Text;
using System.Text.Json;
using MongoDB.Bson;
using MongoDB.Driver;

namespace MongoTokenPaging.Lib;

public static class MongoCursorPagination
{
    public static async Task<PageResult<T>> GetPagedAsync<T>(
        this IMongoCollection<T> collection,
        IFilter<T> filter,
        SortBuilder<T> baseSortBuilder,
        int pageSize,
        string? encodedToken
        )
    where T: class
    {
        Dictionary<string, object>? lastValues = null;
        if (!string.IsNullOrEmpty(encodedToken))
        {
            lastValues = DecodeToken(encodedToken);
        }

        var baseFilter = filter.BuildFilter();
        var sortCriteria = baseSortBuilder.Build();
        
        var builder = Builders<T>.Filter;
        var finalFilter = baseFilter;
        
        if (lastValues is {Count: > 0})
        {
            var cursorConditions = new List<FilterDefinition<T>>();
            var previousEqualities = new List<FilterDefinition<T>>();

            foreach (var sort in sortCriteria)
            {
                if (!lastValues.TryGetValue(sort.FieldName, out var lastValueObj)) continue;
                
                var lastValue = BsonValue.Create(lastValueObj);

                FilterDefinition<T> rangeFilter;
                rangeFilter = sort.SortDir == SortDir.Asc 
                    ? builder.Gt(sort.FieldName, lastValue) 
                    : builder.Lt(sort.FieldName, lastValue);

                var levelFilter = previousEqualities.Count > 0
                    ? builder.And(builder.And(previousEqualities), rangeFilter)
                    : rangeFilter;

                cursorConditions.Add(levelFilter);

                previousEqualities.Add(builder.Eq(sort.FieldName, lastValue));
            }

            if (cursorConditions.Count > 0)
            {
                var cursorFilter = builder.Or(cursorConditions);
                finalFilter = builder.And(baseFilter, cursorFilter);
            }
        }

        var sortBuilder = Builders<T>.Sort;
        var sortDefs = new List<SortDefinition<T>>();
        foreach (var sort in sortCriteria)
        {
            sortDefs.Add(sort.SortDir == SortDir.Asc 
                ? sortBuilder.Ascending(sort.FieldName) 
                : sortBuilder.Descending(sort.FieldName));
        }
        var finalSort = sortBuilder.Combine(sortDefs);

        var data = await collection.Find(finalFilter)
                                   .Sort(finalSort)
                                   .Limit(pageSize)
                                   .ToListAsync();

        string? nextToken = null;
        if (data.Count > 0)
        {
            var lastItem = data.Last();
            var tokenData = new Dictionary<string, object>();
            
            var bsonDoc = lastItem.ToBsonDocument(); 

            foreach (var sort in sortCriteria)
            {
                if (bsonDoc.Contains(sort.FieldName))
                {
                    var val = BsonTypeMapper.MapToDotNetValue(bsonDoc[sort.FieldName]);
                    tokenData[sort.FieldName] = val;
                }
            }
            nextToken = EncodeToken(tokenData);
        }

        return new (data, nextToken);
    }

    private static string EncodeToken(Dictionary<string, object> token)
    {
        var json = JsonSerializer.Serialize(token);
        return Convert.ToBase64String(Encoding.UTF8.GetBytes(json));
    }

    private static Dictionary<string, object>? DecodeToken(string token)
    {
        try 
        {
            var json = Encoding.UTF8.GetString(Convert.FromBase64String(token));
            return JsonSerializer.Deserialize<Dictionary<string, object>>(json);
        }
        catch
        {
            return null; 
        }
    }
}