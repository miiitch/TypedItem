using System.Collections.Generic;

namespace TypedItem.Lib
{
    public record DataQueryResponse<T>
    {
        public List<T> Results { get; set; }

        public string? ContinuationToken { get; set;}
        
    }
}
