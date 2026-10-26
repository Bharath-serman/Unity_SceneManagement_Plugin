# Changelog

All notable changes to this package will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.0.0] - 2026-10-23

### Added
- Multi-Tagging System: Assign multiple custom tags per GameObject using stable GUID identifiers without modifying Unity's native tag field.
- Tag Manager Window: Hierarchical taxonomy editor with custom color palettes, group selection checkboxes, and batch deletion operations.
- Scene Explorer: Filter and search GameObjects across loaded scenes using names, custom tags, layers, and component types.
- Multiple Explorer Views: Collapsible Hierarchy Tree view, Grouped by Tag view, and dense Compact Table view for managing massive scene hierarchies.
- Scene Analyzer: Automated scene validation with 10 built-in rules covering missing scripts, broken references, duplicate sibling names, and component constraints.
- High-Visibility Severity Colors: Color-coded cards for Errors (crimson red), Warnings (amber/yellow), and Info (slate blue).
- Collapsible System Rules: Collapsible Core System Rules section in Rule Configuration for streamlined custom rule setup.
- Safe Fix System: Automated non-destructive corrections and batch repairs with full Unity Undo integration.
- SceneSentry Dashboard: Central hub providing instant scene health metrics, tag statistics, and single-click diagnostics.
- Automated Test Suite: Complete NUnit test coverage for tag registry, rule evaluation, explorer filtering, and safe remediation.
- Documentation & Samples: Complete User Guide, API Reference, Custom Rules Guide, and interactive sample workflows.
