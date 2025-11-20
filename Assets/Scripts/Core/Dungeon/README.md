# ProBuilder Dungeon Generator

A procedural dungeon generator for Unity using the ProBuilder API. Generates dungeons entirely from code with no prefabs required.

## Features

- **Procedural Generation**: Creates random dungeon layouts with rooms and corridors
- **ProBuilder Integration**: All meshes generated programmatically using ProBuilder API
- **No Prefabs**: Everything is created from code at runtime or in the editor
- **Customizable Parameters**: Control room count, sizes, wall heights, and more
- **Editor Integration**: Custom inspector with quick generation buttons
- **Material Support**: Assign different materials for floors, walls, and corridors

## Prerequisites

**Unity ProBuilder must be installed:**

1. Open Unity Package Manager (Window > Package Manager)
2. Search for "ProBuilder"
3. Click Install

Or add via manifest.json:
```json
"com.unity.probuilder": "5.2.2"
```

## Quick Start

### Setup

1. Create an empty GameObject in your scene
2. Add the `ProBuilderDungeonGenerator` component
3. Configure the settings in the inspector
4. Click "Generate Dungeon" button

### Basic Configuration

```
Dungeon Settings:
├── Room Count: 10                  # Number of rooms to generate
├── Min Room Size: (4, 4)           # Minimum room dimensions
├── Max Room Size: (10, 10)         # Maximum room dimensions
├── Wall Height: 3                  # Height of walls in units
├── Wall Thickness: 0.2             # Thickness of wall meshes
├── Corridor Width: 2               # Width of connecting corridors
├── Max Attempts: 100               # Max attempts to place rooms
└── Grid Spread: 50                 # Area for room placement
```

## Components

### ProBuilderDungeonGenerator.cs
Main generator component that orchestrates dungeon creation.

**Key Methods:**
- `GenerateDungeon()`: Create a new dungeon
- `ClearDungeon()`: Remove existing dungeon

**Inspector Settings:**
- **Room Count**: Number of rooms to generate (default: 10)
- **Min/Max Room Size**: Range for random room dimensions
- **Wall Height**: Height of walls (default: 3 units)
- **Wall Thickness**: Thickness of wall geometry (default: 0.2)
- **Corridor Width**: Width of corridors connecting rooms (default: 2)
- **Max Attempts**: Maximum placement attempts per room (default: 100)
- **Grid Spread**: Spatial area for room distribution (default: 50)
- **Materials**: Optional materials for floors, walls, and corridors
- **Seed**: Random seed (0 = random, any other value = deterministic)
- **Generate On Start**: Auto-generate when scene starts

### DungeonRoom.cs
Data class representing a single room in the dungeon.

**Properties:**
- `Position`: World position (Vector3Int)
- `Size`: Room dimensions (Vector3Int)
- `RoomObject`: Generated GameObject reference

**Methods:**
- `GetCenter()`: Returns room center in world space
- `Overlaps()`: Check collision with another room

### ProBuilderDungeonGeneratorEditor.cs
Custom Unity Editor for the generator with convenient buttons.

## How It Works

### Generation Process

1. **Initialization**
   - Clear any existing dungeon
   - Initialize random generator with seed
   - Create parent GameObject for organization

2. **Room Generation**
   - Randomly place rooms within grid spread
   - Check for overlaps with existing rooms
   - Create ProBuilder meshes for floors and walls
   - Retry up to max attempts if placement fails

3. **Corridor Generation**
   - Connect adjacent rooms in sequence
   - Create L-shaped corridors (horizontal + vertical segments)
   - Generate ProBuilder meshes for corridor floors and walls
   - Optionally create a loop by connecting last to first room

### Mesh Generation

All geometry is created using ProBuilder's `CreateShapeFromPolygon()` method:

```csharp
// Example: Creating a floor
pbMesh.CreateShapeFromPolygon(
    new Vector3[] {
        new Vector3(0, 0, 0),
        new Vector3(width, 0, 0),
        new Vector3(width, 0, depth),
        new Vector3(0, 0, depth)
    },
    0f,    // extrusion height (0 for flat floor)
    false  // flip normals
);
```

**Mesh Components:**
- **Floors**: Flat polygons extruded to 0 height
- **Walls**: Polygons extruded vertically to wall height
- **Corridors**: Thin rectangular meshes connecting rooms

## Usage Examples

### Generate at Runtime

```csharp
using RpgTalentTree.Core.Dungeon;

public class GameManager : MonoBehaviour
{
    [SerializeField] private ProBuilderDungeonGenerator generator;

    void Start()
    {
        // Generate with default settings
        generator.GenerateDungeon();
    }

    public void RegenerateDungeon()
    {
        generator.ClearDungeon();
        generator.GenerateDungeon();
    }
}
```

### Configure Programmatically

```csharp
// Get or add component
var generator = gameObject.AddComponent<ProBuilderDungeonGenerator>();

// Configure via reflection (private fields)
// Or expose public methods/properties as needed
generator.GenerateDungeon();
```

### Deterministic Generation

Set the `Seed` field to any non-zero value for reproducible dungeons:
```
Seed: 12345  // Will always generate the same dungeon
```

## Customization

### Adding Materials

1. Create materials in Unity
2. Assign to generator component:
   - Floor Material: Material for room floors
   - Wall Material: Material for all walls
   - Corridor Material: Material for corridor floors (uses floor material if null)

### Extending the Generator

Common modifications:

**Add Doorways:**
```csharp
// In CreateWalls(), check for adjacent rooms
// Skip wall segments where rooms connect
```

**Different Room Shapes:**
```csharp
// Modify CreateFloor() to use different polygon points
// Example: octagonal rooms, circular rooms, etc.
```

**Multi-level Dungeons:**
```csharp
// Add Y-axis variation in room placement
// Create stairs/ramps connecting different levels
```

**BSP Algorithm:**
```csharp
// Replace random placement with Binary Space Partitioning
// Creates more organized, tree-like dungeon structures
```

## Architecture

```
ProBuilderDungeonGenerator (MonoBehaviour)
├── Room Generation
│   ├── Random Placement Algorithm
│   ├── Overlap Detection
│   └── ProBuilder Mesh Creation
│       ├── Floor Polygons
│       └── Wall Extrusions
│
├── Corridor Generation
│   ├── L-shaped Path Finding
│   └── ProBuilder Mesh Creation
│       ├── Corridor Floors
│       └── Corridor Walls
│
└── Mesh Operations
    ├── CreateShapeFromPolygon()
    ├── ToMesh()
    └── Refresh()
```

## Performance Notes

- Generation is CPU-intensive (runs on main thread)
- ProBuilder mesh operations are synchronous
- Consider coroutine-based generation for large dungeons:

```csharp
IEnumerator GenerateDungeonAsync()
{
    // Generate a few rooms per frame
    yield return null;
}
```

## Troubleshooting

### Dungeon doesn't appear
- Check ProBuilder is installed
- Verify materials are assigned (optional but helpful for visibility)
- Check camera position/orientation
- Look for errors in Console

### Rooms overlap
- Increase `Max Attempts`
- Increase `Grid Spread`
- Decrease `Room Count`
- Adjust Min/Max Room Size

### Not enough rooms generated
- Increase `Max Attempts`
- Increase `Grid Spread`
- Reduce room sizes

### Performance issues
- Reduce `Room Count`
- Reduce `Wall Height` (fewer vertices)
- Simplify wall generation (remove corridor walls)
- Use coroutines for large dungeons

## Future Enhancements

Potential improvements:
- [ ] Doorway generation between rooms
- [ ] BSP-based room placement
- [ ] Multi-floor dungeons
- [ ] Room templates/types (treasure rooms, boss rooms)
- [ ] Decorative elements (pillars, furniture)
- [ ] Navmesh generation
- [ ] Minimap generation
- [ ] Spawn points for enemies/items
- [ ] Custom room shapes beyond rectangles

## References

- [Unity ProBuilder Documentation](https://docs.unity3d.com/Packages/com.unity.probuilder@latest)
- [ProBuilder API Reference](https://docs.unity3d.com/Packages/com.unity.probuilder@5.0/api/index.html)

## License

Part of the RpgTalentTree project.
