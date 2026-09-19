# Agent Instructions

See [docs/Agents.md](docs/Agents.md) for how to build applications with Zerra (start with "Building an Application"), the framework's architecture, and the conventions for changing the framework itself. `Demo/Store` is the reference sample.

Before writing any handler, read [Command or Event?](docs/Agents.md#command-or-event-read-this-first): a command is handled once, an event reaches every replica of every subscriber, so work that must happen exactly once never goes in an event handler.
