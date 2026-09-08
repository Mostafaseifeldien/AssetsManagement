# API documentation

Base path: `/api`. JSON responses use:

```json
{"success":true,"message":"...","data":{},"errors":[]}
```

Collection `data` contains `items`, `pageNumber`, `pageSize`, `totalCount`, `totalPages`,
`hasPreviousPage`, and `hasNextPage`. Common query parameters are `pageNumber` (default 1),
`pageSize` (default 20, maximum 100), `search`, `sortBy`, `sortDirection` (`asc`/`desc`), `isActive`,
`relatedEntityId`, and `status`.

All writes require an Admin JWT. Reads are anonymous to support prototype lookups. Validation errors
return 400, missing records 404, duplicate/concurrent updates 409, domain-rule failures 422,
authentication failures 401, and role failures 403.

## Authentication

`POST /api/auth/login` (anonymous, limited to 10 attempts/minute/IP)

```json
{"username":"admin","password":"Admin@123"}
```

Returns the bearer token, UTC expiry, username, and roles. Passwords and tokens are never logged.

## Master-data resources

The following resources share these operations:

- `/api/suppliers`
- `/api/asset-statuses`
- `/api/custom-attribute-definitions`

Operations:

- `GET /` — paged search/filter/sort.
- `GET /{id}` — details.
- `GET /lookup?search=` — active dropdown records, maximum 50.
- `GET /exists?code=&excludingId=` — exact uniqueness check.
- `POST /` — create.
- `PUT /{id}` — update; optional base64 row version in `If-Match`.
- `DELETE /{id}` — recoverable soft deletion.
- `POST /{id}/restore` — restore.

Common request fields are `code`, `name`, `alternateName`, and `description`. Resource-specific
fields are:

- Supplier: `supplierKind`, `taxRegistration`, `contactPerson`, `telephone`, `email`, `address`,
  `country`, `paymentTerms`, `rating`, `externalIdentifier`.
- Asset Status: `statusCategory`, `color`, `isOperational`, `isTerminal`, `blocksMovement`,
  `displayOrder`.
- Custom Attribute Definition: `dataType` (`Text`, `Number`, `Money`, `Date`, `YesNo`, `List`),
  `listValues`, `unit`, `helpText`, `alternateHelpText`.

Codes are required, maximum 50 characters, and unique per resource. Names are required and maximum
200 characters. Category parent cycles and invalid model relationships are rejected.

## Asset categories

Dedicated screen-aligned contract. List items return only `id`, `name`, `code`, and `active`.

- `GET /api/asset-categories` — query: `search`, `parentCategory` (parent name), `active`,
  `pageNumber`, `pageSize`, `sortBy`, `sortDirection`.
- `GET /api/asset-categories/{id}` — update form: name, code, active, optional more-information
  fields, lifecycle, and related asset types.
- `GET /api/asset-categories/lookup?search=` — parent-category dropdown names.
- `GET /api/asset-categories/{id}/history` — change history.
- `POST /api/asset-categories`
- `PUT /api/asset-categories/{id}`
- `DELETE /api/asset-categories/{id}` — deactivate.
- `POST /api/asset-categories/{id}/restore`

Create/update body:

```json
{
  "name": "IT Equipment",
  "code": "IT",
  "active": true,
  "alternateName": "",
  "parentCategory": "IT Equipment",
  "accountCode": "1520"
}
```

`name` and `active` are required. `code` is optional text. More-information fields are optional.
`parentCategory` is the **name** of an existing active category; cycles and self-parenting are
rejected.

History items:

```json
{
  "when": "2026-09-07T07:45:00Z",
  "change": "Account Code set to '12345'",
  "field": "Account Code",
  "oldValue": null,
  "newValue": "12345",
  "by": "Administrator",
  "source": "Screen"
}
```

`by` is the display name of the authenticated user. `source` is `Screen` for API form changes.

## Manufacturers

Dedicated screen-aligned contract. List items return only `id`, `name`, `code`, and `active`.

- `GET /api/manufacturers` — query: `search`, `active`, `pageNumber`, `pageSize`, `sortBy`,
  `sortDirection`.
- `GET /api/manufacturers/{id}` — details form: name, code, active (`Yes`/`No`), optional more
  information, and lifecycle `Active → Inactive`.
- `GET /api/manufacturers/lookup?search=` — manufacturer dropdowns.
- `GET /api/manufacturers/{id}/history` — change history (`when`, `change`, `by`, `source`).
- `POST /api/manufacturers`
- `PUT /api/manufacturers/{id}`
- `DELETE /api/manufacturers/{id}` — deactivate.
- `POST /api/manufacturers/{id}/restore`

Create/update body:

```json
{
  "name": "Dell",
  "code": "DELL",
  "active": "Yes",
  "moreInformation": "Yes",
  "alternateName": "",
  "country": "United States",
  "supportContact": "",
  "website": "https://www.dell.com"
}
```

`name`, `code`, `active`, and `moreInformation` are required. `active` and `moreInformation` must be
`Yes` or `No`. If `moreInformation` is `Yes`, `alternateName`, `country`, `supportContact`, and
`website` are optional. If `moreInformation` is `No`, those fields are ignored (create leaves them
empty; update keeps the current values). Manufacturer name and code are unique.

Detail response:

```json
{
  "id": "guid",
  "name": "Dell",
  "code": "DELL",
  "active": "Yes",
  "alternateName": null,
  "country": "United States",
  "supportContact": null,
  "website": null,
  "lifecycle": "Active → Inactive"
}
```

History items:

```json
{
  "when": "2026-09-07T07:45:00Z",
  "change": "Record created",
  "by": "Administrator",
  "source": "Screen"
}
```

`by` is the display name of the authenticated user. `source` is `Screen` for API form changes.

## Asset types

Dedicated screen-aligned contract. List items return `id`, `name`, `code`, `assetCategory`,
`requiresSerialNumber`, `defaultStatus`, and `active`.

- `GET /api/asset-types` — query: `search`, `assetCategory` (category name), `active`,
  `pageNumber`, `pageSize`, `sortBy`, `sortDirection`.
- `GET /api/asset-types/{id}` — details form matching New/Edit Asset Type.
- `GET /api/asset-types/lookup?search=` — asset-type dropdowns.
- `GET /api/asset-types/{id}/history` — change history (`when`, `change`, `by`, `source`).
- `POST /api/asset-types`
- `PUT /api/asset-types/{id}`
- `DELETE /api/asset-types/{id}` — deactivate.
- `POST /api/asset-types/{id}/restore`

Create/update body:

```json
{
  "name": "Laptop",
  "code": "LAPTOP",
  "assetCategory": "IT Equipment",
  "requiresSerialNumber": "Yes",
  "defaultStatus": "Working",
  "active": "Yes",
  "moreInformation": "Yes",
  "alternateName": "",
  "requiresRfidTag": "No",
  "requiresBarcode": "No",
  "permittedStatusTransitions": "",
  "customAttributeSchema": "",
  "defaultDepreciationMethod": "Straight line",
  "defaultUsefulLife": 36,
  "numberingScheme": "LAP-#####"
}
```

`name`, `code`, `requiresSerialNumber`, `active`, and `moreInformation` are required. Yes/No fields
must be `Yes` or `No`. `assetCategory` and `defaultStatus` are optional; each accepts the **name**
of an existing active record (what the dropdown shows) or its **id**. Unknown references return 404.

If `moreInformation` is `Yes`, the remaining fields are optional. Depreciation method and useful
life are stored when sent; they are present on the screen when depreciation is enabled.
`numberingScheme` is the per-type numbering format reference (for example `LAP-#####`). If
`moreInformation` is `No`, those fields are ignored (create leaves them empty; update keeps the
current values).

Detail response uses names for category and status, `Yes`/`No` for booleans, and
`lifecycle`: `Active → Inactive`.

## Asset models

Dedicated screen-aligned contract matching New Asset Model.

- `GET /api/asset-models` — query: `search`, `manufacturer` (manufacturer name), `assetType`
  (asset type name), `active`, `pageNumber`, `pageSize`, `sortBy`, `sortDirection`. List items
  return `id`, `name`, `manufacturer`, `modelNumber`, and `active`.
- `GET /api/asset-models/{id}` — details form matching New/Edit Asset Model.
- `GET /api/asset-models/lookup?search=` — asset-model dropdowns.
- `GET /api/asset-models/{id}/history` — change history (`when`, `change`, `by`, `source`).
- `POST /api/asset-models`
- `PUT /api/asset-models/{id}`
- `DELETE /api/asset-models/{id}` — deactivate.
- `POST /api/asset-models/{id}/restore`

Create/update body:

```json
{
  "name": "Latitude 5540",
  "manufacturer": "Dell",
  "modelNumber": "5540",
  "active": "Yes",
  "moreInformation": "Yes",
  "alternateName": "",
  "assetType": "Laptop",
  "specifications": "",
  "expectedUsefulLife": 36,
  "documentation": ""
}
```

`name`, `manufacturer`, `modelNumber`, `active`, and `moreInformation` are required. `active` and
`moreInformation` must be `Yes` or `No`. `manufacturer` is the **name** of an existing active
manufacturer (dropdown). `assetType` is the **name** of an existing active asset type.

If `moreInformation` is `Yes`, `alternateName`, `assetType`, `specifications`, `expectedUsefulLife`,
and `documentation` are optional and validated. If `moreInformation` is `No`, those fields are not
validated and are ignored (create leaves them empty; update keeps the current values). Model number
is unique per manufacturer. Unknown manufacturer or asset type names return 404.

## Type attributes

- `GET /asset-type-attributes` — filter by asset type using `relatedEntityId`.
- `GET /asset-type-attributes/{id}`.
- `POST /asset-type-attributes`
- `PUT /asset-type-attributes/{id}`
- `DELETE /asset-type-attributes/{id}`
- `POST /asset-type-attributes/{id}/restore`

```json
{
  "assetTypeId":"guid",
  "customAttributeDefinitionId":"guid",
  "requirement":"Required",
  "displayOrder":1,
  "showInList":true
}
```

An attribute definition can be assigned only once per type. Requirement is `Optional`,
`Recommended`, or `Required`.

## RFID tags

- `GET /rfid-tags` and `GET /rfid-tags/{id}`
- `POST /rfid-tags` and `PUT /rfid-tags/{id}`
- `POST /rfid-tags/{id}/assign` with `{"assetId":"guid"}`
- `POST /rfid-tags/{id}/unassign`
- `POST /rfid-tags/{id}/replace` with `{"replacementId":"guid"}`
- `GET /rfid-tags/exists?value=`, `DELETE /rfid-tags/{id}`, and `POST /rfid-tags/{id}/restore`

Create example:

```json
{"tagIdentifier":"EPC:3034-ABC-100","tagType":"Passive UHF","encodingStandard":"GS1 SGTIN"}
```

Identifiers are globally unique and never reused. Only unassigned identifiers can be assigned.
Replacement retires the old tag and preserves the relationship through `replacedById`.

## Barcodes

- `GET /barcodes` and `GET /barcodes/{id}`
- `POST /barcodes` and `PUT /barcodes/{id}`
- `POST /barcodes/generate?symbology=Code128`
- `POST /barcodes/{id}/assign`, `/unassign`, and `/replace`
- `GET /barcodes/exists?value=`, `DELETE /barcodes/{id}`, and `POST /barcodes/{id}/restore`

Supported prototype-backed symbologies are Code128, Code39, QR, DataMatrix, and EAN.
Value plus symbology is unique. Assignment/replacement request
formats match RFID. Print execution itself is frontend/external-printer integration and is not
performed by this API.

## Combined Type Attribute workflow

- `GET /asset-types/{assetTypeId}/attributes`
- `POST /asset-types/{assetTypeId}/attributes`

The POST operation atomically creates the definition and assigns it to the type, matching the
prototype's “Add an attribute” modal. Its request accepts `code`, `label`, `alternateName`,
`dataType`, `listValues`, `unit`, `requirement`, `displayOrder`, `showInList`, and help text.
Attribute code is unique within the selected type.

## Asset status transitions

- `GET /asset-statuses/{id}/allowed-transitions`
- `PUT /asset-statuses/{id}/allowed-transitions`

Update body: `{"allowedToStatusIds":["guid"]}`. Self-transitions, duplicates, and missing/inactive
targets are rejected. The transition matrix is administered here; applying a transition to an Asset
belongs to the Asset Register API outside this menu scope.

## Asset images

- `GET /asset-images` — use `relatedEntityId` for an asset.
- `GET /asset-images/{id}` — metadata.
- `GET /asset-images/{id}/content` — display/download bytes with range support.
- `POST /asset-images?assetId={guid}` — `multipart/form-data`: `file`, optional `caption`, `purpose`,
  and `isPrimary`.
- `PUT /asset-images/{id}` — `{"caption":"Front","purpose":"Identification"}`.
- `POST /asset-images/{id}/primary`.
- `DELETE /asset-images/{id}`.

Extension, declared MIME type, magic bytes, and the 10 MB limit are checked. Stored names are
generated server-side. Deleting a primary image promotes the oldest remaining active image.
Prototype purposes are Identification, Condition record, Damage evidence, and Nameplate.
Damage-evidence photographs are immutable and cannot be removed.

## Supporting lookup

`GET /api/assets/lookup?search=` returns active assets by name, code, or asset number for tag,
barcode, and image workflows.
