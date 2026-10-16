namespace M.Core.Base;

public static class Pagination
{
    public const int MaxPageSize = 200;

    public static (int PageNumber, int PageSize) Normalize(int pageNumber, int pageSize)
    {
        if (pageNumber < 1 || pageSize < 1)
            throw new BaseException.ErrorException(400, ResponseCodeConstants.BAD_REQUEST,
                "Page number and page size must be positive.");
        return (pageNumber, Math.Min(pageSize, MaxPageSize));
    }
}
