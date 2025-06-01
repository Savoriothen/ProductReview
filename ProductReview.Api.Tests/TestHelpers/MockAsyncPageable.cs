#nullable enable
using Azure;
using Moq;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace ProductReview.Api.Tests.TestHelpers
{
    public class MockAsyncPageable<T> : AsyncPageable<T> where T : notnull
    {
        private readonly IEnumerable<T> _items;

        public MockAsyncPageable(IEnumerable<T> items)
        {
            _items = items;
        }

        public async IAsyncEnumerable<T> AsAsyncEnumerable([EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            foreach (var item in _items)
            {
                yield return item;
                await Task.Yield(); // Simulate async behavior
            }
        }

        public override async IAsyncEnumerable<Page<T>> AsPages(string? continuationToken = null,
            int? pageSizeHint = null)
        {
            var page = Page<T>.FromValues(_items.ToList(), null, Mock.Of<Response>());
            yield return page;
            await Task.Yield();
        }
    }
}
