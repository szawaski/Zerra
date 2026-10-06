# Security Policy

## Supported Versions

| Version | Supported |
|---|---|
| 6.0 (in development, `master`) | yes |
| 5.4 (current NuGet release, `release/5.4.0`) | yes |
| older | no |

## Reporting a Vulnerability

Report vulnerabilities privately through GitHub's [private vulnerability reporting](https://github.com/szawaski/Zerra/security/advisories/new), not in a public issue. Include the affected package and version, how to reproduce it, and its impact. A fix is released for the supported versions before the report is made public.

## Security Model

Zerra separates public traffic from service-to-service traffic. [Security](docs/Security.md) describes it in full; in short:

- **The `Zerra.Web` API gateway is the public entry point.** It authorizes every request through `ICqrsAuthorizer` or ASP.NET Core authentication, restricts browser origins, exposes only the interfaces its bus registers, and never accepts events from outside.
- **Direct TCP, HTTP, and broker connections are for services inside a private network.** They trust the claims a caller sends so that a handler sees the original user's identity across service hops. Message encryption with a shared key limits valid senders to services holding the key.
- **A handler's exception, including its stack trace, is returned to the caller** so failures can be diagnosed across services.

Reports showing that the gateway can be bypassed, that a message can be read or forged without the encryption key, or that a malformed message can crash a server or consume unbounded resources are in scope. Reports that internal transports trust caller claims, or that exceptions reach the caller, describe the design above.
