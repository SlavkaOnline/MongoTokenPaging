using System.Diagnostics;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using MongoTokenPaging.Lib;

var client = new MongoClient("mongodb://root:example@localhost:27017");
var db = client.GetDatabase("test_paging");
var collection = db.GetCollection<Model>("Model");

Console.WriteLine("TokenPager");
await TokenPager();

Console.WriteLine();

Console.WriteLine("CursorPager");
await CursorPager();

Console.WriteLine();

Console.WriteLine("SimplePager");
await SimplePager();


async Task InsertData(IMongoCollection<Model> collection)
{
    var random = new Random();
    var data = Enumerable.Range(0, 10_0000).Select(_ => new Model(
        Prop1: $"prop{random.NextInt64(0, 10_0000)}",
        Prop2: random.Next(0, 10_0000),
        Prop3: random.Next(0, 10_0000) % 2 == 0
    ));

    await collection.InsertManyAsync(data);
}

async Task TokenPager()
{
    var pageCount = 1;
    var filter = new Filter();
    var sort = new SortBuilder<Model>().WithIdAsc();
    var page = 20;
    
    var lastId = "";
    
    var sw = new Stopwatch();
    sw.Start();

    var response = await collection.GetPageAsync(filter, sort, page);
    while (response.NextToken is not null)
    {
        pageCount++;
        response = await collection.GetPageAsync(filter, sort, page, response.NextToken);

        if (response.Items.Any())
            lastId = response.Items.Last().Id;
    }
    sw.Stop();
    Console.WriteLine($"page count {pageCount}");
    Console.WriteLine($"lastId {lastId}");
    Console.WriteLine($"Elapsed: {sw.Elapsed}");
}

async Task SimplePager()
{
    var pageCount = 1;
    var page = 0;
    var sw = new Stopwatch();

    var lastId = "";
    
    sw.Start();
    var response = await collection.Find(FilterDefinition<Model>.Empty)
        .Sort(Builders<Model>.Sort.Ascending(x => x.Id))
        .Skip(page)
        .Limit(20)
        .ToListAsync();
    
    while (response.Count > 0)
    {
        pageCount++;
        page += 20;
        response = await collection.Find(FilterDefinition<Model>.Empty)
            .Sort(Builders<Model>.Sort.Ascending(x => x.Id))
            .Skip(page)
            .Limit(20)
            .ToListAsync();

        if (response.Any()) 
            lastId = response.Last().Id;
    }
    
    sw.Stop();
    Console.WriteLine($"page count {pageCount}");
    Console.WriteLine($"lastId {lastId}");
    Console.WriteLine($"Elapsed: {sw.Elapsed}");
}

async Task CursorPager()
{
    var pageCount = 1;
    var lastId = "";
    
    var sw = new Stopwatch();
    sw.Start();
    
    using var cursor = await collection.Find(FilterDefinition<Model>.Empty, new FindOptions() {BatchSize = 20})
        .Sort(Builders<Model>.Sort.Ascending(x => x.Id))
        .ToCursorAsync();
    
    while (await cursor.MoveNextAsync())
    {
        pageCount++;
        if (cursor.Current.Any())
            lastId = cursor.Current.Last().Id;
    }
    sw.Stop();
    Console.WriteLine($"page count {pageCount}");
    Console.WriteLine($"lastId {lastId}");
    Console.WriteLine($"Elapsed: {sw.Elapsed}");
}


record Filter() : IFilter<Model>
{
    public FilterDefinition<Model> BuildFilter()
    {
        return Builders<Model>.Filter.Empty;
    }
}


record Model(
    string Prop1,
    int Prop2,
    bool Prop3
)
{
    [property: BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; init; } = ObjectId.GenerateNewId().ToString();
}