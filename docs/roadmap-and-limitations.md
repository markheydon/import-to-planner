# Roadmap and Limitations

## Known Limitations

- Single-tenant configuration only; multi-tenant support is not yet implemented.
- Hosted deployment implementation is not yet included.
- Certificate loading uses local path-based secrets; Key Vault integration is future work.
- Graph beta dependency may require adjustments if API contracts change.

## Implementation Roadmap

Tracked in [issue #1](https://github.com/markheydon/import-to-planner/issues/1) with child issues:

1. [#2](https://github.com/markheydon/import-to-planner/issues/2): Implement Graph gateway with beta support
2. [#3](https://github.com/markheydon/import-to-planner/issues/3): Add Entra delegated auth wiring
3. [#4](https://github.com/markheydon/import-to-planner/issues/4): Use real user-accessible containers and plans
4. [#5](https://github.com/markheydon/import-to-planner/issues/5): Add Graph error handling and retries
5. [#6](https://github.com/markheydon/import-to-planner/issues/6): Add Graph gateway test coverage
6. [#7](https://github.com/markheydon/import-to-planner/issues/7): Improve README for new-developer setup
7. [#8](https://github.com/markheydon/import-to-planner/issues/8): Entra app registration and permissions (manual task)
8. [#9](https://github.com/markheydon/import-to-planner/issues/9): Local secret configuration for tenant values (manual task)
