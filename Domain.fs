namespace TaleOfKaistania

open System

type DebuffType =
    | Tired      
    | BurnOut    
    | Poison     
    | Sicken     
    | Lockout    

type EnemyIntent =
    | WillAttack of Damage: int
    | WillBuff         
    | WillDebuff of Debuff: DebuffType
    | ChargingUp       

type Enemy = { 
    Name: string
    Hp: int
    MaxHp: int
    BaseDamage: int
    CurrentIntent: EnemyIntent 
    TurnsSinceLastAttack: int 
}

type OutfitType = 
    | GenericClothes
    | BuilderGetup     // Linked to N7
    | GymClothes       // Linked to Sports Complex
    | ChefClothes      // Linked to Kaimaru
    | Pajamas          // Linked to Dorm
    | LabCoat          // Linked to E3
    | HackerClothes    // Linked to N1

type Skill = { 
    Name: string
    CreditCost: int 
    BaseDamage: int 
    BaseHealing: int
    Description: string
}

type Outfit = { 
    Type: OutfitType 
    Name: string 
    Skills: Skill list 
}

type RelicType =
    | KaistCoffeeCard     
    | ReinforcedClipboard 
    | GoldenPlaque        
    | EmergencyDefib      
    | GradStudentCap      
    | ShintongKeyring     
    | LabSafetyGoggles    
    | OverheatedHeatsink  
    | ResearchEndowment   

type PotionType =
    | ThesisAidJuice      
    | OverclockSerum      
    | SmokescreenVial     
    | AntidotePill        
    | ElixirOfFocus       
    | VitaminGummy        
    | StarlightBrew       
    | NaniteShieldVial    
    | UnstableCatalyst    

type EventId =
    | E1 | E2 | E3 | E4 | E5 | E6 | E7 | E8 | E9 | E10
    | E11 | E12 | E13 | E14 | E15 | E16 | E17 | E18 | E19 | E20
    | E21 | E22 | E23 | E24 | E25 | E26 | E27 | E28 | E29 | E30
    | E31 | E32 | E33 | E34 | E35

type PlayerState = { 
    Hp: int
    MaxHp: int
    Credits: int
    CurrentOutfit: Outfit
    AvailableOutfits: Outfit list
    Loot: int                     
    ActiveDebuffs: (DebuffType * int) list 
    VisitedEvents: EventId list
    OwnedRelics: RelicType list 
    OwnedPotions: PotionType list 
    FloorsCleared: int            
    PotionMultiplier: float       
    DoubleCreditsNextTurn: bool   
}

type EventChoice = {
    Text: string
    RequirementText: string
    IsAvailable: PlayerState -> bool
    Execute: PlayerState -> PlayerState * string
}

type CampusEvent = {
    Id: EventId
    Title: string
    Description: string
    Choices: EventChoice list
}

type PermanentUpgrades = { 
    LabFunds: int                 
    N7Level: int                  
    SportsComplexLevel: int       
    KaimaruLevel: int             
    DormLevel: int                
    E3Level: int                  
    N1Level: int                  
}

type FloorPosition = {
    Act: int
    Floor: int
}

type GameScreen =
    | MainMenu
    | OutfitSelection of Outfit list 
    | MapSelection of FloorPosition * StageType list 
    | Gameplay of FloorPosition * StageType
    | CampusMenu
    | GameOver of bool * int 

and StageType =
    | Combat of Enemy
    | BossCombat of Enemy
    | Reward          
    | Occurrence of CampusEvent 
    | MiniGame        
    | Campsite        
    | Store           

type SaveData = {
    Player: PlayerState
    Upgrades: PermanentUpgrades
    CurrentScreen: GameScreen
}

module InitialData =
    let OUTFIT_SWAP_COST = 1
    let MAX_POTION_SLOTS = 3

    let getOutfitDescription = function
        | GenericClothes -> "Balanced distribution layout traits."
        | BuilderGetup -> "Specializes in heavy burst damage and scaling potion item effectiveness."
        | GymClothes -> "Specializes in absolute high damage output and status recovery self-buffs."
        | ChefClothes -> "Specializes in purging negative status debuffs and heavy single target healing."
        | Pajamas -> "Focuses on massive utility setup, doubling turn stipend incomes."
        | LabCoat -> "Focuses on manipulating vulnerabilities, ticking down toxic poison damage over time."
        | HackerClothes -> "Inflicts catastrophic utility shutdowns while siphoning credit reserves."

    let getPotionDescription = function
        | ThesisAidJuice   -> "Instantly restores 20 HP safely (scales with Builder Getup traits)."
        | OverclockSerum   -> "Instantly injects +3 Credits directly into your current action phase turn pool."
        | SmokescreenVial  -> "Deals 15 damage and forces an enemy intent transformation reset into a standard Buff."
        | AntidotePill     -> "Completely purges all active player negative debuff metrics instantly."
        | ElixirOfFocus    -> "Deals 30 raw armor-shredding damage straight to the enemy target."
        | VitaminGummy     -> "Restores 10 HP and removes Tired state restrictions if active."
        | StarlightBrew    -> "Generates a massive +6 Credit action surge, but inflicts BurnOut for 2 turns."
        | NaniteShieldVial -> "Applies cell shield healing for 12 HP and neutralizes ticking Poison tracks."
        | UnstableCatalyst -> "Sprays corrosive elements dealing 18 damage from tactical fallout."

    // BALANCING ADJUSTMENT: Significantly smoothed and cheaper entry pricing curve
    let getUpgradeCost currentLevel =
        if currentLevel >= 10 then Int32.MaxValue
        else (currentLevel * 10) + 15 // Level 0->1: 15, Level 1->2: 25, Level 2->3: 35... Max level: 105

    let getBuildingHpBonus lvl =
        let mutable bonus = 0
        if lvl >= 5 then bonus <- bonus + 15
        if lvl >= 10 then bonus <- bonus + 20
        bonus

    let getTotalBonusHp (upgrades: PermanentUpgrades) =
        getBuildingHpBonus upgrades.N7Level +
        getBuildingHpBonus upgrades.SportsComplexLevel +
        getBuildingHpBonus upgrades.KaimaruLevel +
        getBuildingHpBonus upgrades.DormLevel +
        getBuildingHpBonus upgrades.E3Level +
        getBuildingHpBonus upgrades.N1Level

    let getSkillScale clothingType (upgrades: PermanentUpgrades) =
        let lvl = 
            match clothingType with
            | GenericClothes -> (upgrades.N7Level + upgrades.SportsComplexLevel + upgrades.KaimaruLevel + upgrades.DormLevel + upgrades.E3Level + upgrades.N1Level) / 6
            | BuilderGetup -> upgrades.N7Level
            | GymClothes -> upgrades.SportsComplexLevel
            | ChefClothes -> upgrades.KaimaruLevel
            | Pajamas -> upgrades.DormLevel
            | LabCoat -> upgrades.E3Level
            | HackerClothes -> upgrades.N1Level
        1.0 + (float lvl * 0.15)

    let getSkillsForOutfit clothingType =
        match clothingType with
        | GenericClothes -> [ 
            { Name = "Standard Strike"; CreditCost = 1; BaseDamage = 10; BaseHealing = 0; Description = "Deals balanced combat damage to target." }
            { Name = "Self Optimize"; CreditCost = 2; BaseDamage = 0; BaseHealing = 0; Description = "Buffs yourself: Permanently increases your baseline turn income by +1 Credit." }
            { Name = "First Aid Patch"; CreditCost = 1; BaseDamage = 0; BaseHealing = 12; Description = "Patches wounds to heal your vitality." } 
          ]
        | BuilderGetup -> [
            { Name = "Wrench Smash"; CreditCost = 2; BaseDamage = 22; BaseHealing = 0; Description = "Deals heavy blunt structural damage." }
            { Name = "Fortify Pack"; CreditCost = 1; BaseDamage = 0; BaseHealing = 0; Description = "Modifies inventory vectors: Instantly increases potion effectiveness by +50% for this combat run." }
          ]
        | GymClothes -> [
            { Name = "Plyo Slam"; CreditCost = 3; BaseDamage = 38; BaseHealing = 0; Description = "Unleashes massive charge burst damage." }
            { Name = "Adrenaline Rush"; CreditCost = 1; BaseDamage = 0; BaseHealing = 0; Description = "Buffs player: Removes 'Tired' status and grants +5 structural Max HP mid-match." }
          ]
        | ChefClothes -> [
            { Name = "Cleaver Slash"; CreditCost = 2; BaseDamage = 18; BaseHealing = 0; Description = "Slashes target efficiently." }
            { Name = "Kaimaru Special"; CreditCost = 2; BaseDamage = 0; BaseHealing = 20; Description = "Completely purges all active debuffs and heals player vitality." }
          ]
        | Pajamas -> [
            { Name = "Pillow Toss"; CreditCost = 1; BaseDamage = 8; BaseHealing = 0; Description = "Light tracking chip attack." }
            { Name = "Power Nap"; CreditCost = 2; BaseDamage = 0; BaseHealing = 0; Description = "Focuses mind: Guarantees your Credit income stipend is DOUBLED next turn loop." }
          ]
        | LabCoat -> [
            { Name = "Acid Spray"; CreditCost = 1; BaseDamage = 8; BaseHealing = 0; Description = "Debuffs enemy: Inflicts 2 turns of Sicken status." }
            { Name = "Poison Detonation"; CreditCost = 2; BaseDamage = 14; BaseHealing = 0; Description = "Instantly stacks 3 turns of severe Poison DoT tracking on target." }
          ]
        | HackerClothes -> [
            { Name = "Mainframe Exploit"; CreditCost = 2; BaseDamage = 0; BaseHealing = 0; Description = "Inflicts severe system lock: Stacks Tired and Lockout on the enemy for 2 turns." }
            { Name = "Siphon Data"; CreditCost = 2; BaseDamage = 12; BaseHealing = 0; Description = "Steals metrics: Deals damage and guarantees your Credit stipend is DOUBLED next turn." }
          ]

    let createOutfit clothingType = {
        Type = clothingType
        Name = match clothingType with
               | GenericClothes -> "Generic Clothes" | BuilderGetup -> "Builder Getup (N7)" | GymClothes -> "Gym Clothes (Sports Complex)"
               | ChefClothes -> "Chef Clothes (Kaimaru)" | Pajamas -> "Pajamas (Dorm)" | LabCoat -> "Lab Coat (E3)" | HackerClothes -> "Hacker Clothes (N1)"
        Skills = getSkillsForOutfit clothingType
    }

    // BALANCING ADJUSTMENT: Boss HP & Base Dmg scaled upwards to match high credit player curves (Slay the Spire style math)
    let act1Boss = { Name = "Midterm Exam (The Gatekeeper)"; Hp = 80; MaxHp = 80; BaseDamage = 16; CurrentIntent = WillAttack 16; TurnsSinceLastAttack = 0 }
    let act2Boss = { Name = "Final Exam (The Filter Course)"; Hp = 140; MaxHp = 140; BaseDamage = 26; CurrentIntent = WillBuff; TurnsSinceLastAttack = 1 }
    let act3Boss = { Name = "The Ultimate Thesis Defense Board"; Hp = 240; MaxHp = 240; BaseDamage = 35; CurrentIntent = ChargingUp; TurnsSinceLastAttack = 1 }