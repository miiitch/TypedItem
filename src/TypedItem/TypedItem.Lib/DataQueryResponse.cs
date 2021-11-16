using System.Collections.Generic;

namespace TypedItem.Lib
{
    public record DataQueryResponse<T>(List<T> Results, string? ContinuationToken=null)
    {
        public List<T> Results { get; } = Results;

        public int Count => Results.Count;
        public string? ContinuationToken { get;} = ContinuationToken;

        public double RequestCharge { get; set; }

        public string? SessionToken { get; set; }
    }
}
