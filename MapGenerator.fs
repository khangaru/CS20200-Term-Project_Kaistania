namespace TaleOfKaistania

open System

module MapGenerator =
    
    let private rand = Random()

    // Enemies scaled by Act containing distinct initialized state patterns
    let private act1Enemies = [ 
        { Name = "Procrastination Ghost"; Hp = 30; MaxHp = 30; BaseDamage = 8; CurrentIntent = WillAttack 8; TurnsSinceLastAttack = 0 }
        { Name = "Sudden Pop Quiz"; Hp = 25; MaxHp = 25; BaseDamage = 10; CurrentIntent = WillDebuff Tired; TurnsSinceLastAttack = 1 } 
    ]
    let private act2Enemies = [ 
        { Name = "Stack Overflow Glitch"; Hp = 55; MaxHp = 55; BaseDamage = 15; CurrentIntent = WillAttack 15; TurnsSinceLastAttack = 0 }
        { Name = "Buggy Pointer Exception"; Hp = 65; MaxHp = 65; BaseDamage = 12; CurrentIntent = WillBuff; TurnsSinceLastAttack = 1 } 
    ]
    let private act3Enemies = [ 
        { Name = "Memory Leak Monster"; Hp = 90; MaxHp = 90; BaseDamage = 24; CurrentIntent = ChargingUp; TurnsSinceLastAttack = 1 }
        { Name = "Refusal of Funding Demon"; Hp = 100; MaxHp = 100; BaseDamage = 20; CurrentIntent = WillAttack 20; TurnsSinceLastAttack = 0 } 
    ]

    let getRandomEnemy act =
        match act with
        | 1 -> act1Enemies.[rand.Next(act1Enemies.Length)]
        | 2 -> act2Enemies.[rand.Next(act2Enemies.Length)]
        | _ -> act3Enemies.[rand.Next(act3Enemies.Length)]

    // State helper wrappers for concise execution pipelines inside events
    let private damagePlayer amt (p : PlayerState) = { p with Hp = Math.Max(0, p.Hp - amt) }
    let private healPlayer amt maxHp (p : PlayerState) = { p with Hp = Math.Min(maxHp, p.Hp + amt) }
    let private addLoot amt (p : PlayerState) = 
        let bonus = if p.OwnedRelics |> List.contains GoldenPlaque then int (float amt * 1.2) else amt
        { p with Loot = p.Loot + bonus }
    let private loseLoot amt (p : PlayerState) = { p with Loot = Math.Max(0, p.Loot - amt) }
    let private addDebuff d t (p : PlayerState) = { p with ActiveDebuffs = (d, t) :: p.ActiveDebuffs }

    let addPotion pot p =
        if p.OwnedPotions.Length >= InitialData.MAX_POTION_SLOTS then p
        else { p with OwnedPotions = p.OwnedPotions @ [pot] }

    let getRandomPotion () =
        match rand.Next(0, 9) with
        | 0 -> ThesisAidJuice  | 1 -> OverclockSerum | 2 -> SmokescreenVial | 3 -> AntidotePill
        | 4 -> ElixirOfFocus   | 5 -> VitaminGummy     | 6 -> StarlightBrew   | 7 -> NaniteShieldVial
        | _ -> UnstableCatalyst

    let getRandomRelic () =
        match rand.Next(0, 9) with
        | 0 -> KaistCoffeeCard | 1 -> ReinforcedClipboard | 2 -> GoldenPlaque | 3 -> EmergencyDefib
        | 4 -> GradStudentCap  | 5 -> ShintongKeyring     | 6 -> LabSafetyGoggles | 7 -> OverheatedHeatsink
        | _ -> ResearchEndowment

    let getRandomDebuff () =
        match rand.Next(0, 5) with | 0 -> Tired | 1 -> BurnOut | 2 -> Poison | 3 -> Sicken | _ -> Lockout

    // --- Master Database of 35 Non-Repeating Campus Encounters ---
    // FIXED: Every single event now contains an unconditional backup option to prevent softlocks!
    let allCampusEvents : CampusEvent list = [
        { Id = E1; Title = "The Broken Coupang Parcel"; Description = "A mysterious, unclaimed delivery box lies outside the E15 computer pool room."; Choices = [
            { Text = "Rip it open"; RequirementText = "Gain 25 Coins, 35% chance to trigger Poison trap"; IsAvailable = (fun _ -> true); Execute = (fun p -> if rand.Next(0, 100) < 35 then (p |> addLoot 25 |> addDebuff Poison 2, "You found processing components, but micro-battery acid leaks onto your hands!") else (p |> addLoot 25, "Jackpot! Free unmonitored development kits.")) }
            { Text = "Walk away cleanly"; RequirementText = "Nothing happens"; IsAvailable = (fun _ -> true); Execute = (fun p -> (p, "Symmetry is preservation. You move on.")) } ] }
        
        { Id = E2; Title = "The Oishi Coffee Machine Bug"; Description = "The vending system in the Creative Learning Building is sparking violently."; Choices = [
            { Text = "Smack the chassis"; RequirementText = "Gain 4 Credits, 50% chance to take 10 HP shock damage"; IsAvailable = (fun _ -> true); Execute = (fun p -> if rand.Next(0, 2) = 0 then (p |> damagePlayer 10, "BZZZT! An intense electrical arc scorches your fingers!") else ({ p with Credits = p.Credits + 4 }, "Free liquid focus cascades down the collection bay!")) }
            { Text = "Ignore the sparks and walk past"; RequirementText = "Nothing happens"; IsAvailable = (fun _ -> true); Execute = (fun p -> (p, "You decide it's safer not to gamble with building electronics today.")) } ] }
        
        { Id = E3; Title = "Ghost of the Mechanical Engineering Workshop"; Description = "A transparent figure floating over a lathe offers to optimize your equipment."; Choices = [
            { Text = "Sacrifice research papers"; RequirementText = "Lose 20 Coins, permanently raise Max HP by 15"; IsAvailable = (fun p -> p.Loot >= 20); Execute = (fun p -> let newMax = p.MaxHp + 15 in ({ p with MaxHp = newMax } |> loseLoot 20 |> healPlayer 15 newMax, "The spirit manifests spiritual stabilizers inside your uniform.")) }
            { Text = "Decline politely"; RequirementText = "Nothing happens"; IsAvailable = (fun _ -> true); Execute = (fun p -> (p, "The phantom vanishes into the vents.")) } ] }
        
        { Id = E4; Title = "The Infinite Loop Crisis"; Description = "A monitor display in the hallway is stuck running an exponential thread cycle."; Choices = [
            { Text = "Attempt to debug it manually"; RequirementText = "Gain 40 Coins, Inflicts (Tired) for 2 floors"; IsAvailable = (fun _ -> true); Execute = (fun p -> (p |> addLoot 40 |> addDebuff Tired 2, "You fixed it, but the memory compilation leaves you mentally exhausted.")) }
            { Text = "Pull the breaker plug"; RequirementText = "Lose 3 Credits due to server outage penalties"; IsAvailable = (fun p -> p.Credits >= 3); Execute = (fun p -> ({ p with Credits = p.Credits - 3 }, "The screaming stops, but admin system alerts log a penalty against your token.")) }
            { Text = "Walk away from the buzzing display"; RequirementText = "Nothing happens"; IsAvailable = (fun _ -> true); Execute = (fun p -> (p, "Not my project, not my problem. You step away.")) } ] }
        
        { Id = E5; Title = "The Crowded Shuttle Bus"; Description = "The direct inter-campus connection is over-capacity. A student pushes past you."; Choices = [
            { Text = "Shove into a standing spot"; RequirementText = "Advance quickly, Inflicts (Sicken) due to stuffiness"; IsAvailable = (fun _ -> true); Execute = (fun p -> (p |> addDebuff Sicken 2, "You survive the tight ride, but feel physically ill from lack of ventilation.")) }
            { Text = "Walk across campus under the hot sun"; RequirementText = "Lose 12 HP from heat fatigue"; IsAvailable = (fun _ -> true); Execute = (fun p -> (p |> damagePlayer 12, "Your soles melt slightly, but your focus remains intact.")) } ] }
        
        { Id = E6; Title = "The Shady Graduate Recruitment Booth"; Description = "A suited recruiter smiles warmly, promising infinite funding if you sign a NDA immediately."; Choices = [
            { Text = "Sign the NDA papers blindly"; RequirementText = "Gain 60 Coins, Inflicts (Lockout) from corporate protocols"; IsAvailable = (fun _ -> true); Execute = (fun p -> (p |> addLoot 60 |> addDebuff Lockout 2, "Your funding expands, but contract limits freeze your attire configurations!")) }
            { Text = "Refuse corporate bondage"; RequirementText = "Gain 2 Credits from independent pride"; IsAvailable = (fun _ -> true); Execute = (fun p -> ({ p with Credits = p.Credits + 2 }, "You stay broke but independent.")) } ] }
        
        { Id = E7; Title = "The Duck Pond Ritual"; Description = "The iconic KAIST ducks are swimming in a perfect circle around a glowing chip."; Choices = [
            { Text = "Toss them your lunch snack"; RequirementText = "Lose 10 Coins, ducks gift you an operational microchip (+4 Credits)"; IsAvailable = (fun p -> p.Loot >= 10); Execute = (fun p -> ({ p with Credits = p.Credits + 4 } |> loseLoot 10, "The ducks honk in programmatic harmony, rolling the pristine hardware over.")) }
            { Text = "Disturb their computation"; RequirementText = "Take 15 HP damage from synchronized pecking attacks"; IsAvailable = (fun _ -> true); Execute = (fun p -> (p |> damagePlayer 15, "Never disrespect the avian overseers.")) }
            { Text = "Back away slowly without making eye contact"; RequirementText = "Nothing happens"; IsAvailable = (fun _ -> true); Execute = (fun p -> (p, "You leave the ritual ground undisturbed.")) } ] }
        
        { Id = E8; Title = "The Abandoned Server Rack"; Description = "Deep within the N1 building basement, an obsolete array core hums with forgotten data files."; Choices = [
            { Text = "Siphon the operational sectors"; RequirementText = "Gain 50 Coins"; IsAvailable = (fun _ -> true); Execute = (fun p -> (p |> addLoot 50, "Raw database assets pour directly into your storage cards.")) }
            { Text = "Leave the dust covered terminal"; RequirementText = "Nothing happens"; IsAvailable = (fun _ -> true); Execute = (fun p -> (p, "You decide not to touch unsecured system hardware.")) } ] }
        
        { Id = E9; Title = "Spilled Chemical Brew"; Description = "A puddle of fluorescent blue fluid has leaked onto the floor tile."; Choices = [
            { Text = "Inhale the fumes directly"; RequirementText = "Gain 5 Credits, Inflicts (Poison) for 2 turns"; IsAvailable = (fun _ -> true); Execute = (fun p -> (p |> addDebuff Poison 2 |> (fun state -> { state with Credits = state.Credits + 5 }), "Your mind unlocks, but your internal biological systems take severe toxicity hits!")) }
            { Text = "Hold your breath and leap over it"; RequirementText = "Nothing happens"; IsAvailable = (fun _ -> true); Execute = (fun p -> (p, "You safely bypass the hazard puddle without breathing it in.")) } ] }
        
        { Id = E10; Title = "The Strict Professor Encounter"; Description = "A tenured faculty legend stops you in the corridor to inspect your dress attire."; Choices = [
            { Text = "Present your pre-equipped uniform"; RequirementText = "Requires Equipped Generic Clothes. Gained 30 Coins approval bonus"; IsAvailable = (fun p -> p.CurrentOutfit.Type = GenericClothes); Execute = (fun p -> (p |> addLoot 30, "The professor nods smoothly at your traditional compliance.")) }
            { Text = "Run down the fire escape stairwell"; RequirementText = "Take 8 HP damage from tripping"; IsAvailable = (fun _ -> true); Execute = (fun p -> (p |> damagePlayer 8, "You escaped the lecture, but twisted your ankle.")) } ] }
        
        { Id = E11; Title = "The Chemical Spill Cleanup"; Description = "A locker in the E3 back-vault contains clean medical supplies."; Choices = [
            { Text = "Snatch items"; RequirementText = "Receive a random Combat Potion"; IsAvailable = (fun p -> p.OwnedPotions.Length < InitialData.MAX_POTION_SLOTS); Execute = (fun p -> (p |> addPotion (getRandomPotion ()), "You stowed a flask safely into your pack!")) }
            { Text = "Leave the locked compartment alone"; RequirementText = "Nothing happens"; IsAvailable = (fun _ -> true); Execute = (fun p -> (p, "You decide your belt inventory is either full or you don't need it.")) } ] }
        
        { Id = E12; Title = "The Late Night Lab Raid"; Description = "An empty office holds clean equipment."; Choices = [
            { Text = "Scavenge shelves"; RequirementText = "+35 Coins, 30% chance to lose 10 HP"; IsAvailable = (fun _ -> true); Execute = (fun p -> if rand.Next(0,10) < 3 then (p |> damagePlayer 10, "Caught by security patrols!") else (p |> addLoot 35, "Clean scavenging run completed.")) }
            { Text = "Pass the room by"; RequirementText = "Nothing happens"; IsAvailable = (fun _ -> true); Execute = (fun p -> (p, "You walk right past the open door.")) } ] }
        
        { Id = E13; Title = "The Gym Plyometrics Station"; Description = "A heavy leaping platform beckons inside the Sports Complex."; Choices = [
            { Text = "Train hard"; RequirementText = "+10 Max HP, Inflicts Tired for 2 turns"; IsAvailable = (fun _ -> true); Execute = (fun p -> let n = p.MaxHp + 10 in ({ p with MaxHp = n } |> addDebuff Tired 2, "Leg day achieved. Your base conditioning is permanently elevated.")) }
            { Text = "Skip the workout platform"; RequirementText = "Nothing happens"; IsAvailable = (fun _ -> true); Execute = (fun p -> (p, "You skip the training box to preserve current breath.")) } ] }
        
        { Id = E14; Title = "The Overheated Power Supply"; Description = "A secondary capacitor room is ready to blow."; Choices = [
            { Text = "Vent heat manually"; RequirementText = "+3 Credits, -5 HP burn damage"; IsAvailable = (fun _ -> true); Execute = (fun p -> ({ p with Credits = p.Credits + 3 } |> damagePlayer 5, "Hot air blasts away as you stabilize the system core.")) }
            { Text = "Run before it vents sparks"; RequirementText = "Nothing happens"; IsAvailable = (fun _ -> true); Execute = (fun p -> (p, "You turn tail and clear the area safely.")) } ] }
        
        { Id = E15; Title = "The Lost ID Card"; Description = "A pristine smart card sits on the student lounge steps."; Choices = [
            { Text = "Return to info desk"; RequirementText = "+15 Coins reward"; IsAvailable = (fun _ -> true); Execute = (fun p -> (p |> addLoot 15, "Good karma returned. Administrative staff wired a modest stipend.")) }
            { Text = "Pocket it and throw it away later"; RequirementText = "Nothing happens"; IsAvailable = (fun _ -> true); Execute = (fun p -> (p, "You throw it in a recycling bin down the line.")) } ] }
        
        { Id = E16; Title = "The Faulty Air Conditioner"; Description = "Freezing coolant ventilation fills the closed lab zone."; Choices = [
            { Text = "Endure the freeze"; RequirementText = "Inflicts Sicken status for 2 turns"; IsAvailable = (fun _ -> true); Execute = (fun p -> (p |> addDebuff Sicken 2, "You freeze completely, compromising your body's immune shield.")) }
            { Text = "Flee the cold zone immediately"; RequirementText = "Nothing happens"; IsAvailable = (fun _ -> true); Execute = (fun p -> (p, "You slam the lab doors shut behind you to find a warmer room.")) } ] }
        
        { Id = E17; Title = "The Midnight Coffee Run"; Description = "An unattended thermos sits warm near the reference books."; Choices = [
            { Text = "Drink it blindly"; RequirementText = "+4 Credits, 20% Poison risk"; IsAvailable = (fun _ -> true); Execute = (fun p -> if rand.Next(5) = 0 then (p |> addDebuff Poison 2, "It tasted completely rancid! Your stomach twists.") else ({ p with Credits = p.Credits + 4 }, "Supercharged focus floods your veins!")) }
            { Text = "Do not trust unknown liquids"; RequirementText = "Nothing happens"; IsAvailable = (fun _ -> true); Execute = (fun p -> (p, "You stick to clean lab tap water.")) } ] }
        
        { Id = E18; Title = "The Cryptography Challenge"; Description = "A hidden terminal panel displays a standard cypher locking prompt."; Choices = [
            { Text = "Solve cipher"; RequirementText = "+40 Coins, requires processing fee of 1 Credit"; IsAvailable = (fun p -> p.Credits >= 1); Execute = (fun p -> ({ p with Credits = p.Credits - 1 } |> addLoot 40, "Decoded successfully. Secure vault assets unlocked.")) }
            { Text = "Force close the terminal console"; RequirementText = "Nothing happens"; IsAvailable = (fun _ -> true); Execute = (fun p -> (p, "You close the tab prompt window.")) } ] }
        
        { Id = E19; Title = "The Cluttered Workbench"; Description = "Tons of sharp scrap iron and frames litter the N7 workspace floors."; Choices = [
            { Text = "Clear the floor spaces"; RequirementText = "Requires Equipped Builder Getup. Gain +30 Coins"; IsAvailable = (fun p -> p.CurrentOutfit.Type = BuilderGetup); Execute = (fun p -> (p |> addLoot 30, "With your heavy gear, you clear the structural debris easily.")) }
            { Text = "Carefully navigate through the sharp scrap metals"; RequirementText = "Nothing happens"; IsAvailable = (fun _ -> true); Execute = (fun p -> (p, "You step carefully between the scattered scraps without sweeping them.")) } ] }
        
        { Id = E20; Title = "The Secret Roof Access"; Description = "An open maintenance access door leads directly to the observation deck."; Choices = [
            { Text = "Climb to the summit"; RequirementText = "+15 Max HP, costs 10 HP from fatigue exhaustion"; IsAvailable = (fun _ -> true); Execute = (fun p -> let n = p.MaxHp + 15 in ({ p with MaxHp = n } |> damagePlayer 10, "Fresh air expands your vital horizon. Your resolve hardens.")) }
            { Text = "Stay inside the safe lower floors"; RequirementText = "Nothing happens"; IsAvailable = (fun _ -> true); Execute = (fun p -> (p, "You decide against climbing redundant stairs today.")) } ] }
        
        { Id = E21; Title = "The Corrupted Compiler Update"; Description = "A strange network update patch manifests over your console loop."; Choices = [
            { Text = "Force installation"; RequirementText = "Inflicts BurnOut for 2 turns"; IsAvailable = (fun _ -> true); Execute = (fun p -> (p |> addDebuff BurnOut 2, "The compiler crashes out, throttling your cognitive memory blocks.")) }
            { Text = "Abort network download thread completely"; RequirementText = "Nothing happens"; IsAvailable = (fun _ -> true); Execute = (fun p -> (p, "You reject the non-verified package updates.")) } ] }
        
        { Id = E22; Title = "The Severe Peer Review Notes"; Description = "Aggressive red pen markings shred your core draft thesis parameters."; Choices = [
            { Text = "Refactor logic errors"; RequirementText = "+25 Coins, Inflicts Tired for 1 turn"; IsAvailable = (fun _ -> true); Execute = (fun p -> (p |> addLoot 25 |> addDebuff Tired 1, "Grinding hard past midnight restores the data, but leaves you depleted.")) }
            { Text = "Toss notes into a drawer for tomorrow morning"; RequirementText = "Nothing happens"; IsAvailable = (fun _ -> true); Execute = (fun p -> (p, "Mental preservation is key. You choose to deal with it later.")) } ] }
        
        { Id = E23; Title = "The Leftover Sub Sandwich"; Description = "A clean plastic package sits deep inside the common room fridge."; Choices = [
            { Text = "Consume it completely"; RequirementText = "+20 HP recovery, Inflicts Poison status"; IsAvailable = (fun _ -> true); Execute = (fun p -> ({ p with Hp = Math.Min(p.MaxHp, p.Hp + 20) } |> addDebuff Poison 2, "It restored physical energy, but bacterial decay hits your vitals shortly after.")) }
            { Text = "Close the fridge door empty handed"; RequirementText = "Nothing happens"; IsAvailable = (fun _ -> true); Execute = (fun p -> (p, "You decide that mystery sandwiches aren't worth food poisoning.")) } ] }
        
        { Id = E24; Title = "The Faulty Oscilloscope calibration"; Description = "Frequency waves are crossing erratically across the workbench monitors."; Choices = [
            { Text = "Align wave parameters"; RequirementText = "Gain +3 Credits stability"; IsAvailable = (fun _ -> true); Execute = (fun p -> ({ p with Credits = p.Credits + 3 }, "Signals cross in clean synchronization now.")) }
            { Text = "Leave the waveform out of sync"; RequirementText = "Nothing happens"; IsAvailable = (fun _ -> true); Execute = (fun p -> (p, "You bypass the diagnostic station.")) } ] }
        
        { Id = E25; Title = "The Sudden Cloud Outage Crisis"; Description = "Your auxiliary remote compilation nodes drop offline mid-execution."; Choices = [
            { Text = "Reroute local servers"; RequirementText = "Lose 25 Coins processing fees"; IsAvailable = (fun p -> p.Loot >= 25); Execute = (fun p -> (p |> loseLoot 25, "Data pipelines secured, but development funding took a hit.")) }
            { Text = "Wait out the network outage link passively"; RequirementText = "Nothing happens"; IsAvailable = (fun _ -> true); Execute = (fun p -> (p, "You sit back and wait for infrastructure engineers to fix the cloud nodes.")) } ] }
        
        { Id = E26; Title = "The Unlocked Locker Keyhole"; Description = "An absolute vintage gym locker door swings slightly ajar in the Sports Complex corridor."; Choices = [
            { Text = "Pry it open"; RequirementText = "Gain +20 Coins"; IsAvailable = (fun _ -> true); Execute = (fun p -> (p |> addLoot 20, "Scrapped core units collected from the dust.")) }
            { Text = "Walk past the metal storage bank"; RequirementText = "Nothing happens"; IsAvailable = (fun _ -> true); Execute = (fun p -> (p, "You pass it by without turning the latch handle.")) } ] }
        
        { Id = E27; Title = "The Erased Whiteboard Blueprint"; Description = "Faint geometric calculation paths remain visible on the board tile."; Choices = [
            { Text = "Decipher the formulas"; RequirementText = "Gain +2 Credits"; IsAvailable = (fun _ -> true); Execute = (fun p -> ({ p with Credits = p.Credits + 2 }, "Hidden calculations copied successfully down into your notepad.")) }
            { Text = "Walk out of the seminar room"; RequirementText = "Nothing happens"; IsAvailable = (fun _ -> true); Execute = (fun p -> (p, "The erased board formulas hold no structural value to your thesis scope.")) } ] }
        
        { Id = E28; Title = "The Heavy Blast Shield Blueprint"; Description = "Advanced metallic schematic blueprints lie flat on the drafting table."; Choices = [
            { Text = "Deconstruct schematic layers"; RequirementText = "Requires Equipped Builder Getup. Gain +40 Coins"; IsAvailable = (fun p -> p.CurrentOutfit.Type = BuilderGetup); Execute = (fun p -> (p |> addLoot 40, "Industrial defense logic structures compiled cleanly into your frame module.")) }
            { Text = "Leave the complex schematics flat on the table"; RequirementText = "Nothing happens"; IsAvailable = (fun _ -> true); Execute = (fun p -> (p, "The blueprint configurations look like gibberish without the right construction training tools.")) } ] }
        
        { Id = E29; Title = "The Shivering Freshman Undergrad"; Description = "A lost student forgot their cold layer garb outside the E3 laboratory rooms."; Choices = [
            { Text = "Donate spare cloth sets"; RequirementText = "Lose 15 Coins, student rep rewards +5 Credits later"; IsAvailable = (fun p -> p.Loot >= 15); Execute = (fun p -> ({ p with Credits = p.Credits + 5 } |> loseLoot 15, "Appreciated greatly. The grateful freshman paid you back using his department stipend voucher.")) }
            { Text = "Offer encouragement and keep your coins"; RequirementText = "Nothing happens"; IsAvailable = (fun _ -> true); Execute = (fun p -> (p, "You give him directions to the student center heater lounge instead.")) } ] }
        
        { Id = E30; Title = "The Intense Static Electricity Field"; Description = "The specialized nylon carpet floor cells are massively holding charge parameters."; Choices = [
            { Text = "Sprint through the hall"; RequirementText = "Take 8 HP structural shock damage"; IsAvailable = (fun _ -> true); Execute = (fun p -> (p |> damagePlayer 8, "Arcs of continuous blue electricity bite your limbs as you pass.")) }
            { Text = "Find a rubber floor mat to walk along carefully"; RequirementText = "Nothing happens"; IsAvailable = (fun _ -> true); Execute = (fun p -> (p, "You safely insulate your step to isolate static discharges.")) } ] }
        
        { Id = E31; Title = "The Shaky Quadcopter Drone Frame"; Description = "A prototype miniature testing drone crashes right through the campus window tile."; Choices = [
            { Text = "Harvest high-voltage core packs"; RequirementText = "Gain +4 Credits focus stability"; IsAvailable = (fun _ -> true); Execute = (fun p -> ({ p with Credits = p.Credits + 4 }, "Fresh energy cells extracted safely before circuit ignition.")) }
            { Text = "Kick the broken drone out of your lane walkway"; RequirementText = "Nothing happens"; IsAvailable = (fun _ -> true); Execute = (fun p -> (p, "You bypass the lithium frame hazard cell.")) } ] }
        
        { Id = E32; Title = "The Cleanroom Inspection Gateway"; Description = "Strict clean environment air-locks seal the entry tunnel. High grade suits monitored."; Choices = [
            { Text = "Pass the dust test checkpoints"; RequirementText = "Requires Equipped Lab Coat. Gain +30 Coins approval bonus"; IsAvailable = (fun p -> p.CurrentOutfit.Type = LabCoat); Execute = (fun p -> (p |> addLoot 30, "Clean match alignment confirmed. Admin rewards verified compliance.")) }
            { Text = "Turn around away from the sealed air-lock chamber"; RequirementText = "Nothing happens"; IsAvailable = (fun _ -> true); Execute = (fun p -> (p, "You don't have the cleanroom layer clearance, so you navigate an alternate corridor block.")) } ] }
        
        { Id = E33; Title = "The Broken Soldering Station"; Description = "Boiling melted lead is dripping directly onto the core circuit ribbons."; Choices = [
            { Text = "Solder connections manually"; RequirementText = "Gain +20 Coins, take 4 HP burn damage"; IsAvailable = (fun _ -> true); Execute = (fun p -> (p |> addLoot 20 |> damagePlayer 4, "A slight splash hits your thumb, but the vital hardware lane is rescued from frying.")) }
            { Text = "Leave the iron to drip into dust boxes"; RequirementText = "Nothing happens"; IsAvailable = (fun _ -> true); Execute = (fun p -> (p, "You back away from the hot lead chemical station.")) } ] }
        
        { Id = E34; Title = "The Corrupted Linux Kernel Compile"; Description = "The core system filesystem experiences a total freeze loop failure."; Choices = [
            { Text = "Execute full system hard reset"; RequirementText = "Inflicts Lockout status for 2 turns"; IsAvailable = (fun _ -> true); Execute = (fun p -> (p |> addDebuff Lockout 2, "Hardware status registers are temporarily frozen during recovery diagnostics.")) }
            { Text = "Unplug your thumb-drive and leave the station completely"; RequirementText = "Nothing happens"; IsAvailable = (fun _ -> true); Execute = (fun p -> (p, "You abandon the corrupted terminal completely before the system locks down.")) } ] }
        
        { Id = E35; Title = "The Legendary Bookstore Plaque"; Description = "An antique shelf near the KAIST vault holds a glowing structural seal."; Choices = [
            { Text = "Touch the relic surface"; RequirementText = "Acquire a random permanent passive Relic"; IsAvailable = (fun _ -> true); Execute = (fun p -> 
                let r = getRandomRelic() 
                let pBase = { p with OwnedRelics = r :: p.OwnedRelics } 
                if r = ResearchEndowment then (addLoot 50 pBase, sprintf "You bonded with the passive relic: %A! (+50 Coins bonus trigger)" r)
                else (pBase, sprintf "You bonded with the passive relic: %A!" r)) }
            { Text = "Leave the mysterious artifact alone"; RequirementText = "Nothing happens"; IsAvailable = (fun _ -> true); Execute = (fun p -> (p, "You ignore the glowing plaque completely and stay baseline focused.")) } ] }
    ]

    // Picks a non-repeating event structure dynamically based on past run logs
    let private generateFreshOccurrenceEvent (visited: EventId list) : StageType =
        let unvisited = allCampusEvents |> List.filter (fun e -> not (List.contains e.Id visited))
        let targetDeck = if List.isEmpty unvisited then allCampusEvents else unvisited
        Occurrence (targetDeck.[rand.Next(targetDeck.Length)])

    /// Generates exactly 2 layout options per node depth based on current act metrics
    /// Generates exactly 2 layout options per node depth based on current act metrics
    let generateFloorChoices (pos: FloorPosition) (visited: EventId list) : StageType list =
        let getStage act =
            // Weights: Combat(45%), Occurrence(25%), Campsite(15%), Store(10%), Reward/Treasure(5%)
            let weights = [ 
                (45, Combat (getRandomEnemy act))
                (25, generateFreshOccurrenceEvent visited)
                (15, Campsite)
                (10, Store)
                (5,  Reward) 
            ]
            let total = weights |> List.sumBy fst
            let roll = rand.Next(0, total)
            
            // Explicitly define parameters to prevent implicit 'function' structural bugs
            let rec scan currentSum elements = 
                match elements with
                | (w, stage) :: tail -> 
                    if roll < currentSum + w then stage 
                    else scan (currentSum + w) tail
                | [] -> Reward
            
            scan 0 weights

        match pos.Act with
        | 1 -> 
            if pos.Floor = 9 then [ Campsite; Store ] 
            elif pos.Floor = 10 then [ BossCombat InitialData.act1Boss ] 
            else [ getStage 1; getStage 1 ]
        | 2 -> 
            if pos.Floor = 9 then [ Campsite; Store ] 
            elif pos.Floor = 10 then [ BossCombat InitialData.act2Boss ] 
            else [ getStage 2; getStage 2 ]
        | 3 -> 
            match pos.Floor with 
            | 1 -> [ Reward ] 
            | 2 -> [ Campsite ] 
            | 3 -> [ Store ] 
            | 4 -> [ generateFreshOccurrenceEvent visited ] 
            | 5 -> [ BossCombat InitialData.act3Boss ] 
            | _ -> [ getStage 3 ]
        | _ -> [ BossCombat InitialData.act3Boss ]
    /// Calculates where Nupjuki steps when transitioning to the next index tier layer
    let getNextPosition (current: FloorPosition) : FloorPosition =
        match current.Act with
        | 1 when current.Floor = 10 -> { Act = 2; Floor = 1 } 
        | 2 when current.Floor = 10 -> { Act = 3; Floor = 1 } 
        | _ -> { current with Floor = current.Floor + 1 }