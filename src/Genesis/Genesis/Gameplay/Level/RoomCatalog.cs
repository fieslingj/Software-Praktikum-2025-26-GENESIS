using System.Collections.Generic;
using System.Linq;
using Genesis.Architecture;

namespace Genesis.Gameplay.Level;

public static class RoomCatalog
{
    private static List<RoomDefinition> sMAllRooms =
    [
        // === TECHDEMO ===
        new RoomDefinition { Id = "techdemo", MapPath ="Maps/map_techdemo", Type = RoomType.TechDemo },
        
        // === STARTING ROOMS ===
        new RoomDefinition { Id = "start_01", MapPath ="Maps/map_level1_start", Type = RoomType.Start },
        new RoomDefinition { Id = "start_02", MapPath ="Maps/map_level2_start", Type = RoomType.Start },
        new RoomDefinition { Id = "start_03", MapPath ="Maps/map_level3_start", Type = RoomType.Start },

        // new RoomDefinition{ Id ="demo_map", MapPath ="Maps/map_demo", RoomType.Start),
        
        // === COMMON ROOMS ===
        new RoomDefinition { Id = "common_1_01", MapPath ="Maps/map_level1_1", Type =RoomType.Common},
        new RoomDefinition{ Id ="common_1_02", MapPath ="Maps/map_level1_2", Type =RoomType.Common},
        new RoomDefinition{ Id ="common_1_03", MapPath ="Maps/map_level1_3", Type =RoomType.Common},
        new RoomDefinition{ Id ="common_1_04", MapPath ="Maps/map_level1_4", Type =RoomType.Common},
        new RoomDefinition{ Id ="common_1_05", MapPath ="Maps/map_level1_5", Type =RoomType.Common},
        new RoomDefinition{ Id ="common_1_06", MapPath ="Maps/map_level1_6", Type =RoomType.Common},
        new RoomDefinition{ Id ="common_2_01", MapPath ="Maps/map_level2_1", Type =RoomType.Common},
        new RoomDefinition{ Id ="common_2_02", MapPath ="Maps/map_level2_2", Type =RoomType.Common},
        new RoomDefinition{ Id ="common_2_03", MapPath ="Maps/map_level2_3", Type =RoomType.Common},
        new RoomDefinition{ Id ="common_2_04", MapPath ="Maps/map_level2_4", Type =RoomType.Common},
        new RoomDefinition{ Id ="common_2_05", MapPath ="Maps/map_level2_5", Type =RoomType.Common},
        new RoomDefinition{ Id ="common_2_06", MapPath ="Maps/map_level2_6", Type =RoomType.Common},
        new RoomDefinition{ Id ="common_3_01", MapPath ="Maps/map_level3_1", Type =RoomType.Common},
        new RoomDefinition{ Id ="common_3_02", MapPath ="Maps/map_level3_2", Type =RoomType.Common},
        new RoomDefinition{ Id ="common_3_03", MapPath ="Maps/map_level3_3", Type =RoomType.Common},
        new RoomDefinition{ Id ="common_3_04", MapPath ="Maps/map_level3_4", Type =RoomType.Common},
        new RoomDefinition{ Id ="common_3_05", MapPath ="Maps/map_level3_5", Type =RoomType.Common},
        new RoomDefinition{ Id ="common_3_06", MapPath ="Maps/map_level3_6", Type =RoomType.Common},
        
        // === BOSS ROOMS ===
        new RoomDefinition { Id = "boss_01", MapPath ="Maps/map_level1_boss", Type = RoomType.Boss },
        new RoomDefinition { Id = "boss_02", MapPath ="Maps/map_level2_boss", Type = RoomType.Boss },
        new RoomDefinition { Id = "boss_03", MapPath ="Maps/map_level3_boss", Type = RoomType.Boss },
    ];
    
    //rooms in layer 1 , for generating layer 1
    private static List<RoomDefinition> sSLayer1Rooms =
    [
        
        // === STARTING ROOMS ===
        new RoomDefinition { Id = "start_01", MapPath ="Maps/map_level1_start", Type = RoomType.Start },
        
        // === COMMON ROOMS ===
        new RoomDefinition{ Id ="common_1_01", MapPath ="Maps/map_level1_1", Type =RoomType.Common},
        new RoomDefinition{ Id ="common_1_02", MapPath ="Maps/map_level1_2", Type =RoomType.Common},
        new RoomDefinition{ Id ="common_1_03", MapPath ="Maps/map_level1_3", Type =RoomType.Common},
        new RoomDefinition{ Id ="common_1_04", MapPath ="Maps/map_level1_4", Type =RoomType.Common},
        new RoomDefinition{ Id ="common_1_05", MapPath ="Maps/map_level1_5", Type =RoomType.Common},
        new RoomDefinition{ Id ="common_1_06", MapPath ="Maps/map_level1_6", Type =RoomType.Common},

        
        // === BOSS ROOMS ===
        new RoomDefinition { Id = "boss_01", MapPath ="Maps/map_level1_boss", Type = RoomType.Boss },
    ];
    
    //rooms in layer 2 , for generating layer 2
    private static List<RoomDefinition> sSLayer2Rooms =
    [
        // === STARTING ROOMS ===
        new RoomDefinition { Id = "start_02", MapPath ="Maps/map_level2_start", Type = RoomType.Start },
        
        // === COMMON ROOMS ===
        new RoomDefinition{ Id ="common_2_01", MapPath ="Maps/map_level2_1", Type =RoomType.Common},
        new RoomDefinition{ Id ="common_2_02", MapPath ="Maps/map_level2_2", Type =RoomType.Common},
        new RoomDefinition{ Id ="common_2_03", MapPath ="Maps/map_level2_3", Type =RoomType.Common},
        new RoomDefinition{ Id ="common_2_04", MapPath ="Maps/map_level2_4", Type =RoomType.Common},
        new RoomDefinition{ Id ="common_2_05", MapPath ="Maps/map_level2_5", Type =RoomType.Common},
        new RoomDefinition{ Id ="common_2_06", MapPath ="Maps/map_level2_6", Type =RoomType.Common},
        
        // === BOSS ROOMS ===
        new RoomDefinition { Id = "boss_02", MapPath ="Maps/map_level2_boss", Type = RoomType.Boss },
    ];
    
    //rooms in layer 3 , for generating layer 3
    private static List<RoomDefinition> sSLayer3Rooms =
    [
        // === STARTING ROOMS ===
        new RoomDefinition{ Id ="start_03", MapPath ="Maps/map_level3_start",Type = RoomType.Start},
        
        // === COMMON ROOMS ===
        new RoomDefinition{ Id ="common_3_01", MapPath ="Maps/map_level3_1", Type =RoomType.Common},
        new RoomDefinition{ Id ="common_3_02", MapPath ="Maps/map_level3_2", Type =RoomType.Common},
        new RoomDefinition{ Id ="common_3_03", MapPath ="Maps/map_level3_3", Type =RoomType.Common},
        new RoomDefinition{ Id ="common_3_04", MapPath ="Maps/map_level3_4", Type =RoomType.Common},
        new RoomDefinition{ Id ="common_3_05", MapPath ="Maps/map_level3_5", Type =RoomType.Common},
        new RoomDefinition{ Id ="common_3_06", MapPath ="Maps/map_level3_6", Type =RoomType.Common},
        
        // === BOSS ROOMS ===
        new RoomDefinition{ Id ="boss_03", MapPath ="Maps/map_level3_boss", Type = RoomType.Boss},
    ];

    public static RoomDefinition PickRandomByType(RandomService random, RoomType type)
    {
        var rooms = new List<RoomDefinition>();
        foreach (var room in sMAllRooms)
        {
            if (type != room.Type) { continue; }
            rooms.Add(room);
        }
        
        return rooms[random.Next(rooms.Count)];
    }
    
    //pick random room only from one layer
    public static RoomDefinition PickRandomByTypeAndLayer(RandomService random, RoomType type, int layerNumber)
    {
        var layerRooms = sMAllRooms;
        switch (layerNumber){
            case 1:
                layerRooms = sSLayer1Rooms;
                break;
            case 2:
                layerRooms = sSLayer2Rooms;
                break;
            case 3:
                layerRooms = sSLayer3Rooms;
                break;
            default:
                layerRooms = sMAllRooms;
                break;
        }
        
        
        
        var rooms = new List<RoomDefinition>();
        foreach (var room in layerRooms)
        {
            if (type != room.Type) { continue; }
            rooms.Add(room);
        }
        
        return rooms[random.Next(rooms.Count)];
    }
    
    public static RoomDefinition GetTechDemoRoom() => sMAllRooms.First(r => r.Type == RoomType.TechDemo);
}