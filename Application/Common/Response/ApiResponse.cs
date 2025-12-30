namespace Application.Common.Response;

// Вспомогательные классы для API ответов
public class ApiResponse<T>
{
    public bool Success { get; set; } = true;
    public string Message { get; set; }
    public T Data { get; set; }

    public ApiResponse(T data, string message = null)
    {
        Data = data;
        Message = message;
    }
}

public class ApiErrorResponse
{
    public bool Success { get; set; } = false;
    public string Message { get; set; }
    public IEnumerable<string> Errors { get; set; }

    public ApiErrorResponse(string message, IEnumerable<string> errors = null)
    {
        Message = message;
        Errors = errors;
    }
}

public class PagedResult<T>
{
    public List<T> Items { get; set; }
    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
}