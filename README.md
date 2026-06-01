# The Chronicles of Kaistania: Nupjuki's Academic Ordeal

Welcome to **Tale of Kaistania**, a terminal-based rogue-lite designed and implemented completely in F# targeting .NET 10. You play as Nupjuki, navigating a dangerous campus departmental labyrinth packed with unpredictable anomalies, punishing status debuffs, and a dynamic merchant economy in order to upgrade and build up KAIST.

---

## 🎮 How to Build and Run the Game

### Prerequisites
* Ensure you have the **.NET 10 SDK** installed on your workstation.

### Compilation and Launch Instructions
1. Open your native terminal app (PowerShell, Command Prompt, or terminal emulator) and navigate to the project directory:
```bash
   cd TaleOfKaistania
```
2. Run the application directly using the .NET CLI environment pipeline:

```bash
   dotnet run
```
---

## 🏗️ Core Game Architecture & Rules

* **Turn Economy:** You start each combat phase turn with 3 Lab Stipend Credits to use skills associated with the current outfit worn. Ending your turn refreshes your stipend balance.
* **Deterministic Intent Engine:** Foes broadcast their precise operational movements (Attacks, Buffs, Debuffs, or Charge-ups) overhead before you commit actions, so you can plan ahead.
* **The 3-Turn Offense Rule:** To prevent passive stalls, enemies are programmatically blocked from choosing non-offensive maneuvers more than twice consecutively. An attack variant is completely guaranteed at least once every 3 consecutive enemy cycles.
* **Status Manifestations:** Watch your status trackers carefully. You can be afflicted with multiple debuffs:
```
    (Tired): Cuts your outgoing skill damage parameters by 25%.
    (Burn-Out): Exhausts your stamina, preventing all Credit regeneration on the upcoming turn.
    (Poison): Inflicts 6 structural HP damage to your vitality at the close of every phase cycle.
    (Sicken): Compromises your defenses, causing you to take 50% amplified damage from incoming attacks.
    (Lockout): Freezes your attire modification vectors, preventing mid-combat garment swapping.
```

* **Item Frameworks:** In your run, you may gain access to 2 temporary recources to help out.
```
    Passive Relics: Permanent, un-stackable artifacts (like the *Kaist Coffee Card* or *Reinforced Clipboard*) that modify math scaling passively across all nodes.
    Consumable Potions: Single-use combat capsules tucked into your belt. They cost 0 Credits to drink mid-battle and can be triggered on your action line by typing `P1`, `P2`, or `P3`.
```


---

## 👕 Structural Garment Specializations (KAIST Hub Connection)

Nupjuki pre-equips **Generic Clothes** baseline and selectively chooses exactly **two auxiliary garments** to bring along right before launching a run. You can use your accumulated Lab-fund to upgrade buildings inside the KAIST Hub before each dungeon run; upgrades' increase skill damage/healing modifiers of correlating clothes up to Tier 10:
```
1. **Generic Clothes** (Scales globally): Deals balanced damage, buffs stipend yields, and self-heals baseline vitality.
2. **Builder Getup** (Linked to **N7 Mechanical Complex**): Focuses on hard wrench strikes and scaling consumable potion effectiveness by +50%.
3. **Gym Clothes** (Linked to **Sports Complex**): Delivers heavy physical slams and activates adrenaline bursts to purge the *Tired* status.
4. **Chef Clothes** (Linked to **Kaimaru Dining Hall**): Specializes in complete status debuff purges and clean baseline health recovery.
5. **Pajamas** (Linked to **Dormitory Arrays**): High utility setup that focuses on deep napping to guarantee your upcoming stipend income is completely doubled.
6. **Lab Coat** (Linked to **E3 Research Labs**): Releases chemical hazards to alter incoming enemy targets into *Sicken* profiles and inject severe poison over time.
7. **Hacker Clothes** (Linked to **N1 IT Core Computing Systems**): Compromises enemy registers to freeze their capabilities via *Lockout* while siphoning metrics to double your credit income.
```
---

## 🏛️ Metagame Progression: The Currency Split

To ensure clear risk-reward curves, the economy uses two completely distinct funding pipelines:
```
* **Self-Fund Coins (In-Run Loot):** Scavenged from defeated mobs, reward desks, or anomalous occurrences. Spent exclusively at the black-market *Campus Commissary Store* to purchase random passive relics or consumable potions. **All Self-Funds are entirely lost when a run concludes (via death or thesis graduation).**
* **Lab-Funds (Meta-Currency):** Calculated dynamically at the end of every run based on your total semester layers cleared, relics collected, and performance parameters. Reclaimed Lab-Funds are banked securely inside your profile permanently to upgrade campus buildings at the central terminal.
```

## 🥚 Easter Egg & Debug Cheat Mode

For ease of evaluation, clearing the game, and overall screwing around, an administrative cheat mode has been explicitly implemented into the core game loop runtime.

### How to Activate:
1. Launch the game to arrive at the **Main Menu**.
2. When prompted with `Choose path:`, type the following secret phrase verbatim and press `Enter`:
   ```text
   4.3 gpa nubzuki
---

## Example Session:

<img width="1119" height="886" alt="image" src="https://github.com/user-attachments/assets/0dcfa265-76c8-41de-8674-d96e00fadbf1" />


## 📂 Project Structure & Module Overview

The codebase is engineered strictly around a modular functional state architecture, cleanly dividing data definitions, calculation pipelines, and user-interface loops into sequential compilation layers:

* **`Domain.fs`**: The structural backbone of the application. It contains all core immutably typed record definitions, variant types, and status effect enumerations (`Player`, `Enemy`, `StatusEffect`, `Relic`, `Potion`, `Outfit`) that define the state of the simulation.
* **`MapGenerator.fs`**: Handles the procedural generation of the campus departmental labyrinth. It builds the branching floor layouts, node option paths, and tracking metrics for visited semester layers.
* **`SaveSystem.fs`**: The persistence pipeline layer. It utilizes .NET JSON compilation serialization to write Nupjuki's state out to a local tracking anchor file, allowing seamless loading from the Main Menu.
* **`GameLogic.fs`**: The central mechanical core of the game. Contains all state-transition engines, turn-evaluation frameworks, intent broadcast mechanics, status debuff ticks, combat resolutions, and the 35-event non-repeating encounter deck.
* **`Program.fs`**: The application's entry point. It handles terminal initialization boundaries, bootstraps the runtime environment, and fires off the recursive main application loop.

## 🤖 Large Language Model (LLM) Attribution

* **What the LLM was used for:**
1. Implenting the save-system used to load previous saved runs,
2. Creating the ASCII representation of Nupjuki in multiple different scenarios,
3. Organizing already written code for ease of editing,
4. Themed text generation used as prompts in the UI, 

* **Manual changes/reprompts required:**
1. Early iterations ran into logic softlocks due to overlooked edge-cases, and a lack of additional "Leave" escape hatch.
2. I had to explicitly request a complete overhaul of the event arrays to append unconditional choice fallbacks to prevent freezing. Additionally, early turn-end evaluation structures misallocated the `LabCoat` poison ticking loops onto the player instead of the enemy, requiring a pipeline rewrite.


* **Main points the LLM failed to do correctly:**
1. The model struggled to maintain strict F# indentation rules across dense, nested recursive state matching inner loops, triggering `FS0010` structured alignment compile errors on local compilation passes.
2. It also struggled with structural definition scope overlaps, accidentally duplicating the `SaveData` record definitions across both the domain types and the serialization subsystem.
3. It struggled heavily with the generation of an ASCII representaion of Nubjuki, as it kept making distorted images.
4. When using it to organize my code, it omitted a key SaveData helper function I had written prior.
5. When using it to generate long text and dialogue, it had a lot of indentation issues as well as syntax errors.



```

```
