# Contributing to SQLiteMemoryCacheSync

Thank you for your interest in contributing! This guide will help you get started.

## Getting Started

1. Fork the repository
2. Clone your fork
3. Create a feature branch (`git checkout -b feature/amazing-feature`)
4. Make your changes
5. Write or update tests
6. Run tests to ensure they pass
7. Commit your changes
8. Push to your fork
9. Open a Pull Request

## Development Setup

```bash
git clone https://github.com/yourusername/SQLiteMemoryCacheSync.git
cd SQLiteMemoryCacheSync
dotnet restore
dotnet build
dotnet test
```

## Code Style

- Follow C# naming conventions (PascalCase for public members, camelCase for private)
- Use meaningful variable and method names
- Add XML documentation comments for public APIs
- Keep methods focused and single-responsibility

## Testing

- Write tests for new features
- Ensure all tests pass before submitting PR
- Aim for >80% code coverage

```bash
dotnet test --verbosity detailed
```

## Pull Request Process

1. Update README.md with any new features or changes
2. Add tests for new functionality
3. Ensure all tests pass
4. Update CHANGELOG.md
5. Request review from maintainers

## Reporting Issues

When reporting bugs, please include:
- .NET version
- Operating system
- Steps to reproduce
- Expected vs. actual behavior
- Error logs and stack traces

## License

By contributing, you agree that your contributions will be licensed under the MIT License.
