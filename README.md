## Description
This is a direct port of [this](https://github.com/sjoerdev?tab=repositories) file dialog project ported to C# and modern dotnet.

## Building:

Download .NET 9: https://dotnet.microsoft.com/en-us/download

Building for Windows: ``dotnet publish -o ./build/windows --sc true -r win-x64 -c release``

Building for Linux: ``dotnet publish -o ./build/linux --sc true -r linux-x64 -c release``
