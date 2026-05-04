# Pull Request Checklist
> Please leave this checklist in your description for reviewers to use.  
> Reviewers, please check off each item as you review the PR.

<details>
<summary> <strong>Billing Team Development Guidelines</strong></summary>

- [ ] Maintain **clean separation of concerns** — Controller → Manager → Handler → Service → Repository.
- [ ] Business logic belongs in **Manager/Service**, not Controller.
- [ ] Validate all billing API endpoints with proper **authorization filters**.
- No duplicate entries.

</details>

<details>
<summary> <strong>Code Standards</strong></summary>
###  General
- [ ] Add **unit tests** as needed — new code should be tested; add tests to legacy code as we go.
- [ ] No new **stored procedures** that are not CRUD. Keep logic in code.
- [ ] No **commented-out code** — use git history instead.
- [ ] No **duplicate code** — keep it DRY (Don’t Repeat Yourself).
- [ ] Organize files into proper **directories** (no dumping into top-level folders).

###  Code Practices
- [ ] No DB connections without a `using` statement — ensure proper disposal.
- [ ] No **hard-coded values** — use constants, enums, or configuration.
- [ ] Avoid **large if-else chains** — use hashtables, dictionaries, or mapping logic.
- [ ] Keep line length ≤ **150 characters** in C#, JS, etc.

###  Readability & Maintainability
- [ ] Add **comments** for new or complex logic; improve legacy documentation.
- [ ] Break **conditional statements or parameters** with >2 items onto separate lines.
- [ ] Follow proper **formatting standards** — use a linter or formatter.

</details

<details>
<summary> <strong>Database & Release Scripts</strong></summary>

- [ ] Release script must follow **template** (with commit and rollback).
- [ ] All **DBML changes** in release scripts must have `IF EXISTS` checks before modification.
- [ ] Any DB changes related to BH, create new JIRA ticket and assign it to BH team (no direct execution from billing team)

</details>


**After completing this checklist:**
> Ensure PR description clearly explains the change, impacted areas, and testing performed.


 **NOTE for Reviewers:**  
You can tick checkboxes directly here in Azure DevOps. Checked boxes are saved when you edit and update the PR description.