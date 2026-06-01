namespace TaleOfKaistania

open System.IO
open System.Text.Json
open System.Text.Json.Serialization

// Note: The 'SaveData' record type has been removed from here 
// because it is already cleanly declared inside Domain.fs!

module SaveSystem =
    let private saveFilePath = "savegame.json"
    
    let private options = 
        let opts = JsonSerializerOptions()
        opts.Converters.Add(JsonFSharpConverter())
        opts

    let saveGame (player: PlayerState) (upgrades: PermanentUpgrades) (screen: GameScreen) =
        try
            let data = { Player = player; Upgrades = upgrades; CurrentScreen = screen }
            let jsonString = JsonSerializer.Serialize(data, options)
            File.WriteAllText(saveFilePath, jsonString)
            printfn "\n💾 Game saved successfully!"
        with ex ->
            printfn "\n❌ Failed to save game: %s" ex.Message

    let loadGame () : SaveData option =
        if not (File.Exists(saveFilePath)) then
            None
        else
            try
                let jsonString = File.ReadAllText(saveFilePath)
                let data = JsonSerializer.Deserialize<SaveData>(jsonString, options)
                Some data
            with ex ->
                printfn "\n❌ Failed to load save file: %s" ex.Message
                None