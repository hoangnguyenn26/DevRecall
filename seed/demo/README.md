# Demo data boundary

The portfolio scenario lives in `tools/DevRecall.SeedTool/PortfolioDemoSeeder.cs`.
Run the explicit `demo` command after importing the catalog; see
`docs/portfolio-dataset-seeding.md` for environment setup and replay behavior.

`verify-portfolio.sql` is a read-only initial-dataset smoke check. Run it before
personally changing the account, and repeat after rerunning the seed command.
User activity is never part of the global LearningContent catalog import.
