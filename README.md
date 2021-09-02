# FriendsAnalyzer

A Roslyn analyzer to flag inappropriate friend method usage.

## Getting Started

These instructions will get you a copy of the project up and running on your local machine for development and testing purposes.

### Prerequisites

.NET 5.0 is required, it can be installed [here](https://dotnet.microsoft.com/download).

### Compiling

Build using the `dotnet` command at the repo root directory.

```
dotnet build
```

### Testing

Test using the `dotnet test` command at the repo root directory.

```
dotnet test
```

## Using the Analyzer

The easiest way to use `Friends.Analyzer` is to use it through NuGet. By adding a PackageReference to [Friends.Attribute](https://www.nuget.org/packages/Friends.Attribute/), your project can use the `FriendsAttribute` to annotate the code as follow:

```c#
namespace Test
{
    using Friends.Attribute;

    public class Owner
    {
        [Friends(typeof(Friend))]
        internal void Sing()
        {

        }
    }

    public class Friend
    {
        public static void Sample()
        {
            new Owner().Sing();
        }
    }

    public class Stranger
    {
        public void Sample(string[] args)
        {
            new Owner().Sing();
        }
    }
}
```

Here we declare our intent that the `Sing` method is meant to be used by `Friend` only, so the method invocation by `Stranger` is detected and issue as a warning as follow:

```
C:\Test\Program.cs(26,13,26,31): warning FriendsAnalyzer: 'new Owner().Sing()' is inaccessible due to its protection level
```

To fix this issue, we could add another `Friends` attribute to the `Owner.Sing` method. In Visual Studio, this can be done automatically by right clicking on the squiggle and select `Add Friends Attribute`.

## Contributing

Please read [CONTRIBUTING.md](CONTRIBUTING.md) for details on our [code of conduct](CODE-OF-CONDUCT.md), and the process for submitting pull requests to us.

## Authors

* **Andrew Au** - *Initial work* - [cshung](https://github.com/cshung)

See also the list of [contributors](https://github.com/cshung/FriendsAnalyzer/graphs/contributors) who participated in this project.

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details
