---
name: commit-to-npc-work-toyproject
description: Commit and push this Unity project to https://github.com/Merkatte/NPC_Work_ToyProject only when the user explicitly invokes this skill by name or another explicitly invoked skill instructs Codex to use it. Do not use automatically for generic work, and do not commit or push unless explicitly requested in the current user turn or by an explicitly invoked skill.
---

# Commit To NPC Work ToyProject

Use this skill only when one of these is true:

- The user explicitly invokes `commit-to-npc-work-toyproject`.
- Another explicitly invoked skill says to use `commit-to-npc-work-toyproject`.

Do not use this skill automatically. Do not commit or push merely because files changed, a task finished, or the user asks for unrelated development work.

## Target Repository

Push commits to:

```text
https://github.com/Merkatte/NPC_Work_ToyProject
```

Use this remote URL:

```text
https://github.com/Merkatte/NPC_Work_ToyProject.git
```

## Safety Rules

- Do not run `git add`, `git commit`, or `git push` unless the user explicitly requested committing/pushing in the current turn or this skill was explicitly invoked for that purpose.
- Never include Unity generated folders such as `Library`, `Temp`, `Logs`, `obj`, or IDE files in commits.
- Respect existing `.gitignore`.
- Check `git status --short` before staging.
- Review staged files before committing.
- If the working tree contains unrelated user changes, do not revert them. Ask before including them if the requested commit scope is unclear.

## Workflow

1. Check repository state:

```powershell
git status --short
git remote -v
git branch --show-current
```

2. Ensure the remote URL points to the target repository. If no remote exists, add:

```powershell
git remote add origin https://github.com/Merkatte/NPC_Work_ToyProject.git
```

If `origin` exists but points elsewhere, ask before changing it.

3. Stage only intended project files.

4. Commit with a concise message describing the actual change.

5. Push to `origin main` unless the user explicitly asks for another branch.

6. Report the commit hash and push result.
