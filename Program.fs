namespace TaleOfKaistania

open System

module MainApp =

    [<EntryPoint>]
    let main argv =
        // The player starts with Generic Clothes pre-equipped.
        // AvailableOutfits starts empty because the user will choose 2 garments 
        // from the OutfitSelection screen right before entering the dungeon.
        let startingGarb = InitialData.createOutfit GenericClothes
        
        let defaultPlayer = {
            Hp = 50
            MaxHp = 50
            Credits = 3
            CurrentOutfit = startingGarb
            AvailableOutfits = [] // Population will be set dynamically via OutfitSelection screen
            Loot = 0
            ActiveDebuffs = []
            VisitedEvents = []
            OwnedRelics = []
            OwnedPotions = []
            FloorsCleared = 0
            PotionMultiplier = 1.0
            DoubleCreditsNextTurn = false
        }
        
        // Initialize the new meta-progression architecture tracking individual building tiers
        let initialUpgrades = { 
            LabFunds = 0
            N7Level = 0
            SportsComplexLevel = 0
            KaimaruLevel = 0
            DormLevel = 0
            E3Level = 0
            N1Level = 0
        }

        // Ignite the game loop terminal window state machine pipeline
        Engine.gameLoop defaultPlayer initialUpgrades MainMenu
        0