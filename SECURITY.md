
# Security Policy

## Security Overview

This repository takes security seriously. We encourage all contributors and users to follow the policies and best practices below to ensure the ongoing protection of user data and integrity of the code base.

---

## Good Practices to Follow

:warning: **Never store credential information, passwords, private keys, or sensitive configuration data in source code or config files within the repository.**

- **Prevent sensitive data from being pushed to GitHub:**  
  - Use tools such as `git-secrets`, `truffleHog`, or `gitleaks` as pre-commit hooks to block credentials or secrets from entering the code base.
- **Regularly audit for accidental secret leakage:**  
  - Run automated scans for secrets after every major commit or before release.
- **Use environment variables and secret managers:**  
  - Configure secrets in CI/CD pipelines using GitHub Secrets, ENV variables, or dedicate secrets managers like AWS Secrets Manager or HashiCorp Vault.
- **Review dependencies for vulnerabilities:**  
  - Enable Dependabot or similar services to automatically monitor and patch security issues in dependencies.

---

## Supported Versions

This project supports the following versions with security updates. Unsupported versions will not receive security patches.

| Version | Supported          |
| ------- | ------------------ |
| 1.5     | :white_check_mark: |
| < 1.4   | :x:                |

---

## Reporting a Vulnerability

If you believe you have discovered a security vulnerability in this repository:

1. **Do not publicly share details** on GitHub issues, pull requests, or discussions.
2. **Contact us directly:**  
   Email: [oss@thalesgroup.com](mailto:oss@thalesgroup.com)
3. **What to expect:**  
   - We will acknowledge receipt within 5 business days.
   - You will receive updates as we investigate and resolve the issue.
   - Once remediated, we may credit the reporter (unless requested otherwise) and publicly release a patch with a summary of the fix.

---

## Disclosure Policy

- Please report potential security issues privately to oss@thalesgroup.com.
- Include as much detail as possible: affected component, steps to reproduce, impact assessment, and potential mitigations.
- We will assess, validate, and provide a resolution timeline if confirmed.
- If necessary, coordinate public disclosure with the reporter once a fix is available, to ensure safe remediation.

---

## Security Update Policy

- Security advisories will be published under the repository's "Security" tab.
- Users are advised to subscribe to releases and security advisories for timely notifications.
- Patches for critical vulnerabilities will be released as hotfixes; moderate and low severity updates will follow the regular release schedule.

---

## Security-related Configuration

When deploying this project, consider the following configuration for optimal protection:

- **HTTPS/TLS:**  
  - Ensure all endpoints communicate securely with HTTPS.
- **Authentication and Authorization:**  
  - Implement granular access controls, least privilege principles, and role-based access.
- **Secrets Management:**  
  - Store secrets outside code (see best practices above).
- **Logging & Monitoring:**  
  - Enable sufficient logging for security events and monitor regularly for suspicious activity.
- **Dependencies:**  
  - Keep all dependencies up to date and monitor for disclosed security vulnerabilities.

---

## Known Security Gaps & Future Enhancements

The following areas have been identified for further security improvements:

- [ ] Improved integration with advanced secrets scanning tools.
- [ ] Automated security testing in CI/CD.
- [ ] Comprehensive input validation and sanitation across all interfaces.
- [ ] Expansion of role-based access controls.
- [ ] Enhanced audit trails for user actions.

**If you wish to contribute an implementation of any of these enhancements, please open an issue or contact the maintainers directly.**

---

**Questions or concerns?**  
Contact: [oss@thalesgroup.com](mailto:oss@thalesgroup.com)