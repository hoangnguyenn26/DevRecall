# Knowledge Workspace

## Purpose

The Knowledge Workspace turns captured notes into a focused browse, organize, read, and edit workflow. On wide screens it uses independent topic, list, and detail panes. On smaller screens the same state moves through a list/detail drill-down without mounting a second editor.

Topics and tags have intentionally different roles: a topic is the primary hierarchical classification, while tags are cross-cutting, multi-dimensional classification. Selecting a topic includes its descendants; selecting multiple tags uses AND semantics.

## Routes and query state

The selected resource is represented by the route:

- `/app/knowledge` shows the list context.
- `/app/knowledge/{knowledgeId}` shows a selected item within that context.

List state is represented by `query`, `topicId`, `topicScope=uncategorized`, `tagIds`, `sort`, and `page`. Canonicalization removes default or invalid values, trims blank search, makes tag IDs distinct and sorted, and prevents `topicId` and `topicScope` from coexisting. If a new filter excludes the selected resource, navigation returns to `/app/knowledge` while preserving the valid filters.

Browser Back and Forward therefore restore meaningful list/detail context. List scroll and keyboard focus are restored only when the canonical filter key and page still match.

## Data boundaries

All reads are owner-scoped and use no-tracking projections. The list is filtered and paginated in PostgreSQL before materialization. Its items contain identity, title, summary, topic summary, limited tag previews, tag count, timestamps, and version; full content and related items are not loaded into the list.

Detail content is fetched separately when the selected route changes. A detail failure does not replace the topic tree or list. The topic tree is read in one bounded projection rather than querying once per node. Its builder tracks visited IDs and stops below the configured maximum depth; malformed cycles are emitted at most once as detached roots instead of recurring indefinitely.

Content is rendered as plain text in the current MVP. It is not inserted with `v-html`, so raw HTML, scripts, event handlers, and `javascript:` links are not executed. Long prose wraps and preformatted code scrolls inside the article boundary.

## Editing and concurrency

Entering edit mode clones title, content, topic, tags, and version from the detail response. Dirty comparison trims domain-normalized text and compares distinct, sorted tag IDs, so tag reordering is a no-op.

Every update sends `expectedVersion`. A successful multi-field change advances the resource once; a normalized no-op keeps its existing version and timestamp. A stale update returns `409 KNOWLEDGE_CONFLICT`. The client keeps the local draft, never retries the mutation automatically, and requires confirmation before reloading the latest version or leaving a dirty editor.

## Related Knowledge and global search

Related Knowledge is owner-scoped, excludes the current resource, and returns at most five items. Ranking favors shared tags, then the same topic, recent updates, and a stable ID fallback. The UI exposes a human-readable reason rather than the internal score. Following a related item clears incompatible list filters.

Global Search crosses only Knowledge, Interview Questions, and DSA Problems. Queries and result sets are bounded, results contain previews rather than full private content, and target paths are produced by trusted metadata mappings. The command palette debounces requests, cancels stale work, does not persist raw queries, and keeps static navigation commands available if resource search fails.

## Query keys and invalidation

Canonical query keys are:

- `knowledge:topic-tree`
- `knowledge:list:{canonicalFilters}`
- `knowledge:detail:{id}` when a cached detail query is used
- `knowledge:related:{id}` when related data is queried separately
- `knowledge:tags:{normalizedQuery}`
- `global-search:{normalizedQuery}`

List filter keys use fields in a fixed order and sorted tag IDs. Mutations invalidate the narrowest affected projections: title/content updates refresh the active list and detail; topic changes additionally refresh the topic tree; tag changes refresh tag options; delete refreshes list, tree, and tags; quick capture refreshes the active list, topic tree, optional tags, and Today summary.

## Responsive and keyboard behavior

Desktop presents three independently scrollable panes. Tablet and mobile use the same route and editor state with a topic drawer and list/detail drill-down. Resizing preserves the draft and version because the editor is mounted only once. Expanded topic IDs are a per-user UI preference; the tree payload itself is never stored in the cookie.

Available shortcuts:

| Shortcut | Action |
| --- | --- |
| `/` | Focus Knowledge search |
| `Arrow Up` / `Arrow Down` | Move the active list item |
| `Enter` | Open the active item |
| `E` | Edit the selected Knowledge item |
| `Ctrl/Cmd + S` | Save changes |
| `Ctrl/Cmd + K` | Open Global Search and commands |
| `G`, then `K` | Open Knowledge |

Shortcuts are ignored inside editable controls, pickers, dialogs, and slideovers. List links use roving `tabindex`; the active keyboard cursor remains separate from the route-selected item. Loading regions expose `aria-busy`, visible focus is preserved, dialogs manage focus, and reduced-motion preferences disable nonessential transitions without hiding progress feedback.
