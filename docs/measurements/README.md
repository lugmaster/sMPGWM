# Archived performance evidence

The 2026-11-12 evidence is preserved in the local, Git-ignored [2026-11-12-results.zip](2026-11-12-results.zip). The ZIP contains a complete snapshot of both dated measurement directories and `docs/plans/evidence`, with repository-relative paths. It includes the interrupted restored scenario as **partial evidence**, not a completed run.

The archive contains 1,382 original files (207,017,908 bytes), compressed to 47,282,953 bytes, plus an internal `ARCHIVE-MANIFEST.json` listing each file's size, SHA-256 and whether its loose copy was removed. Every original entry was decompressed and hash-checked against the source before removal. The archive checksum and verification record are in [2026-11-12-results.archive.json](2026-11-12-results.archive.json).

Bulk CSV samples, logs, test outputs, definition copies and isolated user data are now archive-only. Reports, summaries, manifests, run outcomes, workload/recorder metadata, marker CSVs, environment information and patches remain readable. Existing links to bulk evidence require extraction first. The ZIP and extracted bulk data are ignored by Git; a Git clone alone does not include this archive. Copy the ZIP separately when transferring the evidence.

## Restore bulk evidence

From the repository root, check the ZIP against the SHA-256 in the verification record:

```powershell
Get-FileHash -Algorithm SHA256 docs/measurements/2026-11-12-results.zip
```

Restore only the archived bulk files, preserving the current reports and metadata:

```powershell
python -c "import json, zipfile; archive = zipfile.ZipFile('docs/measurements/2026-11-12-results.zip'); [archive.extract(item['path'], '.') for item in json.loads(archive.read('ARCHIVE-MANIFEST.json')) if item['archivedOnly']]"
```

When analysis is resumed, existing results can be summarized without rerunning benchmarks:

```powershell
python tools/summarize_performance.py docs/measurements/2026-11-12-full/release-matrix
python tools/summarize_performance.py docs/measurements/2026-11-12-full
```

## Current status

All six main Release runs completed; current summaries cover the first five. Follow-up measurements were stopped at the user's request during the first restored scenario. No benchmarks or tests were resumed while archiving. The user subsequently authorized the [audited cleanup](2026-11-12-full/cleanup-audit.md), which has been implemented and regression-tested separately. This ZIP remains the unchanged pre-cleanup evidence. The paused status takes precedence over historical running-job instructions in the handoff or archived documents.

See the [plan](../plans/multiplayer-performance-and-final-cleanup.md), [handoff](../plans/multiplayer-performance-handoff.md), and [provisional report](2026-11-12-full/report.md) for remaining work.
