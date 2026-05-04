# CovenantCouncil

![Covenant Council logo](docs/assets/covenant-council-logo.svg)

Covenant Council is a .NET MAUI Native application for generating, viewing, and managing certificate-signed licenses as defined by `TIKSN-Framework`.

## Projects

- `src/CovenantCouncil.App` is the executable MAUI Native application for Windows, macOS, iOS, and Android.
- `src/CovenantCouncil.Core` owns domain-level composition and registers TIKSN, Fossa, and Verdant licensing services.
- `src/CovenantCouncil.Infrastructure` owns SQLite/EF Core persistence, SQL bootstrap scripts, password-based protection, certificate import, and license storage.
- `src/CovenantCouncil.UseCases` defines application-facing service contracts and request/response models.
- `src/CovenantCouncil.ViewModels` contains ReactiveUI view models for the MAUI app.

## Data

The main database is a portable SQLite file with the `.ccdb` extension. The app prompts the user to create or open a database and requires the password on every startup. License exports use the `.cclic` extension.

EF Core is used for data access, but EF migrations are intentionally not used. The database schema is initialized by the embedded idempotent SQL script at `src/CovenantCouncil.Infrastructure/Data/Scripts/bootstrap.sql`.

Sensitive license payloads are encrypted before persistence. PFX passwords are never stored; they are used only during the current license issuing operation.

## Features

- Open or create a portable `.ccdb` database, then unlock it with a password.
- Keep the database gate as the only visible shell page until a database is loaded.
- Manage parties in one list, filtered by individual or organization.
- Add individual parties with email, website, first name, last name, and full name.
- Add organization parties with email, website, short name, and long name.
- Avoid duplicate party creation when every party field, including party kind, already matches an existing party.
- Delete parties that do not have dependent licenses.
- Manage public certificates as a certificate tree.
- Block certificate import unless the full issuer chain is supplied.
- Convert selected PFX files to public certificate records when issuing a license.
- Issue signed licenses from PFX certificates using TIKSN `LicenseFactory`.
- Generate license serial numbers automatically and show them as read-only during issuing.
- Select licensor and licensee parties from the opened database.
- Enter required not-before and not-after validity dates.
- Enter descriptor-specific entitlements for Fossa company, Fossa system, and VerdantApp system licenses.
- Select supported countries from a multi-select country list for system licenses.
- List immutable issued licenses with descriptor filtering.
- Discover license descriptors through compile-time service registration.
- Configure OpenTelemetry through the `COVENANTCOUNCIL_OTLP_ENDPOINT` environment variable.

## Build

Install the .NET 10 SDK and the .NET MAUI workload, then build:

```powershell
dotnet build CovenantCouncil.slnx
```

Run the MAUI app for a target platform from `src/CovenantCouncil.App`.
