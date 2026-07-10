Prompt:
You are working in my existing Next.js web application: Neuro Bridge.

This is Refactor 6B: Client Submission real read integration only.

Refactor 6A completed a strict read-only Client Submission Review audit.

## Confirmed Refactor 6A findings

The current Client Submission panel is mock only.

Current file:

`features/workspace/submission/components/ClientProjectSubmissionPanel.tsx`

Confirmed problems:

- It initializes local state from `mockPackages`.
- Package history is fake.
- Deliverable files are fake.
- File review statuses are fake.
- Approve and Reject actions only mutate local component state.
- No real Submission read hook is used.
- No real review mutation exists.
- No feedback request is sent to the backend.
- It assumes `packages[0]` is the latest Package.
- The Client panel can render even when the Workspace ID is invalid or Workspace Detail fails.

The audit confirmed that real read integration is available through existing hooks for:

- Submission Packages by Workspace
- Submission Package Detail
- Deliverable Files by Package
- File Types

The audit did not find backend endpoints for:

- Whole-Package Accept
- Whole-Package Reject
- Workspace Completion
- Escrow release

Therefore, this task must be read-only from the business perspective.

Do not create or simulate review actions.

## Final audit decision

`GO FOR READ INTEGRATION ONLY`

This task may implement:

- Real Client Package history
- Real selected Package detail
- Real Deliverable Files
- Loading states
- Error states
- Empty states
- Backend Package status display
- Backend Package feedback display
- Safe read-only UI

This task must not implement:

- Accept
- Reject
- File review
- Feedback submission
- Completion
- Escrow release
- Kafka behavior

## Completed frontend foundations

The following foundations are already complete.

### Workspace foundation

- Workspace types are centralized.
- Workspace Detail uses the shared API client.
- Workspace query keys are stable.
- Workspace feature code lives under `features/workspace/**`.
- `/chat-room/[workspaceId]` remains a thin route shell.

### Submission read foundation

Existing typed reads include:

- `GET /workspace/api/v1/submission-packages/workspaces/{workspaceId}`
- `GET /workspace/api/v1/submission-packages/{packageId}`
- `GET /workspace/api/v1/deliverable-files/packages/{packageId}`
- `GET /workspace/api/v1/file-types`

Existing hooks conceptually include:

- `useSubmissionPackagesByWorkspace`
- `useSubmissionPackageDetail`
- `useDeliverableFilesByPackage`
- `useSubmissionFileTypes`

Use the actual current hook names and signatures from source.

### Submission write foundation

Create and Resubmit mutations already exist for the Expert panel.

Do not change them.

### Upload foundation

Submission and avatar upload now use the shared Supabase Storage boundary.

Do not modify upload code in this task.

## Project architecture convention

The project convention is:

- `app/**`
  - Thin route and layout shells
  - Route params
  - Rendering feature views

- `features/**`
  - Feature UI
  - Components
  - Local state
  - Feature orchestration
  - Feature-specific mappings

- `services/**`
  - Backend API functions
  - Query hooks
  - Mutation hooks
  - Query keys
  - Shared external integrations

- `types/**`
  - Domain types
  - Request types
  - Response types
  - Shared integration types

Do not move Client Submission implementation back under `app/**`.

## Main goal

Replace the mock Client Submission read experience with real backend Package and Deliverable data.

After this task:

1. `ClientProjectSubmissionPanel` receives a valid real `workspaceId`.
2. It loads real Submission Package history.
3. It selects a real current/latest Package deterministically.
4. It loads the selected Package detail or Deliverable Files without N+1 requests.
5. It displays backend status and feedback without inventing a status machine.
6. It handles loading, error, empty, and selected-Package states.
7. It contains no mock Package or mock Deliverable data.
8. It contains no local fake Approve or Reject state.
9. It does not show active Accept/Reject actions.
10. It does not render as a functional review panel when Workspace access failed.

## Strict scope restrictions

Do NOT implement Accept Package.
Do NOT implement Reject Package.
Do NOT implement per-file Approve.
Do NOT implement per-file Reject.
Do NOT implement feedback submission.
Do NOT create review mutation hooks.
Do NOT invent review endpoints.
Do NOT invent Package statuses.
Do NOT implement Workspace Completion.
Do NOT implement Project Completion.
Do NOT implement Escrow release.
Do NOT publish Kafka events.
Do NOT call internal backend endpoints.
Do NOT implement auto-release behavior.
Do NOT add an auto-release countdown.
Do NOT interpret `autoReleaseAt` as confirmed payment release.
Do NOT modify Expert Create Submission.
Do NOT modify Expert Resubmit.
Do NOT modify Submission upload.
Do NOT modify Supabase files.
Do NOT modify Proposal, Payment, Hire, Contract, Wallet, Notification, or Chat messages.
Do NOT redesign the entire Chat Room.
Do NOT install packages.
Do NOT upgrade dependencies.
Do NOT fix unrelated TypeScript or lint errors.

## Required initial inspection

Before editing, inspect the current versions of:

- `features/workspace/submission/components/ClientProjectSubmissionPanel.tsx`
- `features/workspace/submission/components/ProjectSubmissionPanel.tsx`
- `features/workspace/chat-room/WorkspaceChatRoomView.tsx`
- `services/submission-package.service.ts`
- `types/submission-package.type.ts`
- `services/workspace.service.ts`
- `types/workspace.type.ts`
- Existing Submission query keys
- Existing loading, empty, error, badge, card, button, dialog, and file-link components
- Existing date formatting helpers
- Existing feature barrel exports
- Every current consumer of `ClientProjectSubmissionPanel`

Search current source for:

- `mockPackages`
- `ReviewStatus`
- `handleReviewFile`
- `APPROVED`
- `REJECTED`
- `PENDING`
- `packages[0]`
- `ClientProjectSubmissionPanel`
- `useSubmissionPackagesByWorkspace`
- `useSubmissionPackageDetail`
- `useDeliverableFilesByPackage`
- `deliverableFiles`
- `feedback`
- `reviewedAt`
- `autoReleaseAt`
- `isDelete`
- `workspaceId`
- Existing Package-history rendering
- Existing download or preview links

Ignore:

- `.next`
- `node_modules`
- generated output

Determine:

1. The exact current Client panel props.
2. Every mock type and mock field.
3. Which mock fields have direct backend equivalents.
4. Which mock fields have no backend equivalent.
5. How current Package history is rendered.
6. How current Package selection works.
7. How current files are displayed.
8. Whether current file buttons open or download real URLs.
9. Whether Package Detail includes Deliverable Files in the actual typed contract.
10. Whether the separate Deliverable Files hook would duplicate Package Detail data.
11. How Workspace loading and error states are currently rendered by the parent.
12. Whether the Client panel can be mounted only after successful Workspace Detail.
13. Whether the Expert and Client panels can ever render simultaneously.
14. Whether any other component consumes the Client panel directly.

Use the actual current source as evidence.

## Workspace access gating

The audit found a critical issue:

The Client panel currently renders for a Client role even when:

- `workspaceId` is invalid
- Workspace Detail is loading
- Workspace Detail fails
- Workspace is inaccessible

Fix this as part of the read integration.

Update:

`features/workspace/chat-room/WorkspaceChatRoomView.tsx`

using the smallest safe change.

The Client Submission panel must render only when:

- The authenticated UI role is Client
- `workspaceId` is non-empty
- Workspace Detail loaded successfully
- A real Workspace object is available

Do not rely only on Client role.

Do not render the real Package list while Workspace access is unresolved or failed.

Do not issue the Package-list request from an inaccessible Client panel.

Preserve the existing Workspace loading/error display.

Do not redesign the whole invalid-Workspace Chat Room behavior.

The surrounding mock Chat UI may remain as existing technical debt, but the Client Submission area must not pretend real review data is available after Workspace failure.

The Expert panel behavior must remain unchanged unless a minimal consistency fix is strictly required.

## Client panel prop contract

Update `ClientProjectSubmissionPanel` to receive the real Workspace identifier.

Conceptually:

- `workspaceId: string`

Use actual project prop naming conventions.

Requirements:

- Pass `workspaceId` from `WorkspaceChatRoomView`.
- Do not read it from localStorage.
- Do not read route params again inside the panel.
- Do not store it globally.
- Do not substitute `jobId`.
- Do not substitute `escrowId`.
- Do not use a mock Workspace ID.
- Do not allow `/submission-packages/workspaces/undefined`.

The read hook must remain disabled when the identifier is empty, even though parent gating should normally prevent that state.

## Real Package history integration

Replace `mockPackages` with the existing Packages-by-Workspace hook.

Use:

`GET /workspace/api/v1/submission-packages/workspaces/{workspaceId}`

Requirements:

- Use the existing shared service and query hook.
- Do not add direct `fetch`.
- Do not add manual Authorization.
- Do not duplicate the endpoint function.
- Do not create a second Package-list hook.
- Do not fabricate Package results after failure.
- Support a successful empty array.
- Keep the API array immutable.

Render real Package history from `SubmissionPackageSummary`.

At minimum, preserve or display when available:

- Version
- Submitted date
- Status
- Notes
- Reviewed date
- Feedback

Do not invent missing values.

Do not display `undefined`, `null`, or invalid dates as user-facing text.

## Latest/current Package selection

Do not assume `packages[0]` is latest.

The backend sort order is unverified.

Determine the current/latest Package using stable backend fields.

Prefer:

1. Highest numeric `version`
2. If versions tie or are unavailable, newest valid `submittedAt`
3. Stable original order only as a final fallback

Do not mutate the original query result.

Do not call `.sort()` directly on cached React Query data.

Use a copied array or a memoized derived list.

The selected Package behavior should be:

- Initially select the derived latest Package.
- Allow the user to select another Package from history.
- Preserve the selected Package across refetch when it still exists.
- If the selected Package disappears, fall back safely to the latest available Package.
- Do not store the entire Package object unnecessarily when `packageId` is sufficient.
- Do not use array index as selection identity.

Use:

`package.packageId`

as the selection identifier.

## Package detail integration

Use the existing Package Detail hook for only the selected Package.

Requirements:

- Query key must include `packageId`.
- Do not request Package Detail for every history item.
- Do not create an N+1 pattern.
- Do not request `/submission-packages/undefined`.
- Keep history visible while selected detail loads.
- Show detail loading only in the detail area.
- Show detail failure only in the detail area.
- Do not replace list failure with detail failure or vice versa.
- Do not show stale detail belonging to a different selected Package.
- Preserve React Query cache isolation by Package ID.

Use actual current hook behavior.

Do not change global retry rules.

## Deliverable File data-source decision

The current type indicates `SubmissionPackageDetail` may contain:

`deliverableFiles`

A separate hook also exists for:

`GET /deliverable-files/packages/{packageId}`

Do not automatically call both.

Inspect the actual current contract and source assumptions.

Choose exactly one canonical file source for the Client panel:

### Preferred option

Use `SubmissionPackageDetail.deliverableFiles` when:

- Package Detail is documented and typed to include all required file data
- No separate file-only refresh is required
- It avoids a duplicate request

### Alternative option

Use `useDeliverableFilesByPackage` when:

- Package Detail does not actually include files
- Existing runtime evidence shows files are separate
- The separate endpoint is the established source of truth

Do not create an N+1 request.

Do not call the file endpoint once per Package history entry.

Only load files for the selected Package.

Report the chosen source and reason.

## Deliverable File rendering

Render real `DeliverableFile` data.

Fields currently available may include:

- `fileId`
- `packageId`
- `fileName`
- `fileUrl`
- `fileSize`
- `fileType`
- `uploadedAt`
- `isDelete`

Requirements:

- Use `fileId` as React identity when valid.
- Do not use file URL as the domain identifier.
- Do not fabricate file review status.
- Do not display mock Approved/Rejected status.
- Do not create local file-review state.
- Handle an empty file list.
- Handle nullable or missing optional display values safely.
- Preserve existing safe file preview/download behavior when possible.
- Use safe external-link attributes where applicable.
- Do not infer that opening a file marks it reviewed.
- Do not call a review endpoint.

If `isDelete` is present:

- Do not silently invent deleted-file business behavior.
- Follow existing backend/source convention if one exists.
- Otherwise prevent crashes and report the limitation.
- Do not automatically filter deleted files without evidence.

## Remove misleading mock review actions

Remove or disable the fake local review behavior.

Remove:

- `mockPackages`
- Mock Package initialization
- Local mock Review status mutation
- `handleReviewFile`
- Fabricated file `APPROVED` or `REJECTED` transitions
- Any UI message claiming review succeeded
- Any local-only action that makes real data appear changed

Do not leave active Approve or Reject buttons that have no backend mutation.

Preferred behavior:

- Preserve the surrounding visual layout.
- Replace the action area with a clear read-only notice consistent with current UI language, such as:
  `Package review actions are not available until backend approval and rejection endpoints are provided.`

Use wording consistent with the existing application language.

The notice must not suggest that review is complete.

Do not add disabled buttons that look actionable unless the existing design convention clearly communicates their unavailable state.

Do not implement temporary local Accept/Reject.

## Backend Package status display

`SubmissionPackageSummary.status` is currently a string.

The backend status machine is not confirmed.

Requirements:

- Display the backend-provided status safely.
- Do not create a speculative TypeScript enum.
- Do not assume file mock statuses are Package statuses.
- Do not infer that `ACCEPTED` means Workspace completion.
- Do not infer that `REJECTED` automatically allows Resubmit unless backend later confirms it.
- Use a generic fallback label for unknown non-empty status values.
- Handle missing or empty status safely.
- Do not change behavior based on status except for clearly read-only visual presentation.

A small presentation-only status formatter is allowed.

Do not encode unverified business transitions into UI logic.

## Package feedback display

The real Package type includes:

`feedback`

Display feedback when:

- It exists
- It is non-empty after trimming

Requirements:

- Treat it as backend read-only text.
- Do not add a feedback editor.
- Do not add a Submit Feedback button.
- Do not fabricate feedback when absent.
- Do not infer who wrote it unless the contract says so.
- Escape/render it through normal React text behavior.

## `reviewedAt` and `autoReleaseAt`

`reviewedAt` may be rendered as neutral Package metadata when valid.

Do not imply review occurred when the field is empty.

`autoReleaseAt` semantics remain unknown.

Do not:

- Add a countdown
- Label it as guaranteed payment release
- Label it as automatic acceptance
- Trigger any action when the time passes
- Poll based on it
- Call an auto-release endpoint

It may be omitted from the Client UI in this task.

If it is already displayed and must be preserved, use neutral wording such as schedule metadata and clearly avoid business interpretation.

## Loading, error, and empty states

The Client panel must distinguish:

### Workspace not ready

The panel should not mount or request Packages until Workspace Detail succeeded.

### Package-list loading

- Show loading within the Submission panel.
- Do not show mock history.
- Do not show a false empty state before the query resolves.

### Package-list failure

- Show a scoped error state.
- Do not fabricate an empty list.
- Do not hide the whole Chat Room.
- Allow a read retry only when consistent with existing query/UI convention.
- Retry must refetch only the Package-list query.

### No Packages

- Show a real no-submissions state.
- Explain that the Expert has not submitted a Package yet.
- Do not show history or review actions.

### Package history available

- Show versions from real data.
- Clearly indicate the selected version.
- Clearly indicate the derived latest version without assuming API order.

### Detail loading

- Keep Package history visible.
- Show loading in the detail/files area.
- Do not display stale files from a previously selected Package.

### Detail failure

- Keep Package history visible.
- Show a scoped error for the selected Package.
- Do not change selected history data locally.

### No Deliverable Files

- Show a no-files state.
- Do not crash.
- Do not show mock files.
- Do not request files with an invalid Package ID.

## Component structure

The current Client panel mixes Package history, file display, and mock review actions.

A small structural cleanup is allowed only when it helps the real read integration.

Possible internal components:

- Client Submission Package History
- Client Submission Package Detail
- Deliverable File List
- Read-only Review Notice

Keep all feature-specific files under:

`features/workspace/submission/**`

Do not create files under `app/**`.

Do not over-split trivial markup.

Do not create a new public feature barrel for every internal component.

Use internal relative imports where appropriate.

Keep query orchestration in the parent Client Submission panel or a focused internal hook.

Do not create a global selected-Package store.

## Type requirements

Use existing centralized types:

- `SubmissionPackageSummary`
- `SubmissionPackageDetail`
- `DeliverableFile`

Remove mock-only local types when they are no longer used.

Do not use:

- `any`
- Broad unsafe assertions
- Non-null assertions to bypass missing IDs
- Mock DTOs pretending to be backend DTOs
- Array index as Package identity
- Array index as File identity when `fileId` exists

Do not change backend types merely to match old mock UI.

If actual source reveals a genuine nullability mismatch:

- Make the smallest evidence-based centralized type correction.
- Report it explicitly.
- Do not make every field optional.

## Query behavior requirements

Package list:

- Enabled only for valid non-empty `workspaceId`
- Existing stable query key
- No polling
- No automatic review action
- No mutation invalidation in this task

Package detail or file query:

- Enabled only for valid selected `packageId`
- Stable Package-specific query key
- No N+1 requests
- No prefetching every history item
- No `/undefined` request
- No stale files from another Package

Do not modify global React Query defaults.

Do not introduce review query keys or mutation keys.

## Parent Workspace behavior

Update `WorkspaceChatRoomView` minimally.

Expected role selection:

- Expert with valid loaded Workspace:
  - Existing Expert `ProjectSubmissionPanel`

- Client with valid loaded Workspace:
  - Real `ClientProjectSubmissionPanel` with `workspaceId`

- Workspace loading:
  - Existing loading/status behavior

- Workspace error or inaccessible Workspace:
  - Do not render Client Submission panel
  - Do not start Package reads

Do not redesign Chat messages, header, composer, profile panel, or surrounding mock data.

## API impact

This task must add no new backend endpoint.

It must use only existing reads.

Expected Client request behavior:

1. Workspace Detail loads.
2. After Workspace success, Package list loads once.
3. Selected Package Detail or selected Package Files loads.
4. Changing selected history Package loads only that Package’s detail/files.
5. No Accept, Reject, Complete, Payment, or release request is made.

Do not trigger File Types unless the Client panel genuinely needs them for display.

## Allowed modifications

Likely allowed:

- `features/workspace/submission/components/ClientProjectSubmissionPanel.tsx`
- `features/workspace/chat-room/WorkspaceChatRoomView.tsx`
- Small new presentational components under `features/workspace/submission/components/**`
- Small pure presentation helpers under `features/workspace/submission/utils/**`
- `types/submission-package.type.ts`, only for a genuine evidence-based compatibility correction
- Internal Workspace feature imports/exports only when required

## Forbidden modifications

Do not modify:

- `services/submission-package.service.ts`, unless a tiny existing-hook compatibility fix is strictly necessary
- Create/Resubmit mutation behavior
- Submission query keys
- Expert `ProjectSubmissionPanel` behavior
- Supabase client
- Storage service
- Submission upload hook
- Avatar upload
- Workspace backend service behavior
- Jobpost services
- Proposal services
- Payment services
- Hire/Contract code
- Notification
- Chat message behavior
- Admin
- Homepage
- Sidebar
- Route structure
- Redux structure
- `.env`
- Supabase policies

## Verification commands

Run:

1. `npx tsc --noEmit --incremental false --pretty false`
2. `npm run lint -- --no-cache`
3. `git diff --check`
4. `git status --short`

The repository has known pre-existing errors.

Do not fix unrelated failures.

Separate:

- Errors introduced by this task
- Errors in modified or created files
- Pre-existing errors outside scope

## Required source verification

After changes, confirm:

1. `ClientProjectSubmissionPanel` no longer imports or contains `mockPackages`.
2. No mock Package history remains.
3. No mock Deliverable Files remain.
4. No local fake `ReviewStatus` remains.
5. No `handleReviewFile` remains.
6. No local Approve/Reject state mutation remains.
7. No active Accept/Reject button suggests a real backend action.
8. The panel receives real `workspaceId`.
9. The Package-list hook uses that `workspaceId`.
10. The Client panel does not mount after Workspace Detail failure.
11. Package selection uses `packageId`.
12. Latest Package is not derived from `packages[0]`.
13. Cached Package-list data is not mutated in place.
14. Package Detail or file reads occur only for the selected Package.
15. No N+1 request is introduced.
16. No `/undefined` endpoint is possible.
17. Backend Package status is displayed without speculative state transitions.
18. Backend feedback is read-only.
19. No review mutation or endpoint was invented.
20. Expert Create/Resubmit behavior remains unchanged.
21. No Workspace initialize call was introduced.

## Manual verification limitations

A real Workspace and real Packages may not currently be available.

Do not claim runtime end-to-end success based only on source checks.

Document runtime verification as deferred when data is unavailable.

## Manual test checklist

### Valid Client Workspace

1. Sign in as the real Client member of a Workspace.
2. Open:
   `/chat-room/{realWorkspaceId}`
3. Confirm Workspace Detail loads successfully.
4. Confirm exactly one Package-list request:
   `/submission-packages/workspaces/{workspaceId}`
5. Confirm no mock Package data appears.
6. Confirm Package history uses real versions and dates.

### Empty Workspace Packages

1. Open a valid Workspace with no Submission Packages.
2. Confirm a real no-submissions state.
3. Confirm no Package Detail request runs.
4. Confirm no Accept/Reject actions appear.

### Multiple Package versions

1. Use a Workspace with multiple versions.
2. Confirm the Package with the highest `version` is selected initially.
3. Confirm API array order does not determine latest selection.
4. Select an older version.
5. Confirm only the selected Package Detail/files request runs.
6. Confirm selected styling moves correctly.
7. Confirm latest-version indication remains correct.

### Package detail and files

1. Select a Package.
2. Confirm its real notes, status, feedback, dates, and files render.
3. Confirm files belong to the selected `packageId`.
4. Confirm no file-level fake status appears.
5. Confirm no N+1 detail/file requests.
6. Confirm an empty file array shows a no-files state.

### Errors

1. Simulate Package-list failure.
2. Confirm scoped list error.
3. Confirm no mock or false empty state.
4. Simulate selected Package Detail failure.
5. Confirm history remains visible.
6. Confirm only detail area shows error.
7. Switch Package and confirm stale files do not flash.

### Invalid or inaccessible Workspace

1. Open an invalid Workspace ID.
2. Confirm Client Submission panel does not mount as a real review panel.
3. Confirm Package-list request does not run after Workspace access failure.
4. Confirm no mock review UI appears.

### Role regression

1. Sign in as Expert.
2. Confirm existing Expert Submission panel still renders.
3. Confirm Client Package history panel does not render.
4. Confirm Create/Resubmit behavior remains unchanged.

### Network exclusions

Confirm no request is made to any invented or unsupported endpoint containing:

- `/accept`
- `/reject`
- `/approve`
- `/review`
- `/complete`
- `/release`

Confirm no Payment, Escrow, Kafka, or Notification request is introduced.

## Required final output

Return a Markdown report with:

# 1. Summary

State whether the Client Submission panel now uses real read data.

# 2. Previous Mock Flow

Explain exact mock data and local review behavior removed.

# 3. Workspace Access Gating

Explain when the Client panel mounts and why inaccessible Workspace states no longer start Package reads.

# 4. Package History Integration

Report:

- Hook
- Endpoint
- Workspace ID source
- Sorting/latest selection
- Selected Package state
- Empty/error behavior

# 5. Package Detail and File Integration

Report:

- Chosen canonical file source
- Why duplicate requests were avoided
- Selected Package query behavior
- File identity
- Empty/error behavior

# 6. Status and Feedback Display

Explain how raw backend status and feedback are rendered without inventing a status machine.

# 7. Review Action Removal

Confirm:

- No mock Approve
- No mock Reject
- No local fake review mutation
- No misleading success state
- Current read-only notice

# 8. Component Structure

List any new internal components or utilities and their responsibilities.

# 9. Files Changed

List every modified or created file and why.

# 10. API and Query Impact

Confirm:

- Existing reads only
- No new endpoint
- No review mutation
- No N+1
- No invalid IDs
- No broad query invalidation

# 11. Behavior Preservation

Confirm:

- Expert Create/Resubmit unchanged
- Upload unchanged
- Chat behavior unchanged
- Route unchanged
- Workspace Detail behavior unchanged except safe Client-panel gating

# 12. Verification Results

Report:

- TypeScript
- Lint
- Diff check
- Git status
- Source-search results

Separate pre-existing failures from task-created failures.

# 13. Manual Test Checklist

Provide exact valid Workspace, empty state, multiple-version, detail/file, error, invalid Workspace, role, and Network tests.

# 14. Remaining Backend Blockers

Clearly list:

- Package Accept endpoint
- Package Reject endpoint
- Feedback contract
- Package status machine
- Completion behavior
- `autoReleaseAt` semantics
- Escrow/Kafka release ownership

# 15. Refactor 6 Status

State:

- Whether 6B read integration is complete
- Why 6C Reject remains blocked
- Why 6D Accept/Completion remains blocked

Important:

- Integrate real reads only.
- Remove all fake review actions.
- Do not invent backend review behavior.
- Gate Client data on successful Workspace access.
- Do not assume Package API ordering.
- Preserve Expert Submission behavior.
- Do not modify files outside scope.
- Make the smallest safe change.

And output:
# 1. Summary

Refactor 6B is complete: `ClientProjectSubmissionPanel` now uses real read data from the existing Submission Package hooks, receives a real `workspaceId`, loads package history, selects a deterministic latest package, and renders selected package files from backend detail data.

# 2. Previous Mock Flow

Removed the old `mockPackages`, mock file DTOs, local `ReviewStatus`, and `handleReviewFile`. The panel no longer fabricates package history, deliverable files, `APPROVED` / `REJECTED` / `PENDING` file review state, or local review success.

# 3. Workspace Access Gating

Updated [WorkspaceChatRoomView.tsx](D:/SU2026/SWD/Neuro-bridge-FE/features/workspace/chat-room/WorkspaceChatRoomView.tsx) so the Client panel mounts only when:
- UI role is Client
- `workspaceId` is non-empty
- Workspace Detail has loaded successfully
- A real `workspace` object exists

Invalid, loading, failed, or inaccessible workspace states no longer start package reads.

# 4. Package History Integration

The panel uses `useSubmissionPackagesByWorkspace(normalizedWorkspaceId)`, backed by `GET /workspace/api/v1/submission-packages/workspaces/{workspaceId}`.

Latest selection no longer uses `packages[0]`; it derives latest by highest `version`, then newest valid `submittedAt`, then stable array order. Selection is stored by `packageId`, not index or object. Empty and list-error states are scoped inside the panel.

# 5. Package Detail and File Integration

Canonical file source: `SubmissionPackageDetail.deliverableFiles`.

Reason: the centralized type already declares detail includes `deliverableFiles`, so the Client panel avoids duplicate selected-package requests and does not call both detail and file endpoints. It loads detail only for the selected `packageId`, uses `fileId` as file identity, and shows scoped loading/error/empty file states.

# 6. Status and Feedback Display

Backend `status` is displayed as read-only presentation text with a safe formatter. No status machine or business transitions were invented.

Backend `feedback` is rendered only when present and non-empty. `reviewedAt` is displayed as neutral metadata when valid. `autoReleaseAt` is intentionally not interpreted or displayed.

# 7. Review Action Removal

Confirmed:
- No mock Approve
- No mock Reject
- No local fake review mutation
- No misleading “review completed” success state
- Read-only notice now says review actions require backend approval/rejection endpoints

# 8. Component Structure

No new files were created. Internal component helpers were added inside [ClientProjectSubmissionPanel.tsx](D:/SU2026/SWD/Neuro-bridge-FE/features/workspace/submission/components/ClientProjectSubmissionPanel.tsx):
- `PackageHistoryModal`
- `DeliverableFileCard`
- `PackageMetadata`
- `StatusBadge`
- `ReadOnlyReviewNotice`
- `PanelState`
- `FilePreviewModal`

# 9. Files Changed

- [ClientProjectSubmissionPanel.tsx](D:/SU2026/SWD/Neuro-bridge-FE/features/workspace/submission/components/ClientProjectSubmissionPanel.tsx): replaced mock review UI with real read-only package/detail/files UI.
- [WorkspaceChatRoomView.tsx](D:/SU2026/SWD/Neuro-bridge-FE/features/workspace/chat-room/WorkspaceChatRoomView.tsx): added Client panel workspace-success gate and passed `workspaceId`.

# 10. API and Query Impact

Existing reads only:
- Package list by workspace
- Package detail by selected package

No new endpoint, no review mutation, no Accept/Reject/Complete/Release call, no query invalidation, no N+1 detail/file requests, and no `/undefined` request path from the panel.

# 11. Behavior Preservation

Expert Create/Resubmit behavior unchanged. Upload code unchanged. Chat messages/header/composer/profile behavior unchanged. Route structure unchanged. Workspace Detail service behavior unchanged except safe Client-panel gating in the parent view.

# 12. Verification Results

TypeScript: failed on pre-existing errors outside modified files:
- `.next` validators for `app/(main)/experts/page.tsx`
- `components/ui/calendar.tsx`
- `configs/index.ts`
- `modules/users/useUserCrud.tsx`

Lint: failed on pre-existing errors outside modified files:
- `features/auth/register/ExpertRegisterForm.tsx`
- `hooks/use-mobile.ts`

Modified files: no lint errors. One existing-style `<img>` warning remains in the Client preview modal.

Diff check: passed.  
Git status: two modified files only.

Source-search results: no `mockPackages`, `ReviewStatus`, `handleReviewFile`, fake review statuses, or invented review endpoints remain in the Client panel.

# 13. Manual Test Checklist

- Valid Client workspace: confirm package-list request and real versions/dates.
- Empty packages: confirm no-submissions state and no detail request.
- Multiple versions: confirm highest `version` selected initially; selecting older version loads only that detail.
- Detail/files: confirm notes, status, feedback, dates, and files match selected package.
- Errors: simulate list and detail failures; confirm scoped error UI.
- Invalid workspace: confirm Client panel does not mount and no package request runs.
- Expert role: confirm existing Expert panel still renders and Create/Resubmit still work.
- Network exclusions: confirm no `/accept`, `/reject`, `/approve`, `/review`, `/complete`, or `/release`.

# 14. Remaining Backend Blockers

Still blocked:
- Package Accept endpoint
- Package Reject endpoint
- Feedback write contract
- Package status machine
- Workspace/project completion behavior
- `autoReleaseAt` semantics
- Escrow/Kafka release ownership

# 15. Refactor 6 Status

6B read integration is complete. 6C Reject remains blocked until backend provides a package-level reject contract. 6D Accept/Completion remains blocked until backend confirms accept, completion, and escrow release semantics.

Evaluate:
Refactor 1–2: Applicant và Expert Profile

Hoàn thành:

Sửa Public Expert Profile.
Route /experts/[expertCode] hoạt động theo convention.
Applicant có thể mở profile Expert bằng expertCode.
Proposal Detail được lazy-load bằng proposalId.
Client không còn phụ thuộc vào protected detail từ applicants list.

Còn blocker backend:

/applies không được trả strategyDetail trước khi unlock.
Refactor 3: Workspace Foundation

Hoàn thành:

Workspace types tập trung.
Workspace service dùng shared API client.
Có Detail, Members và stable query keys.
Không còn direct fetch/manual Authorization trong foundation mới.
Refactor 4: Submission Package Foundation

Hoàn thành:

Package list/detail/files/file-types reads.
Create Package.
Resubmit Package.
Scoped query invalidation.
Chat Room feature extraction.
Route giữ dạng thin shell.
Create/Resubmit không còn direct fetch.
Refactor 5: Supabase Upload Consolidation

Hoàn thành toàn bộ:

5A Audit
5B Canonical client
5C Low-level Storage service
5D Submission upload orchestration
5E Storage error contract
5F Avatar migration

Kết quả:

Một Supabase client duy nhất.
Một nơi duy nhất gọi Supabase Storage SDK.
Submission và Avatar đều đi qua shared Storage boundary.
Error contract rõ ràng.
Không còn debug log nhạy cảm.
Không còn uploadFiles dead code.
Refactor 6: Client Submission Read

Hoàn thành trong phạm vi API hiện có:

Client Package history thật.
Selected Package detail thật.
Deliverable files thật theo detail contract.
Loading/error/empty states.
Không còn review mock.
Không còn action giả.
Workspace access gating an toàn hơn.

Bị chặn bởi BE:

Reject whole Package.
Accept whole Package.
Feedback write contract.
Package status machine.
Workspace completion.
Escrow/Kafka release.
autoReleaseAt.
Đánh giá giai đoạn hiện tại
Về kiến trúc frontend

Đã sẵn sàng cho Main Flow.

Các boundary chính đã tương đối rõ:

app
→ thin routes

features
→ UI và feature orchestration

services
→ API, React Query, external integration

types
→ centralized contracts

configs
→ initialized clients

Các phần từng có technical debt lớn như Workspace, Submission và Supabase đã được tách tương đối sạch.

Về nghiệp vụ end-to-end

Chưa sẵn sàng chạy toàn bộ Main Flow thật.

Không phải do frontend foundation nữa, mà do các contract backend quan trọng còn thiếu hoặc chưa rõ.

Proposal Payment / Unlock

Frontend foundation có thể tiếp tục, nhưng real unlock đang bị chặn bởi:

Thiếu endpoint atomic purchase/unlock.
Cần xác nhận trừ tiền và unlock xảy ra trong cùng backend transaction.
strategyDetail phải bị loại khỏi response trước khi unlock.
Cần contract rõ cho insufficient balance và already-unlocked.
Hire / Contract

Bị chặn bởi:

Chưa rõ nơi truyền và lưu negotiated contract amount.
Chưa rõ Create Contract/Escrow và Finalize Hire tách thành mấy endpoint.
Cần retry behavior khi Wallet không đủ tiền.
Cần idempotency để tránh tạo trùng Contract/Escrow/Workspace.
Workspace Proposal

Còn cần:

Endpoint tạo hoặc gắn Proposal vào Workspace.
Xác định Proposal nào là source of truth sau Hire.
Submission Review / Completion

Bị chặn bởi:

Accept Package.
Reject Package + feedback.
Package status transitions.
Workspace/Project completion.
Escrow release event.
autoReleaseAt.
Một vấn đề chưa thể bỏ qua: Build baseline vẫn đỏ

Các refactor mới không tạo lỗi, nhưng repository vẫn còn lỗi TypeScript/build cũ:

app/(main)/experts/page.tsx không phải module hợp lệ.
components/ui/calendar.tsx không khớp DayPicker API.
configs/index.ts import module không tồn tại.
modules/users/useUserCrud.tsx thiếu UserSchema.

Ngoài ra còn lint error ở:

ExpertRegisterForm.tsx
hooks/use-mobile.ts

Các lỗi này không thuộc Workspace/Submission, nhưng chúng khiến chưa thể tuyên bố repository có build baseline sạch.

Vì vậy chưa nên nhảy thẳng từ 6B sang code Main Flow lớn mà bỏ qua bước đóng refactor.

Gửi backend danh sách contract blocker

Ưu tiên theo thứ tự:

Atomic Proposal purchase/unlock và xóa strategyDetail khỏi locked responses.
Negotiated Contract amount và Hire orchestration.
Reject Submission Package với feedback.
Accept Submission Package.
Completion + Escrow release ownership.
Package status enum và transitions.
autoReleaseAt semantics.
Proposal creation/association trong Workspace.

======================================================

You are working in my existing Next.js web application: Neuro Bridge.

This is Refactor 7A: Architecture Closure Audit and Build Baseline Diagnosis only.

This task is strictly read-only for application source files.

Do NOT modify application source files.
Do NOT create application source files.
Do NOT delete, rename, or move application source files.
Do NOT run formatter or auto-fix commands.
Do NOT run ESLint with `--fix`.
Do NOT change package versions.
Do NOT install packages.
Do NOT upgrade dependencies.
Do NOT edit generated route types manually.
Do NOT implement Main Flow business features.
Do NOT implement missing backend endpoints.
Do NOT replace remaining mocks during this audit.
Do NOT fix build or lint errors yet.

You may run build, TypeScript, lint, and read-only search commands.

Generated build output such as `.next/**` may be created by normal Next.js commands, but no tracked application source file may change.

Confirm `git status --short` before and after the audit.

## Current project

Application:

- Neuro Bridge frontend
- Next.js App Router
- TypeScript
- Tailwind
- shadcn-style UI
- React Query
- Redux
- Supabase browser Storage

Project architecture convention:

- `app/**`
  - Thin route and layout shells only
  - Route parameters
  - Render feature views

- `features/**`
  - Feature views
  - Components
  - Local UI state
  - Feature orchestration
  - Feature-specific hooks and mappings
  - Temporary mock data only when explicitly documented

- `services/**`
  - Backend API functions
  - React Query hooks
  - Query keys
  - Mutations
  - External integration boundaries

- `types/**`
  - Domain types
  - Request types
  - Response types
  - Integration contracts

- `configs/**`
  - Initialized shared clients
  - Environment configuration

## Completed refactors

The following architecture work is complete and must not be reopened without concrete evidence.

### Refactor 1 — Public Expert Profile repair

Completed:

- `/experts/[expertCode]` uses a thin route shell.
- Public profile view lives under `features/profile/expert/**`.
- Real Expert and Skill reads are used.
- Invalid code and empty-skill states were handled.
- Broken imports and local type assumptions were repaired.

Known remaining issue:

- The base `/experts` page has previously been reported as not being a valid module.

### Refactor 2 — Applicant to Expert Profile

Completed:

- Client applicant card links to the public Expert profile.
- It uses `applicant.expertApply.expertCode`.
- Missing codes do not create invalid URLs.
- Proposal Unlock remains independent.

### Proposal protected-detail preparation

Completed:

- Proposal Detail is fetched lazily by `proposalId`.
- The dialog no longer uses protected Strategy Detail from the applicant-list response.
- The frontend ignores premature `strategyDetail` fields.

Backend/security concern still open:

- Backend must not expose protected `strategyDetail` before unlock.

### Refactor 3 — Workspace foundation

Completed:

- Centralized Workspace types.
- Shared API client usage.
- Workspace Detail and Members reads.
- Stable Workspace query keys.
- No manual Authorization header in the new Workspace service.

### Refactor 4 — Submission Package foundation

Completed:

- Package list by Workspace.
- Package Detail.
- Deliverable Files by Package.
- File Types.
- Create Submission Package.
- Resubmit Submission Package.
- Scoped query invalidation.
- Chat Room feature extraction.
- Thin Chat Room route shell.

### Refactor 5 — Supabase upload consolidation

Completed:

- One canonical Supabase client in `configs/supabase.ts`.
- One direct Storage SDK boundary in `services/supabase-storage.service.ts`.
- Submission upload feature hook.
- Submission upload utilities.
- Normalized Storage errors.
- Avatar helper migrated to the shared Storage boundary.
- Unused `uploadFiles` removed.
- No direct Supabase Storage SDK call should remain outside the generic service.

Runtime Storage testing and policy verification remain deferred.

### Refactor 6 — Client Submission Review preparation

Completed:

- 6A API and frontend audit.
- 6B real Client Package read integration.
- Mock Client Package history removed.
- Mock Deliverable Files removed.
- Fake Approve/Reject behavior removed.
- Real Package list and selected Package Detail are used.
- Client panel is gated by successful Workspace access.
- Latest Package does not rely on `packages[0]`.
- Status and feedback are read-only.

Backend blockers remain:

- Package Accept endpoint.
- Package Reject endpoint.
- Feedback write contract.
- Package status machine.
- Workspace/Project completion behavior.
- `autoReleaseAt` semantics.
- Escrow/Kafka release ownership.

## Main goal

Produce a complete evidence-based closure report for Refactors 1–6 and establish the exact current build/lint baseline before Main Flow implementation begins.

The audit must answer:

1. Whether the architecture convention is now consistently followed.
2. Whether remaining violations affect the Main Flow.
3. Whether direct API, token, Supabase, or DTO duplication remains.
4. Whether misleading mock actions remain in the primary business flow.
5. Whether invalid identifiers can still trigger real requests.
6. Whether query keys and invalidations are safe enough for Main Flow work.
7. Whether current TypeScript, lint, and build failures are understood.
8. Which failures must be fixed in Refactor 7B.
9. Which warnings may remain as non-blocking debt.
10. Whether frontend architecture is ready to enter Main Flow after targeted baseline fixes.

## Strict scope

This is diagnosis only.

Do NOT:

- Fix `/experts`.
- Fix Calendar.
- Fix `configs/index.ts`.
- Fix `useUserCrud.tsx`.
- Fix lint errors.
- Replace Chat mocks.
- Replace Notification mocks.
- Add Accept/Reject.
- Add Payment Unlock.
- Add Hire/Contract.
- Add Workspace initialization.
- Add Completion.
- Add Escrow release.
- Change API response shapes.
- Change query invalidation.
- Change role logic.
- Change Supabase configuration.
- Change route structure.
- Delete dead code.
- Modify barrel exports.
- Change TypeScript configuration.
- Change ESLint configuration.
- Change Next.js configuration.

Only inspect and report.

## Required initial repository state

Before inspection:

1. Run `git status --short`.
2. Record whether the worktree is clean.
3. If tracked source changes exist, do not edit or reset them.
4. Report the exact paths and continue read-only only when the source can still be inspected safely.
5. Do not use `git checkout`, `git restore`, `git reset`, or `git clean`.

## Required configuration inspection

Inspect:

- `package.json`
- Lockfile
- `tsconfig.json`
- Next.js config
- ESLint config
- Path aliases
- React Query provider setup
- Redux provider setup
- Shared API client configuration
- `configs/supabase.ts`
- Current Next.js version
- Current React version
- Current `react-day-picker` version
- Current TypeScript version
- Current ESLint version

Determine:

- Exact build script.
- Exact lint script.
- Whether build invokes lint separately.
- Whether TypeScript incremental output may affect results.
- Whether `.next` validators are generated from a currently invalid route.
- Whether path aliases match current source layout.
- Whether known errors are caused by version mismatch, missing file, stale export, or invalid source.

Do not alter configuration.

## Audit area 1 — Route and layout convention

Inspect all application routes under:

`app/**`

Pay special attention to:

- `/experts`
- `/experts/[id]` or `/experts/[expertCode]`
- `/client/dashboard/my-job-post`
- `/client/dashboard/my-job-post/[id]`
- `/client/dashboard/create-job-post`
- `/chat-room/[workspaceId]`
- Auth routes
- Profile routes
- Available AI routes
- Client and Expert dashboard routes

Classify each route as:

- CLEAN THIN SHELL
- ACCEPTABLE ROUTE-SPECIFIC LOGIC
- FEATURE LOGIC LEAKED INTO APP
- INVALID ROUTE MODULE
- DEAD OR DUPLICATE ROUTE
- REQUIRES RUNTIME VERIFICATION

Check for:

- Large JSX trees in route files.
- Local API calls in route files.
- Local DTOs in route files.
- Mock data in route files.
- Business mutations in route files.
- Direct Supabase use in route files.
- Duplicate route implementations.
- Empty files.
- Files that export no default React component.
- Invalid route groups.
- Route links pointing to nonexistent routes.
- Dynamic parameter naming inconsistencies.
- Native Next.js page files that are not valid modules.

Do not refactor any route.

## Audit area 2 — Direct network and authorization usage

Search application source for:

- `fetch(`
- `axios`
- `.get(`
- `.post(`
- `.put(`
- `.patch(`
- `.delete(`
- `Authorization`
- `Bearer `
- `localStorage`
- `sessionStorage`
- `document.cookie`
- Manual token extraction
- Manual API base URLs
- Duplicate API client initialization
- Direct Workspace endpoint strings
- Direct Jobpost endpoint strings
- Direct Payment endpoint strings
- Direct Proposal endpoint strings
- Direct Notification endpoint strings

Ignore:

- Shared API-client implementation.
- Framework internals.
- `node_modules`.
- `.next`.
- Generated output.

For every direct request outside an approved service boundary, report:

- File.
- Operation.
- Endpoint.
- Authentication method.
- Whether it belongs under `services/**`.
- Whether it affects Main Flow.
- Severity.
- Recommended future action.

Distinguish:

- Legitimate external fetches.
- API service functions.
- Legacy direct backend calls.
- Mock-only code.
- Dead code.
- Browser asset requests.

Do not assume every occurrence of `.get` is HTTP.

## Audit area 3 — Supabase closure verification

Search for:

- `createClient(`
- `@supabase/supabase-js`
- `@/configs/supabase`
- `supabase.storage`
- `.storage.from(`
- `.upload(`
- `.getPublicUrl(`
- `.createSignedUrl(`
- `.remove(`
- `listBuckets`
- `service_role`
- Supabase key logging
- Session logging
- Token logging

Confirm:

1. `createClient` exists only in `configs/supabase.ts`.
2. Storage SDK calls exist only in `services/supabase-storage.service.ts`.
3. Submission uses the shared Storage boundary.
4. Avatar upload uses the shared Storage boundary.
5. No service-role key exists in browser source.
6. No credential/session/token logging exists.
7. No duplicate Supabase config remains.
8. No feature imports Supabase directly for Storage.

Report any exception.

Do not modify Supabase code.

## Audit area 4 — Service and type architecture

Inspect:

- `services/**`
- `types/**`
- Feature-local API helpers
- Local request/response interfaces
- Query-key declarations
- Mutation hooks
- Response unwrapping helpers
- Error normalization helpers

Search for:

- Duplicate API DTO definitions.
- Local interfaces matching centralized types.
- `any`.
- `unknown as`.
- Broad type assertions.
- Non-null assertions around API identifiers.
- Response types typed differently across consumers.
- Universal envelope assumptions.
- Direct object versus `{ code, message, result }` ambiguity.
- Hooks without enabled guards.
- Query keys missing identifiers.
- Mutation functions embedded in UI.
- API functions embedded in feature components.

For each issue classify:

- MAIN FLOW BLOCKER
- SHOULD FIX IN 7B
- SAFE TO DEFER
- INTENTIONAL FEATURE-LOCAL TYPE
- RUNTIME CONTRACT VERIFICATION NEEDED

Do not centralize types during this audit.

## Audit area 5 — Query keys and invalidation

Inspect actual current keys for:

- Jobpost.
- Applicants.
- Proposal Unlock.
- Workspace.
- Submission Packages.
- Deliverable Files.
- Payment Wallet.
- Payment Transactions.
- Notifications.
- Expert/Profile data.

Check:

- Whether query keys include required IDs.
- Whether optional IDs can create collisions.
- Whether hooks are disabled for empty IDs.
- Whether mutations invalidate only affected data.
- Whether broad root invalidation is used unnecessarily.
- Whether new Package versions invalidate old/new detail correctly.
- Whether selected Package Detail is isolated by package ID.
- Whether switching IDs can display stale data.
- Whether list and detail keys are structurally consistent.
- Whether manual query keys duplicate centralized factories.

Do not change keys.

Report exact current key structures when relevant.

## Audit area 6 — Identifier safety

Search for API calls or links using:

- `workspaceId`
- `packageId`
- `proposalId`
- `jobPostId`
- `expertCode`
- `fileId`
- `escrowId`
- `userId`
- Array indexes
- Placeholder IDs
- Empty strings
- `undefined`
- `null`

Identify:

- Hooks that may call `/undefined`.
- Links that may produce invalid routes.
- ID substitutions such as job ID used as Workspace ID.
- Package ID used as File ID.
- Array index used as domain identity.
- Missing enabled guards.
- IDs read from localStorage instead of route or real response.
- Mock IDs entering real services.

Separate:

- Confirmed safe.
- Potentially unsafe.
- Runtime-dependent.
- Already guarded by parent rendering.

## Audit area 7 — Main Flow mock and placeholder audit

Search primary Client–Expert flow for:

- `mock`
- `fake`
- `demo`
- `placeholder`
- `TODO`
- `FIXME`
- `setTimeout`
- Local action success
- Static status transitions
- Hardcoded Package/Proposal/Workspace data
- Hardcoded wallet values
- Fake Contract or Hire state
- Buttons that only mutate local state
- Buttons that navigate without a real backend action
- Toasts claiming success without a mutation

Inspect at minimum:

- Client Jobpost pages.
- Expert marketplace/apply flow.
- Applicants.
- Proposal detail/unlock.
- Hire.
- Contract.
- Workspace Chat Room.
- Submission.
- Client review.
- Payment/Wallet.
- Notification.
- Dispute.
- Completion.

Classify every relevant mock as:

- SAFE PRESENTATIONAL MOCK
- KNOWN TEMPORARY MOCK
- MISLEADING BUSINESS ACTION
- MAIN FLOW BLOCKER
- DEAD CODE
- OUT OF SCOPE

Pay special attention to:

- Chat messages.
- Notification data.
- Hire actions.
- Payment unlock.
- Client Submission actions.
- Completion actions.
- Wallet balances.
- Success toasts.

Do not remove mocks.

## Audit area 8 — Barrel exports and import health

Inspect:

- `features/**/index.ts`
- `services` barrels if any.
- `types` barrels if any.
- `configs/index.ts`
- Module aliases.
- Re-export chains.

Search for:

- Exports pointing to missing files.
- Imports from deleted modules.
- Circular-looking barrel chains.
- Route imports that depend on broken barrels.
- Duplicate export names.
- Default/named export mismatch.
- Case-sensitive path mismatch.
- Legacy `modules/**` imports.
- References to removed components.
- Dead barrels used only by invalid code.

Do not rewrite barrels.

Report whether direct imports are safer for each known failure.

## Audit area 9 — Known TypeScript and build errors

The following errors have repeatedly appeared and must be diagnosed precisely.

### Error A — Experts base page

Known symptom:

`app/(main)/experts/page.tsx` is not a module.

Inspect:

- Exact file content.
- Whether it is empty.
- Whether it has invalid commented-out content.
- Whether it exports a default component.
- Whether a valid Expert listing feature already exists elsewhere.
- Whether links currently target `/experts`.
- Whether repairing this page is required before Main Flow.
- Smallest safe future fix.

Do not implement the fix.

### Error B — Calendar / react-day-picker

Known symptom:

`components/ui/calendar.tsx` uses an invalid `table` property in DayPicker class names or related incompatible API.

Inspect:

- Installed `react-day-picker` version.
- Current Calendar component implementation.
- Correct class-name keys for the installed version.
- Whether the component was generated for another version.
- Every Calendar consumer.
- Whether a minimal compatibility fix is possible.
- Whether UI behavior may change.

Use installed package types and official local type declarations as evidence.

Do not use web search unless explicitly needed.

Do not upgrade `react-day-picker`.

Do not implement the fix.

### Error C — Broken config barrel import

Known symptom:

`configs/index.ts` references:

`@/modules/users/forms/UserForm`

Inspect:

- Exact import/export.
- Whether the referenced file exists.
- Whether the path is stale.
- Whether the export is used anywhere.
- Whether `modules/users/**` is legacy architecture.
- Whether removing the export or correcting the path is safer.
- Whether another `UserForm` exists.
- Main Flow impact.

Do not change the file.

### Error D — Missing UserSchema

Known symptom:

`modules/users/useUserCrud.tsx` references missing `UserSchema`.

Inspect:

- Current imports.
- Existing user schemas.
- Whether this module is used.
- Whether it belongs to legacy Admin CRUD.
- Whether the missing symbol was renamed.
- Whether the module can be excluded or must compile.
- Smallest safe future fix.
- Main Flow relevance.

Do not fix it.

### Error E — Expert registration `any`

Known lint symptom:

`features/auth/register/ExpertRegisterForm.tsx` contains unexpected `any`, previously reported around line 88.

Inspect:

- Exact variable or callback.
- Correct available type.
- Whether a local type exists.
- Whether the fix changes runtime behavior.
- Whether it is safe for 7B.

Do not fix it.

### Error F — `use-mobile` state update in effect

Known lint symptom:

`hooks/use-mobile.ts` calls state setter in an effect in a way rejected by current lint rules.

Inspect:

- Exact hook implementation.
- Current lint rule.
- Consumer count.
- Whether it causes runtime hydration issues.
- Whether the common `matchMedia` lazy-state pattern can fix it.
- Whether it affects Main Flow.
- Smallest safe future fix.

Do not implement it.

## Audit area 10 — Warning baseline

Record lint warnings grouped by rule and path.

Pay attention to:

- `<img>` warnings.
- Missing hook dependencies.
- Unused imports.
- Explicit `any`.
- Accessibility.
- React Compiler-related warnings.
- Next.js image optimization.
- Deprecated APIs.
- Console statements.

Classify warnings:

- MUST FIX BEFORE MAIN FLOW
- SHOULD FIX IN 7B
- SAFE TO DEFER
- FALSE POSITIVE OR CONVENTION DECISION

Do not attempt to remove all warnings.

The goal is a stable baseline, not warning-zero at any cost.

## Audit area 11 — Main Flow backend blockers

Consolidate currently known backend blockers.

At minimum inspect frontend evidence for:

### Proposal purchase/unlock

- Missing atomic purchase/unlock endpoint.
- Protected Strategy Detail exposure.
- Wallet insufficiency response.
- Already-unlocked behavior.
- Required invalidations.

### Hire and Contract

- Negotiated amount request field.
- Create Contract/Escrow.
- Finalize after signatures/funding.
- Retry behavior.
- Idempotency.
- Workspace creation trigger.

### Workspace Proposal

- Proposal creation or association endpoint.
- Proposal source of truth after Hire.

### Submission review

- Package Reject.
- Feedback body.
- Package Accept.
- Status transitions.
- Completion behavior.
- Escrow release event.
- `autoReleaseAt`.

### Notification and Dispute

- Real notification contract.
- Dispute transitions.
- Completion/release interactions.

For each blocker classify:

- FRONTEND READY
- FRONTEND PREPARATION POSSIBLE
- BACKEND CONTRACT REQUIRED
- BACKEND SECURITY FIX REQUIRED
- RUNTIME VERIFICATION REQUIRED

Do not design speculative frontend mutations.

## Required commands

Run these commands and capture exact results.

1. `git status --short`
2. `npx tsc --noEmit --incremental false --pretty false`
3. `npm run lint -- --no-cache`
4. `npm run build`
5. `git diff --check`
6. `git status --short`

If the build command fails:

- Record the first root-cause error.
- Continue source inspection.
- Do not repeatedly rerun the same failing command without new evidence.
- Do not edit files.

If `.next` generated output affects TypeScript error reporting:

- Explain the relationship.
- Do not manually edit `.next`.
- Do not delete tracked source.
- You may inspect generated validator paths read-only.

Do not use destructive Git commands.

## Build baseline classification

For every TypeScript, lint, or build failure, report:

- Command.
- File.
- Line.
- Error code or lint rule.
- Root cause.
- Whether it is pre-existing.
- Whether it was introduced by Refactors 1–6.
- Main Flow impact.
- Minimal future fix.
- Expected files for 7B.
- Risk of fixing.

Classify each failure:

- BASELINE BLOCKER
- MAIN FLOW BLOCKER
- ROUTE-SPECIFIC BLOCKER
- LINT-ONLY BLOCKER
- NON-BLOCKING WARNING
- GENERATED CONSEQUENCE OF SOURCE ERROR

Do not merely repeat compiler output.

## Refactor regression check

Verify Refactors 1–6 did not introduce obvious architecture regressions.

Confirm or refute:

1. Public Expert Profile route remains thin.
2. Applicant link still uses `expertCode`.
3. Proposal Detail remains lazy by `proposalId`.
4. Workspace services use shared API client.
5. Submission reads/writes use service hooks.
6. Chat Room route remains thin.
7. Submission upload uses feature hook and shared Storage service.
8. Avatar uses shared Storage service.
9. Client Package panel uses real reads.
10. Fake Client review actions remain removed.
11. Client panel is gated by real Workspace success.
12. No direct Storage SDK call returned to feature code.
13. No new manual Authorization/token logic was introduced.
14. No unsupported Accept/Reject endpoint was invented.

## Required readiness matrix

Produce a readiness matrix for:

- Public Expert Profile.
- Expert listing.
- Client Jobpost list/detail.
- Expert marketplace.
- Apply Jobpost.
- Applicant list.
- Proposal summary.
- Proposal protected detail.
- Proposal payment/unlock.
- Hire.
- Contract.
- Workspace reads.
- Workspace members.
- Workspace Chat shell.
- Expert Package create.
- Expert Package resubmit.
- Client Package history.
- Client Package detail/files.
- Client Reject.
- Client Accept.
- Completion.
- Wallet/Transactions.
- Notification.
- Dispute.
- Repository build baseline.

Use only:

- READY
- READY AFTER 7B BASELINE FIX
- READY FOR READS ONLY
- FRONTEND FOUNDATION READY
- BLOCKED BY BACKEND CONTRACT
- BLOCKED BY BACKEND SECURITY
- MOCK ONLY
- RUNTIME VERIFICATION REQUIRED
- NOT AUDITED

## Refactor 7B planning requirements

Based on evidence, propose a minimal 7B fix set.

The plan must:

- Fix only build/lint baseline blockers.
- Avoid broad architecture refactors.
- Avoid business-flow implementation.
- Avoid dependency upgrades unless absolutely unavoidable.
- Keep each fix independently reviewable.
- Rank fixes by dependency and risk.
- State exact expected files.
- State whether each fix requires manual UI regression testing.
- State whether each fix can be committed independently.

A likely 7B plan may include:

- Repair invalid `/experts` route module.
- Repair Calendar compatibility with installed DayPicker.
- Remove or correct stale `configs/index.ts` export.
- Repair or isolate legacy `useUserCrud.tsx`.
- Replace explicit `any` in Expert registration.
- Repair `use-mobile` lint pattern.

Do not assume all six should be fixed together.

Use source evidence to decide.

## No-write verification

At the end:

1. Run `git diff --check`.
2. Run `git status --short`.
3. Confirm no tracked application source file changed.
4. Report generated ignored artifacts separately when relevant.
5. Do not clean or reset the repository.

## Required final output

Return a detailed Markdown report with these exact sections.

# 1. Executive Summary

State:

- Whether Refactors 1–6 are structurally closed.
- Whether major architecture boundaries are now consistent.
- Whether the repository currently type-checks.
- Whether lint passes.
- Whether production build passes.
- Whether Main Flow implementation may start before 7B.
- Highest-priority blockers.

# 2. Repository and Tooling Baseline

Report:

- Framework and package versions.
- Build/lint/typecheck scripts.
- Initial Git status.
- Final Git status.
- Generated-output considerations.

# 3. Refactor 1–6 Regression Check

Report every required regression check as PASS, FAIL, or RUNTIME UNVERIFIED.

# 4. Route Architecture Audit

Provide a table with:

- Route.
- Route file.
- Classification.
- Main issue.
- Main Flow impact.
- Recommended action.

# 5. Direct Network and Auth Audit

List every direct or suspicious API/auth occurrence outside approved boundaries.

# 6. Supabase Closure Audit

Report:

- Client initialization count.
- Direct Storage SDK call locations.
- Submission consumer path.
- Avatar consumer path.
- Credential/logging findings.
- Remaining runtime/security limitations.

# 7. Service and Type Architecture Audit

Report:

- Duplicate DTOs.
- Local API types.
- Unsafe assertions.
- Response-envelope inconsistencies.
- Hook enabled guards.
- Service-boundary violations.

# 8. Query Key and Invalidation Audit

Report actual strengths, risks, collisions, and invalidation concerns.

# 9. Identifier Safety Audit

Report confirmed safe and unsafe identifier flows.

# 10. Main Flow Mock and Placeholder Audit

Provide a table with:

- Feature.
- File.
- Mock or placeholder.
- User-visible behavior.
- Risk classification.
- Backend dependency.
- Recommended action.

# 11. Barrel Export and Import Health

Report stale exports, missing modules, possible cycles, and direct-import recommendations.

# 12. Build and TypeScript Baseline

For each error provide:

- Command.
- File.
- Error.
- Root cause.
- Classification.
- Main Flow impact.
- Minimal 7B fix.

# 13. Lint Baseline

Group errors and warnings by rule and severity.

# 14. Known Error Deep Diagnosis

Create subsections for:

- Experts base page.
- Calendar and DayPicker.
- `configs/index.ts`.
- `useUserCrud.tsx`.
- Expert registration `any`.
- `use-mobile`.

# 15. Backend Contract and Security Blockers

Group by:

- Proposal Unlock.
- Hire/Contract.
- Workspace Proposal.
- Submission Review.
- Completion/Release.
- Notification/Dispute.

# 16. Frontend Readiness Matrix

Use the required readiness classifications.

# 17. Main Flow Entry Assessment

Explain:

- Which Main Flow phase could technically start.
- Which phase should start first.
- Which phases are blocked.
- Whether starting before build baseline cleanup is advisable.

# 18. Recommended Refactor 7B Fix Set

Provide ordered, minimal, independently reviewable tasks.

For each task include:

- Goal.
- Exact files.
- Expected change.
- Risk.
- Verification.
- Suggested commit boundary.

# 19. Files Expected to Change in 7B

List exact paths grouped by task.

# 20. Manual Regression Plan

Include tests for:

- `/experts`.
- Public Expert Profile.
- Calendar consumers.
- Client/Expert registration.
- Client Jobpost routes.
- Workspace Chat Room.
- Expert Submission.
- Client Package reads.

# 21. Verification Results

Report exact results for:

- TypeScript.
- Lint.
- Build.
- Diff check.
- Git status.
- Confirmation that no source files changed.

# 22. Final Decision

Return exactly one:

- GO FOR REFACTOR 7B TARGETED BASELINE FIXES
- GO FOR MAIN FLOW
- NO-GO UNTIL REPOSITORY STRUCTURE IS REPAIRED

Explain the decision precisely.

Important:

- This is read-only diagnosis.
- Do not fix any source file.
- Do not implement Main Flow.
- Do not invent backend endpoints.
- Distinguish source-confirmed facts from inference.
- Separate architecture readiness from business-contract readiness.
- Preserve a clean Git worktree.
- You are working in my existing Next.js web application: Neuro Bridge.

This is Refactor 7A: Architecture Closure Audit and Build Baseline Diagnosis only.

This task is strictly read-only for application source files.

Do NOT modify application source files.
Do NOT create application source files.
Do NOT delete, rename, or move application source files.
Do NOT run formatter or auto-fix commands.
Do NOT run ESLint with `--fix`.
Do NOT change package versions.
Do NOT install packages.
Do NOT upgrade dependencies.
Do NOT edit generated route types manually.
Do NOT implement Main Flow business features.
Do NOT implement missing backend endpoints.
Do NOT replace remaining mocks during this audit.
Do NOT fix build or lint errors yet.

You may run build, TypeScript, lint, and read-only search commands.

Generated build output such as `.next/**` may be created by normal Next.js commands, but no tracked application source file may change.

Confirm `git status --short` before and after the audit.

## Current project

Application:

- Neuro Bridge frontend
- Next.js App Router
- TypeScript
- Tailwind
- shadcn-style UI
- React Query
- Redux
- Supabase browser Storage

Project architecture convention:

- `app/**`
  - Thin route and layout shells only
  - Route parameters
  - Render feature views

- `features/**`
  - Feature views
  - Components
  - Local UI state
  - Feature orchestration
  - Feature-specific hooks and mappings
  - Temporary mock data only when explicitly documented

- `services/**`
  - Backend API functions
  - React Query hooks
  - Query keys
  - Mutations
  - External integration boundaries

- `types/**`
  - Domain types
  - Request types
  - Response types
  - Integration contracts

- `configs/**`
  - Initialized shared clients
  - Environment configuration

## Completed refactors

The following architecture work is complete and must not be reopened without concrete evidence.

### Refactor 1 — Public Expert Profile repair

Completed:

- `/experts/[expertCode]` uses a thin route shell.
- Public profile view lives under `features/profile/expert/**`.
- Real Expert and Skill reads are used.
- Invalid code and empty-skill states were handled.
- Broken imports and local type assumptions were repaired.

Known remaining issue:

- The base `/experts` page has previously been reported as not being a valid module.

### Refactor 2 — Applicant to Expert Profile

Completed:

- Client applicant card links to the public Expert profile.
- It uses `applicant.expertApply.expertCode`.
- Missing codes do not create invalid URLs.
- Proposal Unlock remains independent.

### Proposal protected-detail preparation

Completed:

- Proposal Detail is fetched lazily by `proposalId`.
- The dialog no longer uses protected Strategy Detail from the applicant-list response.
- The frontend ignores premature `strategyDetail` fields.

Backend/security concern still open:

- Backend must not expose protected `strategyDetail` before unlock.

### Refactor 3 — Workspace foundation

Completed:

- Centralized Workspace types.
- Shared API client usage.
- Workspace Detail and Members reads.
- Stable Workspace query keys.
- No manual Authorization header in the new Workspace service.

### Refactor 4 — Submission Package foundation

Completed:

- Package list by Workspace.
- Package Detail.
- Deliverable Files by Package.
- File Types.
- Create Submission Package.
- Resubmit Submission Package.
- Scoped query invalidation.
- Chat Room feature extraction.
- Thin Chat Room route shell.

### Refactor 5 — Supabase upload consolidation

Completed:

- One canonical Supabase client in `configs/supabase.ts`.
- One direct Storage SDK boundary in `services/supabase-storage.service.ts`.
- Submission upload feature hook.
- Submission upload utilities.
- Normalized Storage errors.
- Avatar helper migrated to the shared Storage boundary.
- Unused `uploadFiles` removed.
- No direct Supabase Storage SDK call should remain outside the generic service.

Runtime Storage testing and policy verification remain deferred.

### Refactor 6 — Client Submission Review preparation

Completed:

- 6A API and frontend audit.
- 6B real Client Package read integration.
- Mock Client Package history removed.
- Mock Deliverable Files removed.
- Fake Approve/Reject behavior removed.
- Real Package list and selected Package Detail are used.
- Client panel is gated by successful Workspace access.
- Latest Package does not rely on `packages[0]`.
- Status and feedback are read-only.

Backend blockers remain:

- Package Accept endpoint.
- Package Reject endpoint.
- Feedback write contract.
- Package status machine.
- Workspace/Project completion behavior.
- `autoReleaseAt` semantics.
- Escrow/Kafka release ownership.

## Main goal

Produce a complete evidence-based closure report for Refactors 1–6 and establish the exact current build/lint baseline before Main Flow implementation begins.

The audit must answer:

1. Whether the architecture convention is now consistently followed.
2. Whether remaining violations affect the Main Flow.
3. Whether direct API, token, Supabase, or DTO duplication remains.
4. Whether misleading mock actions remain in the primary business flow.
5. Whether invalid identifiers can still trigger real requests.
6. Whether query keys and invalidations are safe enough for Main Flow work.
7. Whether current TypeScript, lint, and build failures are understood.
8. Which failures must be fixed in Refactor 7B.
9. Which warnings may remain as non-blocking debt.
10. Whether frontend architecture is ready to enter Main Flow after targeted baseline fixes.

## Strict scope

This is diagnosis only.

Do NOT:

- Fix `/experts`.
- Fix Calendar.
- Fix `configs/index.ts`.
- Fix `useUserCrud.tsx`.
- Fix lint errors.
- Replace Chat mocks.
- Replace Notification mocks.
- Add Accept/Reject.
- Add Payment Unlock.
- Add Hire/Contract.
- Add Workspace initialization.
- Add Completion.
- Add Escrow release.
- Change API response shapes.
- Change query invalidation.
- Change role logic.
- Change Supabase configuration.
- Change route structure.
- Delete dead code.
- Modify barrel exports.
- Change TypeScript configuration.
- Change ESLint configuration.
- Change Next.js configuration.

Only inspect and report.

## Required initial repository state

Before inspection:

1. Run `git status --short`.
2. Record whether the worktree is clean.
3. If tracked source changes exist, do not edit or reset them.
4. Report the exact paths and continue read-only only when the source can still be inspected safely.
5. Do not use `git checkout`, `git restore`, `git reset`, or `git clean`.

## Required configuration inspection

Inspect:

- `package.json`
- Lockfile
- `tsconfig.json`
- Next.js config
- ESLint config
- Path aliases
- React Query provider setup
- Redux provider setup
- Shared API client configuration
- `configs/supabase.ts`
- Current Next.js version
- Current React version
- Current `react-day-picker` version
- Current TypeScript version
- Current ESLint version

Determine:

- Exact build script.
- Exact lint script.
- Whether build invokes lint separately.
- Whether TypeScript incremental output may affect results.
- Whether `.next` validators are generated from a currently invalid route.
- Whether path aliases match current source layout.
- Whether known errors are caused by version mismatch, missing file, stale export, or invalid source.

Do not alter configuration.

## Audit area 1 — Route and layout convention

Inspect all application routes under:

`app/**`

Pay special attention to:

- `/experts`
- `/experts/[id]` or `/experts/[expertCode]`
- `/client/dashboard/my-job-post`
- `/client/dashboard/my-job-post/[id]`
- `/client/dashboard/create-job-post`
- `/chat-room/[workspaceId]`
- Auth routes
- Profile routes
- Available AI routes
- Client and Expert dashboard routes

Classify each route as:

- CLEAN THIN SHELL
- ACCEPTABLE ROUTE-SPECIFIC LOGIC
- FEATURE LOGIC LEAKED INTO APP
- INVALID ROUTE MODULE
- DEAD OR DUPLICATE ROUTE
- REQUIRES RUNTIME VERIFICATION

Check for:

- Large JSX trees in route files.
- Local API calls in route files.
- Local DTOs in route files.
- Mock data in route files.
- Business mutations in route files.
- Direct Supabase use in route files.
- Duplicate route implementations.
- Empty files.
- Files that export no default React component.
- Invalid route groups.
- Route links pointing to nonexistent routes.
- Dynamic parameter naming inconsistencies.
- Native Next.js page files that are not valid modules.

Do not refactor any route.

## Audit area 2 — Direct network and authorization usage

Search application source for:

- `fetch(`
- `axios`
- `.get(`
- `.post(`
- `.put(`
- `.patch(`
- `.delete(`
- `Authorization`
- `Bearer `
- `localStorage`
- `sessionStorage`
- `document.cookie`
- Manual token extraction
- Manual API base URLs
- Duplicate API client initialization
- Direct Workspace endpoint strings
- Direct Jobpost endpoint strings
- Direct Payment endpoint strings
- Direct Proposal endpoint strings
- Direct Notification endpoint strings

Ignore:

- Shared API-client implementation.
- Framework internals.
- `node_modules`.
- `.next`.
- Generated output.

For every direct request outside an approved service boundary, report:

- File.
- Operation.
- Endpoint.
- Authentication method.
- Whether it belongs under `services/**`.
- Whether it affects Main Flow.
- Severity.
- Recommended future action.

Distinguish:

- Legitimate external fetches.
- API service functions.
- Legacy direct backend calls.
- Mock-only code.
- Dead code.
- Browser asset requests.

Do not assume every occurrence of `.get` is HTTP.

## Audit area 3 — Supabase closure verification

Search for:

- `createClient(`
- `@supabase/supabase-js`
- `@/configs/supabase`
- `supabase.storage`
- `.storage.from(`
- `.upload(`
- `.getPublicUrl(`
- `.createSignedUrl(`
- `.remove(`
- `listBuckets`
- `service_role`
- Supabase key logging
- Session logging
- Token logging

Confirm:

1. `createClient` exists only in `configs/supabase.ts`.
2. Storage SDK calls exist only in `services/supabase-storage.service.ts`.
3. Submission uses the shared Storage boundary.
4. Avatar upload uses the shared Storage boundary.
5. No service-role key exists in browser source.
6. No credential/session/token logging exists.
7. No duplicate Supabase config remains.
8. No feature imports Supabase directly for Storage.

Report any exception.

Do not modify Supabase code.

## Audit area 4 — Service and type architecture

Inspect:

- `services/**`
- `types/**`
- Feature-local API helpers
- Local request/response interfaces
- Query-key declarations
- Mutation hooks
- Response unwrapping helpers
- Error normalization helpers

Search for:

- Duplicate API DTO definitions.
- Local interfaces matching centralized types.
- `any`.
- `unknown as`.
- Broad type assertions.
- Non-null assertions around API identifiers.
- Response types typed differently across consumers.
- Universal envelope assumptions.
- Direct object versus `{ code, message, result }` ambiguity.
- Hooks without enabled guards.
- Query keys missing identifiers.
- Mutation functions embedded in UI.
- API functions embedded in feature components.

For each issue classify:

- MAIN FLOW BLOCKER
- SHOULD FIX IN 7B
- SAFE TO DEFER
- INTENTIONAL FEATURE-LOCAL TYPE
- RUNTIME CONTRACT VERIFICATION NEEDED

Do not centralize types during this audit.

## Audit area 5 — Query keys and invalidation

Inspect actual current keys for:

- Jobpost.
- Applicants.
- Proposal Unlock.
- Workspace.
- Submission Packages.
- Deliverable Files.
- Payment Wallet.
- Payment Transactions.
- Notifications.
- Expert/Profile data.

Check:

- Whether query keys include required IDs.
- Whether optional IDs can create collisions.
- Whether hooks are disabled for empty IDs.
- Whether mutations invalidate only affected data.
- Whether broad root invalidation is used unnecessarily.
- Whether new Package versions invalidate old/new detail correctly.
- Whether selected Package Detail is isolated by package ID.
- Whether switching IDs can display stale data.
- Whether list and detail keys are structurally consistent.
- Whether manual query keys duplicate centralized factories.

Do not change keys.

Report exact current key structures when relevant.

## Audit area 6 — Identifier safety

Search for API calls or links using:

- `workspaceId`
- `packageId`
- `proposalId`
- `jobPostId`
- `expertCode`
- `fileId`
- `escrowId`
- `userId`
- Array indexes
- Placeholder IDs
- Empty strings
- `undefined`
- `null`

Identify:

- Hooks that may call `/undefined`.
- Links that may produce invalid routes.
- ID substitutions such as job ID used as Workspace ID.
- Package ID used as File ID.
- Array index used as domain identity.
- Missing enabled guards.
- IDs read from localStorage instead of route or real response.
- Mock IDs entering real services.

Separate:

- Confirmed safe.
- Potentially unsafe.
- Runtime-dependent.
- Already guarded by parent rendering.

## Audit area 7 — Main Flow mock and placeholder audit

Search primary Client–Expert flow for:

- `mock`
- `fake`
- `demo`
- `placeholder`
- `TODO`
- `FIXME`
- `setTimeout`
- Local action success
- Static status transitions
- Hardcoded Package/Proposal/Workspace data
- Hardcoded wallet values
- Fake Contract or Hire state
- Buttons that only mutate local state
- Buttons that navigate without a real backend action
- Toasts claiming success without a mutation

Inspect at minimum:

- Client Jobpost pages.
- Expert marketplace/apply flow.
- Applicants.
- Proposal detail/unlock.
- Hire.
- Contract.
- Workspace Chat Room.
- Submission.
- Client review.
- Payment/Wallet.
- Notification.
- Dispute.
- Completion.

Classify every relevant mock as:

- SAFE PRESENTATIONAL MOCK
- KNOWN TEMPORARY MOCK
- MISLEADING BUSINESS ACTION
- MAIN FLOW BLOCKER
- DEAD CODE
- OUT OF SCOPE

Pay special attention to:

- Chat messages.
- Notification data.
- Hire actions.
- Payment unlock.
- Client Submission actions.
- Completion actions.
- Wallet balances.
- Success toasts.

Do not remove mocks.

## Audit area 8 — Barrel exports and import health

Inspect:

- `features/**/index.ts`
- `services` barrels if any.
- `types` barrels if any.
- `configs/index.ts`
- Module aliases.
- Re-export chains.

Search for:

- Exports pointing to missing files.
- Imports from deleted modules.
- Circular-looking barrel chains.
- Route imports that depend on broken barrels.
- Duplicate export names.
- Default/named export mismatch.
- Case-sensitive path mismatch.
- Legacy `modules/**` imports.
- References to removed components.
- Dead barrels used only by invalid code.

Do not rewrite barrels.

Report whether direct imports are safer for each known failure.

## Audit area 9 — Known TypeScript and build errors

The following errors have repeatedly appeared and must be diagnosed precisely.

### Error A — Experts base page

Known symptom:

`app/(main)/experts/page.tsx` is not a module.

Inspect:

- Exact file content.
- Whether it is empty.
- Whether it has invalid commented-out content.
- Whether it exports a default component.
- Whether a valid Expert listing feature already exists elsewhere.
- Whether links currently target `/experts`.
- Whether repairing this page is required before Main Flow.
- Smallest safe future fix.

Do not implement the fix.

### Error B — Calendar / react-day-picker

Known symptom:

`components/ui/calendar.tsx` uses an invalid `table` property in DayPicker class names or related incompatible API.

Inspect:

- Installed `react-day-picker` version.
- Current Calendar component implementation.
- Correct class-name keys for the installed version.
- Whether the component was generated for another version.
- Every Calendar consumer.
- Whether a minimal compatibility fix is possible.
- Whether UI behavior may change.

Use installed package types and official local type declarations as evidence.

Do not use web search unless explicitly needed.

Do not upgrade `react-day-picker`.

Do not implement the fix.

### Error C — Broken config barrel import

Known symptom:

`configs/index.ts` references:

`@/modules/users/forms/UserForm`

Inspect:

- Exact import/export.
- Whether the referenced file exists.
- Whether the path is stale.
- Whether the export is used anywhere.
- Whether `modules/users/**` is legacy architecture.
- Whether removing the export or correcting the path is safer.
- Whether another `UserForm` exists.
- Main Flow impact.

Do not change the file.

### Error D — Missing UserSchema

Known symptom:

`modules/users/useUserCrud.tsx` references missing `UserSchema`.

Inspect:

- Current imports.
- Existing user schemas.
- Whether this module is used.
- Whether it belongs to legacy Admin CRUD.
- Whether the missing symbol was renamed.
- Whether the module can be excluded or must compile.
- Smallest safe future fix.
- Main Flow relevance.

Do not fix it.

### Error E — Expert registration `any`

Known lint symptom:

`features/auth/register/ExpertRegisterForm.tsx` contains unexpected `any`, previously reported around line 88.

Inspect:

- Exact variable or callback.
- Correct available type.
- Whether a local type exists.
- Whether the fix changes runtime behavior.
- Whether it is safe for 7B.

Do not fix it.

### Error F — `use-mobile` state update in effect

Known lint symptom:

`hooks/use-mobile.ts` calls state setter in an effect in a way rejected by current lint rules.

Inspect:

- Exact hook implementation.
- Current lint rule.
- Consumer count.
- Whether it causes runtime hydration issues.
- Whether the common `matchMedia` lazy-state pattern can fix it.
- Whether it affects Main Flow.
- Smallest safe future fix.

Do not implement it.

## Audit area 10 — Warning baseline

Record lint warnings grouped by rule and path.

Pay attention to:

- `<img>` warnings.
- Missing hook dependencies.
- Unused imports.
- Explicit `any`.
- Accessibility.
- React Compiler-related warnings.
- Next.js image optimization.
- Deprecated APIs.
- Console statements.

Classify warnings:

- MUST FIX BEFORE MAIN FLOW
- SHOULD FIX IN 7B
- SAFE TO DEFER
- FALSE POSITIVE OR CONVENTION DECISION

Do not attempt to remove all warnings.

The goal is a stable baseline, not warning-zero at any cost.

## Audit area 11 — Main Flow backend blockers

Consolidate currently known backend blockers.

At minimum inspect frontend evidence for:

### Proposal purchase/unlock

- Missing atomic purchase/unlock endpoint.
- Protected Strategy Detail exposure.
- Wallet insufficiency response.
- Already-unlocked behavior.
- Required invalidations.

### Hire and Contract

- Negotiated amount request field.
- Create Contract/Escrow.
- Finalize after signatures/funding.
- Retry behavior.
- Idempotency.
- Workspace creation trigger.

### Workspace Proposal

- Proposal creation or association endpoint.
- Proposal source of truth after Hire.

### Submission review

- Package Reject.
- Feedback body.
- Package Accept.
- Status transitions.
- Completion behavior.
- Escrow release event.
- `autoReleaseAt`.

### Notification and Dispute

- Real notification contract.
- Dispute transitions.
- Completion/release interactions.

For each blocker classify:

- FRONTEND READY
- FRONTEND PREPARATION POSSIBLE
- BACKEND CONTRACT REQUIRED
- BACKEND SECURITY FIX REQUIRED
- RUNTIME VERIFICATION REQUIRED

Do not design speculative frontend mutations.

## Required commands

Run these commands and capture exact results.

1. `git status --short`
2. `npx tsc --noEmit --incremental false --pretty false`
3. `npm run lint -- --no-cache`
4. `npm run build`
5. `git diff --check`
6. `git status --short`

If the build command fails:

- Record the first root-cause error.
- Continue source inspection.
- Do not repeatedly rerun the same failing command without new evidence.
- Do not edit files.

If `.next` generated output affects TypeScript error reporting:

- Explain the relationship.
- Do not manually edit `.next`.
- Do not delete tracked source.
- You may inspect generated validator paths read-only.

Do not use destructive Git commands.

## Build baseline classification

For every TypeScript, lint, or build failure, report:

- Command.
- File.
- Line.
- Error code or lint rule.
- Root cause.
- Whether it is pre-existing.
- Whether it was introduced by Refactors 1–6.
- Main Flow impact.
- Minimal future fix.
- Expected files for 7B.
- Risk of fixing.

Classify each failure:

- BASELINE BLOCKER
- MAIN FLOW BLOCKER
- ROUTE-SPECIFIC BLOCKER
- LINT-ONLY BLOCKER
- NON-BLOCKING WARNING
- GENERATED CONSEQUENCE OF SOURCE ERROR

Do not merely repeat compiler output.

## Refactor regression check

Verify Refactors 1–6 did not introduce obvious architecture regressions.

Confirm or refute:

1. Public Expert Profile route remains thin.
2. Applicant link still uses `expertCode`.
3. Proposal Detail remains lazy by `proposalId`.
4. Workspace services use shared API client.
5. Submission reads/writes use service hooks.
6. Chat Room route remains thin.
7. Submission upload uses feature hook and shared Storage service.
8. Avatar uses shared Storage service.
9. Client Package panel uses real reads.
10. Fake Client review actions remain removed.
11. Client panel is gated by real Workspace success.
12. No direct Storage SDK call returned to feature code.
13. No new manual Authorization/token logic was introduced.
14. No unsupported Accept/Reject endpoint was invented.

## Required readiness matrix

Produce a readiness matrix for:

- Public Expert Profile.
- Expert listing.
- Client Jobpost list/detail.
- Expert marketplace.
- Apply Jobpost.
- Applicant list.
- Proposal summary.
- Proposal protected detail.
- Proposal payment/unlock.
- Hire.
- Contract.
- Workspace reads.
- Workspace members.
- Workspace Chat shell.
- Expert Package create.
- Expert Package resubmit.
- Client Package history.
- Client Package detail/files.
- Client Reject.
- Client Accept.
- Completion.
- Wallet/Transactions.
- Notification.
- Dispute.
- Repository build baseline.

Use only:

- READY
- READY AFTER 7B BASELINE FIX
- READY FOR READS ONLY
- FRONTEND FOUNDATION READY
- BLOCKED BY BACKEND CONTRACT
- BLOCKED BY BACKEND SECURITY
- MOCK ONLY
- RUNTIME VERIFICATION REQUIRED
- NOT AUDITED

## Refactor 7B planning requirements

Based on evidence, propose a minimal 7B fix set.

The plan must:

- Fix only build/lint baseline blockers.
- Avoid broad architecture refactors.
- Avoid business-flow implementation.
- Avoid dependency upgrades unless absolutely unavoidable.
- Keep each fix independently reviewable.
- Rank fixes by dependency and risk.
- State exact expected files.
- State whether each fix requires manual UI regression testing.
- State whether each fix can be committed independently.

A likely 7B plan may include:

- Repair invalid `/experts` route module.
- Repair Calendar compatibility with installed DayPicker.
- Remove or correct stale `configs/index.ts` export.
- Repair or isolate legacy `useUserCrud.tsx`.
- Replace explicit `any` in Expert registration.
- Repair `use-mobile` lint pattern.

Do not assume all six should be fixed together.

Use source evidence to decide.

## No-write verification

At the end:

1. Run `git diff --check`.
2. Run `git status --short`.
3. Confirm no tracked application source file changed.
4. Report generated ignored artifacts separately when relevant.
5. Do not clean or reset the repository.

## Required final output

Return a detailed Markdown report with these exact sections.

# 1. Executive Summary

State:

- Whether Refactors 1–6 are structurally closed.
- Whether major architecture boundaries are now consistent.
- Whether the repository currently type-checks.
- Whether lint passes.
- Whether production build passes.
- Whether Main Flow implementation may start before 7B.
- Highest-priority blockers.

# 2. Repository and Tooling Baseline

Report:

- Framework and package versions.
- Build/lint/typecheck scripts.
- Initial Git status.
- Final Git status.
- Generated-output considerations.

# 3. Refactor 1–6 Regression Check

Report every required regression check as PASS, FAIL, or RUNTIME UNVERIFIED.

# 4. Route Architecture Audit

Provide a table with:

- Route.
- Route file.
- Classification.
- Main issue.
- Main Flow impact.
- Recommended action.

# 5. Direct Network and Auth Audit

List every direct or suspicious API/auth occurrence outside approved boundaries.

# 6. Supabase Closure Audit

Report:

- Client initialization count.
- Direct Storage SDK call locations.
- Submission consumer path.
- Avatar consumer path.
- Credential/logging findings.
- Remaining runtime/security limitations.

# 7. Service and Type Architecture Audit

Report:

- Duplicate DTOs.
- Local API types.
- Unsafe assertions.
- Response-envelope inconsistencies.
- Hook enabled guards.
- Service-boundary violations.

# 8. Query Key and Invalidation Audit

Report actual strengths, risks, collisions, and invalidation concerns.

# 9. Identifier Safety Audit

Report confirmed safe and unsafe identifier flows.

# 10. Main Flow Mock and Placeholder Audit

Provide a table with:

- Feature.
- File.
- Mock or placeholder.
- User-visible behavior.
- Risk classification.
- Backend dependency.
- Recommended action.

# 11. Barrel Export and Import Health

Report stale exports, missing modules, possible cycles, and direct-import recommendations.

# 12. Build and TypeScript Baseline

For each error provide:

- Command.
- File.
- Error.
- Root cause.
- Classification.
- Main Flow impact.
- Minimal 7B fix.

# 13. Lint Baseline

Group errors and warnings by rule and severity.

# 14. Known Error Deep Diagnosis

Create subsections for:

- Experts base page.
- Calendar and DayPicker.
- `configs/index.ts`.
- `useUserCrud.tsx`.
- Expert registration `any`.
- `use-mobile`.

# 15. Backend Contract and Security Blockers

Group by:

- Proposal Unlock.
- Hire/Contract.
- Workspace Proposal.
- Submission Review.
- Completion/Release.
- Notification/Dispute.

# 16. Frontend Readiness Matrix

Use the required readiness classifications.

# 17. Main Flow Entry Assessment

Explain:

- Which Main Flow phase could technically start.
- Which phase should start first.
- Which phases are blocked.
- Whether starting before build baseline cleanup is advisable.

# 18. Recommended Refactor 7B Fix Set

Provide ordered, minimal, independently reviewable tasks.

For each task include:

- Goal.
- Exact files.
- Expected change.
- Risk.
- Verification.
- Suggested commit boundary.

# 19. Files Expected to Change in 7B

List exact paths grouped by task.

# 20. Manual Regression Plan

Include tests for:

- `/experts`.
- Public Expert Profile.
- Calendar consumers.
- Client/Expert registration.
- Client Jobpost routes.
- Workspace Chat Room.
- Expert Submission.
- Client Package reads.

# 21. Verification Results

Report exact results for:

- TypeScript.
- Lint.
- Build.
- Diff check.
- Git status.
- Confirmation that no source files changed.

# 22. Final Decision

Return exactly one:

- GO FOR REFACTOR 7B TARGETED BASELINE FIXES
- GO FOR MAIN FLOW
- NO-GO UNTIL REPOSITORY STRUCTURE IS REPAIRED

Explain the decision precisely.

Important:

- This is read-only diagnosis.
- Do not fix any source file.
- Do not implement Main Flow.
- Do not invent backend endpoints.
- Distinguish source-confirmed facts from inference.
- Separate architecture readiness from business-contract readiness.
- Preserve a clean Git worktree.
- You are working in my existing Next.js web application: Neuro Bridge.

This is Refactor 7A: Architecture Closure Audit and Build Baseline Diagnosis only.

This task is strictly read-only for application source files.

Do NOT modify application source files.
Do NOT create application source files.
Do NOT delete, rename, or move application source files.
Do NOT run formatter or auto-fix commands.
Do NOT run ESLint with `--fix`.
Do NOT change package versions.
Do NOT install packages.
Do NOT upgrade dependencies.
Do NOT edit generated route types manually.
Do NOT implement Main Flow business features.
Do NOT implement missing backend endpoints.
Do NOT replace remaining mocks during this audit.
Do NOT fix build or lint errors yet.

You may run build, TypeScript, lint, and read-only search commands.

Generated build output such as `.next/**` may be created by normal Next.js commands, but no tracked application source file may change.

Confirm `git status --short` before and after the audit.

## Current project

Application:

- Neuro Bridge frontend
- Next.js App Router
- TypeScript
- Tailwind
- shadcn-style UI
- React Query
- Redux
- Supabase browser Storage

Project architecture convention:

- `app/**`
  - Thin route and layout shells only
  - Route parameters
  - Render feature views

- `features/**`
  - Feature views
  - Components
  - Local UI state
  - Feature orchestration
  - Feature-specific hooks and mappings
  - Temporary mock data only when explicitly documented

- `services/**`
  - Backend API functions
  - React Query hooks
  - Query keys
  - Mutations
  - External integration boundaries

- `types/**`
  - Domain types
  - Request types
  - Response types
  - Integration contracts

- `configs/**`
  - Initialized shared clients
  - Environment configuration

## Completed refactors

The following architecture work is complete and must not be reopened without concrete evidence.

### Refactor 1 — Public Expert Profile repair

Completed:

- `/experts/[expertCode]` uses a thin route shell.
- Public profile view lives under `features/profile/expert/**`.
- Real Expert and Skill reads are used.
- Invalid code and empty-skill states were handled.
- Broken imports and local type assumptions were repaired.

Known remaining issue:

- The base `/experts` page has previously been reported as not being a valid module.

### Refactor 2 — Applicant to Expert Profile

Completed:

- Client applicant card links to the public Expert profile.
- It uses `applicant.expertApply.expertCode`.
- Missing codes do not create invalid URLs.
- Proposal Unlock remains independent.

### Proposal protected-detail preparation

Completed:

- Proposal Detail is fetched lazily by `proposalId`.
- The dialog no longer uses protected Strategy Detail from the applicant-list response.
- The frontend ignores premature `strategyDetail` fields.

Backend/security concern still open:

- Backend must not expose protected `strategyDetail` before unlock.

### Refactor 3 — Workspace foundation

Completed:

- Centralized Workspace types.
- Shared API client usage.
- Workspace Detail and Members reads.
- Stable Workspace query keys.
- No manual Authorization header in the new Workspace service.

### Refactor 4 — Submission Package foundation

Completed:

- Package list by Workspace.
- Package Detail.
- Deliverable Files by Package.
- File Types.
- Create Submission Package.
- Resubmit Submission Package.
- Scoped query invalidation.
- Chat Room feature extraction.
- Thin Chat Room route shell.

### Refactor 5 — Supabase upload consolidation

Completed:

- One canonical Supabase client in `configs/supabase.ts`.
- One direct Storage SDK boundary in `services/supabase-storage.service.ts`.
- Submission upload feature hook.
- Submission upload utilities.
- Normalized Storage errors.
- Avatar helper migrated to the shared Storage boundary.
- Unused `uploadFiles` removed.
- No direct Supabase Storage SDK call should remain outside the generic service.

Runtime Storage testing and policy verification remain deferred.

### Refactor 6 — Client Submission Review preparation

Completed:

- 6A API and frontend audit.
- 6B real Client Package read integration.
- Mock Client Package history removed.
- Mock Deliverable Files removed.
- Fake Approve/Reject behavior removed.
- Real Package list and selected Package Detail are used.
- Client panel is gated by successful Workspace access.
- Latest Package does not rely on `packages[0]`.
- Status and feedback are read-only.

Backend blockers remain:

- Package Accept endpoint.
- Package Reject endpoint.
- Feedback write contract.
- Package status machine.
- Workspace/Project completion behavior.
- `autoReleaseAt` semantics.
- Escrow/Kafka release ownership.

## Main goal

Produce a complete evidence-based closure report for Refactors 1–6 and establish the exact current build/lint baseline before Main Flow implementation begins.

The audit must answer:

1. Whether the architecture convention is now consistently followed.
2. Whether remaining violations affect the Main Flow.
3. Whether direct API, token, Supabase, or DTO duplication remains.
4. Whether misleading mock actions remain in the primary business flow.
5. Whether invalid identifiers can still trigger real requests.
6. Whether query keys and invalidations are safe enough for Main Flow work.
7. Whether current TypeScript, lint, and build failures are understood.
8. Which failures must be fixed in Refactor 7B.
9. Which warnings may remain as non-blocking debt.
10. Whether frontend architecture is ready to enter Main Flow after targeted baseline fixes.

## Strict scope

This is diagnosis only.

Do NOT:

- Fix `/experts`.
- Fix Calendar.
- Fix `configs/index.ts`.
- Fix `useUserCrud.tsx`.
- Fix lint errors.
- Replace Chat mocks.
- Replace Notification mocks.
- Add Accept/Reject.
- Add Payment Unlock.
- Add Hire/Contract.
- Add Workspace initialization.
- Add Completion.
- Add Escrow release.
- Change API response shapes.
- Change query invalidation.
- Change role logic.
- Change Supabase configuration.
- Change route structure.
- Delete dead code.
- Modify barrel exports.
- Change TypeScript configuration.
- Change ESLint configuration.
- Change Next.js configuration.

Only inspect and report.

## Required initial repository state

Before inspection:

1. Run `git status --short`.
2. Record whether the worktree is clean.
3. If tracked source changes exist, do not edit or reset them.
4. Report the exact paths and continue read-only only when the source can still be inspected safely.
5. Do not use `git checkout`, `git restore`, `git reset`, or `git clean`.

## Required configuration inspection

Inspect:

- `package.json`
- Lockfile
- `tsconfig.json`
- Next.js config
- ESLint config
- Path aliases
- React Query provider setup
- Redux provider setup
- Shared API client configuration
- `configs/supabase.ts`
- Current Next.js version
- Current React version
- Current `react-day-picker` version
- Current TypeScript version
- Current ESLint version

Determine:

- Exact build script.
- Exact lint script.
- Whether build invokes lint separately.
- Whether TypeScript incremental output may affect results.
- Whether `.next` validators are generated from a currently invalid route.
- Whether path aliases match current source layout.
- Whether known errors are caused by version mismatch, missing file, stale export, or invalid source.

Do not alter configuration.

## Audit area 1 — Route and layout convention

Inspect all application routes under:

`app/**`

Pay special attention to:

- `/experts`
- `/experts/[id]` or `/experts/[expertCode]`
- `/client/dashboard/my-job-post`
- `/client/dashboard/my-job-post/[id]`
- `/client/dashboard/create-job-post`
- `/chat-room/[workspaceId]`
- Auth routes
- Profile routes
- Available AI routes
- Client and Expert dashboard routes

Classify each route as:

- CLEAN THIN SHELL
- ACCEPTABLE ROUTE-SPECIFIC LOGIC
- FEATURE LOGIC LEAKED INTO APP
- INVALID ROUTE MODULE
- DEAD OR DUPLICATE ROUTE
- REQUIRES RUNTIME VERIFICATION

Check for:

- Large JSX trees in route files.
- Local API calls in route files.
- Local DTOs in route files.
- Mock data in route files.
- Business mutations in route files.
- Direct Supabase use in route files.
- Duplicate route implementations.
- Empty files.
- Files that export no default React component.
- Invalid route groups.
- Route links pointing to nonexistent routes.
- Dynamic parameter naming inconsistencies.
- Native Next.js page files that are not valid modules.

Do not refactor any route.

## Audit area 2 — Direct network and authorization usage

Search application source for:

- `fetch(`
- `axios`
- `.get(`
- `.post(`
- `.put(`
- `.patch(`
- `.delete(`
- `Authorization`
- `Bearer `
- `localStorage`
- `sessionStorage`
- `document.cookie`
- Manual token extraction
- Manual API base URLs
- Duplicate API client initialization
- Direct Workspace endpoint strings
- Direct Jobpost endpoint strings
- Direct Payment endpoint strings
- Direct Proposal endpoint strings
- Direct Notification endpoint strings

Ignore:

- Shared API-client implementation.
- Framework internals.
- `node_modules`.
- `.next`.
- Generated output.

For every direct request outside an approved service boundary, report:

- File.
- Operation.
- Endpoint.
- Authentication method.
- Whether it belongs under `services/**`.
- Whether it affects Main Flow.
- Severity.
- Recommended future action.

Distinguish:

- Legitimate external fetches.
- API service functions.
- Legacy direct backend calls.
- Mock-only code.
- Dead code.
- Browser asset requests.

Do not assume every occurrence of `.get` is HTTP.

## Audit area 3 — Supabase closure verification

Search for:

- `createClient(`
- `@supabase/supabase-js`
- `@/configs/supabase`
- `supabase.storage`
- `.storage.from(`
- `.upload(`
- `.getPublicUrl(`
- `.createSignedUrl(`
- `.remove(`
- `listBuckets`
- `service_role`
- Supabase key logging
- Session logging
- Token logging

Confirm:

1. `createClient` exists only in `configs/supabase.ts`.
2. Storage SDK calls exist only in `services/supabase-storage.service.ts`.
3. Submission uses the shared Storage boundary.
4. Avatar upload uses the shared Storage boundary.
5. No service-role key exists in browser source.
6. No credential/session/token logging exists.
7. No duplicate Supabase config remains.
8. No feature imports Supabase directly for Storage.

Report any exception.

Do not modify Supabase code.

## Audit area 4 — Service and type architecture

Inspect:

- `services/**`
- `types/**`
- Feature-local API helpers
- Local request/response interfaces
- Query-key declarations
- Mutation hooks
- Response unwrapping helpers
- Error normalization helpers

Search for:

- Duplicate API DTO definitions.
- Local interfaces matching centralized types.
- `any`.
- `unknown as`.
- Broad type assertions.
- Non-null assertions around API identifiers.
- Response types typed differently across consumers.
- Universal envelope assumptions.
- Direct object versus `{ code, message, result }` ambiguity.
- Hooks without enabled guards.
- Query keys missing identifiers.
- Mutation functions embedded in UI.
- API functions embedded in feature components.

For each issue classify:

- MAIN FLOW BLOCKER
- SHOULD FIX IN 7B
- SAFE TO DEFER
- INTENTIONAL FEATURE-LOCAL TYPE
- RUNTIME CONTRACT VERIFICATION NEEDED

Do not centralize types during this audit.

## Audit area 5 — Query keys and invalidation

Inspect actual current keys for:

- Jobpost.
- Applicants.
- Proposal Unlock.
- Workspace.
- Submission Packages.
- Deliverable Files.
- Payment Wallet.
- Payment Transactions.
- Notifications.
- Expert/Profile data.

Check:

- Whether query keys include required IDs.
- Whether optional IDs can create collisions.
- Whether hooks are disabled for empty IDs.
- Whether mutations invalidate only affected data.
- Whether broad root invalidation is used unnecessarily.
- Whether new Package versions invalidate old/new detail correctly.
- Whether selected Package Detail is isolated by package ID.
- Whether switching IDs can display stale data.
- Whether list and detail keys are structurally consistent.
- Whether manual query keys duplicate centralized factories.

Do not change keys.

Report exact current key structures when relevant.

## Audit area 6 — Identifier safety

Search for API calls or links using:

- `workspaceId`
- `packageId`
- `proposalId`
- `jobPostId`
- `expertCode`
- `fileId`
- `escrowId`
- `userId`
- Array indexes
- Placeholder IDs
- Empty strings
- `undefined`
- `null`

Identify:

- Hooks that may call `/undefined`.
- Links that may produce invalid routes.
- ID substitutions such as job ID used as Workspace ID.
- Package ID used as File ID.
- Array index used as domain identity.
- Missing enabled guards.
- IDs read from localStorage instead of route or real response.
- Mock IDs entering real services.

Separate:

- Confirmed safe.
- Potentially unsafe.
- Runtime-dependent.
- Already guarded by parent rendering.

## Audit area 7 — Main Flow mock and placeholder audit

Search primary Client–Expert flow for:

- `mock`
- `fake`
- `demo`
- `placeholder`
- `TODO`
- `FIXME`
- `setTimeout`
- Local action success
- Static status transitions
- Hardcoded Package/Proposal/Workspace data
- Hardcoded wallet values
- Fake Contract or Hire state
- Buttons that only mutate local state
- Buttons that navigate without a real backend action
- Toasts claiming success without a mutation

Inspect at minimum:

- Client Jobpost pages.
- Expert marketplace/apply flow.
- Applicants.
- Proposal detail/unlock.
- Hire.
- Contract.
- Workspace Chat Room.
- Submission.
- Client review.
- Payment/Wallet.
- Notification.
- Dispute.
- Completion.

Classify every relevant mock as:

- SAFE PRESENTATIONAL MOCK
- KNOWN TEMPORARY MOCK
- MISLEADING BUSINESS ACTION
- MAIN FLOW BLOCKER
- DEAD CODE
- OUT OF SCOPE

Pay special attention to:

- Chat messages.
- Notification data.
- Hire actions.
- Payment unlock.
- Client Submission actions.
- Completion actions.
- Wallet balances.
- Success toasts.

Do not remove mocks.

## Audit area 8 — Barrel exports and import health

Inspect:

- `features/**/index.ts`
- `services` barrels if any.
- `types` barrels if any.
- `configs/index.ts`
- Module aliases.
- Re-export chains.

Search for:

- Exports pointing to missing files.
- Imports from deleted modules.
- Circular-looking barrel chains.
- Route imports that depend on broken barrels.
- Duplicate export names.
- Default/named export mismatch.
- Case-sensitive path mismatch.
- Legacy `modules/**` imports.
- References to removed components.
- Dead barrels used only by invalid code.

Do not rewrite barrels.

Report whether direct imports are safer for each known failure.

## Audit area 9 — Known TypeScript and build errors

The following errors have repeatedly appeared and must be diagnosed precisely.

### Error A — Experts base page

Known symptom:

`app/(main)/experts/page.tsx` is not a module.

Inspect:

- Exact file content.
- Whether it is empty.
- Whether it has invalid commented-out content.
- Whether it exports a default component.
- Whether a valid Expert listing feature already exists elsewhere.
- Whether links currently target `/experts`.
- Whether repairing this page is required before Main Flow.
- Smallest safe future fix.

Do not implement the fix.

### Error B — Calendar / react-day-picker

Known symptom:

`components/ui/calendar.tsx` uses an invalid `table` property in DayPicker class names or related incompatible API.

Inspect:

- Installed `react-day-picker` version.
- Current Calendar component implementation.
- Correct class-name keys for the installed version.
- Whether the component was generated for another version.
- Every Calendar consumer.
- Whether a minimal compatibility fix is possible.
- Whether UI behavior may change.

Use installed package types and official local type declarations as evidence.

Do not use web search unless explicitly needed.

Do not upgrade `react-day-picker`.

Do not implement the fix.

### Error C — Broken config barrel import

Known symptom:

`configs/index.ts` references:

`@/modules/users/forms/UserForm`

Inspect:

- Exact import/export.
- Whether the referenced file exists.
- Whether the path is stale.
- Whether the export is used anywhere.
- Whether `modules/users/**` is legacy architecture.
- Whether removing the export or correcting the path is safer.
- Whether another `UserForm` exists.
- Main Flow impact.

Do not change the file.

### Error D — Missing UserSchema

Known symptom:

`modules/users/useUserCrud.tsx` references missing `UserSchema`.

Inspect:

- Current imports.
- Existing user schemas.
- Whether this module is used.
- Whether it belongs to legacy Admin CRUD.
- Whether the missing symbol was renamed.
- Whether the module can be excluded or must compile.
- Smallest safe future fix.
- Main Flow relevance.

Do not fix it.

### Error E — Expert registration `any`

Known lint symptom:

`features/auth/register/ExpertRegisterForm.tsx` contains unexpected `any`, previously reported around line 88.

Inspect:

- Exact variable or callback.
- Correct available type.
- Whether a local type exists.
- Whether the fix changes runtime behavior.
- Whether it is safe for 7B.

Do not fix it.

### Error F — `use-mobile` state update in effect

Known lint symptom:

`hooks/use-mobile.ts` calls state setter in an effect in a way rejected by current lint rules.

Inspect:

- Exact hook implementation.
- Current lint rule.
- Consumer count.
- Whether it causes runtime hydration issues.
- Whether the common `matchMedia` lazy-state pattern can fix it.
- Whether it affects Main Flow.
- Smallest safe future fix.

Do not implement it.

## Audit area 10 — Warning baseline

Record lint warnings grouped by rule and path.

Pay attention to:

- `<img>` warnings.
- Missing hook dependencies.
- Unused imports.
- Explicit `any`.
- Accessibility.
- React Compiler-related warnings.
- Next.js image optimization.
- Deprecated APIs.
- Console statements.

Classify warnings:

- MUST FIX BEFORE MAIN FLOW
- SHOULD FIX IN 7B
- SAFE TO DEFER
- FALSE POSITIVE OR CONVENTION DECISION

Do not attempt to remove all warnings.

The goal is a stable baseline, not warning-zero at any cost.

## Audit area 11 — Main Flow backend blockers

Consolidate currently known backend blockers.

At minimum inspect frontend evidence for:

### Proposal purchase/unlock

- Missing atomic purchase/unlock endpoint.
- Protected Strategy Detail exposure.
- Wallet insufficiency response.
- Already-unlocked behavior.
- Required invalidations.

### Hire and Contract

- Negotiated amount request field.
- Create Contract/Escrow.
- Finalize after signatures/funding.
- Retry behavior.
- Idempotency.
- Workspace creation trigger.

### Workspace Proposal

- Proposal creation or association endpoint.
- Proposal source of truth after Hire.

### Submission review

- Package Reject.
- Feedback body.
- Package Accept.
- Status transitions.
- Completion behavior.
- Escrow release event.
- `autoReleaseAt`.

### Notification and Dispute

- Real notification contract.
- Dispute transitions.
- Completion/release interactions.

For each blocker classify:

- FRONTEND READY
- FRONTEND PREPARATION POSSIBLE
- BACKEND CONTRACT REQUIRED
- BACKEND SECURITY FIX REQUIRED
- RUNTIME VERIFICATION REQUIRED

Do not design speculative frontend mutations.

## Required commands

Run these commands and capture exact results.

1. `git status --short`
2. `npx tsc --noEmit --incremental false --pretty false`
3. `npm run lint -- --no-cache`
4. `npm run build`
5. `git diff --check`
6. `git status --short`

If the build command fails:

- Record the first root-cause error.
- Continue source inspection.
- Do not repeatedly rerun the same failing command without new evidence.
- Do not edit files.

If `.next` generated output affects TypeScript error reporting:

- Explain the relationship.
- Do not manually edit `.next`.
- Do not delete tracked source.
- You may inspect generated validator paths read-only.

Do not use destructive Git commands.

## Build baseline classification

For every TypeScript, lint, or build failure, report:

- Command.
- File.
- Line.
- Error code or lint rule.
- Root cause.
- Whether it is pre-existing.
- Whether it was introduced by Refactors 1–6.
- Main Flow impact.
- Minimal future fix.
- Expected files for 7B.
- Risk of fixing.

Classify each failure:

- BASELINE BLOCKER
- MAIN FLOW BLOCKER
- ROUTE-SPECIFIC BLOCKER
- LINT-ONLY BLOCKER
- NON-BLOCKING WARNING
- GENERATED CONSEQUENCE OF SOURCE ERROR

Do not merely repeat compiler output.

## Refactor regression check

Verify Refactors 1–6 did not introduce obvious architecture regressions.

Confirm or refute:

1. Public Expert Profile route remains thin.
2. Applicant link still uses `expertCode`.
3. Proposal Detail remains lazy by `proposalId`.
4. Workspace services use shared API client.
5. Submission reads/writes use service hooks.
6. Chat Room route remains thin.
7. Submission upload uses feature hook and shared Storage service.
8. Avatar uses shared Storage service.
9. Client Package panel uses real reads.
10. Fake Client review actions remain removed.
11. Client panel is gated by real Workspace success.
12. No direct Storage SDK call returned to feature code.
13. No new manual Authorization/token logic was introduced.
14. No unsupported Accept/Reject endpoint was invented.

## Required readiness matrix

Produce a readiness matrix for:

- Public Expert Profile.
- Expert listing.
- Client Jobpost list/detail.
- Expert marketplace.
- Apply Jobpost.
- Applicant list.
- Proposal summary.
- Proposal protected detail.
- Proposal payment/unlock.
- Hire.
- Contract.
- Workspace reads.
- Workspace members.
- Workspace Chat shell.
- Expert Package create.
- Expert Package resubmit.
- Client Package history.
- Client Package detail/files.
- Client Reject.
- Client Accept.
- Completion.
- Wallet/Transactions.
- Notification.
- Dispute.
- Repository build baseline.

Use only:

- READY
- READY AFTER 7B BASELINE FIX
- READY FOR READS ONLY
- FRONTEND FOUNDATION READY
- BLOCKED BY BACKEND CONTRACT
- BLOCKED BY BACKEND SECURITY
- MOCK ONLY
- RUNTIME VERIFICATION REQUIRED
- NOT AUDITED

## Refactor 7B planning requirements

Based on evidence, propose a minimal 7B fix set.

The plan must:

- Fix only build/lint baseline blockers.
- Avoid broad architecture refactors.
- Avoid business-flow implementation.
- Avoid dependency upgrades unless absolutely unavoidable.
- Keep each fix independently reviewable.
- Rank fixes by dependency and risk.
- State exact expected files.
- State whether each fix requires manual UI regression testing.
- State whether each fix can be committed independently.

A likely 7B plan may include:

- Repair invalid `/experts` route module.
- Repair Calendar compatibility with installed DayPicker.
- Remove or correct stale `configs/index.ts` export.
- Repair or isolate legacy `useUserCrud.tsx`.
- Replace explicit `any` in Expert registration.
- Repair `use-mobile` lint pattern.

Do not assume all six should be fixed together.

Use source evidence to decide.

## No-write verification

At the end:

1. Run `git diff --check`.
2. Run `git status --short`.
3. Confirm no tracked application source file changed.
4. Report generated ignored artifacts separately when relevant.
5. Do not clean or reset the repository.

## Required final output

Return a detailed Markdown report with these exact sections.

# 1. Executive Summary

State:

- Whether Refactors 1–6 are structurally closed.
- Whether major architecture boundaries are now consistent.
- Whether the repository currently type-checks.
- Whether lint passes.
- Whether production build passes.
- Whether Main Flow implementation may start before 7B.
- Highest-priority blockers.

# 2. Repository and Tooling Baseline

Report:

- Framework and package versions.
- Build/lint/typecheck scripts.
- Initial Git status.
- Final Git status.
- Generated-output considerations.

# 3. Refactor 1–6 Regression Check

Report every required regression check as PASS, FAIL, or RUNTIME UNVERIFIED.

# 4. Route Architecture Audit

Provide a table with:

- Route.
- Route file.
- Classification.
- Main issue.
- Main Flow impact.
- Recommended action.

# 5. Direct Network and Auth Audit

List every direct or suspicious API/auth occurrence outside approved boundaries.

# 6. Supabase Closure Audit

Report:

- Client initialization count.
- Direct Storage SDK call locations.
- Submission consumer path.
- Avatar consumer path.
- Credential/logging findings.
- Remaining runtime/security limitations.

# 7. Service and Type Architecture Audit

Report:

- Duplicate DTOs.
- Local API types.
- Unsafe assertions.
- Response-envelope inconsistencies.
- Hook enabled guards.
- Service-boundary violations.

# 8. Query Key and Invalidation Audit

Report actual strengths, risks, collisions, and invalidation concerns.

# 9. Identifier Safety Audit

Report confirmed safe and unsafe identifier flows.

# 10. Main Flow Mock and Placeholder Audit

Provide a table with:

- Feature.
- File.
- Mock or placeholder.
- User-visible behavior.
- Risk classification.
- Backend dependency.
- Recommended action.

# 11. Barrel Export and Import Health

Report stale exports, missing modules, possible cycles, and direct-import recommendations.

# 12. Build and TypeScript Baseline

For each error provide:

- Command.
- File.
- Error.
- Root cause.
- Classification.
- Main Flow impact.
- Minimal 7B fix.

# 13. Lint Baseline

Group errors and warnings by rule and severity.

# 14. Known Error Deep Diagnosis

Create subsections for:

- Experts base page.
- Calendar and DayPicker.
- `configs/index.ts`.
- `useUserCrud.tsx`.
- Expert registration `any`.
- `use-mobile`.

# 15. Backend Contract and Security Blockers

Group by:

- Proposal Unlock.
- Hire/Contract.
- Workspace Proposal.
- Submission Review.
- Completion/Release.
- Notification/Dispute.

# 16. Frontend Readiness Matrix

Use the required readiness classifications.

# 17. Main Flow Entry Assessment

Explain:

- Which Main Flow phase could technically start.
- Which phase should start first.
- Which phases are blocked.
- Whether starting before build baseline cleanup is advisable.

# 18. Recommended Refactor 7B Fix Set

Provide ordered, minimal, independently reviewable tasks.

For each task include:

- Goal.
- Exact files.
- Expected change.
- Risk.
- Verification.
- Suggested commit boundary.

# 19. Files Expected to Change in 7B

List exact paths grouped by task.

# 20. Manual Regression Plan

Include tests for:

- `/experts`.
- Public Expert Profile.
- Calendar consumers.
- Client/Expert registration.
- Client Jobpost routes.
- Workspace Chat Room.
- Expert Submission.
- Client Package reads.

# 21. Verification Results

Report exact results for:

- TypeScript.
- Lint.
- Build.
- Diff check.
- Git status.
- Confirmation that no source files changed.

# 22. Final Decision

Return exactly one:

- GO FOR REFACTOR 7B TARGETED BASELINE FIXES
- GO FOR MAIN FLOW
- NO-GO UNTIL REPOSITORY STRUCTURE IS REPAIRED

Explain the decision precisely.

Important:

- This is read-only diagnosis.
- Do not fix any source file.
- Do not implement Main Flow.
- Do not invent backend endpoints.
- Distinguish source-confirmed facts from inference.
- Separate architecture readiness from business-contract readiness.
- Preserve a clean Git worktree.
- You are working in my existing Next.js web application: Neuro Bridge.

This is Refactor 7A: Architecture Closure Audit and Build Baseline Diagnosis only.

This task is strictly read-only for application source files.

Do NOT modify application source files.
Do NOT create application source files.
Do NOT delete, rename, or move application source files.
Do NOT run formatter or auto-fix commands.
Do NOT run ESLint with `--fix`.
Do NOT change package versions.
Do NOT install packages.
Do NOT upgrade dependencies.
Do NOT edit generated route types manually.
Do NOT implement Main Flow business features.
Do NOT implement missing backend endpoints.
Do NOT replace remaining mocks during this audit.
Do NOT fix build or lint errors yet.

You may run build, TypeScript, lint, and read-only search commands.

Generated build output such as `.next/**` may be created by normal Next.js commands, but no tracked application source file may change.

Confirm `git status --short` before and after the audit.

## Current project

Application:

- Neuro Bridge frontend
- Next.js App Router
- TypeScript
- Tailwind
- shadcn-style UI
- React Query
- Redux
- Supabase browser Storage

Project architecture convention:

- `app/**`
  - Thin route and layout shells only
  - Route parameters
  - Render feature views

- `features/**`
  - Feature views
  - Components
  - Local UI state
  - Feature orchestration
  - Feature-specific hooks and mappings
  - Temporary mock data only when explicitly documented

- `services/**`
  - Backend API functions
  - React Query hooks
  - Query keys
  - Mutations
  - External integration boundaries

- `types/**`
  - Domain types
  - Request types
  - Response types
  - Integration contracts

- `configs/**`
  - Initialized shared clients
  - Environment configuration

## Completed refactors

The following architecture work is complete and must not be reopened without concrete evidence.

### Refactor 1 — Public Expert Profile repair

Completed:

- `/experts/[expertCode]` uses a thin route shell.
- Public profile view lives under `features/profile/expert/**`.
- Real Expert and Skill reads are used.
- Invalid code and empty-skill states were handled.
- Broken imports and local type assumptions were repaired.

Known remaining issue:

- The base `/experts` page has previously been reported as not being a valid module.

### Refactor 2 — Applicant to Expert Profile

Completed:

- Client applicant card links to the public Expert profile.
- It uses `applicant.expertApply.expertCode`.
- Missing codes do not create invalid URLs.
- Proposal Unlock remains independent.

### Proposal protected-detail preparation

Completed:

- Proposal Detail is fetched lazily by `proposalId`.
- The dialog no longer uses protected Strategy Detail from the applicant-list response.
- The frontend ignores premature `strategyDetail` fields.

Backend/security concern still open:

- Backend must not expose protected `strategyDetail` before unlock.

### Refactor 3 — Workspace foundation

Completed:

- Centralized Workspace types.
- Shared API client usage.
- Workspace Detail and Members reads.
- Stable Workspace query keys.
- No manual Authorization header in the new Workspace service.

### Refactor 4 — Submission Package foundation

Completed:

- Package list by Workspace.
- Package Detail.
- Deliverable Files by Package.
- File Types.
- Create Submission Package.
- Resubmit Submission Package.
- Scoped query invalidation.
- Chat Room feature extraction.
- Thin Chat Room route shell.

### Refactor 5 — Supabase upload consolidation

Completed:

- One canonical Supabase client in `configs/supabase.ts`.
- One direct Storage SDK boundary in `services/supabase-storage.service.ts`.
- Submission upload feature hook.
- Submission upload utilities.
- Normalized Storage errors.
- Avatar helper migrated to the shared Storage boundary.
- Unused `uploadFiles` removed.
- No direct Supabase Storage SDK call should remain outside the generic service.

Runtime Storage testing and policy verification remain deferred.

### Refactor 6 — Client Submission Review preparation

Completed:

- 6A API and frontend audit.
- 6B real Client Package read integration.
- Mock Client Package history removed.
- Mock Deliverable Files removed.
- Fake Approve/Reject behavior removed.
- Real Package list and selected Package Detail are used.
- Client panel is gated by successful Workspace access.
- Latest Package does not rely on `packages[0]`.
- Status and feedback are read-only.

Backend blockers remain:

- Package Accept endpoint.
- Package Reject endpoint.
- Feedback write contract.
- Package status machine.
- Workspace/Project completion behavior.
- `autoReleaseAt` semantics.
- Escrow/Kafka release ownership.

## Main goal

Produce a complete evidence-based closure report for Refactors 1–6 and establish the exact current build/lint baseline before Main Flow implementation begins.

The audit must answer:

1. Whether the architecture convention is now consistently followed.
2. Whether remaining violations affect the Main Flow.
3. Whether direct API, token, Supabase, or DTO duplication remains.
4. Whether misleading mock actions remain in the primary business flow.
5. Whether invalid identifiers can still trigger real requests.
6. Whether query keys and invalidations are safe enough for Main Flow work.
7. Whether current TypeScript, lint, and build failures are understood.
8. Which failures must be fixed in Refactor 7B.
9. Which warnings may remain as non-blocking debt.
10. Whether frontend architecture is ready to enter Main Flow after targeted baseline fixes.

## Strict scope

This is diagnosis only.

Do NOT:

- Fix `/experts`.
- Fix Calendar.
- Fix `configs/index.ts`.
- Fix `useUserCrud.tsx`.
- Fix lint errors.
- Replace Chat mocks.
- Replace Notification mocks.
- Add Accept/Reject.
- Add Payment Unlock.
- Add Hire/Contract.
- Add Workspace initialization.
- Add Completion.
- Add Escrow release.
- Change API response shapes.
- Change query invalidation.
- Change role logic.
- Change Supabase configuration.
- Change route structure.
- Delete dead code.
- Modify barrel exports.
- Change TypeScript configuration.
- Change ESLint configuration.
- Change Next.js configuration.

Only inspect and report.

## Required initial repository state

Before inspection:

1. Run `git status --short`.
2. Record whether the worktree is clean.
3. If tracked source changes exist, do not edit or reset them.
4. Report the exact paths and continue read-only only when the source can still be inspected safely.
5. Do not use `git checkout`, `git restore`, `git reset`, or `git clean`.

## Required configuration inspection

Inspect:

- `package.json`
- Lockfile
- `tsconfig.json`
- Next.js config
- ESLint config
- Path aliases
- React Query provider setup
- Redux provider setup
- Shared API client configuration
- `configs/supabase.ts`
- Current Next.js version
- Current React version
- Current `react-day-picker` version
- Current TypeScript version
- Current ESLint version

Determine:

- Exact build script.
- Exact lint script.
- Whether build invokes lint separately.
- Whether TypeScript incremental output may affect results.
- Whether `.next` validators are generated from a currently invalid route.
- Whether path aliases match current source layout.
- Whether known errors are caused by version mismatch, missing file, stale export, or invalid source.

Do not alter configuration.

## Audit area 1 — Route and layout convention

Inspect all application routes under:

`app/**`

Pay special attention to:

- `/experts`
- `/experts/[id]` or `/experts/[expertCode]`
- `/client/dashboard/my-job-post`
- `/client/dashboard/my-job-post/[id]`
- `/client/dashboard/create-job-post`
- `/chat-room/[workspaceId]`
- Auth routes
- Profile routes
- Available AI routes
- Client and Expert dashboard routes

Classify each route as:

- CLEAN THIN SHELL
- ACCEPTABLE ROUTE-SPECIFIC LOGIC
- FEATURE LOGIC LEAKED INTO APP
- INVALID ROUTE MODULE
- DEAD OR DUPLICATE ROUTE
- REQUIRES RUNTIME VERIFICATION

Check for:

- Large JSX trees in route files.
- Local API calls in route files.
- Local DTOs in route files.
- Mock data in route files.
- Business mutations in route files.
- Direct Supabase use in route files.
- Duplicate route implementations.
- Empty files.
- Files that export no default React component.
- Invalid route groups.
- Route links pointing to nonexistent routes.
- Dynamic parameter naming inconsistencies.
- Native Next.js page files that are not valid modules.

Do not refactor any route.

## Audit area 2 — Direct network and authorization usage

Search application source for:

- `fetch(`
- `axios`
- `.get(`
- `.post(`
- `.put(`
- `.patch(`
- `.delete(`
- `Authorization`
- `Bearer `
- `localStorage`
- `sessionStorage`
- `document.cookie`
- Manual token extraction
- Manual API base URLs
- Duplicate API client initialization
- Direct Workspace endpoint strings
- Direct Jobpost endpoint strings
- Direct Payment endpoint strings
- Direct Proposal endpoint strings
- Direct Notification endpoint strings

Ignore:

- Shared API-client implementation.
- Framework internals.
- `node_modules`.
- `.next`.
- Generated output.

For every direct request outside an approved service boundary, report:

- File.
- Operation.
- Endpoint.
- Authentication method.
- Whether it belongs under `services/**`.
- Whether it affects Main Flow.
- Severity.
- Recommended future action.

Distinguish:

- Legitimate external fetches.
- API service functions.
- Legacy direct backend calls.
- Mock-only code.
- Dead code.
- Browser asset requests.

Do not assume every occurrence of `.get` is HTTP.

## Audit area 3 — Supabase closure verification

Search for:

- `createClient(`
- `@supabase/supabase-js`
- `@/configs/supabase`
- `supabase.storage`
- `.storage.from(`
- `.upload(`
- `.getPublicUrl(`
- `.createSignedUrl(`
- `.remove(`
- `listBuckets`
- `service_role`
- Supabase key logging
- Session logging
- Token logging

Confirm:

1. `createClient` exists only in `configs/supabase.ts`.
2. Storage SDK calls exist only in `services/supabase-storage.service.ts`.
3. Submission uses the shared Storage boundary.
4. Avatar upload uses the shared Storage boundary.
5. No service-role key exists in browser source.
6. No credential/session/token logging exists.
7. No duplicate Supabase config remains.
8. No feature imports Supabase directly for Storage.

Report any exception.

Do not modify Supabase code.

## Audit area 4 — Service and type architecture

Inspect:

- `services/**`
- `types/**`
- Feature-local API helpers
- Local request/response interfaces
- Query-key declarations
- Mutation hooks
- Response unwrapping helpers
- Error normalization helpers

Search for:

- Duplicate API DTO definitions.
- Local interfaces matching centralized types.
- `any`.
- `unknown as`.
- Broad type assertions.
- Non-null assertions around API identifiers.
- Response types typed differently across consumers.
- Universal envelope assumptions.
- Direct object versus `{ code, message, result }` ambiguity.
- Hooks without enabled guards.
- Query keys missing identifiers.
- Mutation functions embedded in UI.
- API functions embedded in feature components.

For each issue classify:

- MAIN FLOW BLOCKER
- SHOULD FIX IN 7B
- SAFE TO DEFER
- INTENTIONAL FEATURE-LOCAL TYPE
- RUNTIME CONTRACT VERIFICATION NEEDED

Do not centralize types during this audit.

## Audit area 5 — Query keys and invalidation

Inspect actual current keys for:

- Jobpost.
- Applicants.
- Proposal Unlock.
- Workspace.
- Submission Packages.
- Deliverable Files.
- Payment Wallet.
- Payment Transactions.
- Notifications.
- Expert/Profile data.

Check:

- Whether query keys include required IDs.
- Whether optional IDs can create collisions.
- Whether hooks are disabled for empty IDs.
- Whether mutations invalidate only affected data.
- Whether broad root invalidation is used unnecessarily.
- Whether new Package versions invalidate old/new detail correctly.
- Whether selected Package Detail is isolated by package ID.
- Whether switching IDs can display stale data.
- Whether list and detail keys are structurally consistent.
- Whether manual query keys duplicate centralized factories.

Do not change keys.

Report exact current key structures when relevant.

## Audit area 6 — Identifier safety

Search for API calls or links using:

- `workspaceId`
- `packageId`
- `proposalId`
- `jobPostId`
- `expertCode`
- `fileId`
- `escrowId`
- `userId`
- Array indexes
- Placeholder IDs
- Empty strings
- `undefined`
- `null`

Identify:

- Hooks that may call `/undefined`.
- Links that may produce invalid routes.
- ID substitutions such as job ID used as Workspace ID.
- Package ID used as File ID.
- Array index used as domain identity.
- Missing enabled guards.
- IDs read from localStorage instead of route or real response.
- Mock IDs entering real services.

Separate:

- Confirmed safe.
- Potentially unsafe.
- Runtime-dependent.
- Already guarded by parent rendering.

## Audit area 7 — Main Flow mock and placeholder audit

Search primary Client–Expert flow for:

- `mock`
- `fake`
- `demo`
- `placeholder`
- `TODO`
- `FIXME`
- `setTimeout`
- Local action success
- Static status transitions
- Hardcoded Package/Proposal/Workspace data
- Hardcoded wallet values
- Fake Contract or Hire state
- Buttons that only mutate local state
- Buttons that navigate without a real backend action
- Toasts claiming success without a mutation

Inspect at minimum:

- Client Jobpost pages.
- Expert marketplace/apply flow.
- Applicants.
- Proposal detail/unlock.
- Hire.
- Contract.
- Workspace Chat Room.
- Submission.
- Client review.
- Payment/Wallet.
- Notification.
- Dispute.
- Completion.

Classify every relevant mock as:

- SAFE PRESENTATIONAL MOCK
- KNOWN TEMPORARY MOCK
- MISLEADING BUSINESS ACTION
- MAIN FLOW BLOCKER
- DEAD CODE
- OUT OF SCOPE

Pay special attention to:

- Chat messages.
- Notification data.
- Hire actions.
- Payment unlock.
- Client Submission actions.
- Completion actions.
- Wallet balances.
- Success toasts.

Do not remove mocks.

## Audit area 8 — Barrel exports and import health

Inspect:

- `features/**/index.ts`
- `services` barrels if any.
- `types` barrels if any.
- `configs/index.ts`
- Module aliases.
- Re-export chains.

Search for:

- Exports pointing to missing files.
- Imports from deleted modules.
- Circular-looking barrel chains.
- Route imports that depend on broken barrels.
- Duplicate export names.
- Default/named export mismatch.
- Case-sensitive path mismatch.
- Legacy `modules/**` imports.
- References to removed components.
- Dead barrels used only by invalid code.

Do not rewrite barrels.

Report whether direct imports are safer for each known failure.

## Audit area 9 — Known TypeScript and build errors

The following errors have repeatedly appeared and must be diagnosed precisely.

### Error A — Experts base page

Known symptom:

`app/(main)/experts/page.tsx` is not a module.

Inspect:

- Exact file content.
- Whether it is empty.
- Whether it has invalid commented-out content.
- Whether it exports a default component.
- Whether a valid Expert listing feature already exists elsewhere.
- Whether links currently target `/experts`.
- Whether repairing this page is required before Main Flow.
- Smallest safe future fix.

Do not implement the fix.

### Error B — Calendar / react-day-picker

Known symptom:

`components/ui/calendar.tsx` uses an invalid `table` property in DayPicker class names or related incompatible API.

Inspect:

- Installed `react-day-picker` version.
- Current Calendar component implementation.
- Correct class-name keys for the installed version.
- Whether the component was generated for another version.
- Every Calendar consumer.
- Whether a minimal compatibility fix is possible.
- Whether UI behavior may change.

Use installed package types and official local type declarations as evidence.

Do not use web search unless explicitly needed.

Do not upgrade `react-day-picker`.

Do not implement the fix.

### Error C — Broken config barrel import

Known symptom:

`configs/index.ts` references:

`@/modules/users/forms/UserForm`

Inspect:

- Exact import/export.
- Whether the referenced file exists.
- Whether the path is stale.
- Whether the export is used anywhere.
- Whether `modules/users/**` is legacy architecture.
- Whether removing the export or correcting the path is safer.
- Whether another `UserForm` exists.
- Main Flow impact.

Do not change the file.

### Error D — Missing UserSchema

Known symptom:

`modules/users/useUserCrud.tsx` references missing `UserSchema`.

Inspect:

- Current imports.
- Existing user schemas.
- Whether this module is used.
- Whether it belongs to legacy Admin CRUD.
- Whether the missing symbol was renamed.
- Whether the module can be excluded or must compile.
- Smallest safe future fix.
- Main Flow relevance.

Do not fix it.

### Error E — Expert registration `any`

Known lint symptom:

`features/auth/register/ExpertRegisterForm.tsx` contains unexpected `any`, previously reported around line 88.

Inspect:

- Exact variable or callback.
- Correct available type.
- Whether a local type exists.
- Whether the fix changes runtime behavior.
- Whether it is safe for 7B.

Do not fix it.

### Error F — `use-mobile` state update in effect

Known lint symptom:

`hooks/use-mobile.ts` calls state setter in an effect in a way rejected by current lint rules.

Inspect:

- Exact hook implementation.
- Current lint rule.
- Consumer count.
- Whether it causes runtime hydration issues.
- Whether the common `matchMedia` lazy-state pattern can fix it.
- Whether it affects Main Flow.
- Smallest safe future fix.

Do not implement it.

## Audit area 10 — Warning baseline

Record lint warnings grouped by rule and path.

Pay attention to:

- `<img>` warnings.
- Missing hook dependencies.
- Unused imports.
- Explicit `any`.
- Accessibility.
- React Compiler-related warnings.
- Next.js image optimization.
- Deprecated APIs.
- Console statements.

Classify warnings:

- MUST FIX BEFORE MAIN FLOW
- SHOULD FIX IN 7B
- SAFE TO DEFER
- FALSE POSITIVE OR CONVENTION DECISION

Do not attempt to remove all warnings.

The goal is a stable baseline, not warning-zero at any cost.

## Audit area 11 — Main Flow backend blockers

Consolidate currently known backend blockers.

At minimum inspect frontend evidence for:

### Proposal purchase/unlock

- Missing atomic purchase/unlock endpoint.
- Protected Strategy Detail exposure.
- Wallet insufficiency response.
- Already-unlocked behavior.
- Required invalidations.

### Hire and Contract

- Negotiated amount request field.
- Create Contract/Escrow.
- Finalize after signatures/funding.
- Retry behavior.
- Idempotency.
- Workspace creation trigger.

### Workspace Proposal

- Proposal creation or association endpoint.
- Proposal source of truth after Hire.

### Submission review

- Package Reject.
- Feedback body.
- Package Accept.
- Status transitions.
- Completion behavior.
- Escrow release event.
- `autoReleaseAt`.

### Notification and Dispute

- Real notification contract.
- Dispute transitions.
- Completion/release interactions.

For each blocker classify:

- FRONTEND READY
- FRONTEND PREPARATION POSSIBLE
- BACKEND CONTRACT REQUIRED
- BACKEND SECURITY FIX REQUIRED
- RUNTIME VERIFICATION REQUIRED

Do not design speculative frontend mutations.

## Required commands

Run these commands and capture exact results.

1. `git status --short`
2. `npx tsc --noEmit --incremental false --pretty false`
3. `npm run lint -- --no-cache`
4. `npm run build`
5. `git diff --check`
6. `git status --short`

If the build command fails:

- Record the first root-cause error.
- Continue source inspection.
- Do not repeatedly rerun the same failing command without new evidence.
- Do not edit files.

If `.next` generated output affects TypeScript error reporting:

- Explain the relationship.
- Do not manually edit `.next`.
- Do not delete tracked source.
- You may inspect generated validator paths read-only.

Do not use destructive Git commands.

## Build baseline classification

For every TypeScript, lint, or build failure, report:

- Command.
- File.
- Line.
- Error code or lint rule.
- Root cause.
- Whether it is pre-existing.
- Whether it was introduced by Refactors 1–6.
- Main Flow impact.
- Minimal future fix.
- Expected files for 7B.
- Risk of fixing.

Classify each failure:

- BASELINE BLOCKER
- MAIN FLOW BLOCKER
- ROUTE-SPECIFIC BLOCKER
- LINT-ONLY BLOCKER
- NON-BLOCKING WARNING
- GENERATED CONSEQUENCE OF SOURCE ERROR

Do not merely repeat compiler output.

## Refactor regression check

Verify Refactors 1–6 did not introduce obvious architecture regressions.

Confirm or refute:

1. Public Expert Profile route remains thin.
2. Applicant link still uses `expertCode`.
3. Proposal Detail remains lazy by `proposalId`.
4. Workspace services use shared API client.
5. Submission reads/writes use service hooks.
6. Chat Room route remains thin.
7. Submission upload uses feature hook and shared Storage service.
8. Avatar uses shared Storage service.
9. Client Package panel uses real reads.
10. Fake Client review actions remain removed.
11. Client panel is gated by real Workspace success.
12. No direct Storage SDK call returned to feature code.
13. No new manual Authorization/token logic was introduced.
14. No unsupported Accept/Reject endpoint was invented.

## Required readiness matrix

Produce a readiness matrix for:

- Public Expert Profile.
- Expert listing.
- Client Jobpost list/detail.
- Expert marketplace.
- Apply Jobpost.
- Applicant list.
- Proposal summary.
- Proposal protected detail.
- Proposal payment/unlock.
- Hire.
- Contract.
- Workspace reads.
- Workspace members.
- Workspace Chat shell.
- Expert Package create.
- Expert Package resubmit.
- Client Package history.
- Client Package detail/files.
- Client Reject.
- Client Accept.
- Completion.
- Wallet/Transactions.
- Notification.
- Dispute.
- Repository build baseline.

Use only:

- READY
- READY AFTER 7B BASELINE FIX
- READY FOR READS ONLY
- FRONTEND FOUNDATION READY
- BLOCKED BY BACKEND CONTRACT
- BLOCKED BY BACKEND SECURITY
- MOCK ONLY
- RUNTIME VERIFICATION REQUIRED
- NOT AUDITED

## Refactor 7B planning requirements

Based on evidence, propose a minimal 7B fix set.

The plan must:

- Fix only build/lint baseline blockers.
- Avoid broad architecture refactors.
- Avoid business-flow implementation.
- Avoid dependency upgrades unless absolutely unavoidable.
- Keep each fix independently reviewable.
- Rank fixes by dependency and risk.
- State exact expected files.
- State whether each fix requires manual UI regression testing.
- State whether each fix can be committed independently.

A likely 7B plan may include:

- Repair invalid `/experts` route module.
- Repair Calendar compatibility with installed DayPicker.
- Remove or correct stale `configs/index.ts` export.
- Repair or isolate legacy `useUserCrud.tsx`.
- Replace explicit `any` in Expert registration.
- Repair `use-mobile` lint pattern.

Do not assume all six should be fixed together.

Use source evidence to decide.

## No-write verification

At the end:

1. Run `git diff --check`.
2. Run `git status --short`.
3. Confirm no tracked application source file changed.
4. Report generated ignored artifacts separately when relevant.
5. Do not clean or reset the repository.

## Required final output

Return a detailed Markdown report with these exact sections.

# 1. Executive Summary

State:

- Whether Refactors 1–6 are structurally closed.
- Whether major architecture boundaries are now consistent.
- Whether the repository currently type-checks.
- Whether lint passes.
- Whether production build passes.
- Whether Main Flow implementation may start before 7B.
- Highest-priority blockers.

# 2. Repository and Tooling Baseline

Report:

- Framework and package versions.
- Build/lint/typecheck scripts.
- Initial Git status.
- Final Git status.
- Generated-output considerations.

# 3. Refactor 1–6 Regression Check

Report every required regression check as PASS, FAIL, or RUNTIME UNVERIFIED.

# 4. Route Architecture Audit

Provide a table with:

- Route.
- Route file.
- Classification.
- Main issue.
- Main Flow impact.
- Recommended action.

# 5. Direct Network and Auth Audit

List every direct or suspicious API/auth occurrence outside approved boundaries.

# 6. Supabase Closure Audit

Report:

- Client initialization count.
- Direct Storage SDK call locations.
- Submission consumer path.
- Avatar consumer path.
- Credential/logging findings.
- Remaining runtime/security limitations.

# 7. Service and Type Architecture Audit

Report:

- Duplicate DTOs.
- Local API types.
- Unsafe assertions.
- Response-envelope inconsistencies.
- Hook enabled guards.
- Service-boundary violations.

# 8. Query Key and Invalidation Audit

Report actual strengths, risks, collisions, and invalidation concerns.

# 9. Identifier Safety Audit

Report confirmed safe and unsafe identifier flows.

# 10. Main Flow Mock and Placeholder Audit

Provide a table with:

- Feature.
- File.
- Mock or placeholder.
- User-visible behavior.
- Risk classification.
- Backend dependency.
- Recommended action.

# 11. Barrel Export and Import Health

Report stale exports, missing modules, possible cycles, and direct-import recommendations.

# 12. Build and TypeScript Baseline

For each error provide:

- Command.
- File.
- Error.
- Root cause.
- Classification.
- Main Flow impact.
- Minimal 7B fix.

# 13. Lint Baseline

Group errors and warnings by rule and severity.

# 14. Known Error Deep Diagnosis

Create subsections for:

- Experts base page.
- Calendar and DayPicker.
- `configs/index.ts`.
- `useUserCrud.tsx`.
- Expert registration `any`.
- `use-mobile`.

# 15. Backend Contract and Security Blockers

Group by:

- Proposal Unlock.
- Hire/Contract.
- Workspace Proposal.
- Submission Review.
- Completion/Release.
- Notification/Dispute.

# 16. Frontend Readiness Matrix

Use the required readiness classifications.

# 17. Main Flow Entry Assessment

Explain:

- Which Main Flow phase could technically start.
- Which phase should start first.
- Which phases are blocked.
- Whether starting before build baseline cleanup is advisable.

# 18. Recommended Refactor 7B Fix Set

Provide ordered, minimal, independently reviewable tasks.

For each task include:

- Goal.
- Exact files.
- Expected change.
- Risk.
- Verification.
- Suggested commit boundary.

# 19. Files Expected to Change in 7B

List exact paths grouped by task.

# 20. Manual Regression Plan

Include tests for:

- `/experts`.
- Public Expert Profile.
- Calendar consumers.
- Client/Expert registration.
- Client Jobpost routes.
- Workspace Chat Room.
- Expert Submission.
- Client Package reads.

# 21. Verification Results

Report exact results for:

- TypeScript.
- Lint.
- Build.
- Diff check.
- Git status.
- Confirmation that no source files changed.

# 22. Final Decision

Return exactly one:

- GO FOR REFACTOR 7B TARGETED BASELINE FIXES
- GO FOR MAIN FLOW
- NO-GO UNTIL REPOSITORY STRUCTURE IS REPAIRED

Explain the decision precisely.

Important:

- This is read-only diagnosis.
- Do not fix any source file.
- Do not implement Main Flow.
- Do not invent backend endpoints.
- Distinguish source-confirmed facts from inference.
- Separate architecture readiness from business-contract readiness.
- Preserve a clean Git worktree.
- You are working in my existing Next.js web application: Neuro Bridge.

This is Refactor 7A: Architecture Closure Audit and Build Baseline Diagnosis only.

This task is strictly read-only for application source files.

Do NOT modify application source files.
Do NOT create application source files.
Do NOT delete, rename, or move application source files.
Do NOT run formatter or auto-fix commands.
Do NOT run ESLint with `--fix`.
Do NOT change package versions.
Do NOT install packages.
Do NOT upgrade dependencies.
Do NOT edit generated route types manually.
Do NOT implement Main Flow business features.
Do NOT implement missing backend endpoints.
Do NOT replace remaining mocks during this audit.
Do NOT fix build or lint errors yet.

You may run build, TypeScript, lint, and read-only search commands.

Generated build output such as `.next/**` may be created by normal Next.js commands, but no tracked application source file may change.

Confirm `git status --short` before and after the audit.

## Current project

Application:

- Neuro Bridge frontend
- Next.js App Router
- TypeScript
- Tailwind
- shadcn-style UI
- React Query
- Redux
- Supabase browser Storage

Project architecture convention:

- `app/**`
  - Thin route and layout shells only
  - Route parameters
  - Render feature views

- `features/**`
  - Feature views
  - Components
  - Local UI state
  - Feature orchestration
  - Feature-specific hooks and mappings
  - Temporary mock data only when explicitly documented

- `services/**`
  - Backend API functions
  - React Query hooks
  - Query keys
  - Mutations
  - External integration boundaries

- `types/**`
  - Domain types
  - Request types
  - Response types
  - Integration contracts

- `configs/**`
  - Initialized shared clients
  - Environment configuration

## Completed refactors

The following architecture work is complete and must not be reopened without concrete evidence.

### Refactor 1 — Public Expert Profile repair

Completed:

- `/experts/[expertCode]` uses a thin route shell.
- Public profile view lives under `features/profile/expert/**`.
- Real Expert and Skill reads are used.
- Invalid code and empty-skill states were handled.
- Broken imports and local type assumptions were repaired.

Known remaining issue:

- The base `/experts` page has previously been reported as not being a valid module.

### Refactor 2 — Applicant to Expert Profile

Completed:

- Client applicant card links to the public Expert profile.
- It uses `applicant.expertApply.expertCode`.
- Missing codes do not create invalid URLs.
- Proposal Unlock remains independent.

### Proposal protected-detail preparation

Completed:

- Proposal Detail is fetched lazily by `proposalId`.
- The dialog no longer uses protected Strategy Detail from the applicant-list response.
- The frontend ignores premature `strategyDetail` fields.

Backend/security concern still open:

- Backend must not expose protected `strategyDetail` before unlock.

### Refactor 3 — Workspace foundation

Completed:

- Centralized Workspace types.
- Shared API client usage.
- Workspace Detail and Members reads.
- Stable Workspace query keys.
- No manual Authorization header in the new Workspace service.

### Refactor 4 — Submission Package foundation

Completed:

- Package list by Workspace.
- Package Detail.
- Deliverable Files by Package.
- File Types.
- Create Submission Package.
- Resubmit Submission Package.
- Scoped query invalidation.
- Chat Room feature extraction.
- Thin Chat Room route shell.

### Refactor 5 — Supabase upload consolidation

Completed:

- One canonical Supabase client in `configs/supabase.ts`.
- One direct Storage SDK boundary in `services/supabase-storage.service.ts`.
- Submission upload feature hook.
- Submission upload utilities.
- Normalized Storage errors.
- Avatar helper migrated to the shared Storage boundary.
- Unused `uploadFiles` removed.
- No direct Supabase Storage SDK call should remain outside the generic service.

Runtime Storage testing and policy verification remain deferred.

### Refactor 6 — Client Submission Review preparation

Completed:

- 6A API and frontend audit.
- 6B real Client Package read integration.
- Mock Client Package history removed.
- Mock Deliverable Files removed.
- Fake Approve/Reject behavior removed.
- Real Package list and selected Package Detail are used.
- Client panel is gated by successful Workspace access.
- Latest Package does not rely on `packages[0]`.
- Status and feedback are read-only.

Backend blockers remain:

- Package Accept endpoint.
- Package Reject endpoint.
- Feedback write contract.
- Package status machine.
- Workspace/Project completion behavior.
- `autoReleaseAt` semantics.
- Escrow/Kafka release ownership.

## Main goal

Produce a complete evidence-based closure report for Refactors 1–6 and establish the exact current build/lint baseline before Main Flow implementation begins.

The audit must answer:

1. Whether the architecture convention is now consistently followed.
2. Whether remaining violations affect the Main Flow.
3. Whether direct API, token, Supabase, or DTO duplication remains.
4. Whether misleading mock actions remain in the primary business flow.
5. Whether invalid identifiers can still trigger real requests.
6. Whether query keys and invalidations are safe enough for Main Flow work.
7. Whether current TypeScript, lint, and build failures are understood.
8. Which failures must be fixed in Refactor 7B.
9. Which warnings may remain as non-blocking debt.
10. Whether frontend architecture is ready to enter Main Flow after targeted baseline fixes.

## Strict scope

This is diagnosis only.

Do NOT:

- Fix `/experts`.
- Fix Calendar.
- Fix `configs/index.ts`.
- Fix `useUserCrud.tsx`.
- Fix lint errors.
- Replace Chat mocks.
- Replace Notification mocks.
- Add Accept/Reject.
- Add Payment Unlock.
- Add Hire/Contract.
- Add Workspace initialization.
- Add Completion.
- Add Escrow release.
- Change API response shapes.
- Change query invalidation.
- Change role logic.
- Change Supabase configuration.
- Change route structure.
- Delete dead code.
- Modify barrel exports.
- Change TypeScript configuration.
- Change ESLint configuration.
- Change Next.js configuration.

Only inspect and report.

## Required initial repository state

Before inspection:

1. Run `git status --short`.
2. Record whether the worktree is clean.
3. If tracked source changes exist, do not edit or reset them.
4. Report the exact paths and continue read-only only when the source can still be inspected safely.
5. Do not use `git checkout`, `git restore`, `git reset`, or `git clean`.

## Required configuration inspection

Inspect:

- `package.json`
- Lockfile
- `tsconfig.json`
- Next.js config
- ESLint config
- Path aliases
- React Query provider setup
- Redux provider setup
- Shared API client configuration
- `configs/supabase.ts`
- Current Next.js version
- Current React version
- Current `react-day-picker` version
- Current TypeScript version
- Current ESLint version

Determine:

- Exact build script.
- Exact lint script.
- Whether build invokes lint separately.
- Whether TypeScript incremental output may affect results.
- Whether `.next` validators are generated from a currently invalid route.
- Whether path aliases match current source layout.
- Whether known errors are caused by version mismatch, missing file, stale export, or invalid source.

Do not alter configuration.

## Audit area 1 — Route and layout convention

Inspect all application routes under:

`app/**`

Pay special attention to:

- `/experts`
- `/experts/[id]` or `/experts/[expertCode]`
- `/client/dashboard/my-job-post`
- `/client/dashboard/my-job-post/[id]`
- `/client/dashboard/create-job-post`
- `/chat-room/[workspaceId]`
- Auth routes
- Profile routes
- Available AI routes
- Client and Expert dashboard routes

Classify each route as:

- CLEAN THIN SHELL
- ACCEPTABLE ROUTE-SPECIFIC LOGIC
- FEATURE LOGIC LEAKED INTO APP
- INVALID ROUTE MODULE
- DEAD OR DUPLICATE ROUTE
- REQUIRES RUNTIME VERIFICATION

Check for:

- Large JSX trees in route files.
- Local API calls in route files.
- Local DTOs in route files.
- Mock data in route files.
- Business mutations in route files.
- Direct Supabase use in route files.
- Duplicate route implementations.
- Empty files.
- Files that export no default React component.
- Invalid route groups.
- Route links pointing to nonexistent routes.
- Dynamic parameter naming inconsistencies.
- Native Next.js page files that are not valid modules.

Do not refactor any route.

## Audit area 2 — Direct network and authorization usage

Search application source for:

- `fetch(`
- `axios`
- `.get(`
- `.post(`
- `.put(`
- `.patch(`
- `.delete(`
- `Authorization`
- `Bearer `
- `localStorage`
- `sessionStorage`
- `document.cookie`
- Manual token extraction
- Manual API base URLs
- Duplicate API client initialization
- Direct Workspace endpoint strings
- Direct Jobpost endpoint strings
- Direct Payment endpoint strings
- Direct Proposal endpoint strings
- Direct Notification endpoint strings

Ignore:

- Shared API-client implementation.
- Framework internals.
- `node_modules`.
- `.next`.
- Generated output.

For every direct request outside an approved service boundary, report:

- File.
- Operation.
- Endpoint.
- Authentication method.
- Whether it belongs under `services/**`.
- Whether it affects Main Flow.
- Severity.
- Recommended future action.

Distinguish:

- Legitimate external fetches.
- API service functions.
- Legacy direct backend calls.
- Mock-only code.
- Dead code.
- Browser asset requests.

Do not assume every occurrence of `.get` is HTTP.

## Audit area 3 — Supabase closure verification

Search for:

- `createClient(`
- `@supabase/supabase-js`
- `@/configs/supabase`
- `supabase.storage`
- `.storage.from(`
- `.upload(`
- `.getPublicUrl(`
- `.createSignedUrl(`
- `.remove(`
- `listBuckets`
- `service_role`
- Supabase key logging
- Session logging
- Token logging

Confirm:

1. `createClient` exists only in `configs/supabase.ts`.
2. Storage SDK calls exist only in `services/supabase-storage.service.ts`.
3. Submission uses the shared Storage boundary.
4. Avatar upload uses the shared Storage boundary.
5. No service-role key exists in browser source.
6. No credential/session/token logging exists.
7. No duplicate Supabase config remains.
8. No feature imports Supabase directly for Storage.

Report any exception.

Do not modify Supabase code.

## Audit area 4 — Service and type architecture

Inspect:

- `services/**`
- `types/**`
- Feature-local API helpers
- Local request/response interfaces
- Query-key declarations
- Mutation hooks
- Response unwrapping helpers
- Error normalization helpers

Search for:

- Duplicate API DTO definitions.
- Local interfaces matching centralized types.
- `any`.
- `unknown as`.
- Broad type assertions.
- Non-null assertions around API identifiers.
- Response types typed differently across consumers.
- Universal envelope assumptions.
- Direct object versus `{ code, message, result }` ambiguity.
- Hooks without enabled guards.
- Query keys missing identifiers.
- Mutation functions embedded in UI.
- API functions embedded in feature components.

For each issue classify:

- MAIN FLOW BLOCKER
- SHOULD FIX IN 7B
- SAFE TO DEFER
- INTENTIONAL FEATURE-LOCAL TYPE
- RUNTIME CONTRACT VERIFICATION NEEDED

Do not centralize types during this audit.

## Audit area 5 — Query keys and invalidation

Inspect actual current keys for:

- Jobpost.
- Applicants.
- Proposal Unlock.
- Workspace.
- Submission Packages.
- Deliverable Files.
- Payment Wallet.
- Payment Transactions.
- Notifications.
- Expert/Profile data.

Check:

- Whether query keys include required IDs.
- Whether optional IDs can create collisions.
- Whether hooks are disabled for empty IDs.
- Whether mutations invalidate only affected data.
- Whether broad root invalidation is used unnecessarily.
- Whether new Package versions invalidate old/new detail correctly.
- Whether selected Package Detail is isolated by package ID.
- Whether switching IDs can display stale data.
- Whether list and detail keys are structurally consistent.
- Whether manual query keys duplicate centralized factories.

Do not change keys.

Report exact current key structures when relevant.

## Audit area 6 — Identifier safety

Search for API calls or links using:

- `workspaceId`
- `packageId`
- `proposalId`
- `jobPostId`
- `expertCode`
- `fileId`
- `escrowId`
- `userId`
- Array indexes
- Placeholder IDs
- Empty strings
- `undefined`
- `null`

Identify:

- Hooks that may call `/undefined`.
- Links that may produce invalid routes.
- ID substitutions such as job ID used as Workspace ID.
- Package ID used as File ID.
- Array index used as domain identity.
- Missing enabled guards.
- IDs read from localStorage instead of route or real response.
- Mock IDs entering real services.

Separate:

- Confirmed safe.
- Potentially unsafe.
- Runtime-dependent.
- Already guarded by parent rendering.

## Audit area 7 — Main Flow mock and placeholder audit

Search primary Client–Expert flow for:

- `mock`
- `fake`
- `demo`
- `placeholder`
- `TODO`
- `FIXME`
- `setTimeout`
- Local action success
- Static status transitions
- Hardcoded Package/Proposal/Workspace data
- Hardcoded wallet values
- Fake Contract or Hire state
- Buttons that only mutate local state
- Buttons that navigate without a real backend action
- Toasts claiming success without a mutation

Inspect at minimum:

- Client Jobpost pages.
- Expert marketplace/apply flow.
- Applicants.
- Proposal detail/unlock.
- Hire.
- Contract.
- Workspace Chat Room.
- Submission.
- Client review.
- Payment/Wallet.
- Notification.
- Dispute.
- Completion.

Classify every relevant mock as:

- SAFE PRESENTATIONAL MOCK
- KNOWN TEMPORARY MOCK
- MISLEADING BUSINESS ACTION
- MAIN FLOW BLOCKER
- DEAD CODE
- OUT OF SCOPE

Pay special attention to:

- Chat messages.
- Notification data.
- Hire actions.
- Payment unlock.
- Client Submission actions.
- Completion actions.
- Wallet balances.
- Success toasts.

Do not remove mocks.

## Audit area 8 — Barrel exports and import health

Inspect:

- `features/**/index.ts`
- `services` barrels if any.
- `types` barrels if any.
- `configs/index.ts`
- Module aliases.
- Re-export chains.

Search for:

- Exports pointing to missing files.
- Imports from deleted modules.
- Circular-looking barrel chains.
- Route imports that depend on broken barrels.
- Duplicate export names.
- Default/named export mismatch.
- Case-sensitive path mismatch.
- Legacy `modules/**` imports.
- References to removed components.
- Dead barrels used only by invalid code.

Do not rewrite barrels.

Report whether direct imports are safer for each known failure.

## Audit area 9 — Known TypeScript and build errors

The following errors have repeatedly appeared and must be diagnosed precisely.

### Error A — Experts base page

Known symptom:

`app/(main)/experts/page.tsx` is not a module.

Inspect:

- Exact file content.
- Whether it is empty.
- Whether it has invalid commented-out content.
- Whether it exports a default component.
- Whether a valid Expert listing feature already exists elsewhere.
- Whether links currently target `/experts`.
- Whether repairing this page is required before Main Flow.
- Smallest safe future fix.

Do not implement the fix.

### Error B — Calendar / react-day-picker

Known symptom:

`components/ui/calendar.tsx` uses an invalid `table` property in DayPicker class names or related incompatible API.

Inspect:

- Installed `react-day-picker` version.
- Current Calendar component implementation.
- Correct class-name keys for the installed version.
- Whether the component was generated for another version.
- Every Calendar consumer.
- Whether a minimal compatibility fix is possible.
- Whether UI behavior may change.

Use installed package types and official local type declarations as evidence.

Do not use web search unless explicitly needed.

Do not upgrade `react-day-picker`.

Do not implement the fix.

### Error C — Broken config barrel import

Known symptom:

`configs/index.ts` references:

`@/modules/users/forms/UserForm`

Inspect:

- Exact import/export.
- Whether the referenced file exists.
- Whether the path is stale.
- Whether the export is used anywhere.
- Whether `modules/users/**` is legacy architecture.
- Whether removing the export or correcting the path is safer.
- Whether another `UserForm` exists.
- Main Flow impact.

Do not change the file.

### Error D — Missing UserSchema

Known symptom:

`modules/users/useUserCrud.tsx` references missing `UserSchema`.

Inspect:

- Current imports.
- Existing user schemas.
- Whether this module is used.
- Whether it belongs to legacy Admin CRUD.
- Whether the missing symbol was renamed.
- Whether the module can be excluded or must compile.
- Smallest safe future fix.
- Main Flow relevance.

Do not fix it.

### Error E — Expert registration `any`

Known lint symptom:

`features/auth/register/ExpertRegisterForm.tsx` contains unexpected `any`, previously reported around line 88.

Inspect:

- Exact variable or callback.
- Correct available type.
- Whether a local type exists.
- Whether the fix changes runtime behavior.
- Whether it is safe for 7B.

Do not fix it.

### Error F — `use-mobile` state update in effect

Known lint symptom:

`hooks/use-mobile.ts` calls state setter in an effect in a way rejected by current lint rules.

Inspect:

- Exact hook implementation.
- Current lint rule.
- Consumer count.
- Whether it causes runtime hydration issues.
- Whether the common `matchMedia` lazy-state pattern can fix it.
- Whether it affects Main Flow.
- Smallest safe future fix.

Do not implement it.

## Audit area 10 — Warning baseline

Record lint warnings grouped by rule and path.

Pay attention to:

- `<img>` warnings.
- Missing hook dependencies.
- Unused imports.
- Explicit `any`.
- Accessibility.
- React Compiler-related warnings.
- Next.js image optimization.
- Deprecated APIs.
- Console statements.

Classify warnings:

- MUST FIX BEFORE MAIN FLOW
- SHOULD FIX IN 7B
- SAFE TO DEFER
- FALSE POSITIVE OR CONVENTION DECISION

Do not attempt to remove all warnings.

The goal is a stable baseline, not warning-zero at any cost.

## Audit area 11 — Main Flow backend blockers

Consolidate currently known backend blockers.

At minimum inspect frontend evidence for:

### Proposal purchase/unlock

- Missing atomic purchase/unlock endpoint.
- Protected Strategy Detail exposure.
- Wallet insufficiency response.
- Already-unlocked behavior.
- Required invalidations.

### Hire and Contract

- Negotiated amount request field.
- Create Contract/Escrow.
- Finalize after signatures/funding.
- Retry behavior.
- Idempotency.
- Workspace creation trigger.

### Workspace Proposal

- Proposal creation or association endpoint.
- Proposal source of truth after Hire.

### Submission review

- Package Reject.
- Feedback body.
- Package Accept.
- Status transitions.
- Completion behavior.
- Escrow release event.
- `autoReleaseAt`.

### Notification and Dispute

- Real notification contract.
- Dispute transitions.
- Completion/release interactions.

For each blocker classify:

- FRONTEND READY
- FRONTEND PREPARATION POSSIBLE
- BACKEND CONTRACT REQUIRED
- BACKEND SECURITY FIX REQUIRED
- RUNTIME VERIFICATION REQUIRED

Do not design speculative frontend mutations.

## Required commands

Run these commands and capture exact results.

1. `git status --short`
2. `npx tsc --noEmit --incremental false --pretty false`
3. `npm run lint -- --no-cache`
4. `npm run build`
5. `git diff --check`
6. `git status --short`

If the build command fails:

- Record the first root-cause error.
- Continue source inspection.
- Do not repeatedly rerun the same failing command without new evidence.
- Do not edit files.

If `.next` generated output affects TypeScript error reporting:

- Explain the relationship.
- Do not manually edit `.next`.
- Do not delete tracked source.
- You may inspect generated validator paths read-only.

Do not use destructive Git commands.

## Build baseline classification

For every TypeScript, lint, or build failure, report:

- Command.
- File.
- Line.
- Error code or lint rule.
- Root cause.
- Whether it is pre-existing.
- Whether it was introduced by Refactors 1–6.
- Main Flow impact.
- Minimal future fix.
- Expected files for 7B.
- Risk of fixing.

Classify each failure:

- BASELINE BLOCKER
- MAIN FLOW BLOCKER
- ROUTE-SPECIFIC BLOCKER
- LINT-ONLY BLOCKER
- NON-BLOCKING WARNING
- GENERATED CONSEQUENCE OF SOURCE ERROR

Do not merely repeat compiler output.

## Refactor regression check

Verify Refactors 1–6 did not introduce obvious architecture regressions.

Confirm or refute:

1. Public Expert Profile route remains thin.
2. Applicant link still uses `expertCode`.
3. Proposal Detail remains lazy by `proposalId`.
4. Workspace services use shared API client.
5. Submission reads/writes use service hooks.
6. Chat Room route remains thin.
7. Submission upload uses feature hook and shared Storage service.
8. Avatar uses shared Storage service.
9. Client Package panel uses real reads.
10. Fake Client review actions remain removed.
11. Client panel is gated by real Workspace success.
12. No direct Storage SDK call returned to feature code.
13. No new manual Authorization/token logic was introduced.
14. No unsupported Accept/Reject endpoint was invented.

## Required readiness matrix

Produce a readiness matrix for:

- Public Expert Profile.
- Expert listing.
- Client Jobpost list/detail.
- Expert marketplace.
- Apply Jobpost.
- Applicant list.
- Proposal summary.
- Proposal protected detail.
- Proposal payment/unlock.
- Hire.
- Contract.
- Workspace reads.
- Workspace members.
- Workspace Chat shell.
- Expert Package create.
- Expert Package resubmit.
- Client Package history.
- Client Package detail/files.
- Client Reject.
- Client Accept.
- Completion.
- Wallet/Transactions.
- Notification.
- Dispute.
- Repository build baseline.

Use only:

- READY
- READY AFTER 7B BASELINE FIX
- READY FOR READS ONLY
- FRONTEND FOUNDATION READY
- BLOCKED BY BACKEND CONTRACT
- BLOCKED BY BACKEND SECURITY
- MOCK ONLY
- RUNTIME VERIFICATION REQUIRED
- NOT AUDITED

## Refactor 7B planning requirements

Based on evidence, propose a minimal 7B fix set.

The plan must:

- Fix only build/lint baseline blockers.
- Avoid broad architecture refactors.
- Avoid business-flow implementation.
- Avoid dependency upgrades unless absolutely unavoidable.
- Keep each fix independently reviewable.
- Rank fixes by dependency and risk.
- State exact expected files.
- State whether each fix requires manual UI regression testing.
- State whether each fix can be committed independently.

A likely 7B plan may include:

- Repair invalid `/experts` route module.
- Repair Calendar compatibility with installed DayPicker.
- Remove or correct stale `configs/index.ts` export.
- Repair or isolate legacy `useUserCrud.tsx`.
- Replace explicit `any` in Expert registration.
- Repair `use-mobile` lint pattern.

Do not assume all six should be fixed together.

Use source evidence to decide.

## No-write verification

At the end:

1. Run `git diff --check`.
2. Run `git status --short`.
3. Confirm no tracked application source file changed.
4. Report generated ignored artifacts separately when relevant.
5. Do not clean or reset the repository.

## Required final output

Return a detailed Markdown report with these exact sections.

# 1. Executive Summary

State:

- Whether Refactors 1–6 are structurally closed.
- Whether major architecture boundaries are now consistent.
- Whether the repository currently type-checks.
- Whether lint passes.
- Whether production build passes.
- Whether Main Flow implementation may start before 7B.
- Highest-priority blockers.

# 2. Repository and Tooling Baseline

Report:

- Framework and package versions.
- Build/lint/typecheck scripts.
- Initial Git status.
- Final Git status.
- Generated-output considerations.

# 3. Refactor 1–6 Regression Check

Report every required regression check as PASS, FAIL, or RUNTIME UNVERIFIED.

# 4. Route Architecture Audit

Provide a table with:

- Route.
- Route file.
- Classification.
- Main issue.
- Main Flow impact.
- Recommended action.

# 5. Direct Network and Auth Audit

List every direct or suspicious API/auth occurrence outside approved boundaries.

# 6. Supabase Closure Audit

Report:

- Client initialization count.
- Direct Storage SDK call locations.
- Submission consumer path.
- Avatar consumer path.
- Credential/logging findings.
- Remaining runtime/security limitations.

# 7. Service and Type Architecture Audit

Report:

- Duplicate DTOs.
- Local API types.
- Unsafe assertions.
- Response-envelope inconsistencies.
- Hook enabled guards.
- Service-boundary violations.

# 8. Query Key and Invalidation Audit

Report actual strengths, risks, collisions, and invalidation concerns.

# 9. Identifier Safety Audit

Report confirmed safe and unsafe identifier flows.

# 10. Main Flow Mock and Placeholder Audit

Provide a table with:

- Feature.
- File.
- Mock or placeholder.
- User-visible behavior.
- Risk classification.
- Backend dependency.
- Recommended action.

# 11. Barrel Export and Import Health

Report stale exports, missing modules, possible cycles, and direct-import recommendations.

# 12. Build and TypeScript Baseline

For each error provide:

- Command.
- File.
- Error.
- Root cause.
- Classification.
- Main Flow impact.
- Minimal 7B fix.

# 13. Lint Baseline

Group errors and warnings by rule and severity.

# 14. Known Error Deep Diagnosis

Create subsections for:

- Experts base page.
- Calendar and DayPicker.
- `configs/index.ts`.
- `useUserCrud.tsx`.
- Expert registration `any`.
- `use-mobile`.

# 15. Backend Contract and Security Blockers

Group by:

- Proposal Unlock.
- Hire/Contract.
- Workspace Proposal.
- Submission Review.
- Completion/Release.
- Notification/Dispute.

# 16. Frontend Readiness Matrix

Use the required readiness classifications.

# 17. Main Flow Entry Assessment

Explain:

- Which Main Flow phase could technically start.
- Which phase should start first.
- Which phases are blocked.
- Whether starting before build baseline cleanup is advisable.

# 18. Recommended Refactor 7B Fix Set

Provide ordered, minimal, independently reviewable tasks.

For each task include:

- Goal.
- Exact files.
- Expected change.
- Risk.
- Verification.
- Suggested commit boundary.

# 19. Files Expected to Change in 7B

List exact paths grouped by task.

# 20. Manual Regression Plan

Include tests for:

- `/experts`.
- Public Expert Profile.
- Calendar consumers.
- Client/Expert registration.
- Client Jobpost routes.
- Workspace Chat Room.
- Expert Submission.
- Client Package reads.

# 21. Verification Results

Report exact results for:

- TypeScript.
- Lint.
- Build.
- Diff check.
- Git status.
- Confirmation that no source files changed.

# 22. Final Decision

Return exactly one:

- GO FOR REFACTOR 7B TARGETED BASELINE FIXES
- GO FOR MAIN FLOW
- NO-GO UNTIL REPOSITORY STRUCTURE IS REPAIRED

Explain the decision precisely.

Important:

- This is read-only diagnosis.
- Do not fix any source file.
- Do not implement Main Flow.
- Do not invent backend endpoints.
- Distinguish source-confirmed facts from inference.
- Separate architecture readiness from business-contract readiness.
- Preserve a clean Git worktree.




You are working in my existing Next.js web application: Neuro Bridge.

This is Refactor 7A: Architecture Closure Audit and Build Baseline Diagnosis only.

This task is strictly read-only for application source files.

Do NOT modify application source files.
Do NOT create application source files.
Do NOT delete, rename, or move application source files.
Do NOT run formatter or auto-fix commands.
Do NOT run ESLint with `--fix`.
Do NOT change package versions.
Do NOT install packages.
Do NOT upgrade dependencies.
Do NOT edit generated route types manually.
Do NOT implement Main Flow business features.
Do NOT implement missing backend endpoints.
Do NOT replace remaining mocks during this audit.
Do NOT fix build or lint errors yet.

You may run build, TypeScript, lint, and read-only search commands.

Generated build output such as `.next/**` may be created by normal Next.js commands, but no tracked application source file may change.

Confirm `git status --short` before and after the audit.

## Current project

Application:

- Neuro Bridge frontend
- Next.js App Router
- TypeScript
- Tailwind
- shadcn-style UI
- React Query
- Redux
- Supabase browser Storage

Project architecture convention:

- `app/**`
  - Thin route and layout shells only
  - Route parameters
  - Render feature views

- `features/**`
  - Feature views
  - Components
  - Local UI state
  - Feature orchestration
  - Feature-specific hooks and mappings
  - Temporary mock data only when explicitly documented

- `services/**`
  - Backend API functions
  - React Query hooks
  - Query keys
  - Mutations
  - External integration boundaries

- `types/**`
  - Domain types
  - Request types
  - Response types
  - Integration contracts

- `configs/**`
  - Initialized shared clients
  - Environment configuration

## Completed refactors

The following architecture work is complete and must not be reopened without concrete evidence.

### Refactor 1 — Public Expert Profile repair

Completed:

- `/experts/[expertCode]` uses a thin route shell.
- Public profile view lives under `features/profile/expert/**`.
- Real Expert and Skill reads are used.
- Invalid code and empty-skill states were handled.
- Broken imports and local type assumptions were repaired.

Known remaining issue:

- The base `/experts` page has previously been reported as not being a valid module.

### Refactor 2 — Applicant to Expert Profile

Completed:

- Client applicant card links to the public Expert profile.
- It uses `applicant.expertApply.expertCode`.
- Missing codes do not create invalid URLs.
- Proposal Unlock remains independent.

### Proposal protected-detail preparation

Completed:

- Proposal Detail is fetched lazily by `proposalId`.
- The dialog no longer uses protected Strategy Detail from the applicant-list response.
- The frontend ignores premature `strategyDetail` fields.

Backend/security concern still open:

- Backend must not expose protected `strategyDetail` before unlock.

### Refactor 3 — Workspace foundation

Completed:

- Centralized Workspace types.
- Shared API client usage.
- Workspace Detail and Members reads.
- Stable Workspace query keys.
- No manual Authorization header in the new Workspace service.

### Refactor 4 — Submission Package foundation

Completed:

- Package list by Workspace.
- Package Detail.
- Deliverable Files by Package.
- File Types.
- Create Submission Package.
- Resubmit Submission Package.
- Scoped query invalidation.
- Chat Room feature extraction.
- Thin Chat Room route shell.

### Refactor 5 — Supabase upload consolidation

Completed:

- One canonical Supabase client in `configs/supabase.ts`.
- One direct Storage SDK boundary in `services/supabase-storage.service.ts`.
- Submission upload feature hook.
- Submission upload utilities.
- Normalized Storage errors.
- Avatar helper migrated to the shared Storage boundary.
- Unused `uploadFiles` removed.
- No direct Supabase Storage SDK call should remain outside the generic service.

Runtime Storage testing and policy verification remain deferred.

### Refactor 6 — Client Submission Review preparation

Completed:

- 6A API and frontend audit.
- 6B real Client Package read integration.
- Mock Client Package history removed.
- Mock Deliverable Files removed.
- Fake Approve/Reject behavior removed.
- Real Package list and selected Package Detail are used.
- Client panel is gated by successful Workspace access.
- Latest Package does not rely on `packages[0]`.
- Status and feedback are read-only.

Backend blockers remain:

- Package Accept endpoint.
- Package Reject endpoint.
- Feedback write contract.
- Package status machine.
- Workspace/Project completion behavior.
- `autoReleaseAt` semantics.
- Escrow/Kafka release ownership.

## Main goal

Produce a complete evidence-based closure report for Refactors 1–6 and establish the exact current build/lint baseline before Main Flow implementation begins.

The audit must answer:

1. Whether the architecture convention is now consistently followed.
2. Whether remaining violations affect the Main Flow.
3. Whether direct API, token, Supabase, or DTO duplication remains.
4. Whether misleading mock actions remain in the primary business flow.
5. Whether invalid identifiers can still trigger real requests.
6. Whether query keys and invalidations are safe enough for Main Flow work.
7. Whether current TypeScript, lint, and build failures are understood.
8. Which failures must be fixed in Refactor 7B.
9. Which warnings may remain as non-blocking debt.
10. Whether frontend architecture is ready to enter Main Flow after targeted baseline fixes.

## Strict scope

This is diagnosis only.

Do NOT:

- Fix `/experts`.
- Fix Calendar.
- Fix `configs/index.ts`.
- Fix `useUserCrud.tsx`.
- Fix lint errors.
- Replace Chat mocks.
- Replace Notification mocks.
- Add Accept/Reject.
- Add Payment Unlock.
- Add Hire/Contract.
- Add Workspace initialization.
- Add Completion.
- Add Escrow release.
- Change API response shapes.
- Change query invalidation.
- Change role logic.
- Change Supabase configuration.
- Change route structure.
- Delete dead code.
- Modify barrel exports.
- Change TypeScript configuration.
- Change ESLint configuration.
- Change Next.js configuration.

Only inspect and report.

## Required initial repository state

Before inspection:

1. Run `git status --short`.
2. Record whether the worktree is clean.
3. If tracked source changes exist, do not edit or reset them.
4. Report the exact paths and continue read-only only when the source can still be inspected safely.
5. Do not use `git checkout`, `git restore`, `git reset`, or `git clean`.

## Required configuration inspection

Inspect:

- `package.json`
- Lockfile
- `tsconfig.json`
- Next.js config
- ESLint config
- Path aliases
- React Query provider setup
- Redux provider setup
- Shared API client configuration
- `configs/supabase.ts`
- Current Next.js version
- Current React version
- Current `react-day-picker` version
- Current TypeScript version
- Current ESLint version

Determine:

- Exact build script.
- Exact lint script.
- Whether build invokes lint separately.
- Whether TypeScript incremental output may affect results.
- Whether `.next` validators are generated from a currently invalid route.
- Whether path aliases match current source layout.
- Whether known errors are caused by version mismatch, missing file, stale export, or invalid source.

Do not alter configuration.

## Audit area 1 — Route and layout convention

Inspect all application routes under:

`app/**`

Pay special attention to:

- `/experts`
- `/experts/[id]` or `/experts/[expertCode]`
- `/client/dashboard/my-job-post`
- `/client/dashboard/my-job-post/[id]`
- `/client/dashboard/create-job-post`
- `/chat-room/[workspaceId]`
- Auth routes
- Profile routes
- Available AI routes
- Client and Expert dashboard routes

Classify each route as:

- CLEAN THIN SHELL
- ACCEPTABLE ROUTE-SPECIFIC LOGIC
- FEATURE LOGIC LEAKED INTO APP
- INVALID ROUTE MODULE
- DEAD OR DUPLICATE ROUTE
- REQUIRES RUNTIME VERIFICATION

Check for:

- Large JSX trees in route files.
- Local API calls in route files.
- Local DTOs in route files.
- Mock data in route files.
- Business mutations in route files.
- Direct Supabase use in route files.
- Duplicate route implementations.
- Empty files.
- Files that export no default React component.
- Invalid route groups.
- Route links pointing to nonexistent routes.
- Dynamic parameter naming inconsistencies.
- Native Next.js page files that are not valid modules.

Do not refactor any route.

## Audit area 2 — Direct network and authorization usage

Search application source for:

- `fetch(`
- `axios`
- `.get(`
- `.post(`
- `.put(`
- `.patch(`
- `.delete(`
- `Authorization`
- `Bearer `
- `localStorage`
- `sessionStorage`
- `document.cookie`
- Manual token extraction
- Manual API base URLs
- Duplicate API client initialization
- Direct Workspace endpoint strings
- Direct Jobpost endpoint strings
- Direct Payment endpoint strings
- Direct Proposal endpoint strings
- Direct Notification endpoint strings

Ignore:

- Shared API-client implementation.
- Framework internals.
- `node_modules`.
- `.next`.
- Generated output.

For every direct request outside an approved service boundary, report:

- File.
- Operation.
- Endpoint.
- Authentication method.
- Whether it belongs under `services/**`.
- Whether it affects Main Flow.
- Severity.
- Recommended future action.

Distinguish:

- Legitimate external fetches.
- API service functions.
- Legacy direct backend calls.
- Mock-only code.
- Dead code.
- Browser asset requests.

Do not assume every occurrence of `.get` is HTTP.

## Audit area 3 — Supabase closure verification

Search for:

- `createClient(`
- `@supabase/supabase-js`
- `@/configs/supabase`
- `supabase.storage`
- `.storage.from(`
- `.upload(`
- `.getPublicUrl(`
- `.createSignedUrl(`
- `.remove(`
- `listBuckets`
- `service_role`
- Supabase key logging
- Session logging
- Token logging

Confirm:

1. `createClient` exists only in `configs/supabase.ts`.
2. Storage SDK calls exist only in `services/supabase-storage.service.ts`.
3. Submission uses the shared Storage boundary.
4. Avatar upload uses the shared Storage boundary.
5. No service-role key exists in browser source.
6. No credential/session/token logging exists.
7. No duplicate Supabase config remains.
8. No feature imports Supabase directly for Storage.

Report any exception.

Do not modify Supabase code.

## Audit area 4 — Service and type architecture

Inspect:

- `services/**`
- `types/**`
- Feature-local API helpers
- Local request/response interfaces
- Query-key declarations
- Mutation hooks
- Response unwrapping helpers
- Error normalization helpers

Search for:

- Duplicate API DTO definitions.
- Local interfaces matching centralized types.
- `any`.
- `unknown as`.
- Broad type assertions.
- Non-null assertions around API identifiers.
- Response types typed differently across consumers.
- Universal envelope assumptions.
- Direct object versus `{ code, message, result }` ambiguity.
- Hooks without enabled guards.
- Query keys missing identifiers.
- Mutation functions embedded in UI.
- API functions embedded in feature components.

For each issue classify:

- MAIN FLOW BLOCKER
- SHOULD FIX IN 7B
- SAFE TO DEFER
- INTENTIONAL FEATURE-LOCAL TYPE
- RUNTIME CONTRACT VERIFICATION NEEDED

Do not centralize types during this audit.

## Audit area 5 — Query keys and invalidation

Inspect actual current keys for:

- Jobpost.
- Applicants.
- Proposal Unlock.
- Workspace.
- Submission Packages.
- Deliverable Files.
- Payment Wallet.
- Payment Transactions.
- Notifications.
- Expert/Profile data.

Check:

- Whether query keys include required IDs.
- Whether optional IDs can create collisions.
- Whether hooks are disabled for empty IDs.
- Whether mutations invalidate only affected data.
- Whether broad root invalidation is used unnecessarily.
- Whether new Package versions invalidate old/new detail correctly.
- Whether selected Package Detail is isolated by package ID.
- Whether switching IDs can display stale data.
- Whether list and detail keys are structurally consistent.
- Whether manual query keys duplicate centralized factories.

Do not change keys.

Report exact current key structures when relevant.

## Audit area 6 — Identifier safety

Search for API calls or links using:

- `workspaceId`
- `packageId`
- `proposalId`
- `jobPostId`
- `expertCode`
- `fileId`
- `escrowId`
- `userId`
- Array indexes
- Placeholder IDs
- Empty strings
- `undefined`
- `null`

Identify:

- Hooks that may call `/undefined`.
- Links that may produce invalid routes.
- ID substitutions such as job ID used as Workspace ID.
- Package ID used as File ID.
- Array index used as domain identity.
- Missing enabled guards.
- IDs read from localStorage instead of route or real response.
- Mock IDs entering real services.

Separate:

- Confirmed safe.
- Potentially unsafe.
- Runtime-dependent.
- Already guarded by parent rendering.

## Audit area 7 — Main Flow mock and placeholder audit

Search primary Client–Expert flow for:

- `mock`
- `fake`
- `demo`
- `placeholder`
- `TODO`
- `FIXME`
- `setTimeout`
- Local action success
- Static status transitions
- Hardcoded Package/Proposal/Workspace data
- Hardcoded wallet values
- Fake Contract or Hire state
- Buttons that only mutate local state
- Buttons that navigate without a real backend action
- Toasts claiming success without a mutation

Inspect at minimum:

- Client Jobpost pages.
- Expert marketplace/apply flow.
- Applicants.
- Proposal detail/unlock.
- Hire.
- Contract.
- Workspace Chat Room.
- Submission.
- Client review.
- Payment/Wallet.
- Notification.
- Dispute.
- Completion.

Classify every relevant mock as:

- SAFE PRESENTATIONAL MOCK
- KNOWN TEMPORARY MOCK
- MISLEADING BUSINESS ACTION
- MAIN FLOW BLOCKER
- DEAD CODE
- OUT OF SCOPE

Pay special attention to:

- Chat messages.
- Notification data.
- Hire actions.
- Payment unlock.
- Client Submission actions.
- Completion actions.
- Wallet balances.
- Success toasts.

Do not remove mocks.

## Audit area 8 — Barrel exports and import health

Inspect:

- `features/**/index.ts`
- `services` barrels if any.
- `types` barrels if any.
- `configs/index.ts`
- Module aliases.
- Re-export chains.

Search for:

- Exports pointing to missing files.
- Imports from deleted modules.
- Circular-looking barrel chains.
- Route imports that depend on broken barrels.
- Duplicate export names.
- Default/named export mismatch.
- Case-sensitive path mismatch.
- Legacy `modules/**` imports.
- References to removed components.
- Dead barrels used only by invalid code.

Do not rewrite barrels.

Report whether direct imports are safer for each known failure.

## Audit area 9 — Known TypeScript and build errors

The following errors have repeatedly appeared and must be diagnosed precisely.

### Error A — Experts base page

Known symptom:

`app/(main)/experts/page.tsx` is not a module.

Inspect:

- Exact file content.
- Whether it is empty.
- Whether it has invalid commented-out content.
- Whether it exports a default component.
- Whether a valid Expert listing feature already exists elsewhere.
- Whether links currently target `/experts`.
- Whether repairing this page is required before Main Flow.
- Smallest safe future fix.

Do not implement the fix.

### Error B — Calendar / react-day-picker

Known symptom:

`components/ui/calendar.tsx` uses an invalid `table` property in DayPicker class names or related incompatible API.

Inspect:

- Installed `react-day-picker` version.
- Current Calendar component implementation.
- Correct class-name keys for the installed version.
- Whether the component was generated for another version.
- Every Calendar consumer.
- Whether a minimal compatibility fix is possible.
- Whether UI behavior may change.

Use installed package types and official local type declarations as evidence.

Do not use web search unless explicitly needed.

Do not upgrade `react-day-picker`.

Do not implement the fix.

### Error C — Broken config barrel import

Known symptom:

`configs/index.ts` references:

`@/modules/users/forms/UserForm`

Inspect:

- Exact import/export.
- Whether the referenced file exists.
- Whether the path is stale.
- Whether the export is used anywhere.
- Whether `modules/users/**` is legacy architecture.
- Whether removing the export or correcting the path is safer.
- Whether another `UserForm` exists.
- Main Flow impact.

Do not change the file.

### Error D — Missing UserSchema

Known symptom:

`modules/users/useUserCrud.tsx` references missing `UserSchema`.

Inspect:

- Current imports.
- Existing user schemas.
- Whether this module is used.
- Whether it belongs to legacy Admin CRUD.
- Whether the missing symbol was renamed.
- Whether the module can be excluded or must compile.
- Smallest safe future fix.
- Main Flow relevance.

Do not fix it.

### Error E — Expert registration `any`

Known lint symptom:

`features/auth/register/ExpertRegisterForm.tsx` contains unexpected `any`, previously reported around line 88.

Inspect:

- Exact variable or callback.
- Correct available type.
- Whether a local type exists.
- Whether the fix changes runtime behavior.
- Whether it is safe for 7B.

Do not fix it.

### Error F — `use-mobile` state update in effect

Known lint symptom:

`hooks/use-mobile.ts` calls state setter in an effect in a way rejected by current lint rules.

Inspect:

- Exact hook implementation.
- Current lint rule.
- Consumer count.
- Whether it causes runtime hydration issues.
- Whether the common `matchMedia` lazy-state pattern can fix it.
- Whether it affects Main Flow.
- Smallest safe future fix.

Do not implement it.

## Audit area 10 — Warning baseline

Record lint warnings grouped by rule and path.

Pay attention to:

- `<img>` warnings.
- Missing hook dependencies.
- Unused imports.
- Explicit `any`.
- Accessibility.
- React Compiler-related warnings.
- Next.js image optimization.
- Deprecated APIs.
- Console statements.

Classify warnings:

- MUST FIX BEFORE MAIN FLOW
- SHOULD FIX IN 7B
- SAFE TO DEFER
- FALSE POSITIVE OR CONVENTION DECISION

Do not attempt to remove all warnings.

The goal is a stable baseline, not warning-zero at any cost.

## Audit area 11 — Main Flow backend blockers

Consolidate currently known backend blockers.

At minimum inspect frontend evidence for:

### Proposal purchase/unlock

- Missing atomic purchase/unlock endpoint.
- Protected Strategy Detail exposure.
- Wallet insufficiency response.
- Already-unlocked behavior.
- Required invalidations.

### Hire and Contract

- Negotiated amount request field.
- Create Contract/Escrow.
- Finalize after signatures/funding.
- Retry behavior.
- Idempotency.
- Workspace creation trigger.

### Workspace Proposal

- Proposal creation or association endpoint.
- Proposal source of truth after Hire.

### Submission review

- Package Reject.
- Feedback body.
- Package Accept.
- Status transitions.
- Completion behavior.
- Escrow release event.
- `autoReleaseAt`.

### Notification and Dispute

- Real notification contract.
- Dispute transitions.
- Completion/release interactions.

For each blocker classify:

- FRONTEND READY
- FRONTEND PREPARATION POSSIBLE
- BACKEND CONTRACT REQUIRED
- BACKEND SECURITY FIX REQUIRED
- RUNTIME VERIFICATION REQUIRED

Do not design speculative frontend mutations.

## Required commands

Run these commands and capture exact results.

1. `git status --short`
2. `npx tsc --noEmit --incremental false --pretty false`
3. `npm run lint -- --no-cache`
4. `npm run build`
5. `git diff --check`
6. `git status --short`

If the build command fails:

- Record the first root-cause error.
- Continue source inspection.
- Do not repeatedly rerun the same failing command without new evidence.
- Do not edit files.

If `.next` generated output affects TypeScript error reporting:

- Explain the relationship.
- Do not manually edit `.next`.
- Do not delete tracked source.
- You may inspect generated validator paths read-only.

Do not use destructive Git commands.

## Build baseline classification

For every TypeScript, lint, or build failure, report:

- Command.
- File.
- Line.
- Error code or lint rule.
- Root cause.
- Whether it is pre-existing.
- Whether it was introduced by Refactors 1–6.
- Main Flow impact.
- Minimal future fix.
- Expected files for 7B.
- Risk of fixing.

Classify each failure:

- BASELINE BLOCKER
- MAIN FLOW BLOCKER
- ROUTE-SPECIFIC BLOCKER
- LINT-ONLY BLOCKER
- NON-BLOCKING WARNING
- GENERATED CONSEQUENCE OF SOURCE ERROR

Do not merely repeat compiler output.

## Refactor regression check

Verify Refactors 1–6 did not introduce obvious architecture regressions.

Confirm or refute:

1. Public Expert Profile route remains thin.
2. Applicant link still uses `expertCode`.
3. Proposal Detail remains lazy by `proposalId`.
4. Workspace services use shared API client.
5. Submission reads/writes use service hooks.
6. Chat Room route remains thin.
7. Submission upload uses feature hook and shared Storage service.
8. Avatar uses shared Storage service.
9. Client Package panel uses real reads.
10. Fake Client review actions remain removed.
11. Client panel is gated by real Workspace success.
12. No direct Storage SDK call returned to feature code.
13. No new manual Authorization/token logic was introduced.
14. No unsupported Accept/Reject endpoint was invented.

## Required readiness matrix

Produce a readiness matrix for:

- Public Expert Profile.
- Expert listing.
- Client Jobpost list/detail.
- Expert marketplace.
- Apply Jobpost.
- Applicant list.
- Proposal summary.
- Proposal protected detail.
- Proposal payment/unlock.
- Hire.
- Contract.
- Workspace reads.
- Workspace members.
- Workspace Chat shell.
- Expert Package create.
- Expert Package resubmit.
- Client Package history.
- Client Package detail/files.
- Client Reject.
- Client Accept.
- Completion.
- Wallet/Transactions.
- Notification.
- Dispute.
- Repository build baseline.

Use only:

- READY
- READY AFTER 7B BASELINE FIX
- READY FOR READS ONLY
- FRONTEND FOUNDATION READY
- BLOCKED BY BACKEND CONTRACT
- BLOCKED BY BACKEND SECURITY
- MOCK ONLY
- RUNTIME VERIFICATION REQUIRED
- NOT AUDITED

## Refactor 7B planning requirements

Based on evidence, propose a minimal 7B fix set.

The plan must:

- Fix only build/lint baseline blockers.
- Avoid broad architecture refactors.
- Avoid business-flow implementation.
- Avoid dependency upgrades unless absolutely unavoidable.
- Keep each fix independently reviewable.
- Rank fixes by dependency and risk.
- State exact expected files.
- State whether each fix requires manual UI regression testing.
- State whether each fix can be committed independently.

A likely 7B plan may include:

- Repair invalid `/experts` route module.
- Repair Calendar compatibility with installed DayPicker.
- Remove or correct stale `configs/index.ts` export.
- Repair or isolate legacy `useUserCrud.tsx`.
- Replace explicit `any` in Expert registration.
- Repair `use-mobile` lint pattern.

Do not assume all six should be fixed together.

Use source evidence to decide.

## No-write verification

At the end:

1. Run `git diff --check`.
2. Run `git status --short`.
3. Confirm no tracked application source file changed.
4. Report generated ignored artifacts separately when relevant.
5. Do not clean or reset the repository.

## Required final output

Return a detailed Markdown report with these exact sections.

# 1. Executive Summary

State:

- Whether Refactors 1–6 are structurally closed.
- Whether major architecture boundaries are now consistent.
- Whether the repository currently type-checks.
- Whether lint passes.
- Whether production build passes.
- Whether Main Flow implementation may start before 7B.
- Highest-priority blockers.

# 2. Repository and Tooling Baseline

Report:

- Framework and package versions.
- Build/lint/typecheck scripts.
- Initial Git status.
- Final Git status.
- Generated-output considerations.

# 3. Refactor 1–6 Regression Check

Report every required regression check as PASS, FAIL, or RUNTIME UNVERIFIED.

# 4. Route Architecture Audit

Provide a table with:

- Route.
- Route file.
- Classification.
- Main issue.
- Main Flow impact.
- Recommended action.

# 5. Direct Network and Auth Audit

List every direct or suspicious API/auth occurrence outside approved boundaries.

# 6. Supabase Closure Audit

Report:

- Client initialization count.
- Direct Storage SDK call locations.
- Submission consumer path.
- Avatar consumer path.
- Credential/logging findings.
- Remaining runtime/security limitations.

# 7. Service and Type Architecture Audit

Report:

- Duplicate DTOs.
- Local API types.
- Unsafe assertions.
- Response-envelope inconsistencies.
- Hook enabled guards.
- Service-boundary violations.

# 8. Query Key and Invalidation Audit

Report actual strengths, risks, collisions, and invalidation concerns.

# 9. Identifier Safety Audit

Report confirmed safe and unsafe identifier flows.

# 10. Main Flow Mock and Placeholder Audit

Provide a table with:

- Feature.
- File.
- Mock or placeholder.
- User-visible behavior.
- Risk classification.
- Backend dependency.
- Recommended action.

# 11. Barrel Export and Import Health

Report stale exports, missing modules, possible cycles, and direct-import recommendations.

# 12. Build and TypeScript Baseline

For each error provide:

- Command.
- File.
- Error.
- Root cause.
- Classification.
- Main Flow impact.
- Minimal 7B fix.

# 13. Lint Baseline

Group errors and warnings by rule and severity.

# 14. Known Error Deep Diagnosis

Create subsections for:

- Experts base page.
- Calendar and DayPicker.
- `configs/index.ts`.
- `useUserCrud.tsx`.
- Expert registration `any`.
- `use-mobile`.

# 15. Backend Contract and Security Blockers

Group by:

- Proposal Unlock.
- Hire/Contract.
- Workspace Proposal.
- Submission Review.
- Completion/Release.
- Notification/Dispute.

# 16. Frontend Readiness Matrix

Use the required readiness classifications.

# 17. Main Flow Entry Assessment

Explain:

- Which Main Flow phase could technically start.
- Which phase should start first.
- Which phases are blocked.
- Whether starting before build baseline cleanup is advisable.

# 18. Recommended Refactor 7B Fix Set

Provide ordered, minimal, independently reviewable tasks.

For each task include:

- Goal.
- Exact files.
- Expected change.
- Risk.
- Verification.
- Suggested commit boundary.

# 19. Files Expected to Change in 7B

List exact paths grouped by task.

# 20. Manual Regression Plan

Include tests for:

- `/experts`.
- Public Expert Profile.
- Calendar consumers.
- Client/Expert registration.
- Client Jobpost routes.
- Workspace Chat Room.
- Expert Submission.
- Client Package reads.

# 21. Verification Results

Report exact results for:

- TypeScript.
- Lint.
- Build.
- Diff check.
- Git status.
- Confirmation that no source files changed.

# 22. Final Decision

Return exactly one:

- GO FOR REFACTOR 7B TARGETED BASELINE FIXES
- GO FOR MAIN FLOW
- NO-GO UNTIL REPOSITORY STRUCTURE IS REPAIRED

Explain the decision precisely.

Important:

- This is read-only diagnosis.
- Do not fix any source file.
- Do not implement Main Flow.
- Do not invent backend endpoints.
- Distinguish source-confirmed facts from inference.
- Separate architecture readiness from business-contract readiness.
- Preserve a clean Git worktree.
- You are working in my existing Next.js web application: Neuro Bridge.

This is Refactor 7A: Architecture Closure Audit and Build Baseline Diagnosis only.

This task is strictly read-only for application source files.

Do NOT modify application source files.
Do NOT create application source files.
Do NOT delete, rename, or move application source files.
Do NOT run formatter or auto-fix commands.
Do NOT run ESLint with `--fix`.
Do NOT change package versions.
Do NOT install packages.
Do NOT upgrade dependencies.
Do NOT edit generated route types manually.
Do NOT implement Main Flow business features.
Do NOT implement missing backend endpoints.
Do NOT replace remaining mocks during this audit.
Do NOT fix build or lint errors yet.

You may run build, TypeScript, lint, and read-only search commands.

Generated build output such as `.next/**` may be created by normal Next.js commands, but no tracked application source file may change.

Confirm `git status --short` before and after the audit.

## Current project

Application:

- Neuro Bridge frontend
- Next.js App Router
- TypeScript
- Tailwind
- shadcn-style UI
- React Query
- Redux
- Supabase browser Storage

Project architecture convention:

- `app/**`
  - Thin route and layout shells only
  - Route parameters
  - Render feature views

- `features/**`
  - Feature views
  - Components
  - Local UI state
  - Feature orchestration
  - Feature-specific hooks and mappings
  - Temporary mock data only when explicitly documented

- `services/**`
  - Backend API functions
  - React Query hooks
  - Query keys
  - Mutations
  - External integration boundaries

- `types/**`
  - Domain types
  - Request types
  - Response types
  - Integration contracts

- `configs/**`
  - Initialized shared clients
  - Environment configuration

## Completed refactors

The following architecture work is complete and must not be reopened without concrete evidence.

### Refactor 1 — Public Expert Profile repair

Completed:

- `/experts/[expertCode]` uses a thin route shell.
- Public profile view lives under `features/profile/expert/**`.
- Real Expert and Skill reads are used.
- Invalid code and empty-skill states were handled.
- Broken imports and local type assumptions were repaired.

Known remaining issue:

- The base `/experts` page has previously been reported as not being a valid module.

### Refactor 2 — Applicant to Expert Profile

Completed:

- Client applicant card links to the public Expert profile.
- It uses `applicant.expertApply.expertCode`.
- Missing codes do not create invalid URLs.
- Proposal Unlock remains independent.

### Proposal protected-detail preparation

Completed:

- Proposal Detail is fetched lazily by `proposalId`.
- The dialog no longer uses protected Strategy Detail from the applicant-list response.
- The frontend ignores premature `strategyDetail` fields.

Backend/security concern still open:

- Backend must not expose protected `strategyDetail` before unlock.

### Refactor 3 — Workspace foundation

Completed:

- Centralized Workspace types.
- Shared API client usage.
- Workspace Detail and Members reads.
- Stable Workspace query keys.
- No manual Authorization header in the new Workspace service.

### Refactor 4 — Submission Package foundation

Completed:

- Package list by Workspace.
- Package Detail.
- Deliverable Files by Package.
- File Types.
- Create Submission Package.
- Resubmit Submission Package.
- Scoped query invalidation.
- Chat Room feature extraction.
- Thin Chat Room route shell.

### Refactor 5 — Supabase upload consolidation

Completed:

- One canonical Supabase client in `configs/supabase.ts`.
- One direct Storage SDK boundary in `services/supabase-storage.service.ts`.
- Submission upload feature hook.
- Submission upload utilities.
- Normalized Storage errors.
- Avatar helper migrated to the shared Storage boundary.
- Unused `uploadFiles` removed.
- No direct Supabase Storage SDK call should remain outside the generic service.

Runtime Storage testing and policy verification remain deferred.

### Refactor 6 — Client Submission Review preparation

Completed:

- 6A API and frontend audit.
- 6B real Client Package read integration.
- Mock Client Package history removed.
- Mock Deliverable Files removed.
- Fake Approve/Reject behavior removed.
- Real Package list and selected Package Detail are used.
- Client panel is gated by successful Workspace access.
- Latest Package does not rely on `packages[0]`.
- Status and feedback are read-only.

Backend blockers remain:

- Package Accept endpoint.
- Package Reject endpoint.
- Feedback write contract.
- Package status machine.
- Workspace/Project completion behavior.
- `autoReleaseAt` semantics.
- Escrow/Kafka release ownership.

## Main goal

Produce a complete evidence-based closure report for Refactors 1–6 and establish the exact current build/lint baseline before Main Flow implementation begins.

The audit must answer:

1. Whether the architecture convention is now consistently followed.
2. Whether remaining violations affect the Main Flow.
3. Whether direct API, token, Supabase, or DTO duplication remains.
4. Whether misleading mock actions remain in the primary business flow.
5. Whether invalid identifiers can still trigger real requests.
6. Whether query keys and invalidations are safe enough for Main Flow work.
7. Whether current TypeScript, lint, and build failures are understood.
8. Which failures must be fixed in Refactor 7B.
9. Which warnings may remain as non-blocking debt.
10. Whether frontend architecture is ready to enter Main Flow after targeted baseline fixes.

## Strict scope

This is diagnosis only.

Do NOT:

- Fix `/experts`.
- Fix Calendar.
- Fix `configs/index.ts`.
- Fix `useUserCrud.tsx`.
- Fix lint errors.
- Replace Chat mocks.
- Replace Notification mocks.
- Add Accept/Reject.
- Add Payment Unlock.
- Add Hire/Contract.
- Add Workspace initialization.
- Add Completion.
- Add Escrow release.
- Change API response shapes.
- Change query invalidation.
- Change role logic.
- Change Supabase configuration.
- Change route structure.
- Delete dead code.
- Modify barrel exports.
- Change TypeScript configuration.
- Change ESLint configuration.
- Change Next.js configuration.

Only inspect and report.

## Required initial repository state

Before inspection:

1. Run `git status --short`.
2. Record whether the worktree is clean.
3. If tracked source changes exist, do not edit or reset them.
4. Report the exact paths and continue read-only only when the source can still be inspected safely.
5. Do not use `git checkout`, `git restore`, `git reset`, or `git clean`.

## Required configuration inspection

Inspect:

- `package.json`
- Lockfile
- `tsconfig.json`
- Next.js config
- ESLint config
- Path aliases
- React Query provider setup
- Redux provider setup
- Shared API client configuration
- `configs/supabase.ts`
- Current Next.js version
- Current React version
- Current `react-day-picker` version
- Current TypeScript version
- Current ESLint version

Determine:

- Exact build script.
- Exact lint script.
- Whether build invokes lint separately.
- Whether TypeScript incremental output may affect results.
- Whether `.next` validators are generated from a currently invalid route.
- Whether path aliases match current source layout.
- Whether known errors are caused by version mismatch, missing file, stale export, or invalid source.

Do not alter configuration.

## Audit area 1 — Route and layout convention

Inspect all application routes under:

`app/**`

Pay special attention to:

- `/experts`
- `/experts/[id]` or `/experts/[expertCode]`
- `/client/dashboard/my-job-post`
- `/client/dashboard/my-job-post/[id]`
- `/client/dashboard/create-job-post`
- `/chat-room/[workspaceId]`
- Auth routes
- Profile routes
- Available AI routes
- Client and Expert dashboard routes

Classify each route as:

- CLEAN THIN SHELL
- ACCEPTABLE ROUTE-SPECIFIC LOGIC
- FEATURE LOGIC LEAKED INTO APP
- INVALID ROUTE MODULE
- DEAD OR DUPLICATE ROUTE
- REQUIRES RUNTIME VERIFICATION

Check for:

- Large JSX trees in route files.
- Local API calls in route files.
- Local DTOs in route files.
- Mock data in route files.
- Business mutations in route files.
- Direct Supabase use in route files.
- Duplicate route implementations.
- Empty files.
- Files that export no default React component.
- Invalid route groups.
- Route links pointing to nonexistent routes.
- Dynamic parameter naming inconsistencies.
- Native Next.js page files that are not valid modules.

Do not refactor any route.

## Audit area 2 — Direct network and authorization usage

Search application source for:

- `fetch(`
- `axios`
- `.get(`
- `.post(`
- `.put(`
- `.patch(`
- `.delete(`
- `Authorization`
- `Bearer `
- `localStorage`
- `sessionStorage`
- `document.cookie`
- Manual token extraction
- Manual API base URLs
- Duplicate API client initialization
- Direct Workspace endpoint strings
- Direct Jobpost endpoint strings
- Direct Payment endpoint strings
- Direct Proposal endpoint strings
- Direct Notification endpoint strings

Ignore:

- Shared API-client implementation.
- Framework internals.
- `node_modules`.
- `.next`.
- Generated output.

For every direct request outside an approved service boundary, report:

- File.
- Operation.
- Endpoint.
- Authentication method.
- Whether it belongs under `services/**`.
- Whether it affects Main Flow.
- Severity.
- Recommended future action.

Distinguish:

- Legitimate external fetches.
- API service functions.
- Legacy direct backend calls.
- Mock-only code.
- Dead code.
- Browser asset requests.

Do not assume every occurrence of `.get` is HTTP.

## Audit area 3 — Supabase closure verification

Search for:

- `createClient(`
- `@supabase/supabase-js`
- `@/configs/supabase`
- `supabase.storage`
- `.storage.from(`
- `.upload(`
- `.getPublicUrl(`
- `.createSignedUrl(`
- `.remove(`
- `listBuckets`
- `service_role`
- Supabase key logging
- Session logging
- Token logging

Confirm:

1. `createClient` exists only in `configs/supabase.ts`.
2. Storage SDK calls exist only in `services/supabase-storage.service.ts`.
3. Submission uses the shared Storage boundary.
4. Avatar upload uses the shared Storage boundary.
5. No service-role key exists in browser source.
6. No credential/session/token logging exists.
7. No duplicate Supabase config remains.
8. No feature imports Supabase directly for Storage.

Report any exception.

Do not modify Supabase code.

## Audit area 4 — Service and type architecture

Inspect:

- `services/**`
- `types/**`
- Feature-local API helpers
- Local request/response interfaces
- Query-key declarations
- Mutation hooks
- Response unwrapping helpers
- Error normalization helpers

Search for:

- Duplicate API DTO definitions.
- Local interfaces matching centralized types.
- `any`.
- `unknown as`.
- Broad type assertions.
- Non-null assertions around API identifiers.
- Response types typed differently across consumers.
- Universal envelope assumptions.
- Direct object versus `{ code, message, result }` ambiguity.
- Hooks without enabled guards.
- Query keys missing identifiers.
- Mutation functions embedded in UI.
- API functions embedded in feature components.

For each issue classify:

- MAIN FLOW BLOCKER
- SHOULD FIX IN 7B
- SAFE TO DEFER
- INTENTIONAL FEATURE-LOCAL TYPE
- RUNTIME CONTRACT VERIFICATION NEEDED

Do not centralize types during this audit.

## Audit area 5 — Query keys and invalidation

Inspect actual current keys for:

- Jobpost.
- Applicants.
- Proposal Unlock.
- Workspace.
- Submission Packages.
- Deliverable Files.
- Payment Wallet.
- Payment Transactions.
- Notifications.
- Expert/Profile data.

Check:

- Whether query keys include required IDs.
- Whether optional IDs can create collisions.
- Whether hooks are disabled for empty IDs.
- Whether mutations invalidate only affected data.
- Whether broad root invalidation is used unnecessarily.
- Whether new Package versions invalidate old/new detail correctly.
- Whether selected Package Detail is isolated by package ID.
- Whether switching IDs can display stale data.
- Whether list and detail keys are structurally consistent.
- Whether manual query keys duplicate centralized factories.

Do not change keys.

Report exact current key structures when relevant.

## Audit area 6 — Identifier safety

Search for API calls or links using:

- `workspaceId`
- `packageId`
- `proposalId`
- `jobPostId`
- `expertCode`
- `fileId`
- `escrowId`
- `userId`
- Array indexes
- Placeholder IDs
- Empty strings
- `undefined`
- `null`

Identify:

- Hooks that may call `/undefined`.
- Links that may produce invalid routes.
- ID substitutions such as job ID used as Workspace ID.
- Package ID used as File ID.
- Array index used as domain identity.
- Missing enabled guards.
- IDs read from localStorage instead of route or real response.
- Mock IDs entering real services.

Separate:

- Confirmed safe.
- Potentially unsafe.
- Runtime-dependent.
- Already guarded by parent rendering.

## Audit area 7 — Main Flow mock and placeholder audit

Search primary Client–Expert flow for:

- `mock`
- `fake`
- `demo`
- `placeholder`
- `TODO`
- `FIXME`
- `setTimeout`
- Local action success
- Static status transitions
- Hardcoded Package/Proposal/Workspace data
- Hardcoded wallet values
- Fake Contract or Hire state
- Buttons that only mutate local state
- Buttons that navigate without a real backend action
- Toasts claiming success without a mutation

Inspect at minimum:

- Client Jobpost pages.
- Expert marketplace/apply flow.
- Applicants.
- Proposal detail/unlock.
- Hire.
- Contract.
- Workspace Chat Room.
- Submission.
- Client review.
- Payment/Wallet.
- Notification.
- Dispute.
- Completion.

Classify every relevant mock as:

- SAFE PRESENTATIONAL MOCK
- KNOWN TEMPORARY MOCK
- MISLEADING BUSINESS ACTION
- MAIN FLOW BLOCKER
- DEAD CODE
- OUT OF SCOPE

Pay special attention to:

- Chat messages.
- Notification data.
- Hire actions.
- Payment unlock.
- Client Submission actions.
- Completion actions.
- Wallet balances.
- Success toasts.

Do not remove mocks.

## Audit area 8 — Barrel exports and import health

Inspect:

- `features/**/index.ts`
- `services` barrels if any.
- `types` barrels if any.
- `configs/index.ts`
- Module aliases.
- Re-export chains.

Search for:

- Exports pointing to missing files.
- Imports from deleted modules.
- Circular-looking barrel chains.
- Route imports that depend on broken barrels.
- Duplicate export names.
- Default/named export mismatch.
- Case-sensitive path mismatch.
- Legacy `modules/**` imports.
- References to removed components.
- Dead barrels used only by invalid code.

Do not rewrite barrels.

Report whether direct imports are safer for each known failure.

## Audit area 9 — Known TypeScript and build errors

The following errors have repeatedly appeared and must be diagnosed precisely.

### Error A — Experts base page

Known symptom:

`app/(main)/experts/page.tsx` is not a module.

Inspect:

- Exact file content.
- Whether it is empty.
- Whether it has invalid commented-out content.
- Whether it exports a default component.
- Whether a valid Expert listing feature already exists elsewhere.
- Whether links currently target `/experts`.
- Whether repairing this page is required before Main Flow.
- Smallest safe future fix.

Do not implement the fix.

### Error B — Calendar / react-day-picker

Known symptom:

`components/ui/calendar.tsx` uses an invalid `table` property in DayPicker class names or related incompatible API.

Inspect:

- Installed `react-day-picker` version.
- Current Calendar component implementation.
- Correct class-name keys for the installed version.
- Whether the component was generated for another version.
- Every Calendar consumer.
- Whether a minimal compatibility fix is possible.
- Whether UI behavior may change.

Use installed package types and official local type declarations as evidence.

Do not use web search unless explicitly needed.

Do not upgrade `react-day-picker`.

Do not implement the fix.

### Error C — Broken config barrel import

Known symptom:

`configs/index.ts` references:

`@/modules/users/forms/UserForm`

Inspect:

- Exact import/export.
- Whether the referenced file exists.
- Whether the path is stale.
- Whether the export is used anywhere.
- Whether `modules/users/**` is legacy architecture.
- Whether removing the export or correcting the path is safer.
- Whether another `UserForm` exists.
- Main Flow impact.

Do not change the file.

### Error D — Missing UserSchema

Known symptom:

`modules/users/useUserCrud.tsx` references missing `UserSchema`.

Inspect:

- Current imports.
- Existing user schemas.
- Whether this module is used.
- Whether it belongs to legacy Admin CRUD.
- Whether the missing symbol was renamed.
- Whether the module can be excluded or must compile.
- Smallest safe future fix.
- Main Flow relevance.

Do not fix it.

### Error E — Expert registration `any`

Known lint symptom:

`features/auth/register/ExpertRegisterForm.tsx` contains unexpected `any`, previously reported around line 88.

Inspect:

- Exact variable or callback.
- Correct available type.
- Whether a local type exists.
- Whether the fix changes runtime behavior.
- Whether it is safe for 7B.

Do not fix it.

### Error F — `use-mobile` state update in effect

Known lint symptom:

`hooks/use-mobile.ts` calls state setter in an effect in a way rejected by current lint rules.

Inspect:

- Exact hook implementation.
- Current lint rule.
- Consumer count.
- Whether it causes runtime hydration issues.
- Whether the common `matchMedia` lazy-state pattern can fix it.
- Whether it affects Main Flow.
- Smallest safe future fix.

Do not implement it.

## Audit area 10 — Warning baseline

Record lint warnings grouped by rule and path.

Pay attention to:

- `<img>` warnings.
- Missing hook dependencies.
- Unused imports.
- Explicit `any`.
- Accessibility.
- React Compiler-related warnings.
- Next.js image optimization.
- Deprecated APIs.
- Console statements.

Classify warnings:

- MUST FIX BEFORE MAIN FLOW
- SHOULD FIX IN 7B
- SAFE TO DEFER
- FALSE POSITIVE OR CONVENTION DECISION

Do not attempt to remove all warnings.

The goal is a stable baseline, not warning-zero at any cost.

## Audit area 11 — Main Flow backend blockers

Consolidate currently known backend blockers.

At minimum inspect frontend evidence for:

### Proposal purchase/unlock

- Missing atomic purchase/unlock endpoint.
- Protected Strategy Detail exposure.
- Wallet insufficiency response.
- Already-unlocked behavior.
- Required invalidations.

### Hire and Contract

- Negotiated amount request field.
- Create Contract/Escrow.
- Finalize after signatures/funding.
- Retry behavior.
- Idempotency.
- Workspace creation trigger.

### Workspace Proposal

- Proposal creation or association endpoint.
- Proposal source of truth after Hire.

### Submission review

- Package Reject.
- Feedback body.
- Package Accept.
- Status transitions.
- Completion behavior.
- Escrow release event.
- `autoReleaseAt`.

### Notification and Dispute

- Real notification contract.
- Dispute transitions.
- Completion/release interactions.

For each blocker classify:

- FRONTEND READY
- FRONTEND PREPARATION POSSIBLE
- BACKEND CONTRACT REQUIRED
- BACKEND SECURITY FIX REQUIRED
- RUNTIME VERIFICATION REQUIRED

Do not design speculative frontend mutations.

## Required commands

Run these commands and capture exact results.

1. `git status --short`
2. `npx tsc --noEmit --incremental false --pretty false`
3. `npm run lint -- --no-cache`
4. `npm run build`
5. `git diff --check`
6. `git status --short`

If the build command fails:

- Record the first root-cause error.
- Continue source inspection.
- Do not repeatedly rerun the same failing command without new evidence.
- Do not edit files.

If `.next` generated output affects TypeScript error reporting:

- Explain the relationship.
- Do not manually edit `.next`.
- Do not delete tracked source.
- You may inspect generated validator paths read-only.

Do not use destructive Git commands.

## Build baseline classification

For every TypeScript, lint, or build failure, report:

- Command.
- File.
- Line.
- Error code or lint rule.
- Root cause.
- Whether it is pre-existing.
- Whether it was introduced by Refactors 1–6.
- Main Flow impact.
- Minimal future fix.
- Expected files for 7B.
- Risk of fixing.

Classify each failure:

- BASELINE BLOCKER
- MAIN FLOW BLOCKER
- ROUTE-SPECIFIC BLOCKER
- LINT-ONLY BLOCKER
- NON-BLOCKING WARNING
- GENERATED CONSEQUENCE OF SOURCE ERROR

Do not merely repeat compiler output.

## Refactor regression check

Verify Refactors 1–6 did not introduce obvious architecture regressions.

Confirm or refute:

1. Public Expert Profile route remains thin.
2. Applicant link still uses `expertCode`.
3. Proposal Detail remains lazy by `proposalId`.
4. Workspace services use shared API client.
5. Submission reads/writes use service hooks.
6. Chat Room route remains thin.
7. Submission upload uses feature hook and shared Storage service.
8. Avatar uses shared Storage service.
9. Client Package panel uses real reads.
10. Fake Client review actions remain removed.
11. Client panel is gated by real Workspace success.
12. No direct Storage SDK call returned to feature code.
13. No new manual Authorization/token logic was introduced.
14. No unsupported Accept/Reject endpoint was invented.

## Required readiness matrix

Produce a readiness matrix for:

- Public Expert Profile.
- Expert listing.
- Client Jobpost list/detail.
- Expert marketplace.
- Apply Jobpost.
- Applicant list.
- Proposal summary.
- Proposal protected detail.
- Proposal payment/unlock.
- Hire.
- Contract.
- Workspace reads.
- Workspace members.
- Workspace Chat shell.
- Expert Package create.
- Expert Package resubmit.
- Client Package history.
- Client Package detail/files.
- Client Reject.
- Client Accept.
- Completion.
- Wallet/Transactions.
- Notification.
- Dispute.
- Repository build baseline.

Use only:

- READY
- READY AFTER 7B BASELINE FIX
- READY FOR READS ONLY
- FRONTEND FOUNDATION READY
- BLOCKED BY BACKEND CONTRACT
- BLOCKED BY BACKEND SECURITY
- MOCK ONLY
- RUNTIME VERIFICATION REQUIRED
- NOT AUDITED

## Refactor 7B planning requirements

Based on evidence, propose a minimal 7B fix set.

The plan must:

- Fix only build/lint baseline blockers.
- Avoid broad architecture refactors.
- Avoid business-flow implementation.
- Avoid dependency upgrades unless absolutely unavoidable.
- Keep each fix independently reviewable.
- Rank fixes by dependency and risk.
- State exact expected files.
- State whether each fix requires manual UI regression testing.
- State whether each fix can be committed independently.

A likely 7B plan may include:

- Repair invalid `/experts` route module.
- Repair Calendar compatibility with installed DayPicker.
- Remove or correct stale `configs/index.ts` export.
- Repair or isolate legacy `useUserCrud.tsx`.
- Replace explicit `any` in Expert registration.
- Repair `use-mobile` lint pattern.

Do not assume all six should be fixed together.

Use source evidence to decide.

## No-write verification

At the end:

1. Run `git diff --check`.
2. Run `git status --short`.
3. Confirm no tracked application source file changed.
4. Report generated ignored artifacts separately when relevant.
5. Do not clean or reset the repository.

## Required final output

Return a detailed Markdown report with these exact sections.

# 1. Executive Summary

State:

- Whether Refactors 1–6 are structurally closed.
- Whether major architecture boundaries are now consistent.
- Whether the repository currently type-checks.
- Whether lint passes.
- Whether production build passes.
- Whether Main Flow implementation may start before 7B.
- Highest-priority blockers.

# 2. Repository and Tooling Baseline

Report:

- Framework and package versions.
- Build/lint/typecheck scripts.
- Initial Git status.
- Final Git status.
- Generated-output considerations.

# 3. Refactor 1–6 Regression Check

Report every required regression check as PASS, FAIL, or RUNTIME UNVERIFIED.

# 4. Route Architecture Audit

Provide a table with:

- Route.
- Route file.
- Classification.
- Main issue.
- Main Flow impact.
- Recommended action.

# 5. Direct Network and Auth Audit

List every direct or suspicious API/auth occurrence outside approved boundaries.

# 6. Supabase Closure Audit

Report:

- Client initialization count.
- Direct Storage SDK call locations.
- Submission consumer path.
- Avatar consumer path.
- Credential/logging findings.
- Remaining runtime/security limitations.

# 7. Service and Type Architecture Audit

Report:

- Duplicate DTOs.
- Local API types.
- Unsafe assertions.
- Response-envelope inconsistencies.
- Hook enabled guards.
- Service-boundary violations.

# 8. Query Key and Invalidation Audit

Report actual strengths, risks, collisions, and invalidation concerns.

# 9. Identifier Safety Audit

Report confirmed safe and unsafe identifier flows.

# 10. Main Flow Mock and Placeholder Audit

Provide a table with:

- Feature.
- File.
- Mock or placeholder.
- User-visible behavior.
- Risk classification.
- Backend dependency.
- Recommended action.

# 11. Barrel Export and Import Health

Report stale exports, missing modules, possible cycles, and direct-import recommendations.

# 12. Build and TypeScript Baseline

For each error provide:

- Command.
- File.
- Error.
- Root cause.
- Classification.
- Main Flow impact.
- Minimal 7B fix.

# 13. Lint Baseline

Group errors and warnings by rule and severity.

# 14. Known Error Deep Diagnosis

Create subsections for:

- Experts base page.
- Calendar and DayPicker.
- `configs/index.ts`.
- `useUserCrud.tsx`.
- Expert registration `any`.
- `use-mobile`.

# 15. Backend Contract and Security Blockers

Group by:

- Proposal Unlock.
- Hire/Contract.
- Workspace Proposal.
- Submission Review.
- Completion/Release.
- Notification/Dispute.

# 16. Frontend Readiness Matrix

Use the required readiness classifications.

# 17. Main Flow Entry Assessment

Explain:

- Which Main Flow phase could technically start.
- Which phase should start first.
- Which phases are blocked.
- Whether starting before build baseline cleanup is advisable.

# 18. Recommended Refactor 7B Fix Set

Provide ordered, minimal, independently reviewable tasks.

For each task include:

- Goal.
- Exact files.
- Expected change.
- Risk.
- Verification.
- Suggested commit boundary.

# 19. Files Expected to Change in 7B

List exact paths grouped by task.

# 20. Manual Regression Plan

Include tests for:

- `/experts`.
- Public Expert Profile.
- Calendar consumers.
- Client/Expert registration.
- Client Jobpost routes.
- Workspace Chat Room.
- Expert Submission.
- Client Package reads.

# 21. Verification Results

Report exact results for:

- TypeScript.
- Lint.
- Build.
- Diff check.
- Git status.
- Confirmation that no source files changed.

# 22. Final Decision

Return exactly one:

- GO FOR REFACTOR 7B TARGETED BASELINE FIXES
- GO FOR MAIN FLOW
- NO-GO UNTIL REPOSITORY STRUCTURE IS REPAIRED

Explain the decision precisely.

Important:

- This is read-only diagnosis.
- Do not fix any source file.
- Do not implement Main Flow.
- Do not invent backend endpoints.
- Distinguish source-confirmed facts from inference.
- Separate architecture readiness from business-contract readiness.
- Preserve a clean Git worktree.
- You are working in my existing Next.js web application: Neuro Bridge.

This is Refactor 7A: Architecture Closure Audit and Build Baseline Diagnosis only.

This task is strictly read-only for application source files.

Do NOT modify application source files.
Do NOT create application source files.
Do NOT delete, rename, or move application source files.
Do NOT run formatter or auto-fix commands.
Do NOT run ESLint with `--fix`.
Do NOT change package versions.
Do NOT install packages.
Do NOT upgrade dependencies.
Do NOT edit generated route types manually.
Do NOT implement Main Flow business features.
Do NOT implement missing backend endpoints.
Do NOT replace remaining mocks during this audit.
Do NOT fix build or lint errors yet.

You may run build, TypeScript, lint, and read-only search commands.

Generated build output such as `.next/**` may be created by normal Next.js commands, but no tracked application source file may change.

Confirm `git status --short` before and after the audit.

## Current project

Application:

- Neuro Bridge frontend
- Next.js App Router
- TypeScript
- Tailwind
- shadcn-style UI
- React Query
- Redux
- Supabase browser Storage

Project architecture convention:

- `app/**`
  - Thin route and layout shells only
  - Route parameters
  - Render feature views

- `features/**`
  - Feature views
  - Components
  - Local UI state
  - Feature orchestration
  - Feature-specific hooks and mappings
  - Temporary mock data only when explicitly documented

- `services/**`
  - Backend API functions
  - React Query hooks
  - Query keys
  - Mutations
  - External integration boundaries

- `types/**`
  - Domain types
  - Request types
  - Response types
  - Integration contracts

- `configs/**`
  - Initialized shared clients
  - Environment configuration

## Completed refactors

The following architecture work is complete and must not be reopened without concrete evidence.

### Refactor 1 — Public Expert Profile repair

Completed:

- `/experts/[expertCode]` uses a thin route shell.
- Public profile view lives under `features/profile/expert/**`.
- Real Expert and Skill reads are used.
- Invalid code and empty-skill states were handled.
- Broken imports and local type assumptions were repaired.

Known remaining issue:

- The base `/experts` page has previously been reported as not being a valid module.

### Refactor 2 — Applicant to Expert Profile

Completed:

- Client applicant card links to the public Expert profile.
- It uses `applicant.expertApply.expertCode`.
- Missing codes do not create invalid URLs.
- Proposal Unlock remains independent.

### Proposal protected-detail preparation

Completed:

- Proposal Detail is fetched lazily by `proposalId`.
- The dialog no longer uses protected Strategy Detail from the applicant-list response.
- The frontend ignores premature `strategyDetail` fields.

Backend/security concern still open:

- Backend must not expose protected `strategyDetail` before unlock.

### Refactor 3 — Workspace foundation

Completed:

- Centralized Workspace types.
- Shared API client usage.
- Workspace Detail and Members reads.
- Stable Workspace query keys.
- No manual Authorization header in the new Workspace service.

### Refactor 4 — Submission Package foundation

Completed:

- Package list by Workspace.
- Package Detail.
- Deliverable Files by Package.
- File Types.
- Create Submission Package.
- Resubmit Submission Package.
- Scoped query invalidation.
- Chat Room feature extraction.
- Thin Chat Room route shell.

### Refactor 5 — Supabase upload consolidation

Completed:

- One canonical Supabase client in `configs/supabase.ts`.
- One direct Storage SDK boundary in `services/supabase-storage.service.ts`.
- Submission upload feature hook.
- Submission upload utilities.
- Normalized Storage errors.
- Avatar helper migrated to the shared Storage boundary.
- Unused `uploadFiles` removed.
- No direct Supabase Storage SDK call should remain outside the generic service.

Runtime Storage testing and policy verification remain deferred.

### Refactor 6 — Client Submission Review preparation

Completed:

- 6A API and frontend audit.
- 6B real Client Package read integration.
- Mock Client Package history removed.
- Mock Deliverable Files removed.
- Fake Approve/Reject behavior removed.
- Real Package list and selected Package Detail are used.
- Client panel is gated by successful Workspace access.
- Latest Package does not rely on `packages[0]`.
- Status and feedback are read-only.

Backend blockers remain:

- Package Accept endpoint.
- Package Reject endpoint.
- Feedback write contract.
- Package status machine.
- Workspace/Project completion behavior.
- `autoReleaseAt` semantics.
- Escrow/Kafka release ownership.

## Main goal

Produce a complete evidence-based closure report for Refactors 1–6 and establish the exact current build/lint baseline before Main Flow implementation begins.

The audit must answer:

1. Whether the architecture convention is now consistently followed.
2. Whether remaining violations affect the Main Flow.
3. Whether direct API, token, Supabase, or DTO duplication remains.
4. Whether misleading mock actions remain in the primary business flow.
5. Whether invalid identifiers can still trigger real requests.
6. Whether query keys and invalidations are safe enough for Main Flow work.
7. Whether current TypeScript, lint, and build failures are understood.
8. Which failures must be fixed in Refactor 7B.
9. Which warnings may remain as non-blocking debt.
10. Whether frontend architecture is ready to enter Main Flow after targeted baseline fixes.

## Strict scope

This is diagnosis only.

Do NOT:

- Fix `/experts`.
- Fix Calendar.
- Fix `configs/index.ts`.
- Fix `useUserCrud.tsx`.
- Fix lint errors.
- Replace Chat mocks.
- Replace Notification mocks.
- Add Accept/Reject.
- Add Payment Unlock.
- Add Hire/Contract.
- Add Workspace initialization.
- Add Completion.
- Add Escrow release.
- Change API response shapes.
- Change query invalidation.
- Change role logic.
- Change Supabase configuration.
- Change route structure.
- Delete dead code.
- Modify barrel exports.
- Change TypeScript configuration.
- Change ESLint configuration.
- Change Next.js configuration.

Only inspect and report.

## Required initial repository state

Before inspection:

1. Run `git status --short`.
2. Record whether the worktree is clean.
3. If tracked source changes exist, do not edit or reset them.
4. Report the exact paths and continue read-only only when the source can still be inspected safely.
5. Do not use `git checkout`, `git restore`, `git reset`, or `git clean`.

## Required configuration inspection

Inspect:

- `package.json`
- Lockfile
- `tsconfig.json`
- Next.js config
- ESLint config
- Path aliases
- React Query provider setup
- Redux provider setup
- Shared API client configuration
- `configs/supabase.ts`
- Current Next.js version
- Current React version
- Current `react-day-picker` version
- Current TypeScript version
- Current ESLint version

Determine:

- Exact build script.
- Exact lint script.
- Whether build invokes lint separately.
- Whether TypeScript incremental output may affect results.
- Whether `.next` validators are generated from a currently invalid route.
- Whether path aliases match current source layout.
- Whether known errors are caused by version mismatch, missing file, stale export, or invalid source.

Do not alter configuration.

## Audit area 1 — Route and layout convention

Inspect all application routes under:

`app/**`

Pay special attention to:

- `/experts`
- `/experts/[id]` or `/experts/[expertCode]`
- `/client/dashboard/my-job-post`
- `/client/dashboard/my-job-post/[id]`
- `/client/dashboard/create-job-post`
- `/chat-room/[workspaceId]`
- Auth routes
- Profile routes
- Available AI routes
- Client and Expert dashboard routes

Classify each route as:

- CLEAN THIN SHELL
- ACCEPTABLE ROUTE-SPECIFIC LOGIC
- FEATURE LOGIC LEAKED INTO APP
- INVALID ROUTE MODULE
- DEAD OR DUPLICATE ROUTE
- REQUIRES RUNTIME VERIFICATION

Check for:

- Large JSX trees in route files.
- Local API calls in route files.
- Local DTOs in route files.
- Mock data in route files.
- Business mutations in route files.
- Direct Supabase use in route files.
- Duplicate route implementations.
- Empty files.
- Files that export no default React component.
- Invalid route groups.
- Route links pointing to nonexistent routes.
- Dynamic parameter naming inconsistencies.
- Native Next.js page files that are not valid modules.

Do not refactor any route.

## Audit area 2 — Direct network and authorization usage

Search application source for:

- `fetch(`
- `axios`
- `.get(`
- `.post(`
- `.put(`
- `.patch(`
- `.delete(`
- `Authorization`
- `Bearer `
- `localStorage`
- `sessionStorage`
- `document.cookie`
- Manual token extraction
- Manual API base URLs
- Duplicate API client initialization
- Direct Workspace endpoint strings
- Direct Jobpost endpoint strings
- Direct Payment endpoint strings
- Direct Proposal endpoint strings
- Direct Notification endpoint strings

Ignore:

- Shared API-client implementation.
- Framework internals.
- `node_modules`.
- `.next`.
- Generated output.

For every direct request outside an approved service boundary, report:

- File.
- Operation.
- Endpoint.
- Authentication method.
- Whether it belongs under `services/**`.
- Whether it affects Main Flow.
- Severity.
- Recommended future action.

Distinguish:

- Legitimate external fetches.
- API service functions.
- Legacy direct backend calls.
- Mock-only code.
- Dead code.
- Browser asset requests.

Do not assume every occurrence of `.get` is HTTP.

## Audit area 3 — Supabase closure verification

Search for:

- `createClient(`
- `@supabase/supabase-js`
- `@/configs/supabase`
- `supabase.storage`
- `.storage.from(`
- `.upload(`
- `.getPublicUrl(`
- `.createSignedUrl(`
- `.remove(`
- `listBuckets`
- `service_role`
- Supabase key logging
- Session logging
- Token logging

Confirm:

1. `createClient` exists only in `configs/supabase.ts`.
2. Storage SDK calls exist only in `services/supabase-storage.service.ts`.
3. Submission uses the shared Storage boundary.
4. Avatar upload uses the shared Storage boundary.
5. No service-role key exists in browser source.
6. No credential/session/token logging exists.
7. No duplicate Supabase config remains.
8. No feature imports Supabase directly for Storage.

Report any exception.

Do not modify Supabase code.

## Audit area 4 — Service and type architecture

Inspect:

- `services/**`
- `types/**`
- Feature-local API helpers
- Local request/response interfaces
- Query-key declarations
- Mutation hooks
- Response unwrapping helpers
- Error normalization helpers

Search for:

- Duplicate API DTO definitions.
- Local interfaces matching centralized types.
- `any`.
- `unknown as`.
- Broad type assertions.
- Non-null assertions around API identifiers.
- Response types typed differently across consumers.
- Universal envelope assumptions.
- Direct object versus `{ code, message, result }` ambiguity.
- Hooks without enabled guards.
- Query keys missing identifiers.
- Mutation functions embedded in UI.
- API functions embedded in feature components.

For each issue classify:

- MAIN FLOW BLOCKER
- SHOULD FIX IN 7B
- SAFE TO DEFER
- INTENTIONAL FEATURE-LOCAL TYPE
- RUNTIME CONTRACT VERIFICATION NEEDED

Do not centralize types during this audit.

## Audit area 5 — Query keys and invalidation

Inspect actual current keys for:

- Jobpost.
- Applicants.
- Proposal Unlock.
- Workspace.
- Submission Packages.
- Deliverable Files.
- Payment Wallet.
- Payment Transactions.
- Notifications.
- Expert/Profile data.

Check:

- Whether query keys include required IDs.
- Whether optional IDs can create collisions.
- Whether hooks are disabled for empty IDs.
- Whether mutations invalidate only affected data.
- Whether broad root invalidation is used unnecessarily.
- Whether new Package versions invalidate old/new detail correctly.
- Whether selected Package Detail is isolated by package ID.
- Whether switching IDs can display stale data.
- Whether list and detail keys are structurally consistent.
- Whether manual query keys duplicate centralized factories.

Do not change keys.

Report exact current key structures when relevant.

## Audit area 6 — Identifier safety

Search for API calls or links using:

- `workspaceId`
- `packageId`
- `proposalId`
- `jobPostId`
- `expertCode`
- `fileId`
- `escrowId`
- `userId`
- Array indexes
- Placeholder IDs
- Empty strings
- `undefined`
- `null`

Identify:

- Hooks that may call `/undefined`.
- Links that may produce invalid routes.
- ID substitutions such as job ID used as Workspace ID.
- Package ID used as File ID.
- Array index used as domain identity.
- Missing enabled guards.
- IDs read from localStorage instead of route or real response.
- Mock IDs entering real services.

Separate:

- Confirmed safe.
- Potentially unsafe.
- Runtime-dependent.
- Already guarded by parent rendering.

## Audit area 7 — Main Flow mock and placeholder audit

Search primary Client–Expert flow for:

- `mock`
- `fake`
- `demo`
- `placeholder`
- `TODO`
- `FIXME`
- `setTimeout`
- Local action success
- Static status transitions
- Hardcoded Package/Proposal/Workspace data
- Hardcoded wallet values
- Fake Contract or Hire state
- Buttons that only mutate local state
- Buttons that navigate without a real backend action
- Toasts claiming success without a mutation

Inspect at minimum:

- Client Jobpost pages.
- Expert marketplace/apply flow.
- Applicants.
- Proposal detail/unlock.
- Hire.
- Contract.
- Workspace Chat Room.
- Submission.
- Client review.
- Payment/Wallet.
- Notification.
- Dispute.
- Completion.

Classify every relevant mock as:

- SAFE PRESENTATIONAL MOCK
- KNOWN TEMPORARY MOCK
- MISLEADING BUSINESS ACTION
- MAIN FLOW BLOCKER
- DEAD CODE
- OUT OF SCOPE

Pay special attention to:

- Chat messages.
- Notification data.
- Hire actions.
- Payment unlock.
- Client Submission actions.
- Completion actions.
- Wallet balances.
- Success toasts.

Do not remove mocks.

## Audit area 8 — Barrel exports and import health

Inspect:

- `features/**/index.ts`
- `services` barrels if any.
- `types` barrels if any.
- `configs/index.ts`
- Module aliases.
- Re-export chains.

Search for:

- Exports pointing to missing files.
- Imports from deleted modules.
- Circular-looking barrel chains.
- Route imports that depend on broken barrels.
- Duplicate export names.
- Default/named export mismatch.
- Case-sensitive path mismatch.
- Legacy `modules/**` imports.
- References to removed components.
- Dead barrels used only by invalid code.

Do not rewrite barrels.

Report whether direct imports are safer for each known failure.

## Audit area 9 — Known TypeScript and build errors

The following errors have repeatedly appeared and must be diagnosed precisely.

### Error A — Experts base page

Known symptom:

`app/(main)/experts/page.tsx` is not a module.

Inspect:

- Exact file content.
- Whether it is empty.
- Whether it has invalid commented-out content.
- Whether it exports a default component.
- Whether a valid Expert listing feature already exists elsewhere.
- Whether links currently target `/experts`.
- Whether repairing this page is required before Main Flow.
- Smallest safe future fix.

Do not implement the fix.

### Error B — Calendar / react-day-picker

Known symptom:

`components/ui/calendar.tsx` uses an invalid `table` property in DayPicker class names or related incompatible API.

Inspect:

- Installed `react-day-picker` version.
- Current Calendar component implementation.
- Correct class-name keys for the installed version.
- Whether the component was generated for another version.
- Every Calendar consumer.
- Whether a minimal compatibility fix is possible.
- Whether UI behavior may change.

Use installed package types and official local type declarations as evidence.

Do not use web search unless explicitly needed.

Do not upgrade `react-day-picker`.

Do not implement the fix.

### Error C — Broken config barrel import

Known symptom:

`configs/index.ts` references:

`@/modules/users/forms/UserForm`

Inspect:

- Exact import/export.
- Whether the referenced file exists.
- Whether the path is stale.
- Whether the export is used anywhere.
- Whether `modules/users/**` is legacy architecture.
- Whether removing the export or correcting the path is safer.
- Whether another `UserForm` exists.
- Main Flow impact.

Do not change the file.

### Error D — Missing UserSchema

Known symptom:

`modules/users/useUserCrud.tsx` references missing `UserSchema`.

Inspect:

- Current imports.
- Existing user schemas.
- Whether this module is used.
- Whether it belongs to legacy Admin CRUD.
- Whether the missing symbol was renamed.
- Whether the module can be excluded or must compile.
- Smallest safe future fix.
- Main Flow relevance.

Do not fix it.

### Error E — Expert registration `any`

Known lint symptom:

`features/auth/register/ExpertRegisterForm.tsx` contains unexpected `any`, previously reported around line 88.

Inspect:

- Exact variable or callback.
- Correct available type.
- Whether a local type exists.
- Whether the fix changes runtime behavior.
- Whether it is safe for 7B.

Do not fix it.

### Error F — `use-mobile` state update in effect

Known lint symptom:

`hooks/use-mobile.ts` calls state setter in an effect in a way rejected by current lint rules.

Inspect:

- Exact hook implementation.
- Current lint rule.
- Consumer count.
- Whether it causes runtime hydration issues.
- Whether the common `matchMedia` lazy-state pattern can fix it.
- Whether it affects Main Flow.
- Smallest safe future fix.

Do not implement it.

## Audit area 10 — Warning baseline

Record lint warnings grouped by rule and path.

Pay attention to:

- `<img>` warnings.
- Missing hook dependencies.
- Unused imports.
- Explicit `any`.
- Accessibility.
- React Compiler-related warnings.
- Next.js image optimization.
- Deprecated APIs.
- Console statements.

Classify warnings:

- MUST FIX BEFORE MAIN FLOW
- SHOULD FIX IN 7B
- SAFE TO DEFER
- FALSE POSITIVE OR CONVENTION DECISION

Do not attempt to remove all warnings.

The goal is a stable baseline, not warning-zero at any cost.

## Audit area 11 — Main Flow backend blockers

Consolidate currently known backend blockers.

At minimum inspect frontend evidence for:

### Proposal purchase/unlock

- Missing atomic purchase/unlock endpoint.
- Protected Strategy Detail exposure.
- Wallet insufficiency response.
- Already-unlocked behavior.
- Required invalidations.

### Hire and Contract

- Negotiated amount request field.
- Create Contract/Escrow.
- Finalize after signatures/funding.
- Retry behavior.
- Idempotency.
- Workspace creation trigger.

### Workspace Proposal

- Proposal creation or association endpoint.
- Proposal source of truth after Hire.

### Submission review

- Package Reject.
- Feedback body.
- Package Accept.
- Status transitions.
- Completion behavior.
- Escrow release event.
- `autoReleaseAt`.

### Notification and Dispute

- Real notification contract.
- Dispute transitions.
- Completion/release interactions.

For each blocker classify:

- FRONTEND READY
- FRONTEND PREPARATION POSSIBLE
- BACKEND CONTRACT REQUIRED
- BACKEND SECURITY FIX REQUIRED
- RUNTIME VERIFICATION REQUIRED

Do not design speculative frontend mutations.

## Required commands

Run these commands and capture exact results.

1. `git status --short`
2. `npx tsc --noEmit --incremental false --pretty false`
3. `npm run lint -- --no-cache`
4. `npm run build`
5. `git diff --check`
6. `git status --short`

If the build command fails:

- Record the first root-cause error.
- Continue source inspection.
- Do not repeatedly rerun the same failing command without new evidence.
- Do not edit files.

If `.next` generated output affects TypeScript error reporting:

- Explain the relationship.
- Do not manually edit `.next`.
- Do not delete tracked source.
- You may inspect generated validator paths read-only.

Do not use destructive Git commands.

## Build baseline classification

For every TypeScript, lint, or build failure, report:

- Command.
- File.
- Line.
- Error code or lint rule.
- Root cause.
- Whether it is pre-existing.
- Whether it was introduced by Refactors 1–6.
- Main Flow impact.
- Minimal future fix.
- Expected files for 7B.
- Risk of fixing.

Classify each failure:

- BASELINE BLOCKER
- MAIN FLOW BLOCKER
- ROUTE-SPECIFIC BLOCKER
- LINT-ONLY BLOCKER
- NON-BLOCKING WARNING
- GENERATED CONSEQUENCE OF SOURCE ERROR

Do not merely repeat compiler output.

## Refactor regression check

Verify Refactors 1–6 did not introduce obvious architecture regressions.

Confirm or refute:

1. Public Expert Profile route remains thin.
2. Applicant link still uses `expertCode`.
3. Proposal Detail remains lazy by `proposalId`.
4. Workspace services use shared API client.
5. Submission reads/writes use service hooks.
6. Chat Room route remains thin.
7. Submission upload uses feature hook and shared Storage service.
8. Avatar uses shared Storage service.
9. Client Package panel uses real reads.
10. Fake Client review actions remain removed.
11. Client panel is gated by real Workspace success.
12. No direct Storage SDK call returned to feature code.
13. No new manual Authorization/token logic was introduced.
14. No unsupported Accept/Reject endpoint was invented.

## Required readiness matrix

Produce a readiness matrix for:

- Public Expert Profile.
- Expert listing.
- Client Jobpost list/detail.
- Expert marketplace.
- Apply Jobpost.
- Applicant list.
- Proposal summary.
- Proposal protected detail.
- Proposal payment/unlock.
- Hire.
- Contract.
- Workspace reads.
- Workspace members.
- Workspace Chat shell.
- Expert Package create.
- Expert Package resubmit.
- Client Package history.
- Client Package detail/files.
- Client Reject.
- Client Accept.
- Completion.
- Wallet/Transactions.
- Notification.
- Dispute.
- Repository build baseline.

Use only:

- READY
- READY AFTER 7B BASELINE FIX
- READY FOR READS ONLY
- FRONTEND FOUNDATION READY
- BLOCKED BY BACKEND CONTRACT
- BLOCKED BY BACKEND SECURITY
- MOCK ONLY
- RUNTIME VERIFICATION REQUIRED
- NOT AUDITED

## Refactor 7B planning requirements

Based on evidence, propose a minimal 7B fix set.

The plan must:

- Fix only build/lint baseline blockers.
- Avoid broad architecture refactors.
- Avoid business-flow implementation.
- Avoid dependency upgrades unless absolutely unavoidable.
- Keep each fix independently reviewable.
- Rank fixes by dependency and risk.
- State exact expected files.
- State whether each fix requires manual UI regression testing.
- State whether each fix can be committed independently.

A likely 7B plan may include:

- Repair invalid `/experts` route module.
- Repair Calendar compatibility with installed DayPicker.
- Remove or correct stale `configs/index.ts` export.
- Repair or isolate legacy `useUserCrud.tsx`.
- Replace explicit `any` in Expert registration.
- Repair `use-mobile` lint pattern.

Do not assume all six should be fixed together.

Use source evidence to decide.

## No-write verification

At the end:

1. Run `git diff --check`.
2. Run `git status --short`.
3. Confirm no tracked application source file changed.
4. Report generated ignored artifacts separately when relevant.
5. Do not clean or reset the repository.

## Required final output

Return a detailed Markdown report with these exact sections.

# 1. Executive Summary

State:

- Whether Refactors 1–6 are structurally closed.
- Whether major architecture boundaries are now consistent.
- Whether the repository currently type-checks.
- Whether lint passes.
- Whether production build passes.
- Whether Main Flow implementation may start before 7B.
- Highest-priority blockers.

# 2. Repository and Tooling Baseline

Report:

- Framework and package versions.
- Build/lint/typecheck scripts.
- Initial Git status.
- Final Git status.
- Generated-output considerations.

# 3. Refactor 1–6 Regression Check

Report every required regression check as PASS, FAIL, or RUNTIME UNVERIFIED.

# 4. Route Architecture Audit

Provide a table with:

- Route.
- Route file.
- Classification.
- Main issue.
- Main Flow impact.
- Recommended action.

# 5. Direct Network and Auth Audit

List every direct or suspicious API/auth occurrence outside approved boundaries.

# 6. Supabase Closure Audit

Report:

- Client initialization count.
- Direct Storage SDK call locations.
- Submission consumer path.
- Avatar consumer path.
- Credential/logging findings.
- Remaining runtime/security limitations.

# 7. Service and Type Architecture Audit

Report:

- Duplicate DTOs.
- Local API types.
- Unsafe assertions.
- Response-envelope inconsistencies.
- Hook enabled guards.
- Service-boundary violations.

# 8. Query Key and Invalidation Audit

Report actual strengths, risks, collisions, and invalidation concerns.

# 9. Identifier Safety Audit

Report confirmed safe and unsafe identifier flows.

# 10. Main Flow Mock and Placeholder Audit

Provide a table with:

- Feature.
- File.
- Mock or placeholder.
- User-visible behavior.
- Risk classification.
- Backend dependency.
- Recommended action.

# 11. Barrel Export and Import Health

Report stale exports, missing modules, possible cycles, and direct-import recommendations.

# 12. Build and TypeScript Baseline

For each error provide:

- Command.
- File.
- Error.
- Root cause.
- Classification.
- Main Flow impact.
- Minimal 7B fix.

# 13. Lint Baseline

Group errors and warnings by rule and severity.

# 14. Known Error Deep Diagnosis

Create subsections for:

- Experts base page.
- Calendar and DayPicker.
- `configs/index.ts`.
- `useUserCrud.tsx`.
- Expert registration `any`.
- `use-mobile`.

# 15. Backend Contract and Security Blockers

Group by:

- Proposal Unlock.
- Hire/Contract.
- Workspace Proposal.
- Submission Review.
- Completion/Release.
- Notification/Dispute.

# 16. Frontend Readiness Matrix

Use the required readiness classifications.

# 17. Main Flow Entry Assessment

Explain:

- Which Main Flow phase could technically start.
- Which phase should start first.
- Which phases are blocked.
- Whether starting before build baseline cleanup is advisable.

# 18. Recommended Refactor 7B Fix Set

Provide ordered, minimal, independently reviewable tasks.

For each task include:

- Goal.
- Exact files.
- Expected change.
- Risk.
- Verification.
- Suggested commit boundary.

# 19. Files Expected to Change in 7B

List exact paths grouped by task.

# 20. Manual Regression Plan

Include tests for:

- `/experts`.
- Public Expert Profile.
- Calendar consumers.
- Client/Expert registration.
- Client Jobpost routes.
- Workspace Chat Room.
- Expert Submission.
- Client Package reads.

# 21. Verification Results

Report exact results for:

- TypeScript.
- Lint.
- Build.
- Diff check.
- Git status.
- Confirmation that no source files changed.

# 22. Final Decision

Return exactly one:

- GO FOR REFACTOR 7B TARGETED BASELINE FIXES
- GO FOR MAIN FLOW
- NO-GO UNTIL REPOSITORY STRUCTURE IS REPAIRED

Explain the decision precisely.

Important:

- This is read-only diagnosis.
- Do not fix any source file.
- Do not implement Main Flow.
- Do not invent backend endpoints.
- Distinguish source-confirmed facts from inference.
- Separate architecture readiness from business-contract readiness.
- Preserve a clean Git worktree.

You are working in my existing Next.js web application: Neuro Bridge.

This is Refactor 7A: Architecture Closure Audit and Build Baseline Diagnosis only.

This task is strictly read-only for application source files.

Do NOT modify application source files.
Do NOT create application source files.
Do NOT delete, rename, or move application source files.
Do NOT run formatter or auto-fix commands.
Do NOT run ESLint with `--fix`.
Do NOT change package versions.
Do NOT install packages.
Do NOT upgrade dependencies.
Do NOT edit generated route types manually.
Do NOT implement Main Flow business features.
Do NOT implement missing backend endpoints.
Do NOT replace remaining mocks during this audit.
Do NOT fix build or lint errors yet.

You may run build, TypeScript, lint, and read-only search commands.

Generated build output such as `.next/**` may be created by normal Next.js commands, but no tracked application source file may change.

Confirm `git status --short` before and after the audit.

## Current project

Application:

- Neuro Bridge frontend
- Next.js App Router
- TypeScript
- Tailwind
- shadcn-style UI
- React Query
- Redux
- Supabase browser Storage

Project architecture convention:

- `app/**`
  - Thin route and layout shells only
  - Route parameters
  - Render feature views

- `features/**`
  - Feature views
  - Components
  - Local UI state
  - Feature orchestration
  - Feature-specific hooks and mappings
  - Temporary mock data only when explicitly documented

- `services/**`
  - Backend API functions
  - React Query hooks
  - Query keys
  - Mutations
  - External integration boundaries

- `types/**`
  - Domain types
  - Request types
  - Response types
  - Integration contracts

- `configs/**`
  - Initialized shared clients
  - Environment configuration

## Completed refactors

The following architecture work is complete and must not be reopened without concrete evidence.

### Refactor 1 — Public Expert Profile repair

Completed:

- `/experts/[expertCode]` uses a thin route shell.
- Public profile view lives under `features/profile/expert/**`.
- Real Expert and Skill reads are used.
- Invalid code and empty-skill states were handled.
- Broken imports and local type assumptions were repaired.

Known remaining issue:

- The base `/experts` page has previously been reported as not being a valid module.

### Refactor 2 — Applicant to Expert Profile

Completed:

- Client applicant card links to the public Expert profile.
- It uses `applicant.expertApply.expertCode`.
- Missing codes do not create invalid URLs.
- Proposal Unlock remains independent.

### Proposal protected-detail preparation

Completed:

- Proposal Detail is fetched lazily by `proposalId`.
- The dialog no longer uses protected Strategy Detail from the applicant-list response.
- The frontend ignores premature `strategyDetail` fields.

Backend/security concern still open:

- Backend must not expose protected `strategyDetail` before unlock.

### Refactor 3 — Workspace foundation

Completed:

- Centralized Workspace types.
- Shared API client usage.
- Workspace Detail and Members reads.
- Stable Workspace query keys.
- No manual Authorization header in the new Workspace service.

### Refactor 4 — Submission Package foundation

Completed:

- Package list by Workspace.
- Package Detail.
- Deliverable Files by Package.
- File Types.
- Create Submission Package.
- Resubmit Submission Package.
- Scoped query invalidation.
- Chat Room feature extraction.
- Thin Chat Room route shell.

### Refactor 5 — Supabase upload consolidation

Completed:

- One canonical Supabase client in `configs/supabase.ts`.
- One direct Storage SDK boundary in `services/supabase-storage.service.ts`.
- Submission upload feature hook.
- Submission upload utilities.
- Normalized Storage errors.
- Avatar helper migrated to the shared Storage boundary.
- Unused `uploadFiles` removed.
- No direct Supabase Storage SDK call should remain outside the generic service.

Runtime Storage testing and policy verification remain deferred.

### Refactor 6 — Client Submission Review preparation

Completed:

- 6A API and frontend audit.
- 6B real Client Package read integration.
- Mock Client Package history removed.
- Mock Deliverable Files removed.
- Fake Approve/Reject behavior removed.
- Real Package list and selected Package Detail are used.
- Client panel is gated by successful Workspace access.
- Latest Package does not rely on `packages[0]`.
- Status and feedback are read-only.

Backend blockers remain:

- Package Accept endpoint.
- Package Reject endpoint.
- Feedback write contract.
- Package status machine.
- Workspace/Project completion behavior.
- `autoReleaseAt` semantics.
- Escrow/Kafka release ownership.

## Main goal

Produce a complete evidence-based closure report for Refactors 1–6 and establish the exact current build/lint baseline before Main Flow implementation begins.

The audit must answer:

1. Whether the architecture convention is now consistently followed.
2. Whether remaining violations affect the Main Flow.
3. Whether direct API, token, Supabase, or DTO duplication remains.
4. Whether misleading mock actions remain in the primary business flow.
5. Whether invalid identifiers can still trigger real requests.
6. Whether query keys and invalidations are safe enough for Main Flow work.
7. Whether current TypeScript, lint, and build failures are understood.
8. Which failures must be fixed in Refactor 7B.
9. Which warnings may remain as non-blocking debt.
10. Whether frontend architecture is ready to enter Main Flow after targeted baseline fixes.

## Strict scope

This is diagnosis only.

Do NOT:

- Fix `/experts`.
- Fix Calendar.
- Fix `configs/index.ts`.
- Fix `useUserCrud.tsx`.
- Fix lint errors.
- Replace Chat mocks.
- Replace Notification mocks.
- Add Accept/Reject.
- Add Payment Unlock.
- Add Hire/Contract.
- Add Workspace initialization.
- Add Completion.
- Add Escrow release.
- Change API response shapes.
- Change query invalidation.
- Change role logic.
- Change Supabase configuration.
- Change route structure.
- Delete dead code.
- Modify barrel exports.
- Change TypeScript configuration.
- Change ESLint configuration.
- Change Next.js configuration.

Only inspect and report.

## Required initial repository state

Before inspection:

1. Run `git status --short`.
2. Record whether the worktree is clean.
3. If tracked source changes exist, do not edit or reset them.
4. Report the exact paths and continue read-only only when the source can still be inspected safely.
5. Do not use `git checkout`, `git restore`, `git reset`, or `git clean`.

## Required configuration inspection

Inspect:

- `package.json`
- Lockfile
- `tsconfig.json`
- Next.js config
- ESLint config
- Path aliases
- React Query provider setup
- Redux provider setup
- Shared API client configuration
- `configs/supabase.ts`
- Current Next.js version
- Current React version
- Current `react-day-picker` version
- Current TypeScript version
- Current ESLint version

Determine:

- Exact build script.
- Exact lint script.
- Whether build invokes lint separately.
- Whether TypeScript incremental output may affect results.
- Whether `.next` validators are generated from a currently invalid route.
- Whether path aliases match current source layout.
- Whether known errors are caused by version mismatch, missing file, stale export, or invalid source.

Do not alter configuration.

## Audit area 1 — Route and layout convention

Inspect all application routes under:

`app/**`

Pay special attention to:

- `/experts`
- `/experts/[id]` or `/experts/[expertCode]`
- `/client/dashboard/my-job-post`
- `/client/dashboard/my-job-post/[id]`
- `/client/dashboard/create-job-post`
- `/chat-room/[workspaceId]`
- Auth routes
- Profile routes
- Available AI routes
- Client and Expert dashboard routes

Classify each route as:

- CLEAN THIN SHELL
- ACCEPTABLE ROUTE-SPECIFIC LOGIC
- FEATURE LOGIC LEAKED INTO APP
- INVALID ROUTE MODULE
- DEAD OR DUPLICATE ROUTE
- REQUIRES RUNTIME VERIFICATION

Check for:

- Large JSX trees in route files.
- Local API calls in route files.
- Local DTOs in route files.
- Mock data in route files.
- Business mutations in route files.
- Direct Supabase use in route files.
- Duplicate route implementations.
- Empty files.
- Files that export no default React component.
- Invalid route groups.
- Route links pointing to nonexistent routes.
- Dynamic parameter naming inconsistencies.
- Native Next.js page files that are not valid modules.

Do not refactor any route.

## Audit area 2 — Direct network and authorization usage

Search application source for:

- `fetch(`
- `axios`
- `.get(`
- `.post(`
- `.put(`
- `.patch(`
- `.delete(`
- `Authorization`
- `Bearer `
- `localStorage`
- `sessionStorage`
- `document.cookie`
- Manual token extraction
- Manual API base URLs
- Duplicate API client initialization
- Direct Workspace endpoint strings
- Direct Jobpost endpoint strings
- Direct Payment endpoint strings
- Direct Proposal endpoint strings
- Direct Notification endpoint strings

Ignore:

- Shared API-client implementation.
- Framework internals.
- `node_modules`.
- `.next`.
- Generated output.

For every direct request outside an approved service boundary, report:

- File.
- Operation.
- Endpoint.
- Authentication method.
- Whether it belongs under `services/**`.
- Whether it affects Main Flow.
- Severity.
- Recommended future action.

Distinguish:

- Legitimate external fetches.
- API service functions.
- Legacy direct backend calls.
- Mock-only code.
- Dead code.
- Browser asset requests.

Do not assume every occurrence of `.get` is HTTP.

## Audit area 3 — Supabase closure verification

Search for:

- `createClient(`
- `@supabase/supabase-js`
- `@/configs/supabase`
- `supabase.storage`
- `.storage.from(`
- `.upload(`
- `.getPublicUrl(`
- `.createSignedUrl(`
- `.remove(`
- `listBuckets`
- `service_role`
- Supabase key logging
- Session logging
- Token logging

Confirm:

1. `createClient` exists only in `configs/supabase.ts`.
2. Storage SDK calls exist only in `services/supabase-storage.service.ts`.
3. Submission uses the shared Storage boundary.
4. Avatar upload uses the shared Storage boundary.
5. No service-role key exists in browser source.
6. No credential/session/token logging exists.
7. No duplicate Supabase config remains.
8. No feature imports Supabase directly for Storage.

Report any exception.

Do not modify Supabase code.

## Audit area 4 — Service and type architecture

Inspect:

- `services/**`
- `types/**`
- Feature-local API helpers
- Local request/response interfaces
- Query-key declarations
- Mutation hooks
- Response unwrapping helpers
- Error normalization helpers

Search for:

- Duplicate API DTO definitions.
- Local interfaces matching centralized types.
- `any`.
- `unknown as`.
- Broad type assertions.
- Non-null assertions around API identifiers.
- Response types typed differently across consumers.
- Universal envelope assumptions.
- Direct object versus `{ code, message, result }` ambiguity.
- Hooks without enabled guards.
- Query keys missing identifiers.
- Mutation functions embedded in UI.
- API functions embedded in feature components.

For each issue classify:

- MAIN FLOW BLOCKER
- SHOULD FIX IN 7B
- SAFE TO DEFER
- INTENTIONAL FEATURE-LOCAL TYPE
- RUNTIME CONTRACT VERIFICATION NEEDED

Do not centralize types during this audit.

## Audit area 5 — Query keys and invalidation

Inspect actual current keys for:

- Jobpost.
- Applicants.
- Proposal Unlock.
- Workspace.
- Submission Packages.
- Deliverable Files.
- Payment Wallet.
- Payment Transactions.
- Notifications.
- Expert/Profile data.

Check:

- Whether query keys include required IDs.
- Whether optional IDs can create collisions.
- Whether hooks are disabled for empty IDs.
- Whether mutations invalidate only affected data.
- Whether broad root invalidation is used unnecessarily.
- Whether new Package versions invalidate old/new detail correctly.
- Whether selected Package Detail is isolated by package ID.
- Whether switching IDs can display stale data.
- Whether list and detail keys are structurally consistent.
- Whether manual query keys duplicate centralized factories.

Do not change keys.

Report exact current key structures when relevant.

## Audit area 6 — Identifier safety

Search for API calls or links using:

- `workspaceId`
- `packageId`
- `proposalId`
- `jobPostId`
- `expertCode`
- `fileId`
- `escrowId`
- `userId`
- Array indexes
- Placeholder IDs
- Empty strings
- `undefined`
- `null`

Identify:

- Hooks that may call `/undefined`.
- Links that may produce invalid routes.
- ID substitutions such as job ID used as Workspace ID.
- Package ID used as File ID.
- Array index used as domain identity.
- Missing enabled guards.
- IDs read from localStorage instead of route or real response.
- Mock IDs entering real services.

Separate:

- Confirmed safe.
- Potentially unsafe.
- Runtime-dependent.
- Already guarded by parent rendering.

## Audit area 7 — Main Flow mock and placeholder audit

Search primary Client–Expert flow for:

- `mock`
- `fake`
- `demo`
- `placeholder`
- `TODO`
- `FIXME`
- `setTimeout`
- Local action success
- Static status transitions
- Hardcoded Package/Proposal/Workspace data
- Hardcoded wallet values
- Fake Contract or Hire state
- Buttons that only mutate local state
- Buttons that navigate without a real backend action
- Toasts claiming success without a mutation

Inspect at minimum:

- Client Jobpost pages.
- Expert marketplace/apply flow.
- Applicants.
- Proposal detail/unlock.
- Hire.
- Contract.
- Workspace Chat Room.
- Submission.
- Client review.
- Payment/Wallet.
- Notification.
- Dispute.
- Completion.

Classify every relevant mock as:

- SAFE PRESENTATIONAL MOCK
- KNOWN TEMPORARY MOCK
- MISLEADING BUSINESS ACTION
- MAIN FLOW BLOCKER
- DEAD CODE
- OUT OF SCOPE

Pay special attention to:

- Chat messages.
- Notification data.
- Hire actions.
- Payment unlock.
- Client Submission actions.
- Completion actions.
- Wallet balances.
- Success toasts.

Do not remove mocks.

## Audit area 8 — Barrel exports and import health

Inspect:

- `features/**/index.ts`
- `services` barrels if any.
- `types` barrels if any.
- `configs/index.ts`
- Module aliases.
- Re-export chains.

Search for:

- Exports pointing to missing files.
- Imports from deleted modules.
- Circular-looking barrel chains.
- Route imports that depend on broken barrels.
- Duplicate export names.
- Default/named export mismatch.
- Case-sensitive path mismatch.
- Legacy `modules/**` imports.
- References to removed components.
- Dead barrels used only by invalid code.

Do not rewrite barrels.

Report whether direct imports are safer for each known failure.

## Audit area 9 — Known TypeScript and build errors

The following errors have repeatedly appeared and must be diagnosed precisely.

### Error A — Experts base page

Known symptom:

`app/(main)/experts/page.tsx` is not a module.

Inspect:

- Exact file content.
- Whether it is empty.
- Whether it has invalid commented-out content.
- Whether it exports a default component.
- Whether a valid Expert listing feature already exists elsewhere.
- Whether links currently target `/experts`.
- Whether repairing this page is required before Main Flow.
- Smallest safe future fix.

Do not implement the fix.

### Error B — Calendar / react-day-picker

Known symptom:

`components/ui/calendar.tsx` uses an invalid `table` property in DayPicker class names or related incompatible API.

Inspect:

- Installed `react-day-picker` version.
- Current Calendar component implementation.
- Correct class-name keys for the installed version.
- Whether the component was generated for another version.
- Every Calendar consumer.
- Whether a minimal compatibility fix is possible.
- Whether UI behavior may change.

Use installed package types and official local type declarations as evidence.

Do not use web search unless explicitly needed.

Do not upgrade `react-day-picker`.

Do not implement the fix.

### Error C — Broken config barrel import

Known symptom:

`configs/index.ts` references:

`@/modules/users/forms/UserForm`

Inspect:

- Exact import/export.
- Whether the referenced file exists.
- Whether the path is stale.
- Whether the export is used anywhere.
- Whether `modules/users/**` is legacy architecture.
- Whether removing the export or correcting the path is safer.
- Whether another `UserForm` exists.
- Main Flow impact.

Do not change the file.

### Error D — Missing UserSchema

Known symptom:

`modules/users/useUserCrud.tsx` references missing `UserSchema`.

Inspect:

- Current imports.
- Existing user schemas.
- Whether this module is used.
- Whether it belongs to legacy Admin CRUD.
- Whether the missing symbol was renamed.
- Whether the module can be excluded or must compile.
- Smallest safe future fix.
- Main Flow relevance.

Do not fix it.

### Error E — Expert registration `any`

Known lint symptom:

`features/auth/register/ExpertRegisterForm.tsx` contains unexpected `any`, previously reported around line 88.

Inspect:

- Exact variable or callback.
- Correct available type.
- Whether a local type exists.
- Whether the fix changes runtime behavior.
- Whether it is safe for 7B.

Do not fix it.

### Error F — `use-mobile` state update in effect

Known lint symptom:

`hooks/use-mobile.ts` calls state setter in an effect in a way rejected by current lint rules.

Inspect:

- Exact hook implementation.
- Current lint rule.
- Consumer count.
- Whether it causes runtime hydration issues.
- Whether the common `matchMedia` lazy-state pattern can fix it.
- Whether it affects Main Flow.
- Smallest safe future fix.

Do not implement it.

## Audit area 10 — Warning baseline

Record lint warnings grouped by rule and path.

Pay attention to:

- `<img>` warnings.
- Missing hook dependencies.
- Unused imports.
- Explicit `any`.
- Accessibility.
- React Compiler-related warnings.
- Next.js image optimization.
- Deprecated APIs.
- Console statements.

Classify warnings:

- MUST FIX BEFORE MAIN FLOW
- SHOULD FIX IN 7B
- SAFE TO DEFER
- FALSE POSITIVE OR CONVENTION DECISION

Do not attempt to remove all warnings.

The goal is a stable baseline, not warning-zero at any cost.

## Audit area 11 — Main Flow backend blockers

Consolidate currently known backend blockers.

At minimum inspect frontend evidence for:

### Proposal purchase/unlock

- Missing atomic purchase/unlock endpoint.
- Protected Strategy Detail exposure.
- Wallet insufficiency response.
- Already-unlocked behavior.
- Required invalidations.

### Hire and Contract

- Negotiated amount request field.
- Create Contract/Escrow.
- Finalize after signatures/funding.
- Retry behavior.
- Idempotency.
- Workspace creation trigger.

### Workspace Proposal

- Proposal creation or association endpoint.
- Proposal source of truth after Hire.

### Submission review

- Package Reject.
- Feedback body.
- Package Accept.
- Status transitions.
- Completion behavior.
- Escrow release event.
- `autoReleaseAt`.

### Notification and Dispute

- Real notification contract.
- Dispute transitions.
- Completion/release interactions.

For each blocker classify:

- FRONTEND READY
- FRONTEND PREPARATION POSSIBLE
- BACKEND CONTRACT REQUIRED
- BACKEND SECURITY FIX REQUIRED
- RUNTIME VERIFICATION REQUIRED

Do not design speculative frontend mutations.

## Required commands

Run these commands and capture exact results.

1. `git status --short`
2. `npx tsc --noEmit --incremental false --pretty false`
3. `npm run lint -- --no-cache`
4. `npm run build`
5. `git diff --check`
6. `git status --short`

If the build command fails:

- Record the first root-cause error.
- Continue source inspection.
- Do not repeatedly rerun the same failing command without new evidence.
- Do not edit files.

If `.next` generated output affects TypeScript error reporting:

- Explain the relationship.
- Do not manually edit `.next`.
- Do not delete tracked source.
- You may inspect generated validator paths read-only.

Do not use destructive Git commands.

## Build baseline classification

For every TypeScript, lint, or build failure, report:

- Command.
- File.
- Line.
- Error code or lint rule.
- Root cause.
- Whether it is pre-existing.
- Whether it was introduced by Refactors 1–6.
- Main Flow impact.
- Minimal future fix.
- Expected files for 7B.
- Risk of fixing.

Classify each failure:

- BASELINE BLOCKER
- MAIN FLOW BLOCKER
- ROUTE-SPECIFIC BLOCKER
- LINT-ONLY BLOCKER
- NON-BLOCKING WARNING
- GENERATED CONSEQUENCE OF SOURCE ERROR

Do not merely repeat compiler output.

## Refactor regression check

Verify Refactors 1–6 did not introduce obvious architecture regressions.

Confirm or refute:

1. Public Expert Profile route remains thin.
2. Applicant link still uses `expertCode`.
3. Proposal Detail remains lazy by `proposalId`.
4. Workspace services use shared API client.
5. Submission reads/writes use service hooks.
6. Chat Room route remains thin.
7. Submission upload uses feature hook and shared Storage service.
8. Avatar uses shared Storage service.
9. Client Package panel uses real reads.
10. Fake Client review actions remain removed.
11. Client panel is gated by real Workspace success.
12. No direct Storage SDK call returned to feature code.
13. No new manual Authorization/token logic was introduced.
14. No unsupported Accept/Reject endpoint was invented.

## Required readiness matrix

Produce a readiness matrix for:

- Public Expert Profile.
- Expert listing.
- Client Jobpost list/detail.
- Expert marketplace.
- Apply Jobpost.
- Applicant list.
- Proposal summary.
- Proposal protected detail.
- Proposal payment/unlock.
- Hire.
- Contract.
- Workspace reads.
- Workspace members.
- Workspace Chat shell.
- Expert Package create.
- Expert Package resubmit.
- Client Package history.
- Client Package detail/files.
- Client Reject.
- Client Accept.
- Completion.
- Wallet/Transactions.
- Notification.
- Dispute.
- Repository build baseline.

Use only:

- READY
- READY AFTER 7B BASELINE FIX
- READY FOR READS ONLY
- FRONTEND FOUNDATION READY
- BLOCKED BY BACKEND CONTRACT
- BLOCKED BY BACKEND SECURITY
- MOCK ONLY
- RUNTIME VERIFICATION REQUIRED
- NOT AUDITED

## Refactor 7B planning requirements

Based on evidence, propose a minimal 7B fix set.

The plan must:

- Fix only build/lint baseline blockers.
- Avoid broad architecture refactors.
- Avoid business-flow implementation.
- Avoid dependency upgrades unless absolutely unavoidable.
- Keep each fix independently reviewable.
- Rank fixes by dependency and risk.
- State exact expected files.
- State whether each fix requires manual UI regression testing.
- State whether each fix can be committed independently.

A likely 7B plan may include:

- Repair invalid `/experts` route module.
- Repair Calendar compatibility with installed DayPicker.
- Remove or correct stale `configs/index.ts` export.
- Repair or isolate legacy `useUserCrud.tsx`.
- Replace explicit `any` in Expert registration.
- Repair `use-mobile` lint pattern.

Do not assume all six should be fixed together.

Use source evidence to decide.

## No-write verification

At the end:

1. Run `git diff --check`.
2. Run `git status --short`.
3. Confirm no tracked application source file changed.
4. Report generated ignored artifacts separately when relevant.
5. Do not clean or reset the repository.

## Required final output

Return a detailed Markdown report with these exact sections.

# 1. Executive Summary

State:

- Whether Refactors 1–6 are structurally closed.
- Whether major architecture boundaries are now consistent.
- Whether the repository currently type-checks.
- Whether lint passes.
- Whether production build passes.
- Whether Main Flow implementation may start before 7B.
- Highest-priority blockers.

# 2. Repository and Tooling Baseline

Report:

- Framework and package versions.
- Build/lint/typecheck scripts.
- Initial Git status.
- Final Git status.
- Generated-output considerations.

# 3. Refactor 1–6 Regression Check

Report every required regression check as PASS, FAIL, or RUNTIME UNVERIFIED.

# 4. Route Architecture Audit

Provide a table with:

- Route.
- Route file.
- Classification.
- Main issue.
- Main Flow impact.
- Recommended action.

# 5. Direct Network and Auth Audit

List every direct or suspicious API/auth occurrence outside approved boundaries.

# 6. Supabase Closure Audit

Report:

- Client initialization count.
- Direct Storage SDK call locations.
- Submission consumer path.
- Avatar consumer path.
- Credential/logging findings.
- Remaining runtime/security limitations.

# 7. Service and Type Architecture Audit

Report:

- Duplicate DTOs.
- Local API types.
- Unsafe assertions.
- Response-envelope inconsistencies.
- Hook enabled guards.
- Service-boundary violations.

# 8. Query Key and Invalidation Audit

Report actual strengths, risks, collisions, and invalidation concerns.

# 9. Identifier Safety Audit

Report confirmed safe and unsafe identifier flows.

# 10. Main Flow Mock and Placeholder Audit

Provide a table with:

- Feature.
- File.
- Mock or placeholder.
- User-visible behavior.
- Risk classification.
- Backend dependency.
- Recommended action.

# 11. Barrel Export and Import Health

Report stale exports, missing modules, possible cycles, and direct-import recommendations.

# 12. Build and TypeScript Baseline

For each error provide:

- Command.
- File.
- Error.
- Root cause.
- Classification.
- Main Flow impact.
- Minimal 7B fix.

# 13. Lint Baseline

Group errors and warnings by rule and severity.

# 14. Known Error Deep Diagnosis

Create subsections for:

- Experts base page.
- Calendar and DayPicker.
- `configs/index.ts`.
- `useUserCrud.tsx`.
- Expert registration `any`.
- `use-mobile`.

# 15. Backend Contract and Security Blockers

Group by:

- Proposal Unlock.
- Hire/Contract.
- Workspace Proposal.
- Submission Review.
- Completion/Release.
- Notification/Dispute.

# 16. Frontend Readiness Matrix

Use the required readiness classifications.

# 17. Main Flow Entry Assessment

Explain:

- Which Main Flow phase could technically start.
- Which phase should start first.
- Which phases are blocked.
- Whether starting before build baseline cleanup is advisable.

# 18. Recommended Refactor 7B Fix Set

Provide ordered, minimal, independently reviewable tasks.

For each task include:

- Goal.
- Exact files.
- Expected change.
- Risk.
- Verification.
- Suggested commit boundary.

# 19. Files Expected to Change in 7B

List exact paths grouped by task.

# 20. Manual Regression Plan

Include tests for:

- `/experts`.
- Public Expert Profile.
- Calendar consumers.
- Client/Expert registration.
- Client Jobpost routes.
- Workspace Chat Room.
- Expert Submission.
- Client Package reads.

# 21. Verification Results

Report exact results for:

- TypeScript.
- Lint.
- Build.
- Diff check.
- Git status.
- Confirmation that no source files changed.

# 22. Final Decision

Return exactly one:

- GO FOR REFACTOR 7B TARGETED BASELINE FIXES
- GO FOR MAIN FLOW
- NO-GO UNTIL REPOSITORY STRUCTURE IS REPAIRED

Explain the decision precisely.

Important:

- This is read-only diagnosis.
- Do not fix any source file.
- Do not implement Main Flow.
- Do not invent backend endpoints.
- Distinguish source-confirmed facts from inference.
- Separate architecture readiness from business-contract readiness.
- Preserve a clean Git worktree.