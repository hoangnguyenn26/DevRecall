# Accessibility baseline

DevRecall targets a practical WCAG 2.2 AA baseline for its authenticated learning workflows and public pages.

## Keyboard model

- `Tab` and `Shift+Tab` traverse interactive controls in visual order.
- `Ctrl+K` or `Cmd+K` opens the command palette; closing it restores focus to the trigger.
- Review focus mode uses `Space` to reveal an answer and `1`–`4` for Again, Hard, Good, and Easy after reveal.
- Focus workspaces keep visible exit controls and do not depend on pointer-only actions.
- Dialogs trap focus, have an accessible title, close with `Escape`, and restore focus when dismissed.

## UI requirements

- Every input and filter has a visible label or an explicit accessible name.
- Validation errors are associated with their fields; asynchronous status and failures use live regions where appropriate.
- Loading, empty, filtered-empty, offline, and error states are distinct. Empty states offer a relevant next action when one exists.
- Charts include a summary sentence and a textual/table equivalent; color is not the only carrier of severity or status.
- Study progress announces completed, skipped, and remaining item counts.
- Light and dark themes retain visible focus indicators and semantic contrast.
- Reduced-motion preferences disable nonessential motion through the global stylesheet.

## Verification checklist

For release validation, complete Login → Today → Knowledge → Review → Interview → DSA → Study Plan → Study Session → Analytics using only the keyboard. Spot-check browser accessibility trees for the app shell, command palette, one mutation dialog, Review rating controls, Study Session progress, and an analytics chart. Test at 200% zoom and at mobile, tablet, and desktop widths.

Automated checks support this baseline but do not replace the keyboard and screen-reader spot checks above.
