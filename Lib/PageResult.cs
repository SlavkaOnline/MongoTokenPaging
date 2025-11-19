namespace MongoTokenPaging.Lib;

public readonly record struct PageResult<TProj>(
    IReadOnlyCollection<TProj> Items,
    string? NextToken
    );