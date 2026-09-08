# Implementation report

## Scope and architecture

Implemented a .NET 8 Clean Architecture solution with Domain, Application, Infrastructure, API,
unit-test, and integration-test projects. Application services are exposed through interfaces;
controllers contain only HTTP concerns. EF Core is used directly in the Infrastructure service
instead of adding a generic repository that would duplicate DbContext.

Implemented modules: Asset Type, Type Attributes, Asset Category, Asset Model, Manufacturer,
Supplier, Asset Status, RFID Tag, Barcode, Custom Attribute Definition, and Asset Image.

Cross-cutting implementation includes Identity, idempotent Admin seeding, JWT, role authorization,
Swagger bearer support, CORS configuration, login rate limiting, health checks, FluentValidation,
central exception mapping, structured logs, UTC audit fields, optimistic concurrency, soft deletion,
async/cancellation support, safe uploads, pagination, lookups, uniqueness constraints, and initial
EF migration.

## Source analysis and limitation

Workspace inventory found `TEAIP_Prototype.html` but none of the eleven PDS DOCX documents named in
the request. The prototype was analyzed as source and workflow data, including all eleven Asset Data
SBO schemas, generic-object forms, Type Attributes, assignment tabs, asset image tabs, data-table
controls, JavaScript state, and synchronized record structures. Browser automation was not available
in this environment; the same rendering/event logic was traced in source.

Because the PDS is the declared primary source of truth, exact PDS compliance cannot be asserted.
No PDS/HTML conflicts can be responsibly reported when the PDS files were not supplied.

## Prototype-backed rules

- Custom fields are the extension mechanism and are associated with an Asset Type.
- Type attribute code is unique within a type; a code becomes immutable after recorded values exist.
- Requirement changes affect new/edited records without retroactively invalidating old values.
- RFID identifiers are never reused. Replacement retires the old identifier so historical reads
  remain meaningful.
- RFID and barcode stock is assigned to assets through explicit actions.
- Barcode symbology is captured.
- Asset images carry asset, file, primary flag, caption, captured timestamp/user, and purpose.
- Asset Type can override asset numbering format. Existing asset numbers are not rewritten.
- Asset Type controls category, serial/RFID/barcode requirements, default status, permitted
  transitions, custom schema, depreciation defaults, useful life, and numbering.
- Asset Model Number is unique within Manufacturer.
- Asset Status carries category, color, operational/terminal flags, and movement-blocking behavior.
- Barcode identity is Value plus Symbology; DataMatrix and EAN are supported alongside Code128,
  Code39, and QR.
- Name-bearing records support an optional alternate-language name.

## Assumptions

1. Business master data uses recoverable soft deletion because no PDS deletion rule was available.
2. Codes are limited to 50, names to 200, descriptions/help to 1000, and images to 10 MB. These are
   conservative implementation limits, not claimed PDS values.
3. One active RFID tag and one active barcode per asset matches the prototype's singular asset fields.
4. Local private file storage is used through `IFileStorageService`; database rows hold metadata only.
5. Supported image types are JPEG, PNG, and WebP. Barcode symbologies follow the prototype:
   Code128, Code39, QR, DataMatrix, and EAN.
6. Read endpoints are anonymous and all writes require Admin. The absent PDS may define finer-grained
   read/write permissions.
7. `Asset` is included as a supporting entity/lookup because assignments and images require a real
   relational subject.

## Unresolved requirements

- PDS-defined canonical names, exact required fields, lengths, indexes, identifier formats, status
  transition matrix, retention periods, audit/event requirements, and authorization matrix.
- Whether Custom Attribute Definition and Type Attribute are distinct PDS SBOs or two views of one
  definition. The implementation separates reusable definition from type-specific assignment.
- Country/organization dictionaries and supplier-rating enumerations.
- Physical tag encoding, batch procurement, and printer integrations. Prototype buttons only display
  toast messages, so no external-system behavior was invented.
- Asset custom-value capture belongs to the Asset Register workflow, not the listed Asset Data menu,
  and was not added without PDS confirmation.

## Prototype inconsistencies and gaps

- Generic Asset CRUD writes to `DB.records['Asset']`, while operational screens use `DB.assets`;
  synchronization is one-way and can overwrite generic edits.
- The Asset SBO lifecycle (`Draft → Active → In Maintenance → Suspended → Disposed → Archived`)
  differs from runtime statuses (`Working`, `Damaged`, `Missing`, `In Transit`, and others).
- Type Attributes has a grouped add-only screen while Custom Attribute Definition has generic flat
  CRUD. The API supports both a combined per-type add operation and separate definition/assignment.
- Tag batch ordering, label printing, image upload, and history are toast/static prototype stubs.
  Storage and image locking are implemented here; hardware printing/encoding is left unresolved.

## Security notes

The production signing key is intentionally blank in `appsettings.json`; deployment must inject it.
The development file contains an explicitly marked local key. Original upload names are never used
as storage names, path components are stripped, content signatures are checked, and stored files are
served through an API endpoint rather than public static-file middleware.

## Verification record

- `dotnet restore AssetsManagement.sln`: succeeded.
- `dotnet build AssetsManagement.sln --no-restore`: succeeded with 0 warnings and 0 errors.
- `dotnet test AssetsManagement.sln --no-restore`: 18 passed (9 unit, 9 integration),
  0 failed.
- Initial and prototype-alignment migrations were created and applied successfully to SQL Server
  LocalDB database `AssetsManagement`; EF reports no pending model changes.
- Live API smoke test: Swagger 200; idempotent Admin seed succeeded; `admin` / `Admin@123` login
  returned an Admin bearer JWT; authenticated create/get succeeded; pagination/search returned the
  expected record; valid PNG upload returned 201-equivalent success, content returned 200 with 68
  bytes, and authenticated deletion succeeded.
- Integration verification also covers invalid login, unauthorized write, validation 400, duplicate
  409, missing 404, category parent relationship, combined type-attribute creation, and invalid
  image-content 422.
