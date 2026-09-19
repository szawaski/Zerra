# Agent Instructions

See [docs/Agents.md](docs/Agents.md) for how to build applications with Zerra (start with "Building an Application"), the framework's architecture, and the conventions for changing the framework itself. `Demo/Store` is the reference sample.

Before writing any handler, read [Command or Event?](docs/Agents.md#command-or-event-read-this-first): a command is handled once, and an event reaches every replica of every subscriber unless that subscriber registers its consumer `EventConsumerMode.PerService`. So work that must happen exactly once goes in a command, or in an event handler whose consumer is registered `PerService` - never in one registered `PerReplica`.

`AddEventConsumer` has no default mode: every registration states `PerReplica` or `PerService`.
