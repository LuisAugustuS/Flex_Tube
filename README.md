# Flex Tube

## Description

"Flex Tube" is an AutoCAD plugin inspired by a discussion on [The Swamp](https://www.theswamp.org/index.php?topic=19272.0) about graphically representing flexible tubes using AutoLISP. This project extends the idea into the .NET environment, utilizing AutoCAD's API to create dynamic representations of flexible tubing within AutoCAD drawings. The primary focus is on exploring and leveraging the capabilities of the `Overrule` class within AutoCAD's API. For more information about the `Overrule` class, see the [AutoCAD Overrule documentation](https://help.autodesk.com/view/OARX/2022/ENU/?guid=OARX-ManagedRefGuide-Autodesk_AutoCAD_Runtime_Overrule).

## Features

- **Dynamic Representation**: Creates detailed and dynamic graphical representations of flexible tubes in AutoCAD drawings.
- **Selective Overruling**: Employs `DrawableOverrule` along with `WorldDraw` and `SetExtensionDictionaryEntryFilter` methods to apply overrides selectively based on specific extension dictionary entries. Further details on `DrawableOverrule` are available [here](https://help.autodesk.com/view/OARX/2022/ENU/?guid=OARX-ManagedRefGuide-Autodesk_AutoCAD_GraphicsInterface_DrawableOverrule).
- **Protection of Custom Objects**: Uses `TransformOverrule` and overrides the `Explode` method to prevent custom objects from being exploded, maintaining the integrity of designs.
- **Grip Customization**: Integrates `GripOverrule` to alter grip behavior for polylines with fillets, enhancing the user interaction experience. This feature is initially commented out in the code and can be activated by uncommenting.

## Getting Started

### Prerequisites

- AutoCAD 2021
- Visual Studio 2022 Community Edition

### Installation

1. Clone or download the repository.
2. Open the solution in Visual Studio 2022.
3. Build the solution to generate the plugin.
4. Load the generated plugin into AutoCAD using the `NETLOAD` command.

### Usage

- Activate the plugin's functionalities with the command: `FLEX`.
- Remove the applied overrules using the command: `REMOVEOVERRULES`.

## Contributing

Contributions are welcome. If you have ideas for bug fixes, enhancements, or documentation improvements, feel free to fork the repository and submit a pull request.

## License

This project is made available under a permissive license, allowing copying and distribution without warranty. Refer to the LICENSE file for more details.

## Disclaimer

"Flex Tube" is developed as an experimental project to explore the `Overrule` class's capabilities in AutoCAD. While it aims to be functional, it is provided "as is" with no guarantees. Use at your own risk.

## Contact

Should you have any questions or require support, please open an issue in this GitHub repository.

