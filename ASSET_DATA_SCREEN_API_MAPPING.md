# Asset Data screen-to-API mapping

## Asset Type

- List (`name`, `code`, `assetCategory`, `requiresSerialNumber`, `defaultStatus`, `active`):
  `GET /api/asset-types`
- Category filter: `assetCategory` (category **name**)
- New / Save: `POST /api/asset-types` with required `name`, `code`, `requiresSerialNumber`
  (`Yes`/`No`), `active` (`Yes`/`No`), and `moreInformation` (`Yes`/`No`).
- `assetCategory` and `defaultStatus` accept an existing record **name** (dropdown) or **id**.
- If more information is `Yes`, optional `alternateName`, `requiresRfidTag`, `requiresBarcode`,
  `permittedStatusTransitions`, `customAttributeSchema`, `defaultDepreciationMethod`,
  `defaultUsefulLife`, and `numberingScheme` are accepted.
- Update screen: `GET /api/asset-types/{id}` then `PUT /api/asset-types/{id}` with the same body.
- History: `GET /api/asset-types/{id}/history` (`when`, `change`, `by` = logged-in user, `source`)
- Dropdowns: `GET /api/asset-types/lookup`
- Delete/restore: `DELETE /api/asset-types/{id}`, `POST /api/asset-types/{id}/restore`
- Asset-number generation belongs to the Asset Register, outside this requested menu section.

## Type Attributes

- Grouped fields and type filter: `GET /api/asset-type-attributes?relatedEntityId={typeId}`
- Add/link definition: `POST /api/asset-type-attributes`
- Change requirement/order/list visibility: `PUT /api/asset-type-attributes/{id}`
- Deactivate/restore: `DELETE`, `POST .../{id}/restore`
- Asset Type and definition dropdowns use their respective `/lookup` endpoints.
- The prototype's one-step Add modal uses `POST /api/asset-types/{typeId}/attributes`; its grouped
  view uses `GET /api/asset-types/{typeId}/attributes`.

## Asset Category

- List (`name`, `code`, `active`): `GET /api/asset-categories`
- Parent and Active filters: `parentCategory`, `active`
- New / Save: `POST /api/asset-categories` with required `name` and `active`; optional `code`,
  `alternateName`, `parentCategory` (existing category **name**), and `accountCode`
- Update screen: `GET /api/asset-categories/{id}` then `PUT /api/asset-categories/{id}`
- Related asset types table is returned on the detail response
- History button/tab/modal: `GET /api/asset-categories/{id}/history` (`when`, `change`, `field`,
  `oldValue`, `newValue`, `by` = logged-in user, `source`)
- Parent dropdown: `GET /api/asset-categories/lookup`
- Delete/restore: `DELETE /api/asset-categories/{id}`, `POST /api/asset-categories/{id}/restore`

## Asset Model

- New / Save: `POST /api/asset-models` with required `name`, `manufacturer` (manufacturer **name**),
  `modelNumber`, `active` (`Yes`/`No`), and `moreInformation` (`Yes`/`No`).
- If more information is `Yes`, optional `alternateName`, `assetType` (asset type **name**),
  `specifications`, `expectedUsefulLife`, and `documentation` are validated. If `No`, those fields
  are ignored and not validated.
- Update: `GET /api/asset-models/{id}` then `PUT /api/asset-models/{id}` with the same body.
- List: `GET /api/asset-models` (`name`, `manufacturer`, `modelNumber`, `active`); filter by
  `manufacturer` and `assetType` names, or free-text `search` (includes asset type name)
- History: `GET /api/asset-models/{id}/history`
- Dropdowns: `GET /api/asset-models/lookup`
- Delete/restore: `DELETE /api/asset-models/{id}`, `POST /api/asset-models/{id}/restore`

## Manufacturer

- List (`name`, `code`, `active`): `GET /api/manufacturers`
- New / Save: `POST /api/manufacturers` with required `name`, `code`, `active` (`Yes`/`No`), and
  `moreInformation` (`Yes`/`No`). If more information is `Yes`, optional `alternateName`, `country`,
  `supportContact`, and `website` are accepted.
- Update screen: `GET /api/manufacturers/{id}` then `PUT /api/manufacturers/{id}` with the same body
  as create.
- History button/modal: `GET /api/manufacturers/{id}/history` (`when`, `change`, `by` = logged-in
  user, `source`)
- Manufacturer dropdowns: `GET /api/manufacturers/lookup`
- Delete/restore: `DELETE /api/manufacturers/{id}`, `POST /api/manufacturers/{id}/restore`
- Country remains free text because the missing PDS does not establish a country dictionary.

## Supplier

- All table/form/search/lifecycle actions use `/api/suppliers`.
- Prototype fields (kind, tax registration, contact, telephone, email, country, payment terms,
  rating, external identifier) are persisted.

## Asset Status

- All table/form/search/lifecycle actions use `/api/asset-statuses`.
- Status category, color, operational/terminal flags, movement blocking, and display order are
  persisted.
- Allowed transitions can be read and replaced through
  `GET/PUT /api/asset-statuses/{id}/allowed-transitions`. The prototype did not expose a definitive
  transition editor, so this is an administrative backend capability rather than a claimed UI button.

## RFID Tag

- Stock/assigned lists, search, and status/asset filters: `GET /api/rfid-tags`
- Create/edit/details: `POST`, `PUT`, `GET /api/rfid-tags[/{id}]`
- Select an asset: `GET /api/assets/lookup`
- Assign/unassign/replace buttons: `POST /{id}/assign`, `/unassign`, `/replace`
- “Order a batch” is an external encoding/procurement integration in the prototype and is not
  represented as a completed backend order.

## Barcode

- Stock/assigned lists and filters: `GET /api/barcodes`
- Create/edit/details: `POST`, `PUT`, `GET /api/barcodes[/{id}]`
- Generate: `POST /api/barcodes/generate`
- Assign/unassign/replace: `POST /{id}/assign`, `/unassign`, `/replace`
- Prototype “Print label” is frontend/external-printer behavior; the API records `printedAtUtc` on
  assignment but does not claim to operate a physical printer.

## Custom Attribute Definition

- Generic definition grid/forms/lifecycle: `/api/custom-attribute-definitions`.
- Type-specific requirement, display order, and list visibility are held by
  `/api/asset-type-attributes`.
- Prototype data types and list values are supported. Capturing values on an Asset form is part of
  Asset Register, outside the listed Asset Data menu APIs.

## Asset Image

- Gallery/table, asset filtering, and metadata: `GET /api/asset-images`.
- Upload modal: multipart `POST /api/asset-images?assetId=...`.
- Display/open: `GET /api/asset-images/{id}/content`.
- Caption/purpose edit: `PUT /api/asset-images/{id}`.
- Primary selection: `POST /api/asset-images/{id}/primary`.
- Delete: `DELETE /api/asset-images/{id}`.
- Asset chooser: `GET /api/assets/lookup`.

All write buttons require an Admin bearer token. Client-side search, modal opening/closing, tab
selection, local presentation, and confirmation dialogs remain frontend-only.
