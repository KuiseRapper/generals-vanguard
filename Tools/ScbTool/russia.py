#	Copyright 2026 KuiseRapper for Generals Vanguard.
#	Additional terms under GNU GPL section 7 apply: see LICENSE.md.
#
#	This program is free software: you can redistribute it and/or modify
#	it under the terms of the GNU General Public License as published by
#	the Free Software Foundation, either version 3 of the License, or
#	(at your option) any later version.
#
#	This program is distributed in the hope that it will be useful,
#	but WITHOUT ANY WARRANTY; without even the implied warranty of
#	MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
#	GNU General Public License for more details.
#
#	You should have received a copy of the GNU General Public License
#	along with this program.  If not, see <http://www.gnu.org/licenses/>.
"""Give the fork's Russia faction its own skirmish AI script in SkirmishScripts.scb.

The engine attaches a skirmish script list to a map side by *name*: SidesList::prepareForMP_or_Skirmish
matches the side's playerName against the file's ScriptsPlayers entries.  A side with no entry of its
own (Russia) fell back on the script of the faction it is built on (China), so its AI built and fought
with Chinese objects.  This copies the China player's script list and its teams under SkirmishRussia,
owned by a Russia player, and rewrites every object type to Russia's roster.  Where Russia has no
counterpart for a China object the action is dropped rather than left pointing at China: a China
building in a Russia build order is the defect this exists to remove.

A script is found by name across the whole file (ScriptEngine::findScript caches every script in one
map), so a rename is only safe if every reference moves with it.  That means the actions and
conditions (ENABLE_SCRIPT, CALL_SUBROUTINE, TEAM_EXECUTE_SEQUENTIAL_SCRIPT) *and* the team records
(teamProductionCondition, teamOnCreateScript, the generic hooks, ...): a Russia team whose onCreate
still names "China Guard Base" runs the China side's script, and a Russia build condition that enables
"China Barracks - N" enables nothing that is in Russia's list.

Running the script again replaces the Russia entry instead of failing, so the generated data can be
rebuilt after a roster change without resetting the .scb to the commit before Russia.

usage: russia.py <in.scb> <out.scb>
"""
import copy
import sys

import scbtool

PLAYER_NAME = "SkirmishRussia"
SOURCE_PLAYER = "SkirmishChina"
OBJECT_TYPE = 15
TEAM = 3
SCRIPT = 2
SCRIPT_SUBROUTINE = 13
INT = 0
COUNTER = 4
SPECIAL_POWER = 30
UPGRADE = 32

# The skeleton roster in RussiaReforged.ini.  Everything else China names has no Russia counterpart
# yet and is dropped (build actions) or emptied (team slots).
UNITS = {
    "ChinaInfantryRedguard": "RussiaMotostrelki",
    "ChinaVehicleDozer": "RussiaSapery",
    "ChinaVehicleSupplyTruck": "RussiaSupplyTruck",
    "ChinaVehicleTroopCrawler": "RussiaBMP3",
    "ChinaTankBattleMaster": "RussiaT72B",
    "ChinaTankOverlord": "RussiaT90",
    "ChinaTankGattling": "RussiaTunguska",
    "ChinaVehicleNukeLauncher": "Russia2S19",
    "ChinaCommandCenter": "RussiaCommandBunker",
    "ChinaPowerPlant": "RussiaThermalPowerPlant",
    "ChinaSupplyCenter": "RussiaSupplyCenter",
    "ChinaBarracks": "RussiaRegimentalDepot",
    "ChinaWarFactory": "RussiaMobilizationPlant",
    "ChinaPropagandaCenter": "RussiaFireDirectionCenter",
    "ChinaGattlingCannon": "RussiaSAM",
    "ChinaNuclearMissileLauncher": "RussiaMissileBrigade",
    "ChinaAirfield": "RussiaAirBase",
    "ChinaJetMIG": "RussiaMiG25",
    "ChinaVehicleHelix": "RussiaMi24",
    # China's anti-tank infantry becomes the Kornet team, and its bunker the Strongpoint, so the
    # cloned teams and build actions field both without a new script.
    "ChinaInfantryTankHunter": "RussiaKornetTeam",
    "ChinaBunker": "RussiaStrongpoint",
}

# Actions that name an upgrade or a building Russia does not have yet; dropped whole, because a
# build request for a China object is exactly the failure this patch removes.  An upgrade the
# faction does have is mapped below instead of dropped.
DROP_ACTIONS = set()

# China's AI buys its upgrades from script; Russia has its own first tier.  Anything not named here
# (the air upgrades above all - Russia has no aircraft) is dropped.
UPGRADES = {
    "Upgrade_InfantryCaptureBuilding": "Upgrade_InfantryCaptureBuilding",
    "Upgrade_Nationalism": "Upgrade_RussiaIgla",
    "Upgrade_ChinaNeutronShells": "Upgrade_RussiaArtilleryTraining",
    "Upgrade_ChinaUraniumShells": "Upgrade_RussiaERAShtora",
    "Upgrade_ChinaNuclearTanks": "Upgrade_RussiaERAShtora",
    "Upgrade_ChinaSubliminalMessaging": "Upgrade_RussiaInfantryTraining",
    "Upgrade_ChinaAircraftArmor": "Upgrade_RussiaAircraftArmor",
}

# China's superweapon power becomes Russia's: the Missile Brigade reuses the neutron missile until
# its own round exists, and the EMP scripts then wait for and fire it.
SPECIAL_POWERS = {
    # China's command centre has the short carpet-bomb power as well as the full one; the command
    # bunker inherits the short one, so the AI's carpet scripts must aim at it.
    "SuperweaponChinaCarpetBomb": "Early_SuperweaponChinaCarpetBomb",
}

# The team record fields that hold a script name rather than a unit or a number.
TEAM_SCRIPT_FIELDS = {
    "teamAllClearScript", "teamEnemySightedScript", "teamGenericScriptHook0",
    "teamGenericScriptHook1", "teamGenericScriptHook2", "teamGenericScriptHook3",
    "teamOnCreateScript", "teamOnDestroyedScript", "teamOnIdleScript",
    "teamOnUnitDestroyedScript", "teamProductionCondition",
}


def team_name(name):
    if name.startswith("China "):
        return "Russia " + name[len("China "):]
    if name.startswith("China"):
        return "Russia" + name[len("China"):]
    if name == "teamSkirmishChina":
        return "teamSkirmishRussia"
    return "Russia " + name


def rename_group(name):
    """The scripts carry their side in their names; a Russia group that says China in every line is
    the kind of thing that makes the next reader distrust the file."""
    if name.startswith("China "):
        return "Russia " + name[len("China "):]
    if name.startswith("China"):
        return "Russia" + name[len("China"):]
    return name


def clone_team(team, renamed_teams, renamed_scripts):
    entries = []
    unit_slot = 0
    for key, kind, value in team:
        value = copy.deepcopy(value)
        if key == "teamName":
            value = renamed_teams[value]
        elif key == "teamOwner" and value == SOURCE_PLAYER:
            value = PLAYER_NAME
        elif key in TEAM_SCRIPT_FIELDS and value in renamed_scripts:
            value = renamed_scripts[value]
        elif key.startswith("teamUnitType") and value and value != "<none>":
            slot = key[len("teamUnitType"):]
            mapped = UNITS.get(value)
            if mapped is None:
                # no Russia counterpart: empty the slot and zero its counts
                unit_slot = int(slot)
                value = "<none>"
            else:
                value = mapped
        entries.append((key, kind, value))
    if unit_slot:
        entries = [(key, kind, 0) if key in (f"teamUnitMinCount{unit_slot}", f"teamUnitMaxCount{unit_slot}") else (key, kind, value)
                   for key, kind, value in entries]
    return entries


def condition_names_china_object(script):
    """A condition that tests for an object Russia has no counterpart for can never be true; the
    script that needs it is dead weight and is dropped rather than left gated on a China object."""
    for child in script.children or []:
        if child.name not in ("Condition", "OrCondition"):
            continue
        candidates = [child] + list(child.children or [])
        for condition in candidates:
            if condition.name != "Condition":
                continue
            for parameter in condition.fields[2]:
                if parameter[0] == OBJECT_TYPE and parameter[3] not in UNITS and parameter[3].startswith("China"):
                    return True
    return False


def drop_dead_scripts(script_list):
    dropped = 0
    for group in list(script_list.children or []):
        for script in list(group.children or []):
            if script.name != "Script":
                continue
            if condition_names_china_object(script):
                group.children.remove(script)
                dropped += 1
            else:
                for sub in list(script.children or []):
                    if sub.name == "OrCondition":
                        for condition in list(sub.children or []):
                            for parameter in condition.fields[2]:
                                if parameter[0] == OBJECT_TYPE and parameter[3] not in UNITS and parameter[3].startswith("China"):
                                    group.children.remove(script)
                                    dropped += 1
                                    break
                            else:
                                continue
                            break
    return dropped


def rename_calls(chunk, renamed_scripts, renamed_powers):
    """Every <This Team> action and condition lives in one of these chunks, and the ones that name a
    script name it through a SCRIPT or SCRIPT_SUBROUTINE parameter.  A superweapon power is named
    by the SPECIAL_POWER parameter and moves the same way."""
    rewritten = 0
    if chunk.name in ("Condition", "ScriptAction", "ScriptActionFalse"):
        for parameter in chunk.fields[2]:
            if parameter[0] in (SCRIPT, SCRIPT_SUBROUTINE) and parameter[3] in renamed_scripts:
                parameter[3] = renamed_scripts[parameter[3]]
                rewritten += 1
            elif parameter[0] == SPECIAL_POWER and parameter[3] in renamed_powers:
                parameter[3] = renamed_powers[parameter[3]]
                rewritten += 1
    for child in chunk.children or []:
        rewritten += rename_calls(child, renamed_scripts, renamed_powers)
    return rewritten


def rename_conditions(script):
    """A condition that asks about an object type asks about the same object type Russia builds, or
    the script is dead: 'has a China dozer' is never true for a Russian player."""
    rewritten = 0
    for or_condition in script.children or []:
        if or_condition.name not in ("Condition", "OrCondition"):
            continue
        candidates = [or_condition] + list(or_condition.children or [])
        for condition in candidates:
            if condition.name != "Condition":
                continue
            for parameter in condition.fields[2]:
                if parameter[0] == OBJECT_TYPE and parameter[3] in UNITS:
                    parameter[3] = UNITS[parameter[3]]
                    rewritten += 1
    return rewritten


def rewrite_scripts(script_list, renamed_teams, renamed_scripts):
    dropped = 0
    rewritten = 0
    for group in script_list.children or []:
        if group.name == "ScriptGroup":
            group.fields[0] = rename_group(group.fields[0])
        for script in list(group.children or []):
            if script.name != "Script":
                continue
            script.fields[0] = rename_group(script.fields[0])
            rewritten += rename_calls(script, renamed_scripts, SPECIAL_POWERS)
            rewritten += rename_conditions(script)
            for child in list(script.children or []):
                if child.name not in ("ScriptAction", "ScriptActionFalse"):
                    continue
                action = child.fields[1]
                doomed = action in DROP_ACTIONS
                for parameter in child.fields[2]:
                    if parameter[0] == OBJECT_TYPE and parameter[3] in UNITS:
                        parameter[3] = UNITS[parameter[3]]
                        rewritten += 1
                    elif parameter[0] == OBJECT_TYPE and parameter[3].startswith("China"):
                        doomed = True
                    elif parameter[0] == TEAM and parameter[3] in renamed_teams:
                        parameter[3] = renamed_teams[parameter[3]]
                    elif parameter[0] == UPGRADE and action == "AI_PLAYER_BUILD_UPGRADE":
                        mapped = UPGRADES.get(parameter[3])
                        if mapped is None:
                            doomed = True
                        else:
                            parameter[3] = mapped
                            rewritten += 1
                if doomed:
                    script.children.remove(child)
                    dropped += 1
    return rewritten, dropped


def replace_power(chunk, old, new):
    if chunk.name in ("Condition", "ScriptAction", "ScriptActionFalse"):
        for parameter in chunk.fields[2]:
            if parameter[3] == old:
                parameter[3] = new
    for child in chunk.children or []:
        replace_power(child, old, new)


SCIENCES = (
    "SCIENCE_ArtilleryBarrage1", "SCIENCE_ArtilleryBarrage2", "SCIENCE_ArtilleryBarrage3",
    "SCIENCE_NapalmStrike", "SCIENCE_ClusterMines", "SCIENCE_EMPPulse",
    "SCIENCE_EmergencyRepair1", "SCIENCE_EmergencyRepair2", "SCIENCE_EmergencyRepair3",
    "Early_SCIENCE_ChinaCarpetBomb", "SCIENCE_Frenzy1", "SCIENCE_Frenzy2", "SCIENCE_Frenzy3",
)


def add_grant_sciences_script(script_list):
    """The AI does not evaluate a science-granting upgrade as worth buying, so when its Fire
    Direction Center stands it grants itself the power sciences outright.  A player still buys the
    (free) Command Doctrine at the centre; the AI gets the same powers at the same building."""
    group = next(g for g in script_list.children
                 if g.name == "ScriptGroup" and g.fields[0] == "Russia Generals Powers")
    template = next(s for s in group.children
                    if s.name == "Script" and s.fields[0] == "Russia Arillery Fire")

    condition = None
    action_template = None
    for group2 in script_list.children:
        if group2.name != "ScriptGroup":
            continue
        for script in group2.children:
            if script.name != "Script":
                continue
            for child in script.children:
                if child.name == "OrCondition":
                    for cond in child.children:
                        if cond.name == "Condition" and cond.fields[1] == "BUILT_BY_PLAYER":
                            condition = copy.deepcopy(child)
                            break
                if child.name in ("ScriptAction", "ScriptActionFalse") and child.fields[1] == "AI_PLAYER_BUILD_UPGRADE":
                    action_template = child
            if condition is not None and action_template is not None:
                break
        if condition is not None and action_template is not None:
            break
    if condition is None or action_template is None:
        return 0
    # keep only the built-by-player check: the donor script also carried a CT flag and a missile
    # brigade, which would gate the power sciences on the wrong thing
    condition.children = [c for c in condition.children
                          if c.name == "Condition" and c.fields[1] == "BUILT_BY_PLAYER"]
    for cond in condition.children:
        for parameter in cond.fields[2]:
            if parameter[3] in ("RussiaAirBase", "RussiaFireDirectionCenter", "RussiaMissileBrigade"):
                parameter[3] = "RussiaFireDirectionCenter"

    script = copy.deepcopy(template)
    script.fields[0] = "Russia Command Doctrine"
    script.fields[4] = 1
    script.fields[5] = 1
    script.fields[6] = 1
    script.fields[7] = 1
    script.fields[8] = 1
    script.fields[9] = 0
    script.fields[10] = 0
    actions = []
    for science in SCIENCES:
        action = copy.deepcopy(action_template)
        action.fields[0] = 276          # PLAYER_GRANT_SCIENCE
        action.fields[1] = "PLAYER_GRANT_SCIENCE"
        action.fields[2] = [[11, 0, 0.0, "<This Player>"], [31, 0, 0.0, science]]
        actions.append(action)
    script.children = [condition] + actions
    group.children.append(script)
    return 1


def add_power_scripts(script_list):
    """The AI has no scripts for three of the bunker's powers, so add one always-active fire script
    per power, cloned from the artillery one.  The artillery script's condition and first action are
    all a fire script needs; the new one does not toggle other scripts."""
    group = next(g for g in script_list.children
                 if g.name == "ScriptGroup" and g.fields[0] == "Russia Generals Powers")
    template = next(s for s in group.children
                    if s.name == "Script" and s.fields[0] == "Russia Arillery Fire")
    added = 0
    for name, power in (("Russia Buratino Salvo", "SuperweaponNapalmStrike"),
                        ("Russia Guards Assault", "SuperweaponFrenzy"),
                        ("Russia Field Repair", "SuperweaponEmergencyRepair")):
        script = copy.deepcopy(template)
        script.fields[0] = name
        script.fields[4] = 1     # active
        script.fields[5] = 0     # not once
        # easy/normal/hard: all three, or the script is never run at any difficulty
        script.fields[6] = 1
        script.fields[7] = 1
        script.fields[8] = 1
        script.fields[9] = 0     # not a subroutine
        script.fields[10] = 0    # no delay
        kept = []
        action_kept = False
        for child in script.children:
            if child.name == "OrCondition":
                replace_power(child, "SuperweaponArtilleryBarrage", power)
                kept.append(child)
            elif child.name in ("ScriptAction", "ScriptActionFalse") and not action_kept:
                replace_power(child, "SuperweaponArtilleryBarrage", power)
                kept.append(child)
                action_kept = True
        script.children = kept
        group.children.append(script)
        added += 1
    return added


def make_repeating(script_list, needle):
    """China's Nuke Silo scripts are once=1 with a condition that is false at frame 0 (a fire
    direction centre and an escalation counter), so they evaluate once, find nothing, and never
    look again: the AI asks for the building's prerequisite but never builds the silo.  Russia's
    copy runs until the condition holds."""
    changed = 0
    for group in script_list.children or []:
        for script in group.children or []:
            if script.name == "Script" and needle in script.fields[0]:
                script.fields[5] = 0
                changed += 1
    return changed


def lower_nuke_timer(script_list, threshold):
    """China's Hard Nuke Silo asks for escalation 75, about twelve and a half minutes of game time.
    Russia's measured matches are decided inside ten, so the Missile Brigade never reached the
    field.  Ask at 30 (five minutes) instead; the script's own 7,000 credits then pay for it."""
    changed = 0
    for group in script_list.children or []:
        for script in group.children or []:
            if script.name != "Script" or "Nuke Silo" not in script.fields[0]:
                continue
            for or_condition in script.children or []:
                conditions = [or_condition] if or_condition.name == "Condition" else list(or_condition.children or [])
                for condition in conditions:
                    if condition.name != "Condition":
                        continue
                    parameters = condition.fields[2]
                    if not parameters or parameters[0][0] != COUNTER or parameters[0][3] != "_ESCALATION":
                        continue
                    for parameter in parameters:
                        if parameter[0] == INT and parameter[1] > threshold:
                            parameter[1] = threshold
                            changed += 1
    return changed


def drop_nuke_gifts(script_list, needle):
    """China's repeating Nuke Silo script pays 7,000 credits each time it runs.  With once=0 that is
    every pass, and a measured Hard Russia's bank ran to 126 million before the Missile Brigade went
    up.  The brigade is a 5,000-credit saving decision here; the script asks for it and the AI saves."""
    dropped = 0
    for group in script_list.children or []:
        for script in group.children or []:
            if script.name != "Script" or needle not in script.fields[0]:
                continue
            for child in list(script.children or []):
                if child.name in ("ScriptAction", "ScriptActionFalse") and child.fields[1] == "PLAYER_GIVE_MONEY":
                    script.children.remove(child)
                    dropped += 1
    return dropped


def remove_existing(chunks):
    """Take a previous run's Russia entry out so the file can be regenerated in place."""
    players = [chunk for chunk in chunks if chunk.name == "ScriptsPlayers"][0]
    names = [name for name, _ in players.fields[1]]
    if PLAYER_NAME in names:
        index = names.index(PLAYER_NAME)
        del players.fields[1][index]
        lists = [chunk for chunk in chunks if chunk.name == "PlayerScriptsList"][0]
        del lists.children[index]
    teams = [chunk for chunk in chunks if chunk.name == "ScriptTeams"][0]
    teams.fields = [team for team in teams.fields
                    if dict((key, value) for key, _, value in team).get("teamOwner") != PLAYER_NAME]


def main():
    source, target = sys.argv[1], sys.argv[2]
    table, chunks = scbtool.load(source)

    remove_existing(chunks)

    players = [chunk for chunk in chunks if chunk.name == "ScriptsPlayers"][0]
    names = [name for name, _ in players.fields[1]]
    source_index = names.index(SOURCE_PLAYER)

    lists = [chunk for chunk in chunks if chunk.name == "PlayerScriptsList"][0]
    source_list = lists.children[source_index]
    russia_list = copy.deepcopy(source_list)

    teams = [chunk for chunk in chunks if chunk.name == "ScriptTeams"][0]
    source_teams = [team for team in teams.fields
                    if dict((key, value) for key, _, value in team).get("teamOwner") == SOURCE_PLAYER]
    renamed_teams = {}
    for team in source_teams:
        old = dict((key, value) for key, _, value in team)["teamName"]
        renamed_teams[old] = team_name(old)
    if len(set(renamed_teams.values())) != len(renamed_teams):
        raise SystemExit("two China teams map to one Russia name")

    # every script the copy will rename, so a reference can move with the definition
    renamed_scripts = {}
    for group in source_list.children or []:
        for script in group.children or []:
            if script.name == "Script":
                renamed_scripts[script.fields[0]] = rename_group(script.fields[0])

    dead = drop_dead_scripts(russia_list)
    rewritten, dropped = rewrite_scripts(russia_list, renamed_teams, renamed_scripts)
    dropped += dead
    repeating = make_repeating(russia_list, "Nuke Silo")
    lowered = lower_nuke_timer(russia_list, 30)
    gifts = drop_nuke_gifts(russia_list, "Nuke Silo")
    powers = add_power_scripts(russia_list)
    doctrine = add_grant_sciences_script(russia_list)
    for team in source_teams:
        teams.fields.append(clone_team(team, renamed_teams, renamed_scripts))

    players.fields[1].append((PLAYER_NAME, None))
    lists.children.append(russia_list)

    scbtool.save(target, table, chunks)
    print(f"{PLAYER_NAME}: {len(source_teams)} teams, {rewritten} names rewritten, "
          f"{dropped} actions dropped, {repeating} scripts made repeating, "
          f"{lowered} nuke timers lowered, {gifts} nuke money gifts dropped, "
          f"{powers} power scripts added, {doctrine} science-grant script")


if __name__ == "__main__":
    main()
