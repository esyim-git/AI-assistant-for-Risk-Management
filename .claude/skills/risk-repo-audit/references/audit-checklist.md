# Repository Audit Checklist

## Evidence snapshot

- Current main SHA / code-test baseline SHA / VERSION.
- Latest published tag and release URL; unreleased candidate SHA and hash separately.
- Build warning/error count; authoritative `Total=N PASS=N FAIL=0`; package manifest entry count.
- Repository visibility, open PR count, workflow triggers, branch protection, secret scanning.

## Capability reachability

For each important capability record:

| Capability | Core | WPF/user path | Published artifact | Test-PC evidence | Approval |
|---|---|---|---|---|---|

Use `VERIFIED`, `PARTIAL`, `SCAFFOLD_ONLY`, `PLACEHOLDER`, `BLOCKED`, `NOT_IMPLEMENTED`, or `APPROVAL_REQUIRED`. Add `user-reported` only as an evidence qualifier.

## Finding classes

- Confirmed bug: reproducible contradiction, incorrect output, unsafe fallback, or failing invariant.
- Reachability gap: code exists but no user-facing call site.
- Documentation drift: version, SHA, release, Gate, workflow, or NEXT UP differs from evidence.
- Operational gap: no tag/release, no branch protection, disabled CI, missing rollback/performance evidence.
- Approval gate: a governed external dependency, credential, real content, runtime, or model decision.
- Enhancement: useful but not required to preserve current correctness or governance.

## Roadmap order

1. Misleading or unsafe output.
2. Release and repository governance.
3. Architecture changes needed before additional UI work.
4. Existing Core capabilities made reachable in WPF.
5. Supported-runtime migration before platform end of support.
6. Gate B/C closure and Team Pilot.
7. Approval-gated LLM, signing, NCR, or internal KB work only after approval.
