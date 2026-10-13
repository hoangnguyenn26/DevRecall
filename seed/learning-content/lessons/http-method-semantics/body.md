<!-- devrecall-section: methods-communicate-intent -->
An HTTP method is part of an API contract. It tells clients, gateways, caches, and diagnostic tools what kind of operation a request intends. `GET` retrieves a representation. `POST` asks a resource to process submitted data, often creating a server-selected child resource. `PUT` creates or replaces the state of a client-selected target. `PATCH` applies a partial modification according to the patch format. `DELETE` requests removal of the target resource.

The URI identifies the resource; the method supplies the operation semantics. A route such as `GET /orders/42?operation=cancel` contradicts that model because a retrieval method now performs a business mutation. Automated crawlers, prefetchers, and retry behavior rely on the declared semantics being truthful.

<!-- devrecall-section: safe-and-idempotent-are-different -->
A method is **safe** when its defined intent is essentially read-only. GET and HEAD are common safe methods. The server may still record logs or metrics, but the client did not request a business-state change.

A method is **idempotent** when applying the same request multiple times has the same intended server effect as applying it once. Safe methods are idempotent. PUT and DELETE are also idempotent even though they can change state. POST and PATCH are not guaranteed to be idempotent; a particular API operation can add that property through its own contract.

Idempotency does not require identical responses. The first DELETE might return `204 No Content`; a later identical request might return `404 Not Found`. The resource is absent after either sequence, so the intended effect is still idempotent. Side effects outside that intent, such as a separate access log entry for every request, do not redefine the method.

<!-- devrecall-section: a-resource-oriented-example -->
```http
GET    /api/orders/42          # retrieve the current representation
POST   /api/orders             # create using a server-selected identifier
PUT    /api/orders/42          # replace the client-selected order representation
PATCH  /api/orders/42          # apply a defined partial update
DELETE /api/orders/42          # request removal
```

This is a semantic starting point, not a rule that every domain action must be forced into CRUD. An action with distinct business meaning can be modeled as a resource of its own, such as `POST /api/orders/42/cancellation`. The important part is that the method and target together state an honest, stable intent.

<!-- devrecall-section: semantics-before-route-style -->
Do not infer retry safety from a method name alone without considering the operation contract. A PUT handler that sends a new payment on every invocation violates the method's intended idempotency. Conversely, a POST operation can be designed for safe retry by assigning a stable operation identity and deduplicating repeats.

Also avoid using status codes to redefine method intent. Returning `200 OK` does not make a state-changing GET safe, and returning `201 Created` does not by itself make a POST retry-safe. Method semantics guide the design; implementation and response semantics must uphold that promise.

<!-- devrecall-section: key-takeaway -->
Choose HTTP methods by the client's intent toward a resource. Safe means read-only intent; idempotent means repeatable intended effect. Correct implementations preserve those semantics even when response details or incidental logging differ.
