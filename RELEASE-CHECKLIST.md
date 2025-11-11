# HSPrint Release Checklist

Use this checklist before each release to ensure quality and consistency.

## Pre-Release (Before Building)

### Version Management
- [ ] Update `version.txt` with new version number (e.g., `1.0.0`)
- [ ] Update version references in documentation if needed
- [ ] Create CHANGELOG.md entry (if you have one) with changes in this release

### Code Quality
- [ ] All tests pass: `dotnet test`
- [ ] Build succeeds: `dotnet build -c Release`
- [ ] No compiler warnings
- [ ] Code review completed (if applicable)
- [ ] All TODO comments addressed or tracked

### Documentation
- [ ] README.md is up to date
- [ ] API documentation is accurate
- [ ] Configuration examples are current
- [ ] Breaking changes are documented
- [ ] Migration guide written (if needed for upgrades)

### Dependencies
- [ ] All NuGet packages are up to date (or pinned to specific versions)
- [ ] No security vulnerabilities in dependencies
- [ ] License compliance checked

## Build Phase

### Local Build
- [ ] Build installer locally: `.\build-installer.ps1 -Version "x.x.x"`
- [ ] Verify MSI is created in `artifacts/`
- [ ] Verify ZIP is created in `artifacts/`
- [ ] Check file sizes are reasonable
- [ ] Scan for malware/viruses (optional but recommended)

### Build Verification
- [ ] MSI opens without errors
- [ ] Version number is correct in installer
- [ ] Company name and copyright are correct
- [ ] Icon displays correctly

## Testing Phase

### Fresh Installation Testing
- [ ] Test on clean Windows 10 machine/VM
- [ ] Test on clean Windows 11 machine/VM
- [ ] MSI installer runs without errors
- [ ] Application installs to correct location
- [ ] All files are present in install directory
- [ ] Logs directory is created
- [ ] Registry keys are created correctly

### Functional Testing
- [ ] Service starts automatically
- [ ] Application is accessible at http://localhost:50246
- [ ] Swagger UI loads correctly
- [ ] Health endpoint responds: `GET /health`
- [ ] Version endpoint shows correct version: `GET /health/version`
- [ ] Printer listing works: `GET /printer`
- [ ] ZPL printing works (if printer available)
- [ ] Image printing works (if printer available)
- [ ] PDF printing works (if printer available)
- [ ] Logging works (check log files)

### Startup Testing
- [ ] Application configured in Windows startup
- [ ] Registry entry created: `HKCU:\Software\Microsoft\Windows\CurrentVersion\Run\HSPrint`
- [ ] Application starts after reboot
- [ ] Service recovers after failure (test by killing process)

### Upgrade Testing
- [ ] Install previous version first
- [ ] Run new installer
- [ ] Old version is automatically uninstalled
- [ ] New version installs successfully
- [ ] Configuration is preserved (if applicable)
- [ ] Service continues working
- [ ] No leftover files from old version

### Uninstall Testing
- [ ] Uninstall via "Add or Remove Programs"
- [ ] All files removed from `C:\Program Files\HSPrint`
- [ ] Service is removed
- [ ] Registry keys are cleaned up
- [ ] Startup entry is removed
- [ ] Logs are preserved (optional, document behavior)

### Compatibility Testing
- [ ] Windows 10 (21H2 or later)
- [ ] Windows 11
- [ ] Windows Server 2016 or later (if applicable)
- [ ] Works with various printer types (USB, Network, Virtual)

### Security Testing
- [ ] Application runs with least privilege
- [ ] No sensitive data in logs
- [ ] CORS settings are correct
- [ ] API only accessible from localhost (unless intended otherwise)
- [ ] No hardcoded secrets or passwords

## Git and Release Preparation

### Git Operations
- [ ] All changes committed
- [ ] Commit messages are clear and descriptive
- [ ] Branch is up to date with master/main
- [ ] Merge conflicts resolved (if any)
- [ ] Code pushed to GitHub: `git push origin master`

### Tag Creation
- [ ] Create annotated tag: `git tag -a vx.x.x -m "Release x.x.x"`
- [ ] Tag message includes major changes
- [ ] Tag pushed to GitHub: `git push origin vx.x.x`

### GitHub Repository
- [ ] Repository is public or accessible to users
- [ ] README displays correctly on GitHub
- [ ] License file is present
- [ ] Contributing guide is present (if applicable)
- [ ] Issue templates configured (if applicable)

## GitHub Actions Verification

### Workflow Check
- [ ] Actions are enabled in repository settings
- [ ] Workflow file is valid YAML
- [ ] Required secrets are configured:
  - [ ] `SUPABASE_URL` (if using Supabase)
  - [ ] `SUPABASE_SERVICE_ROLE_KEY` (if using Supabase)
  - [ ] `GITHUB_TOKEN` (automatically provided)

### Workflow Execution
- [ ] Workflow triggered by tag push
- [ ] All jobs complete successfully
- [ ] Build artifacts are created
- [ ] MSI is uploaded
- [ ] ZIP is uploaded
- [ ] install.ps1 is included
- [ ] No errors in workflow logs

### Build Artifacts
- [ ] Artifacts are downloadable from Actions tab
- [ ] File sizes are correct
- [ ] Files are not corrupted

## GitHub Release

### Release Creation
- [ ] Release is created automatically by workflow
- [ ] Release title is correct: "HSPrint vx.x.x"
- [ ] Tag is correct: "vx.x.x"
- [ ] Release is not marked as draft
- [ ] Release is not marked as pre-release (unless it is)

### Release Assets
- [ ] `HSPrintSetup-x.x.x.msi` is attached
- [ ] `HSPrint-x.x.x.zip` is attached
- [ ] `install.ps1` is attached
- [ ] All assets are downloadable
- [ ] Asset names are consistent with version

### Release Notes
- [ ] Release notes are clear and informative
- [ ] Installation instructions are included
- [ ] New features are listed
- [ ] Bug fixes are listed
- [ ] Breaking changes are highlighted (if any)
- [ ] Known issues are documented (if any)
- [ ] Links to documentation are included

## Post-Release Verification

### Download and Test
- [ ] Download MSI from GitHub release
- [ ] Verify download completes successfully
- [ ] File size matches built version
- [ ] Checksum matches (if provided)
- [ ] Install downloaded MSI on test machine
- [ ] Verify functionality works

### External Storage (if applicable)
- [ ] Files uploaded to Supabase Storage
- [ ] Download URLs are accessible
- [ ] Files are public or properly authenticated

### Update Service (if applicable)
- [ ] Update check endpoint returns new version
- [ ] Download URL in update API is correct
- [ ] Auto-update feature works (if implemented)

## Documentation Updates

### Repository Documentation
- [ ] README.md links to latest release
- [ ] Installation instructions are current
- [ ] Screenshots are up to date (if any)
- [ ] API documentation matches actual API

### External Documentation
- [ ] Website updated with new version (if applicable)
- [ ] User manual updated (if exists)
- [ ] API documentation site updated (if exists)

## Communication

### Announcements
- [ ] Release announcement prepared
- [ ] Changelog formatted for users
- [ ] Social media posts drafted (if applicable)
- [ ] Email notification prepared (if applicable)
- [ ] Blog post written (if applicable)

### User Notification
- [ ] Existing users notified of update
- [ ] Migration instructions provided (if needed)
- [ ] Support channels ready for questions

## Monitoring and Support

### Monitoring Setup
- [ ] Error tracking enabled (if applicable)
- [ ] Analytics enabled (if applicable)
- [ ] Logging is sufficient for debugging

### Support Preparation
- [ ] GitHub Issues enabled and monitored
- [ ] Common issues documented
- [ ] FAQ updated
- [ ] Support email configured (if applicable)
- [ ] Support team informed of release

## Rollback Plan

### Rollback Preparation
- [ ] Previous version installer is available
- [ ] Rollback procedure documented
- [ ] Database migration rollback tested (if applicable)
- [ ] Know how to un-publish release if critical bug found

### Emergency Contacts
- [ ] Team members are available for support
- [ ] Escalation path is defined
- [ ] Critical bug response plan is ready

## Sign-Off

Before marking release as complete:

- [ ] **Development Lead** - Code quality and functionality verified
- [ ] **QA/Tester** - All tests passed
- [ ] **Product Owner** - Features match requirements
- [ ] **Release Manager** - All checklist items completed

### Final Checks
- [ ] Version number is correct everywhere
- [ ] No known critical bugs
- [ ] Performance is acceptable
- [ ] Security has been considered
- [ ] Documentation is complete

### Release Decision
- [ ] **GO** - Release is approved and published
- [ ] **NO-GO** - Issues found, release postponed

---

## Post-Release Activities

### Immediate (Within 24 hours)
- [ ] Monitor for installation issues
- [ ] Check error logs/reports
- [ ] Respond to user questions
- [ ] Watch GitHub issues

### Short-term (Within 1 week)
- [ ] Collect user feedback
- [ ] Track adoption rate
- [ ] Document any issues found
- [ ] Plan hotfix if needed

### Long-term
- [ ] Analyze usage metrics
- [ ] Plan next release features
- [ ] Update roadmap
- [ ] Deprecate old versions (if applicable)

---

## Notes

**Version Numbering:**
- Major.Minor.Patch (e.g., 1.0.0)
- Major: Breaking changes
- Minor: New features, backward compatible
- Patch: Bug fixes only

**Testing Environments:**
- Windows 10 21H2 (minimum supported)
- Windows 11 23H2
- Windows Server 2019/2022

**Critical Paths to Test:**
1. Fresh installation
2. Upgrade from previous version
3. Uninstall
4. Service operation
5. Basic API functionality

---

**Release Manager:** _______________  
**Date:** _______________  
**Version:** _______________  
**Status:** ? GO  ? NO-GO
