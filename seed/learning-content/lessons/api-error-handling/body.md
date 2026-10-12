<!-- devrecall-section: errors-are-part-of-the-contract -->
Invalid input, a missing resource or a stale edit are ordinary application outcomes. Clients need to know what action is possible: fix input, reload, sign in or stop. An unexpected exception is different: the service could not complete the request as designed. Do not collapse every outcome into 500 or return 200 with a hidden failure flag.

<!-- devrecall-section: an-intentional-representation -->
Problem Details gives errors a common structure, with status, title, type and optional detail/instance. Validation errors can add field messages; an application can add a stable code. Status expresses the broad HTTP outcome, while a code distinguishes actionable causes within that category. Human text can improve or be localized without forcing clients to parse it as an identifier.

<!-- devrecall-section: a-stale-edit-response -->
```json
{
  "type": "https://example.com/problems/order-edit-conflict",
  "title": "Order changed since it was loaded",
  "status": 409,
  "code": "ORDER_EDIT_CONFLICT",
  "traceId": "opaque-correlation-id"
}
```

This is illustrative: the HTTP response status must also be 409. The client can offer reload rather than automatically repeating a stale mutation. The URI identifies an error type; it is not a lesson source URL.

<!-- devrecall-section: keep-the-boundary-safe -->
Translate known failures consistently at the API boundary: validation, not found, permission and conflict retain meaningful semantics. Central exception handling can give unknown failures a generic 500 response and an opaque correlation ID. Do not send stack traces, SQL, secrets or personal answers to clients. Server diagnostics still require redaction and access control; logging every request body is not a safe substitute. Requested cancellation is not automatically an unexpected error.

<!-- devrecall-section: key-takeaway -->
API errors should be deliberate contracts. Use HTTP semantics and stable machine-readable error identifiers for expected outcomes; expose safe generic responses for unexpected failures, with correlation rather than private implementation details.
