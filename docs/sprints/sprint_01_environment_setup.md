# Sprint 01 Environment Setup

## Sprint Name

Sprint 1: Environment Setup & Project Foundation

## Sprint Goal

Prepare the PropCare Cloud project foundation, verify the development environment, create project documentation, create helper scripts, and confirm the workspace is ready before application coding starts.

## Date/Time

Environment check completed on 2026-07-02 00:18:24 +03:00.

## Files Created/Changed

Created or verified folder structure:

- `aws/`
- `backend/`
- `frontend/`
- `docs/`
- `docs/architecture/`
- `docs/assignment/`
- `docs/sprints/`
- `scripts/`

Created project files:

- `README.md`
- `.gitignore`
- `docs/assignment/project_overview.md`
- `docs/architecture/architecture_decision_record_001.md`
- `docs/sprints/sprint_01_environment_setup.md`
- `scripts/check-environment.ps1`

Updated during Sprint 1 closure:

- `docs/sprints/sprint_01_environment_setup.md`
- `scripts/check-environment.ps1`

## Commands/Checks Performed

Workspace and folder checks:

```powershell
Get-ChildItem -Force | Select-Object Name,Mode
rg --files
New-Item -ItemType Directory -Force -Path aws, backend, frontend, docs\architecture, docs\assignment, docs\sprints, scripts
```

Environment check:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\check-environment.ps1
```

Git checks and setup:

```powershell
git rev-parse --is-inside-work-tree
git config --get user.name
git config --get user.email
git init
git status --short
git branch --show-current
```

## Environment Check Results

System information:

- Operating system: Microsoft Windows NT 10.0.26200.0
- Working directory: `D:\SWE  MOODLE\Year 3 Semester 2\Designing and Developing Applications on the Cloud\aws-workspace\PropCareCloud`
- PowerShell version: 5.1.26100.8737

Critical tools:

- Git: PASS, `git version 2.48.1.windows.1`
- .NET SDK: PASS, `8.0.422`
- Installed .NET SDK list: `8.0.422 [C:\Program Files\dotnet\sdk]`
- Node.js: PASS, `v24.18.0`
- npm: PASS, `11.16.0`
- AWS CLI: PASS, `aws-cli/2.35.14 Python/3.14.5 Windows/11 exe/AMD64`

Overall critical environment:

- PASS, all critical tools are available.

Recommended tools:

- Visual Studio Code: PASS, version 1.126.0
- Visual Studio 2022: PASS, Community edition detected
- PostgreSQL psql: MISSING, `psql` command was not found

## Missing Tools

Critical missing or unusable tools:

- None

Recommended missing tools:

- PostgreSQL psql

PostgreSQL `psql` is recommended only and is not required to close Sprint 1. It can be installed later during the database sprint if direct PostgreSQL command-line access is needed.

## Issues Found

- The workspace was not previously a Git repository. `git init` was run successfully.
- The current Git branch after initialization is `master`.
- Git user configuration exists:
  - User name: `ALMAFLEHI`
  - User email: `almaflehi0@gmail.com`
- Earlier critical-tool issues were resolved before Sprint 1 closure.
- The environment check script was adjusted to use working Windows executable paths for tools with PowerShell wrapper or PATH detection differences.
- PostgreSQL `psql` is not installed or not available on PATH.

## Sprint Closure Notes

- Sprint 1 foundation is complete.
- Project structure is ready.
- Environment is ready for Sprint 2 backend setup.
- No AWS configuration or cloud deployment was performed yet.

## Final Status

COMPLETE

The required project structure, documentation, Git initialization, and environment check script are complete. Sprint 1 is marked COMPLETE because all critical tools are available. PostgreSQL `psql` remains missing, but it is optional/recommended for now and can be installed later if needed.
