# Review fixes – DMS_API

Changes on branch `review-fixes`, from the code review of 1 Oct 2026. Finding IDs (S1, A1 …) refer to the review doc.

## Before you run it

1. **Change the leaked passwords and JWT key (S1).** They were in `appsettings.json` and are still in git history.
   Removing them from the file does not make them safe.
2. Run `Database/001_HQ_Users_PasswordHash.sql` on the database (adds `HQ_Users.PasswordHash`).
3. Set secrets (never in appsettings):
   ```bash
   cd DMS.API
   dotnet user-secrets set "ConnectionStrings:LocalDBConnection" "Server=...;Database=TestDms;..."
   dotnet user-secrets set "Jwt:Key" "<at least 32 random characters>"
   ```
   On servers use environment variables `ConnectionStrings__LocalDBConnection` and `Jwt__Key`.
4. Give each HQ user a password (stored as a hash):
   ```bash
   dotnet run --project DMS.API -- set-password <UserID> <password>
   ```
5. Add the frontend URL(s) to `AllowedCorsOrigin` in `appsettings.json`.

## What changed

| Finding | Change | Files |
| --- | --- | --- |
| S1 | Secrets removed from `appsettings.json`; user-secrets in Development, environment variables elsewhere | `appsettings*.json`, `AddServiceExtension.cs`, `DMS.API.csproj` |
| S2 | Login checks `HQ_Users` with ASP.NET Core `PasswordHasher` hashes. Hard-coded `admin/admin123` removed. `ChangePassword` endpoint added | `LoginController.cs`, `HQUserService.cs`, `HqUser.cs`, `UserConstants.cs` (deleted) |
| S3 | Every controller requires a login (`DmsControllerBase` has `[Authorize]`) | `Controllers/*` |
| S4 | One auth scheme: JWT bearer. The Azure AD *web-app* (cookie) setup and its packages were removed | `AddServiceExtension.cs`, `DMS.API.csproj` |
| S6 | The user ID comes from the token. `userId` query parameters are still accepted but ignored | `DMSController.cs`, `CircularController.cs` |
| S7 | `Authorization` header no longer written to HTTP logs | `AddServiceExtension.cs` |
| S8 | CORS allows only `AllowedCorsOrigin` (was any origin) | `AddServiceExtension.cs`, `Program.cs` |
| S9 | `Login/dbinfo` is admin-only (the frontend no longer calls it) | `LoginController.cs` |
| A1 | `AddToFavourite` is awaited (errors were lost, DbContext could be disposed mid-save) | both controllers |
| A3 | `MarkNotificationRead` implemented for manuals and circulars (accepts query or JSON body) | controllers, `DMSService.cs`, `CIRService.cs` |
| – | Circular notifications now return their ID (`CIRNotificationId` was always 0) | `CircularNotificationMappingProfile.cs` |
| F5 | New `GET api/DMS/GetManualVersionHistory?dm_ManualID=` for the Version History dialog | `DMSController.cs`, `DMSService.cs`, `ManualVersionHistoryDto.cs` |
| Errors | Services rethrow the original exception instead of wrapping it in `new Exception(...)`; middleware maps `KeyNotFoundException` → 404 and argument errors → 400; error JSON is no longer double-encoded; middleware now runs first | `Services/*`, `Query/*`, `ExceptionHandlingMiddleware.cs`, `Program.cs` |
| Structure | Empty root `Program.cs` removed; `! ! DMS.sln` renamed `DMS.sln`; duplicate `DBConnectionString` registration, unused session and hard-coded Swagger server removed; `AddProblemDetails()` added | various |
| CI | GitHub Actions build on every push | `.github/workflows/build.yml` |

## Not changed (needs the database to test)

* **A4 – `MAX(id) + 1` keys** (22 places). Moving to `IDENTITY` or a `SEQUENCE` changes table definitions; do it
  with a migration script and test against a copy of the database.
* **A5 – transactions** around multi-step commands (e.g. `ApproveManualCommandHandler`).
* **A6 – temp circular records**, **A7 – `@CIR_CategoryID` passed for document type** (check the stored procedure).
* No unit tests yet; the CI workflow will run them once a test project is added.

## How it was checked

NuGet was not reachable in the environment used, so a full `dotnet build` could not run. The changed API layer
(`DMS.API`, `DMS.Application`) was compiled against stand-in package definitions with no errors or warnings. The
Infrastructure edits follow the existing patterns in the same files. **Run `dotnet build` before deploying.**
