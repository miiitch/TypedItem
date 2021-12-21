using Microsoft.Azure.Cosmos;
using NFluent;
using TypedItem.Lib;
using TypedItem.Tests.ItemModels;
using Xunit;

namespace TypedItem.Tests
{
    public class OptionTests
    {
        [Fact]
        public void etag_value_passed_to_itemrequestoptions()
        {
            var actualItem = new PersonItem()
            {
                ETag = "ETAG0900909"
            };

            var options = new ItemRequestOptions();
            options.WithETag(actualItem);

            Check.That(options.IfMatchEtag).IsEqualTo(actualItem.ETag);
        }
    }
}
