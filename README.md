# Mirror's Edge Map Manager (MEMM)

A tool for downloading, installing, and managing custom maps for Mirror's Edge.

![Version](https://img.shields.io/badge/version-2.1.0-blue.svg)
![.NET](https://img.shields.io/badge/.NET-8.0-purple.svg)
![Platform](https://img.shields.io/badge/platform-Windows-lightgrey.svg)

<img width="960" height="540" alt="MEMM" src="https://github.com/user-attachments/assets/7ad3ba2b-b248-420c-9c57-cd92c164bbe2" />

&nbsp;

[![Ko-fi](https://img.shields.io/badge/support_me_on_ko--fi-F16061?style=for-the-badge&logo=kofi&logoColor=f5f5f5)](https://ko-fi.com/softsoundd)

If you like what I do and would like to support my work, please consider visiting my Ko-fi page.

## Features

- Curated map browser for:
  - Custom Maps
  - Custom Time Trials (Pure Time Trial DLC maps included)
  - Story Experiences
- One-click map download and installation
- Utilises Keku's Custom Map Menu mod for easy in-game launching of maps (located in the Extras main menu panel)
- Speedrun.com API integration — see at a glance the top runs for a map!

## Requirements

- **OS**: Windows 10 or later
- **.NET Runtime**: .NET 8.0 or later
- **Game**: Mirror's Edge (Steam, GOG, EA App/Xbox Game Pass for PC, Retail platforms). Versions 1.0.0.0 - 1.0.1.0 supported.
> [!NOTE]
> Version 1.1.0.0 DLC is not currently supported due to the way MEMM replicates DLC functionality on lower versions.

## Installation

1. Download the latest release from the [Releases](../../releases) page
2. Extract the zip contents to a location of your choice
3. Run `Mirror's Edge Map Manager.exe`
4. On first launch:
   - Click **Select Game Directory** and choose your Mirror's Edge install folder. Typical install locations:
     - Steam: `C:\Program Files (x86)\Steam\steamapps\common\mirrors edge`
     - GOG: `C:\Program Files (x86)\GOG Galaxy\Games\Mirror's Edge`
     - EA: `C:\Program Files\EA Games\Mirrors Edge`
   - Apply the config patch
   - Install required dependencies

## Building from Source

### Prerequisites

- Visual Studio 2022 or later, or .NET SDK
- .NET SDK 8.0 or later
- Windows 10/11

### Build Steps

```bash
# Clone the repository
git clone https://github.com/<your-account>/MirrorsEdgeMapManager.git
cd MirrorsEdgeMapManager

# Restore NuGet packages
dotnet restore MirrorsEdgeMapManager/MirrorsEdgeMapManager.sln

# Build the solution
dotnet build MirrorsEdgeMapManager/MirrorsEdgeMapManager.sln
```

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request. For major changes, please open an issue first to discuss what you would like to change.

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Acknowledgments

- EA DICE for Mirror's Edge
- Keku for the Custom Map Menu mod
- Custom map speedrun leaderboard moderators
- The Mirror's Edge custom map creators and community

## Changelog

Refer to the [CHANGELOG](CHANGELOG.md) file for changes.
