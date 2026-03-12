# Mirror's Edge Map Manager (MEMM)

A tool for downloading, installing, and managing custom maps for Mirror's Edge.

![Version](https://img.shields.io/badge/version-2.0.0-blue.svg)
![.NET](https://img.shields.io/badge/.NET-8.0-purple.svg)
![Platform](https://img.shields.io/badge/platform-Windows-lightgrey.svg)

<img width="1236" height="793" alt="Screenshot 2026-03-12 205311" src="https://github.com/user-attachments/assets/ef5f072a-010b-4c45-bbb5-cfc4f91a8fa9" />

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
- **Game**: Mirror's Edge (Steam, GOG, EA/Retail)
  - **Note**: Gamepass executable patching is currently unsupported (until EA App decides to let me launch the games I've paid for so I can debug them)

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

## Dependencies

- **MaterialDesignThemes** (v5.2.1)
- **CommunityToolkit.Mvvm** (v8.3.2)
- **System.Text.Json** (v9.0.0)
- **System.Text.Encoding.CodePages** (v9.0.0)

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

---

Made with ❤️ by softsoundd

