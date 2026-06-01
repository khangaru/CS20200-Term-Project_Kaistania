namespace TaleOfKaistania

open System

module Engine =

    let private rand = System.Random()

    let private hasDebuff debuffType (player: PlayerState) =
        player.ActiveDebuffs |> List.exists (fun (t, _) -> t = debuffType)

    let private rollNextIntent (enemy: Enemy) =
        if enemy.TurnsSinceLastAttack >= 2 then
            match rand.Next(0, 2) with
            | 0 -> { enemy with CurrentIntent = WillAttack enemy.BaseDamage; TurnsSinceLastAttack = 0 }
            | _ -> { enemy with CurrentIntent = ChargingUp; TurnsSinceLastAttack = 0 }
        else
            match rand.Next(0, 4) with
            | 0 -> { enemy with CurrentIntent = WillAttack enemy.BaseDamage; TurnsSinceLastAttack = 0 }
            | 1 -> { enemy with CurrentIntent = WillBuff; TurnsSinceLastAttack = enemy.TurnsSinceLastAttack + 1 }
            | 2 -> { enemy with CurrentIntent = WillDebuff (MapGenerator.getRandomDebuff()); TurnsSinceLastAttack = enemy.TurnsSinceLastAttack + 1 }
            | _ -> { enemy with CurrentIntent = ChargingUp; TurnsSinceLastAttack = enemy.TurnsSinceLastAttack + 1 }

    let private getIntentWarning = function
        | WillAttack dmg -> sprintf "⚔️  [ATTACK] Prepares to assault your sanity for %d points!" dmg
        | WillBuff       -> "🛡️  [BUFF] Channeling metrics to permanently increase base damage (+4)!"
        | WillDebuff deb -> sprintf "💤  [DEBUFF] Schemes to inflict (%A) upon you next turn!" deb
        | ChargingUp     -> "🔥  [CHARGE-UP] WARNING! Next turn's strike will deal DOUBLE damage!"

    let private executePotionEffect potion enemy player =
        let modScale = player.PotionMultiplier
        match potion with
        | ThesisAidJuice ->
            let amt = int (20.0 * modScale)
            printfn "\n🧪 You chug Thesis-Aid Juice! Restored %d HP (Scale: x%.1f)." amt modScale
            (enemy, { player with Hp = Math.Min(player.MaxHp, player.Hp + amt) })
        | OverclockSerum ->
            let amt = int (3.0 * modScale)
            printfn "\n🧪 You shatter Overclock Serum! Flooded +%d Credits (Scale: x%.1f)." amt modScale
            (enemy, { player with Credits = player.Credits + amt })
        | SmokescreenVial ->
            let amt = int (15.0 * modScale)
            printfn "\n🧪 You hurl a Smokescreen Vial! Dealt %d blast damage and reset intent." amt
            ( { enemy with Hp = enemy.Hp - amt; CurrentIntent = WillBuff }, player )
        | AntidotePill ->
            printfn "\n🧪 You consume an Antidote Pill! All status constraints cleansed cleanly."
            (enemy, { player with ActiveDebuffs = [] })
        | ElixirOfFocus ->
            printfn "\n🧪 You drink Elixir of Focus! Deals 30 explosive blast damage!"
            ({ enemy with Hp = enemy.Hp - 30 }, player)
        | VitaminGummy ->
            printfn "\n🧪 You chew a Vitamin Gummy! Restored 10 HP and cleared Tired state."
            let filtered = player.ActiveDebuffs |> List.filter (fun (t, _) -> t <> Tired)
            (enemy, { player with Hp = Math.Min(player.MaxHp, player.Hp + 10); ActiveDebuffs = filtered })
        | StarlightBrew ->
            printfn "\n🧪 You chug Starlight Brew! Gained +6 Credits, but inflicts BurnOut!"
            (enemy, { player with Credits = player.Credits + 6; ActiveDebuffs = (BurnOut, 2) :: player.ActiveDebuffs })
        | NaniteShieldVial ->
            printfn "\n🧪 You apply Nanite Shield! Restored 12 HP and neutralizes Poison status."
            let filtered = player.ActiveDebuffs |> List.filter (fun (t, _) -> t <> Poison)
            (enemy, { player with Hp = Math.Min(player.MaxHp, player.Hp + 12); ActiveDebuffs = filtered })
        | UnstableCatalyst ->
            printfn "\n🧪 You spray Unstable Catalyst! Foe takes 18 damage from tactical fallout!"
            ({ enemy with Hp = enemy.Hp - 18 }, player)

    let private calculateEndRunFunds (player: PlayerState) (won: bool) =
        let baseScore = player.FloorsCleared * 12 + (player.OwnedRelics.Length * 20)
        if won then baseScore + 200 else baseScore + (player.Loot / 4)

    let rec private gameLoopWithEmotion (player: PlayerState) (upgrades: PermanentUpgrades) (screen: GameScreen) (emotion: EmotionalState) =
        Console.Clear()
        let relicMaxHpBonus = if player.OwnedRelics |> List.contains GradStudentCap then 25 else 0
        let campusHpBonus = InitialData.getTotalBonusHp upgrades
        // Some PermanentUpgrades versions may not include a direct BonusMaxHp field.
        // Gracefully handle absence by falling back to zero bonus here.
        let upgradeHpBonus = 0
        let maxHp = player.MaxHp + upgradeHpBonus + relicMaxHpBonus + campusHpBonus
        
        printfn "==========================================================="
        printfn " 🔺  THE CHRONICLES OF KAISTANIA: NUPJUKI'S ACADEMIC ORDEAL  🔺"
        printfn "==========================================================="
        
        match screen with
        | MapSelection (pos, _) | Gameplay (pos, _) ->
            printfn "  📍  Location: Act %d — Labyrinth Floor %d" pos.Act pos.Floor
        | _ -> ()

        printfn "  ❤️  Vitality:      %d / %d HP (Campus Bonus: +%d)" (Math.Min(player.Hp, maxHp)) maxHp campusHpBonus
        printfn "  🪙  Lab Stipend:   %d Credits" player.Credits
        printfn "  🪙  Self-Funds:    %d Coins" player.Loot               
        printfn "  🏛️  Hub Lab-Funds: %d Account Balance" upgrades.LabFunds 
        printfn "  🛡️  Equipped Garb: [%s]" player.CurrentOutfit.Name
        
        if not (List.isEmpty player.OwnedRelics) then
            let relicStrings = player.OwnedRelics |> List.map (fun r -> sprintf "[%A]" r)
            printfn "  🔮  Passive Relics: %s" (String.concat " " relicStrings)
        
        if not (List.isEmpty player.ActiveDebuffs) then
            let debuffStrings = player.ActiveDebuffs |> List.map (fun (t, d) -> sprintf "%A(%dt)" t d)
            printfn "  ⚠️  Active Status: [%s]" (String.concat ", " debuffStrings)
        
        printfn "-----------------------------------------------------------"

        NupjukiRenderer.drawNupjukiFace (Math.Min(player.Hp, maxHp)) maxHp player.CurrentOutfit.Name emotion

        match screen with
        | MainMenu ->
            printfn "Welcome, weary researcher Nupjuki. The campus expansion demands your sacrifice."
            printfn "\n  [1] ⚔️  Venture into Labyrinth (Choose Loadout & Start)"
            printfn "  [2] 💾  Invoke Quantum Anchor (Load Previous Progress Save)"
            printfn "  [3] 🏛️  Visit the KAIST Infrastructure (Upgrade Campus Buildings)"
            printf "\nChoose path: "
            let input = Console.ReadLine()
            
            // --- SECRET CHEAT CODE DETECTOR PHASES ---
            if input = "4.3 gpa nubzuki" then
                printfn "\n🎓 [ADMIN PROTOCOL]: RADICAL ACADEMIC EXCELLENCE INJECTED!"
                printfn "Everything maxed out successfully. You have ascended."
                Console.ReadKey() |> ignore
                let maxedUpgrades = { LabFunds = 9999; N7Level = 10; SportsComplexLevel = 10; KaimaruLevel = 10; DormLevel = 10; E3Level = 10; N1Level = 10 }
                gameLoopWithEmotion player maxedUpgrades MainMenu Happy
            else
                match input with
                | "1" -> 
                    // BALANCING ADJUSTMENT: Starting Credits bumped to 3 budget baseline
                    let freshPlayer = { player with Credits = 3 }
                    let allAuxPool = [ BuilderGetup; GymClothes; ChefClothes; Pajamas; LabCoat; HackerClothes ] |> List.map InitialData.createOutfit
                    gameLoopWithEmotion freshPlayer upgrades (OutfitSelection allAuxPool) Normal
                | "2" ->
                    match SaveSystem.loadGame() with
                    | Some savedData ->
                        printfn "\n💾 Quantum anchor found! Restoring baseline timeline parameters..."
                        Console.ReadKey() |> ignore
                        gameLoopWithEmotion savedData.Player savedData.Upgrades savedData.CurrentScreen Normal
                    | None ->
                        printfn "\n[!] The simulation backup files are completely empty."
                        Console.ReadKey() |> ignore
                        gameLoopWithEmotion player upgrades MainMenu Normal
                | "3" -> gameLoopWithEmotion player upgrades CampusMenu Normal
                | _ -> gameLoopWithEmotion player upgrades MainMenu Normal

        | OutfitSelection availablePool ->
            printfn "👕 LOADOUT CHOICE TERMINAL: Select exactly TWO auxiliary garments to bring along."
            printfn "Generic Clothes are already pre-equipped automatically."
            printfn "-----------------------------------------------------------"
            availablePool |> List.iteri (fun idx o -> printfn "  [%d] %s" (idx + 1) o.Name)
            printf "\nEnter index number of your FIRST auxiliary garment choice: "
            let firstInput = Console.ReadLine()
            printf "Enter index number of your SECOND auxiliary garment choice: "
            let secondInput = Console.ReadLine()
            
            match Int32.TryParse(firstInput), Int32.TryParse(secondInput) with
            | (true, fIdx), (true, sIdx) when fIdx > 0 && fIdx <= availablePool.Length && sIdx > 0 && sIdx <= availablePool.Length && fIdx <> sIdx ->
                let chosenFirst = availablePool.[fIdx - 1]
                let chosenSecond = availablePool.[sIdx - 1]
                
                let startOutfit = InitialData.createOutfit GenericClothes
                // BALANCING ADJUSTMENT: Baseline run turn credits explicitly anchored at 3
                let freshPlayer = { Hp = maxHp; MaxHp = player.MaxHp; Credits = 3; CurrentOutfit = startOutfit; AvailableOutfits = [startOutfit; chosenFirst; chosenSecond]; Loot = 0; ActiveDebuffs = []; VisitedEvents = []; OwnedRelics = []; OwnedPotions = []; FloorsCleared = 0; PotionMultiplier = 1.0; DoubleCreditsNextTurn = false }
                let startPos = { Act = 1; Floor = 1 }
                let firstChoices = MapGenerator.generateFloorChoices startPos []
                printfn "\nGarments anchored successfully! Plunging down into the fog gate..."
                Console.ReadKey() |> ignore
                gameLoopWithEmotion freshPlayer upgrades (MapSelection (startPos, firstChoices)) Normal
            | _ -> 
                printfn "\n❌ Invalid choices index selections. Try matching again."
                Console.ReadKey() |> ignore
                gameLoopWithEmotion player upgrades (OutfitSelection availablePool) Normal

        | MapSelection (pos, stages) ->
            printfn "🗺️  NAVI-MAP: Plotting paths through anomalies..."
            stages |> List.iteri (fun idx stage -> 
                match stage with
                | Combat e -> printfn "  [%d] ⚔️  FOE DETECTED: %s blocks the way!" (idx + 1) e.Name
                | Reward -> printfn "  [%d] 📦  TREASURE CACHE: Unclaimed Supplies." (idx + 1)
                | Campsite -> printfn "  [%d] 🏕️  BONFIRE: Safe Haven Pods." (idx + 1)
                | Store -> printfn "  [%d] 🏪  CAMPUS COMMISSARY Storefront" (idx + 1)
                | Occurrence e -> printfn "  [%d] 💬  CAMPUS ANOMALY: %s" (idx + 1) e.Title
                | BossCombat e -> printfn "  [%d] 👹  MILESTONE TRIAL: %s!" (idx + 1) e.Name
                | MiniGame -> printfn "  [%d] 🎮  MINI-GAME: Casual Challenge Event." (idx + 1)
            )
            printfn "  [S] 💾  SECURE SIMULATION: Save Current Run Progress & Quit"
            
            printf "\nMake your move: "
            let input = Console.ReadLine()
            
            if not (String.IsNullOrEmpty(input)) && input.ToUpper() = "S" then
                SaveSystem.saveGame player upgrades screen
                printfn "\nSystem parameters written to file core. Terminating execution loops..."
                Console.ReadKey() |> ignore
                gameLoopWithEmotion player upgrades MainMenu Normal
            else
                match Int32.TryParse(input) with
                | true, choice when choice > 0 && choice <= stages.Length ->
                    let chosenStage = stages.[choice - 1]
                    let startPlayer = if player.OwnedRelics |> List.contains KaistCoffeeCard then { player with Credits = player.Credits + 1 } else player
                    gameLoopWithEmotion startPlayer upgrades (Gameplay (pos, chosenStage)) Normal
                | _ -> gameLoopWithEmotion player upgrades (MapSelection (pos, stages)) Normal

        | Gameplay (pos, screen_stage) ->
            let nextPos = MapGenerator.getNextPosition pos
            let incrementedPlayer = { player with FloorsCleared = player.FloorsCleared + 1 }
            let nextChoices = MapGenerator.generateFloorChoices nextPos incrementedPlayer.VisitedEvents
            let currentScale = InitialData.getSkillScale player.CurrentOutfit.Type upgrades

            match screen_stage with
            | Combat enemy | BossCombat enemy ->
                printfn "⚠️  ENEMY ENGAGED: %s" (enemy.Name.ToUpper())
                printfn "❤️  Hostile Integrity: [%d/%d HP]" enemy.Hp enemy.MaxHp
                printfn "🔮  ENEMY INTENT: %s" (getIntentWarning enemy.CurrentIntent)
                printfn "✨  Current Garb Multiplier Factor: x%.2f" currentScale
                
                if not (List.isEmpty player.OwnedPotions) then
                    printfn "\n🧪 --- THY BELT POTIONS (0 Credit Cost) ---"
                    player.OwnedPotions |> List.iteri (fun idx p ->
                        printfn "  (P%d) %A: %s" (idx + 1) p (InitialData.getPotionDescription p)
                    )
                
                printfn "\n--- YOUR MANEUVERS ---"
                let skills = player.CurrentOutfit.Skills
                skills |> List.iteri (fun idx s -> 
                    let dmgEval = if s.BaseDamage > 0 then int (float s.BaseDamage * currentScale) else 0
                    let finalDmg = if hasDebuff Tired player && dmgEval > 0 then int (float dmgEval * 0.75) else dmgEval
                    let healEval = if s.BaseHealing > 0 then int (float s.BaseHealing * currentScale) else 0
                    printfn "  (%d) 🔥  %s [Cost: %d Credits] — Dmg: %d | Heal: %d\n      └ %s" (idx + 1) s.Name s.CreditCost finalDmg healEval s.Description
                )
                
                let swapIdxStart = skills.Length + 1
                let alternativeOutfits = player.AvailableOutfits |> List.filter (fun o -> o.Type <> player.CurrentOutfit.Type)
                alternativeOutfits |> List.iteri (fun idx o ->
                    printfn "  (%d) 🔄  GEAR SWAP: Shift to [%s] (Cost: %d Credit)" (swapIdxStart + idx) o.Name InitialData.OUTFIT_SWAP_COST
                )
                printfn "  [E] ⌛  END TURN: Commit actions and brace for incoming intent"
                printf "\nUnleash action: "
                let input = Console.ReadLine().ToUpper()
                
                if input.StartsWith("P") && input.Length > 1 then
                    match Int32.TryParse(input.Substring(1)) with
                    | true, idx when idx > 0 && idx <= player.OwnedPotions.Length ->
                        let targetPotion = player.OwnedPotions.[idx - 1]
                        let cleanPotions = player.OwnedPotions |> List.indexed |> List.filter (fun (i, _) -> i <> (idx - 1)) |> List.map snd
                        let midEnemy, midPlayer = executePotionEffect targetPotion enemy { player with OwnedPotions = cleanPotions }
                        Console.ReadKey() |> ignore
                        if midEnemy.Hp <= 0 then
                            printfn "\n🎉 TRIUMPH! The target collapsed!"
                            let droppedPot = MapGenerator.getRandomPotion()
                            let pWithDrops = MapGenerator.addPotion droppedPot incrementedPlayer
                            let rewardsPlayer = { pWithDrops with Loot = pWithDrops.Loot + 15; Credits = pWithDrops.Credits + 3 }
                            gameLoopWithEmotion rewardsPlayer upgrades (MapSelection (nextPos, nextChoices)) Happy
                        else
                            let nextCombatState = match screen_stage with BossCombat _ -> BossCombat midEnemy | _ -> Combat midEnemy
                            gameLoopWithEmotion midPlayer upgrades (Gameplay (pos, nextCombatState)) Normal
                    | _ -> gameLoopWithEmotion player upgrades (Gameplay (pos, screen_stage)) Normal

                elif input = "E" then
                    // BALANCING ADJUSTMENT: Baseline regeneration stipend increased to 3 Credits each turn!
                    let baseIncome = if hasDebuff BurnOut player then 0 else 3
                    let dynamicStipend = if player.DoubleCreditsNextTurn then baseIncome * 2 else baseIncome
                    let stipendIncome = if player.OwnedRelics |> List.contains KaistCoffeeCard && dynamicStipend > 0 then dynamicStipend + 1 else dynamicStipend
                    
                    let mutable finalPlayerState = { player with DoubleCreditsNextTurn = false }
                    let mutable resolvedEnemy = enemy
                    
                    match enemy.CurrentIntent with
                    | WillAttack dmg ->
                        let rawDmg = if hasDebuff Sicken player then int (float dmg * 1.5) else dmg
                        let finalDmg = if player.OwnedRelics |> List.contains ReinforcedClipboard then Math.Max(1, rawDmg - 2) else rawDmg
                        printfn "\n💥 ATTACK! %s strikes, dealing %d damage!" enemy.Name finalDmg
                        finalPlayerState <- { finalPlayerState with Hp = finalPlayerState.Hp - finalDmg }
                        resolvedEnemy <- rollNextIntent { enemy with TurnsSinceLastAttack = 0 }
                    | WillBuff ->
                        printfn "\n🔥 BUFF! %s scales base parameters permanently (+4 Damage)!" enemy.Name
                        resolvedEnemy <- rollNextIntent { enemy with BaseDamage = enemy.BaseDamage + 4; TurnsSinceLastAttack = enemy.TurnsSinceLastAttack + 1 }
                    | WillDebuff deb ->
                        if deb = Sicken && player.OwnedRelics |> List.contains LabSafetyGoggles then
                            printfn "\n🔮 GOGGLES: Sicken blocked!"
                            resolvedEnemy <- rollNextIntent { enemy with TurnsSinceLastAttack = enemy.TurnsSinceLastAttack + 1 }
                        else
                            printfn "\n💤 DEBUFF! %s inflicts %A for 2 turns!" enemy.Name deb
                            finalPlayerState <- { finalPlayerState with ActiveDebuffs = (deb, 2) :: finalPlayerState.ActiveDebuffs }
                            resolvedEnemy <- rollNextIntent { enemy with TurnsSinceLastAttack = enemy.TurnsSinceLastAttack + 1 }
                    | ChargingUp ->
                        printfn "\n⚡ CHARGE COMPLETE! %s completes charging — dual strike incoming next phase!" enemy.Name
                        resolvedEnemy <- { enemy with CurrentIntent = WillAttack (enemy.BaseDamage * 2); TurnsSinceLastAttack = 0 }

                    if hasDebuff Poison finalPlayerState then
                        finalPlayerState <- { finalPlayerState with Hp = finalPlayerState.Hp - 6 }
                    let tickedDebuffs = finalPlayerState.ActiveDebuffs |> List.map (fun (t, d) -> (t, d - 1)) |> List.filter (fun (_, d) -> d > 0)
                    finalPlayerState <- { finalPlayerState with Credits = finalPlayerState.Credits + stipendIncome; ActiveDebuffs = tickedDebuffs }
                    Console.ReadKey() |> ignore

                    if finalPlayerState.Hp <= 0 && finalPlayerState.OwnedRelics |> List.contains EmergencyDefib then
                        printfn "\n⚡ EMERGENCY REVIVE: Resetting back at 20 HP!"
                        let savedRelics = finalPlayerState.OwnedRelics |> List.filter (fun r -> r <> EmergencyDefib)
                        finalPlayerState <- { finalPlayerState with Hp = 20; OwnedRelics = savedRelics }
                        Console.ReadKey() |> ignore

                    if finalPlayerState.Hp <= 0 then 
                        let earnedFunds = calculateEndRunFunds incrementedPlayer false
                        gameLoopWithEmotion { finalPlayerState with Hp = 0 } upgrades (GameOver (false, earnedFunds)) Pain
                    else 
                        gameLoopWithEmotion finalPlayerState upgrades (Gameplay (pos, match screen_stage with BossCombat _ -> BossCombat resolvedEnemy | _ -> Combat resolvedEnemy)) Normal
                else
                    match Int32.TryParse(input) with
                    | true, choice when choice > 0 && choice <= skills.Length ->
                        let selectedSkill = skills.[choice - 1]
                        if player.Credits < selectedSkill.CreditCost then
                            Console.ReadKey() |> ignore
                            gameLoopWithEmotion player upgrades (Gameplay (pos, screen_stage)) Normal
                        else
                            let baseDmgCalc = if selectedSkill.BaseDamage > 0 then int (float selectedSkill.BaseDamage * currentScale) else 0
                            let actualDmg = if hasDebuff Tired player && baseDmgCalc > 0 then int (float baseDmgCalc * 0.75) else baseDmgCalc
                            let actualHeal = if selectedSkill.BaseHealing > 0 then int (float selectedSkill.BaseHealing * currentScale) else 0
                            
                            let mutable updatedPlayer = { player with Credits = player.Credits - selectedSkill.CreditCost; Hp = Math.Min(player.Hp + actualHeal, maxHp) }
                            let mutable updatedEnemy = { enemy with Hp = enemy.Hp - actualDmg }
                            
                            printfn "\n💥 Executed [%s] with skill factor multiplier!" selectedSkill.Name
                            
                            // --- REFIXED / SOFTLOCK PURGED STATE ENGINE LINKS ---
                            match player.CurrentOutfit.Type with
                            | BuilderGetup when selectedSkill.Name = "Fortify Pack" ->
                                updatedPlayer <- { updatedPlayer with PotionMultiplier = updatedPlayer.PotionMultiplier + 0.5 }
                            | GymClothes when selectedSkill.Name = "Adrenaline Rush" ->
                                let cleanDebuffs = updatedPlayer.ActiveDebuffs |> List.filter (fun (t, _) -> t <> Tired)
                                updatedPlayer <- { updatedPlayer with ActiveDebuffs = cleanDebuffs; MaxHp = updatedPlayer.MaxHp + 5; Hp = updatedPlayer.Hp + 5 }
                            | ChefClothes when selectedSkill.Name = "Kaimaru Special" ->
                                updatedPlayer <- { updatedPlayer with ActiveDebuffs = [] }
                            | Pajamas when selectedSkill.Name = "Power Nap" ->
                                updatedPlayer <- { updatedPlayer with DoubleCreditsNextTurn = true }
                            | LabCoat ->
                                if selectedSkill.Name = "Acid Spray" then
                                    updatedEnemy <- { updatedEnemy with CurrentIntent = WillDebuff Sicken }
                                elif selectedSkill.Name = "Poison Detonation" then
                                    updatedEnemy <- { updatedEnemy with TurnsSinceLastAttack = 0 }
                                    updatedPlayer <- { updatedPlayer with ActiveDebuffs = (Poison, 3) :: updatedPlayer.ActiveDebuffs }
                            | HackerClothes ->
                                if selectedSkill.Name = "Mainframe Exploit" then
                                    updatedEnemy <- { updatedEnemy with CurrentIntent = WillDebuff Lockout; TurnsSinceLastAttack = 0 }
                                elif selectedSkill.Name = "Siphon Data" then
                                    updatedPlayer <- { updatedPlayer with DoubleCreditsNextTurn = true }
                            | _ -> ()

                            Console.ReadKey() |> ignore
                            if updatedEnemy.Hp <= 0 then
                                printfn "\n🎉 TRIUMPH! Foe sundered!"
                                let pWithDrops = MapGenerator.addPotion (MapGenerator.getRandomPotion()) updatedPlayer
                                let rewardsPlayer = { pWithDrops with Loot = pWithDrops.Loot + 15; Credits = pWithDrops.Credits + 3 }
                                Console.ReadKey() |> ignore
                                if pos.Act = 3 && pos.Floor = 5 then 
                                    let earnedFunds = calculateEndRunFunds rewardsPlayer true
                                    gameLoopWithEmotion rewardsPlayer upgrades (GameOver (true, earnedFunds)) Happy
                                else gameLoopWithEmotion rewardsPlayer upgrades (MapSelection (nextPos, nextChoices)) Happy
                            else
                                let updatedCombatState = match screen_stage with BossCombat _ -> BossCombat updatedEnemy | _ -> Combat updatedEnemy
                                gameLoopWithEmotion updatedPlayer upgrades (Gameplay (pos, updatedCombatState)) Normal

                    | true, choice when choice >= swapIdxStart && choice < swapIdxStart + alternativeOutfits.Length ->
                        let targetOutfit = alternativeOutfits.[choice - swapIdxStart]
                        if hasDebuff Lockout player then
                            Console.ReadKey() |> ignore
                            gameLoopWithEmotion player upgrades (Gameplay (pos, screen_stage)) Normal
                        elif player.Credits < InitialData.OUTFIT_SWAP_COST then
                            Console.ReadKey() |> ignore
                            gameLoopWithEmotion player upgrades (Gameplay (pos, screen_stage)) Normal
                        else
                            Console.Clear()
                            printfn "==========================================================="
                            printfn " 👕  WARDROBE PARADIGM SHIFT"
                            printfn "==========================================================="
                            printfn "Target Garb:    %s" targetOutfit.Name
                            printf "Are you sure you want to change gear? (Y/N): "
                            match Console.ReadLine().ToUpper() with
                            | "Y" ->
                                let mutable updatedEnemy = enemy
                                if player.OwnedRelics |> List.contains OverheatedHeatsink then
                                    printfn "\n🔮 HEATSINK EXPLOSION: Switching garments vents heat, dealing 8 damage to the enemy!"
                                    updatedEnemy <- { enemy with Hp = enemy.Hp - 8 }
                                
                                let updatedPlayer = { player with CurrentOutfit = targetOutfit; Credits = player.Credits - InitialData.OUTFIT_SWAP_COST }
                                Console.ReadKey() |> ignore
                                if updatedEnemy.Hp <= 0 then
                                    let pWithDrops = MapGenerator.addPotion (MapGenerator.getRandomPotion()) incrementedPlayer
                                    let rewardsPlayer = { pWithDrops with Loot = pWithDrops.Loot + 15; Credits = pWithDrops.Credits + 3 }
                                    gameLoopWithEmotion rewardsPlayer upgrades (MapSelection (nextPos, nextChoices)) Happy
                                else
                                    gameLoopWithEmotion updatedPlayer upgrades (Gameplay (pos, match screen_stage with BossCombat _ -> BossCombat updatedEnemy | _ -> Combat updatedEnemy)) Normal
                            | _ -> gameLoopWithEmotion player upgrades (Gameplay (pos, screen_stage)) Normal
                    | _ -> gameLoopWithEmotion player upgrades (Gameplay (pos, screen_stage)) Normal

            | Occurrence ev ->
                printfn "\n💬  CAMPUS EVENT: %s" ev.Title
                printfn "%s\n" ev.Description
                ev.Choices |> List.iteri (fun idx c ->
                    let availabilitySymbol = if c.IsAvailable player then "[✓]" else "[LOCKED]"
                    printfn "  (%d) %s %s — %s" (idx + 1) availabilitySymbol c.Text c.RequirementText
                )
                printf "\nMake your selection: "
                let input = Console.ReadLine()
                match Int32.TryParse(input) with
                | true, choice when choice > 0 && choice <= ev.Choices.Length ->
                    let targetChoice = ev.Choices.[choice - 1]
                    if not (targetChoice.IsAvailable player) then
                        Console.ReadKey() |> ignore
                        gameLoopWithEmotion player upgrades (Gameplay (pos, Occurrence ev)) Normal
                    else
                        let updatedPlayer, outcomeNarrative = targetChoice.Execute player
                        let finalPlayerWithHistory = { updatedPlayer with VisitedEvents = ev.Id :: updatedPlayer.VisitedEvents; FloorsCleared = updatedPlayer.FloorsCleared + 1 }
                        printfn "\n📜 %s" outcomeNarrative
                        Console.ReadKey() |> ignore
                        if finalPlayerWithHistory.Hp <= 0 then 
                            let earnedFunds = calculateEndRunFunds finalPlayerWithHistory false
                            gameLoopWithEmotion { finalPlayerWithHistory with Hp = 0 } upgrades (GameOver (false, earnedFunds)) Pain
                        else gameLoopWithEmotion finalPlayerWithHistory upgrades (MapSelection (nextPos, MapGenerator.generateFloorChoices nextPos finalPlayerWithHistory.VisitedEvents)) Happy
                | _ -> gameLoopWithEmotion player upgrades (Gameplay (pos, Occurrence ev)) Normal

            | Campsite ->
                printfn "🏕️  BONFIRE Pod Terminal Bay."
                printfn "  1. 💤  Power Nap: Restore your psyche (+25 HP)"
                printfn "  2. ☕  Caffeine Overdose: Synthesize +3 Credits"
                printf "\nChoose recovery: "
                match Console.ReadLine() with
                | "1" -> 
                    let p = { incrementedPlayer with Hp = Math.Min(player.Hp + 25, maxHp) }
                    Console.ReadKey() |> ignore
                    gameLoopWithEmotion p upgrades (MapSelection (nextPos, nextChoices)) Happy
                | _ -> 
                    let p = { incrementedPlayer with Credits = player.Credits + 3 }
                    Console.ReadKey() |> ignore
                    gameLoopWithEmotion p upgrades (MapSelection (nextPos, nextChoices)) Happy

            | Reward ->
                let droppedPotion = MapGenerator.getRandomPotion()
                let rollsRelic = rand.Next(0, 5) = 0 
                let structuralRelic = if rollsRelic then [MapGenerator.getRandomRelic()] else []
                printfn "📦  TREASURE CACHE UNLOCKED!"
                if rollsRelic then printfn "🔮 Passive Relic uncovered: %A" structuralRelic.[0]
                printfn "🧪 Stowed item: %A and gained +30 Coins!" droppedPotion
                // addLoot is not accessible from here; update Loot directly then add the potion
                let collectionPlayer = { incrementedPlayer with OwnedRelics = player.OwnedRelics @ structuralRelic; Loot = incrementedPlayer.Loot + 30 } |> MapGenerator.addPotion droppedPotion
                Console.ReadKey() |> ignore
                gameLoopWithEmotion collectionPlayer upgrades (MapSelection (nextPos, nextChoices)) Happy

            | Store ->
                printfn "🏪  CAMPUS COMMISSARY MERCHANT GATEWAY"
                printfn "-----------------------------------------------------------"
                printfn "  [1] 💉  Thesis-Aid Medkit (+20 HP)               — Costs 15 Coins"
                printfn "  [2] 🧪  Buy Random Potion (Combat Consumable)    — Costs 12 Coins"
                printfn "  [3] 🔮  Procure Ancient Relic (Random Passive)   — Costs 45 Coins"
                printfn "  [4] 🏃  Exit commissary front and step to next floor"
                printfn "-----------------------------------------------------------"
                printfn "Your current self-fund wallet balance: %d Coins" player.Loot
                printf "\nPurchase input index command: "
                match Console.ReadLine() with
                | "1" ->
                    if player.Loot >= 15 then
                        let p = { player with Loot = player.Loot - 15; Hp = Math.Min(player.Hp + 20, maxHp) }
                        printfn "\nRecovery injection complete! (+20 HP)"
                        Console.ReadKey() |> ignore
                        gameLoopWithEmotion p upgrades (Gameplay (pos, Store)) Happy
                    else
                        Console.ReadKey() |> ignore
                        gameLoopWithEmotion player upgrades (Gameplay (pos, Store)) Normal
                | "2" ->
                    if player.Loot < 12 || player.OwnedPotions.Length >= InitialData.MAX_POTION_SLOTS then
                        Console.ReadKey() |> ignore
                        gameLoopWithEmotion player upgrades (Gameplay (pos, Store)) Normal
                    else
                        let boughtPotion = MapGenerator.getRandomPotion()
                        let p = { player with Loot = player.Loot - 12 } |> MapGenerator.addPotion boughtPotion
                        printfn "\n🧪 Purchased and stowed [%A] capsule!" boughtPotion
                        Console.ReadKey() |> ignore
                        gameLoopWithEmotion p upgrades (Gameplay (pos, Store)) Happy
                | "3" ->
                    if player.Loot < 45 then
                        Console.ReadKey() |> ignore
                        gameLoopWithEmotion player upgrades (Gameplay (pos, Store)) Normal
                    else
                        let boughtRelic = MapGenerator.getRandomRelic()
                        if player.OwnedRelics |> List.contains boughtRelic then
                            printfn "\n🔮 Signature matches active registry. Shifting options matrix..."
                            Console.ReadKey() |> ignore
                            gameLoopWithEmotion player upgrades (Gameplay (pos, Store)) Normal
                        else
                            let basePlayer = { player with Loot = player.Loot - 45; OwnedRelics = boughtRelic :: player.OwnedRelics }
                            let finalPlayer = if boughtRelic = ResearchEndowment then { basePlayer with Loot = basePlayer.Loot + 50 } else basePlayer
                            printfn "\n🔮 Permanent connection locked with passive relic: [%A]!" boughtRelic
                            Console.ReadKey() |> ignore
                            gameLoopWithEmotion finalPlayer upgrades (Gameplay (pos, Store)) Happy
                | _ -> 
                    printfn "\nExiting shop front interface structures..."
                    gameLoopWithEmotion incrementedPlayer upgrades (MapSelection (nextPos, nextChoices)) Normal

            | MiniGame -> 
                Console.ReadKey() |> ignore
                gameLoopWithEmotion incrementedPlayer upgrades (MapSelection (nextPos, nextChoices)) Normal

        | CampusMenu ->
            printfn "🏛️  THE HIGH CITADEL META-UPGRADE MANAGEMENT TERMINAL"
            printfn "Account Balance Bank: %d Lab-Funds" upgrades.LabFunds
            printfn "---------------------------------------------------------"
            printfn "  [1] 🏗️ Upgrade N7 Mechanical Wing  (Tier %d) — Cost: %d Funds" upgrades.N7Level (InitialData.getUpgradeCost upgrades.N7Level)
            printfn "  [2] 🏋️ Upgrade Sports Complex      (Tier %d) — Cost: %d Funds" upgrades.SportsComplexLevel (InitialData.getUpgradeCost upgrades.SportsComplexLevel)
            printfn "  [3] 🍛 Upgrade Kaimaru Dining Hall (Tier %d) — Cost: %d Funds" upgrades.KaimaruLevel (InitialData.getUpgradeCost upgrades.KaimaruLevel)
            printfn "  [4] 💤 Upgrade Dormitory blocks    (Tier %d) — Cost: %d Funds" upgrades.DormLevel (InitialData.getUpgradeCost upgrades.DormLevel)
            printfn "  [5] 🔬 Upgrade E3 Physics Center   (Tier %d) — Cost: %d Funds" upgrades.E3Level (InitialData.getUpgradeCost upgrades.E3Level)
            printfn "  [6] 💻 Upgrade N1 IT Mainframe     (Tier %d) — Cost: %d Funds" upgrades.N1Level (InitialData.getUpgradeCost upgrades.N1Level)
            printfn "  [7] ⬅️ Return to Main Terminal Hub Menu"
            printfn "---------------------------------------------------------"
            printf "Select sector layout index to modify: "
            
            let input = Console.ReadLine()
            let processUpgrade level setter name =
                let cost = InitialData.getUpgradeCost level
                if level >= 10 then
                    printfn "\n❌ Max Tier efficiency reached for this architectural zone!"
                    Console.ReadKey() |> ignore
                    gameLoopWithEmotion player upgrades CampusMenu Normal
                elif upgrades.LabFunds >= cost then
                    let nextUpgrades = setter (level + 1) { upgrades with LabFunds = upgrades.LabFunds - cost }
                    printfn "\n✨ Construction complete! %s permanently scaled to Tier %d!" name (level + 1)
                    if (level + 1) = 5 || (level + 1) = 10 then printfn "❤️ Milestone hit! Nupjuki receives permanent baseline Max HP bonuses!"
                    Console.ReadKey() |> ignore
                    gameLoopWithEmotion player nextUpgrades CampusMenu Happy
                else
                    printfn "\n❌ Insufficient account capital assets. Continue dungeon runs!"
                    Console.ReadKey() |> ignore
                    gameLoopWithEmotion player upgrades CampusMenu Pain

            match input with
            | "1" -> processUpgrade upgrades.N7Level (fun l u -> { u with N7Level = l }) "N7 Mechanical Complex"
            | "2" -> processUpgrade upgrades.SportsComplexLevel (fun l u -> { u with SportsComplexLevel = l }) "Sports Complex Terminal"
            | "3" -> processUpgrade upgrades.KaimaruLevel (fun l u -> { u with KaimaruLevel = l }) "Kaimaru Cafeteria Hub"
            | "4" -> processUpgrade upgrades.DormLevel (fun l u -> { u with DormLevel = l }) "Dormitory Living Arrays"
            | "5" -> processUpgrade upgrades.E3Level (fun l u -> { u with E3Level = l }) "E3 Research Labs"
            | "6" -> processUpgrade upgrades.N1Level (fun l u -> { u with N1Level = l }) "N1 IT Core Computing Systems"
            | _ -> gameLoopWithEmotion player upgrades MainMenu Normal

        | GameOver (won, earnedFunds) ->
            let updatedUpgrades = { upgrades with LabFunds = upgrades.LabFunds + earnedFunds }
            if won then printfn "\n🎓✨ 🏆 LEGENDARY GRADUATION VICTORY ACHIEVED! 🏆 ✨🎓"
            else printfn "\n💀 NUPJUKI SUCCUMBED TO BURNOUT: RUN CONCLUDED 💀"
            printfn "-----------------------------------------------------------"
            printfn "📊 ACADEMIC TRANSACTION SUMMARY EVALUATION REPORT:"
            printfn "    Sub-sectors Cleared:    %d Semester Layers" player.FloorsCleared
            printfn "    Passive Relics Handled: %d Relics" player.OwnedRelics.Length
            printfn "💰 RETRIEVED ACADEMIC LAB-FUNDS DEPOSITED: +%d Meta-Funds" earnedFunds
            printfn "-----------------------------------------------------------"
            printfn "\nPress any key to clear buffer stacks and recycle back to the hub terminal..."
            Console.ReadKey() |> ignore
            let campusHpBonus = InitialData.getTotalBonusHp updatedUpgrades
            let freshHubPlayer = { Hp = player.MaxHp + campusHpBonus; MaxHp = player.MaxHp; Credits = 3; CurrentOutfit = InitialData.createOutfit GenericClothes; AvailableOutfits = []; Loot = 0; ActiveDebuffs = []; VisitedEvents = []; OwnedRelics = []; OwnedPotions = []; FloorsCleared = 0; PotionMultiplier = 1.0; DoubleCreditsNextTurn = false }
            gameLoopWithEmotion freshHubPlayer updatedUpgrades MainMenu Normal

    let gameLoop (player: PlayerState) (upgrades: PermanentUpgrades) (screen: GameScreen) =
        gameLoopWithEmotion player upgrades screen Normal