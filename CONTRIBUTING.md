# Contributing Guidelines

This document explains how your team is organized, the assigned roles, communication methods, and the rules to follow to contribute to this project.

## Team Organization

Our team is structured to ensure effective collaboration and transparent management of contributions.

### Roles

- **Project Lead:** Responsible for the vision, key decisions, and final validation of important changes.
- **Maintainers:** In charge of reviewing pull requests, triaging issues, and overall repository maintenance.
- **Developers/Contributors:** Write code, report bugs, propose features, and help with code reviews.
- **Documentation Specialist:** Ensures documentation is clear, correct, and up to date.
- **Quality Assurance:** Responsible for testing, feature validation, and quality maintenance.

### Communication

- **Main Channels:**
  - GitHub Discussions (or other suitable tool, e.g., Slack, Teams)
  - Email for important exchanges
- **Communication Rules:**
  - Always show respect and courtesy.
  - Use the project’s primary language (e.g., English or French).

## Rules for Future Contributors

- Respect the project’s code of conduct.
- Commit to writing readable, documented, and tested code.
- Always discuss a new feature through an Issue before starting work on it.
- Be receptive to feedback during code reviews.

## How to Become a Contributor

To become a contributor:

1. Open an issue to propose a new contribution or to fix a bug.
2. Wait for approval from a maintainer/lead.
3. Fork the repository and work on a dedicated branch.
4. Follow the pull request checklist before submission.

### Contributor License Agreements

Thank you for your interest in contributing to this project!

To protect both you and the maintainers of this Project, and to clarify intellectual property rights, we ask you to read and accept the terms below.

#### 1. Grant of Copyright License

You hereby grant, free of charge and irrevocably, to all recipients of the Project, the right to use, reproduce, modify, display, perform, sublicense, and distribute your contributions, in any form, for the purpose of developing, distributing, and improving the Project.

#### 2. Grant of Patent License

You also grant, free of charge and irrevocably, to all recipients of the Project, a worldwide license to use any patents you may hold regarding your contributions in the context of the Project.

#### 3. Originality of Work

You affirm that each contribution you submit is an original work that you created, or that you have the right to submit it under the terms of this CLA. You warrant that this contribution does not violate any copyright, patent, or other proprietary right of any third party.

#### 4. No Obligation

You understand that the use of your contribution does not create any obligation for the Project or its maintainers.

#### 5. Signature

By submitting a contribution (for example, via a pull request), you indicate your agreement to all the terms of this Contributor License Agreement.

### Contributing Code

- Use descriptive branches (`feature/feature-name`, `fix/bug-name`).
- Document your code with explanatory comments.
- Respect the coding style described in the following section.

## Pull Request Checklist

Before submitting a pull request, contributors must ensure:

- [ ] Code compiles/runs without errors.
- [ ] All dependencies and required files are included.
- [ ] Documentation is updated if necessary.
- [ ] New tests are added and pass.
- [ ] The pull request is linked to an existing issue (if applicable).
- [ ] Reviewers are mentioned for validation.

### License

All contributions are under the GNU GENERAL PUBLIC LICENSE V2, which must be respected by all contributors.

### Coding Style

- Follow the linter configuration (e.g., eslint, black, etc.).
- Use the variable, function, and class naming conventions defined in the README or project documentation.
- Keep lines of code under 120 characters.
- Avoid redundant code; favor factorization.

### Testing

#### Running Sanity Check

- Run automated verification scripts (`sanity-check.sh`, `lint` or equivalent).
- Fix any errors reported before submission.

#### Running Unit Tests

- Run all unit tests (`pytest`, `unittest`, `jest`, etc.).
- Tests should pass with no errors or warnings.

### Issues Management

- Every bug, feature request, or suggestion should be submitted as an issue.
- Issues should follow the provided template (clear title, precise description, logs/errors if applicable).
- Labels must be used to categorize each issue (bug, enhancement, documentation, etc.).
- Any discussion/evolution must be documented in the issue.
