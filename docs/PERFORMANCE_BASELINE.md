# DevRecall MVP Performance Baseline

Measured on 1 August 2026 against the local production Docker Compose profile after a warm-up request.

Bundle boundaries were rechecked on 10 August 2026 after the Week 24 reliability
work. The largest client chunk is 548.8 kB raw (186.8 kB gzip), a 0.6% raw increase
from the original baseline. Its only application importer is the Analytics route
chunk and it contains ECharts; public landing code does not import it.

## Results

| Check | Result |
| --- | ---: |
| Landing SSR response, five-request average | 65.4 ms |
| Auth API through the same-origin Nuxt proxy, warm unauthorized requests | 6.8–8.0 ms |
| Largest emitted JavaScript chunk | 548.8 kB raw / 186.8 kB gzip |
| npm production dependency audit | 0 vulnerabilities |

The largest chunk contains the analytics visualization path. Analytics charts are client-only and lazy-loaded, so this payload is not required for the landing page or the initial authenticated shell.

## Method

- Stack: the production images from `deploy/docker-compose.yml` on a local Windows development machine.
- Response timings: PowerShell `Measure-Command`/`Stopwatch`, without network latency or browser rendering time.
- Bundle sizes: files emitted by `nuxt build`; sizes are raw, before transport compression.

These numbers are a regression baseline, not a production service-level objective. Hardware, container cache state, TLS, and remote network latency will change the observed values.
