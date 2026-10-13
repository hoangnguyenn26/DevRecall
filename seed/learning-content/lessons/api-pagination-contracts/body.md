<!-- devrecall-section: pagination-is-an-api-contract -->
A list endpoint should not load every matching row and then slice the result in memory. The request needs a bounded window, and the database query should apply filters, ordering, projection, and pagination before materialization. This protects response time and memory as data grows.

The public contract should define defaults and limits: page or cursor input, maximum page size, returned items, and navigation information. A client-supplied `pageSize=1000000` is not meaningful pagination. Reject or clamp out-of-range values consistently rather than allowing one request to become an accidental export job.

<!-- devrecall-section: ordering-before-windowing -->
Pagination without deterministic ordering has no stable meaning. Even ordering by a non-unique field is incomplete. If many records share the same `createdAt`, the database is free to return ties in different orders. Add a unique tie-breaker, such as `id`, and keep direction consistent:

```text
ORDER BY created_at_utc DESC, id DESC
```

The same ordering definition must be used when creating or interpreting a cursor. Changing filters or sort order while reusing an old cursor should be rejected or modeled as a new traversal.

<!-- devrecall-section: offset-pagination -->
```csharp
var items = await query
    .OrderByDescending(item => item.CreatedAtUtc)
    .ThenByDescending(item => item.Id)
    .Select(item => new ItemSummary(item.Id, item.Title))
    .Skip((page - 1) * pageSize)
    .Take(pageSize)
    .ToListAsync(cancellationToken);
```

Offset pagination maps naturally to page numbers and arbitrary jumps. It is often appropriate for bounded management screens. Its cost can grow for deep pages because the database still has to locate and skip earlier rows. Concurrent inserts or deletes before the current offset can also shift later windows, so a user may see an item twice or miss one while paging.

<!-- devrecall-section: keyset-pagination -->
Keyset pagination continues after the last ordering values from the previous page. For descending `created_at_utc, id`, the conceptual PostgreSQL predicate is:

```sql
WHERE created_at_utc < @last_created_at
   OR (created_at_utc = @last_created_at AND id < @last_id)
ORDER BY created_at_utc DESC, id DESC
LIMIT @page_size;
```

The cursor can encode those values as an opaque token. Protect it from malformed or incompatible input, and do not expose a token as if it were a permanent record identifier. Keyset pagination avoids scanning an ever-growing offset and is more stable for forward traversal, but it does not naturally support jumping directly to page 57.

<!-- devrecall-section: choose-for-the-navigation-model -->
Choose based on the product interaction and query shape. Offset pagination is reasonable when datasets are bounded and users need page numbers. Keyset pagination is a strong fit for feeds, history streams, and "load more" navigation. Neither choice removes the need for suitable indexes aligned with filters and ordering.

Total counts also have a cost. Include them only when the UI needs them and the query can provide them affordably. A continuation indicator such as `hasMore` can be enough for sequential navigation.

<!-- devrecall-section: key-takeaway -->
Paginate inside the database using a bounded page size and a fully unique order. Use offsets for simple page navigation and keysets for efficient, stable sequential traversal; choose the contract from the user interaction rather than habit.
