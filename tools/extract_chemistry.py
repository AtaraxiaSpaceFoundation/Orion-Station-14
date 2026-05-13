#!/usr/bin/env python3
# SPDX-License-Identifier: AGPL-3.0-or-later
"""
Extracts chemistry data (reagents + reactions) from SS14 prototypes
and locale files, outputting JSON suitable for wiki import.

Usage:
    python3 tools/extract_chemistry.py [--output-dir ./wiki_data]

Outputs:
    reagent.json  - All reagents with Russian names, descriptions, recipes, metabolisms
    reaction.json - All reactions (effect-only, no products)
"""

import yaml
import glob
import os
import re
import json
import argparse
from collections import defaultdict

BASE = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
PROTO = os.path.join(BASE, "Resources", "Prototypes")
LOCALE = os.path.join(BASE, "Resources", "Locale", "ru-RU")


# --- YAML loader that handles custom tags like !type:HealthChange ---

def _yaml_tag_ctor(loader, tag_suffix, node):
    if isinstance(node, yaml.MappingNode):
        return {"_type": tag_suffix, **loader.construct_mapping(node, deep=True)}
    elif isinstance(node, yaml.SequenceNode):
        return {"_type": tag_suffix, "_list": loader.construct_sequence(node, deep=True)}
    else:
        return {"_type": tag_suffix, "_value": loader.construct_scalar(node)}

yaml.add_multi_constructor("!", _yaml_tag_ctor, Loader=yaml.SafeLoader)


# --- Helpers ---

def load_yaml_dir(patterns):
    """Load all YAML docs matching glob patterns under PROTO."""
    entries = []
    for pat in patterns:
        for fpath in sorted(glob.glob(os.path.join(PROTO, pat), recursive=True)):
            try:
                with open(fpath, encoding="utf-8") as f:
                    docs = yaml.safe_load(f)
                if isinstance(docs, list):
                    for doc in docs:
                        if isinstance(doc, dict):
                            entries.append(doc)
            except Exception as e:
                print(f"WARN: {fpath}: {e}")
    return entries


def load_locale():
    """Parse all .ftl files under ru-RU locale into a flat dict."""
    loc = {}
    for fpath in glob.glob(os.path.join(LOCALE, "**", "*.ftl"), recursive=True):
        try:
            with open(fpath, encoding="utf-8") as f:
                for line in f:
                    line = line.strip()
                    if not line or line.startswith("#") or "=" not in line:
                        continue
                    key, _, val = line.partition("=")
                    key, val = key.strip(), val.strip()
                    if key and val:
                        loc[key] = val
        except Exception:
            pass
    return loc


def loc(locale, key):
    """Resolve locale key, return None if not found."""
    v = locale.get(key)
    return v if v and v != key else None


def _kebab(s):
    """Convert PascalCase/camelCase to kebab-case for locale key lookup."""
    # Insert hyphen before uppercase letters, then lowercase
    s = re.sub(r"(?<=[a-z0-9])([A-Z])", r"-\1", s)
    s = re.sub(r"(?<=[A-Z])([A-Z][a-z])", r"-\1", s)
    return s.lower()


def resolve_name(locale, reagent_id, name_key=None):
    """Try to resolve Russian name for a reagent."""
    # Try explicit name key from prototype
    if name_key:
        v = loc(locale, name_key)
        if v:
            return v
    # Try standard pattern
    v = loc(locale, f"reagent-name-{_kebab(reagent_id)}")
    if v:
        return v
    # Try lowercase id directly
    v = loc(locale, f"reagent-name-{reagent_id.lower()}")
    if v:
        return v
    return reagent_id


def resolve_desc(locale, reagent_id, desc_key=None):
    if desc_key:
        v = loc(locale, desc_key)
        if v:
            return v
    v = loc(locale, f"reagent-desc-{_kebab(reagent_id)}")
    if v:
        return v
    v = loc(locale, f"reagent-desc-{reagent_id.lower()}")
    if v:
        return v
    return ""


def resolve_phys(locale, phys_key):
    if not phys_key:
        return ""
    v = loc(locale, phys_key)
    return v or ""


# --- Effect serialization ---

def serialize_effects(effects):
    """Convert effect list to a simplified JSON-friendly structure."""
    if not isinstance(effects, list):
        return []
    result = []
    for eff in effects:
        if not isinstance(eff, dict):
            continue
        entry = {"type": eff.get("_type", "Unknown")}

        # Conditions
        conds = eff.get("conditions", [])
        if isinstance(conds, list) and conds:
            entry["conditions"] = []
            for c in conds:
                if not isinstance(c, dict):
                    continue
                ct = c.get("_type", "")
                cond = {"type": ct}
                if ct == "ReagentThreshold":
                    if "min" in c:
                        cond["min"] = c["min"]
                    if "max" in c:
                        cond["max"] = c["max"]
                elif ct == "OrganType":
                    cond["organType"] = c.get("type", "")
                    cond["shouldHave"] = c.get("shouldHave", True)
                elif ct == "Temperature":
                    if "min" in c:
                        cond["min"] = c["min"]
                    if "max" in c:
                        cond["max"] = c["max"]
                else:
                    # Include all fields
                    for k, v in c.items():
                        if k not in ("_type", "conditions"):
                            cond[k] = v
                entry["conditions"].append(cond)

        # Effect-specific fields
        etype = eff.get("_type", "")
        if etype == "HealthChange":
            damage = eff.get("damage", {})
            entry["damage"] = {}
            for cat in ("types", "groups"):
                d = damage.get(cat, {})
                if isinstance(d, dict) and d:
                    entry["damage"][cat] = dict(d)
        elif etype == "AdjustReagent":
            entry["reagent"] = eff.get("reagent", "")
            entry["amount"] = eff.get("amount", 0)
        elif etype == "GenericStatusEffect":
            entry["key"] = eff.get("key", "")
            if "time" in eff:
                entry["time"] = eff["time"]
            if "type" in eff and eff["type"] != eff.get("_type"):
                entry["effectAction"] = eff["type"]
        elif etype == "ModifyStatusEffect":
            entry["effectProto"] = eff.get("effectProto", "")
            if "time" in eff:
                entry["time"] = eff["time"]
        elif etype == "Drunk":
            if "boozePower" in eff:
                entry["boozePower"] = eff["boozePower"]
        elif etype == "SatiateThirst":
            entry["factor"] = eff.get("factor", 1)
        elif etype == "SatiateHunger":
            entry["factor"] = eff.get("factor", 1)
        elif etype == "ChemVomit":
            entry["probability"] = eff.get("probability", 1.0)
        elif etype == "PopupMessage":
            entry["probability"] = eff.get("probability", 1.0)
            entry["messages"] = eff.get("messages", [])
        elif etype == "ChemAddMoodlet":
            entry["mood"] = eff.get("moodPrototype", "")
        elif etype == "ExplosionReactionEffect":
            entry["explosionType"] = eff.get("explosionType", "Default")
            entry["intensityPerUnit"] = eff.get("intensityPerUnit", 0)
        elif etype == "AreaReactionEffect":
            entry["protoId"] = eff.get("protoId", "")
        elif etype == "CreateGas":
            entry["gas"] = eff.get("gas", "")
            entry["moles"] = eff.get("moles", 0)
        elif etype == "Emote":
            entry["emote"] = eff.get("emote", "")
        else:
            # Dump remaining fields
            for k, v in eff.items():
                if k not in ("_type", "conditions") and not k.startswith("_"):
                    try:
                        json.dumps(v)
                        entry[k] = v
                    except (TypeError, ValueError):
                        entry[k] = str(v)

        result.append(entry)
    return result


# --- Main ---

def main():
    parser = argparse.ArgumentParser(description="Extract chemistry data for wiki")
    parser.add_argument("--output-dir", default=os.path.join(BASE, "wiki_data"),
                        help="Output directory for JSON files")
    args = parser.parse_args()

    os.makedirs(args.output_dir, exist_ok=True)

    print("Loading locale files...")
    locale = load_locale()
    print(f"  {len(locale)} locale keys loaded")

    # --- Reagents ---
    print("Loading reagent prototypes...")
    reagent_entries = load_yaml_dir([
        "Reagents/**/*.yml",
        "_*/Reagents/**/*.yml",
    ])
    reagents = {}
    for e in reagent_entries:
        if e.get("type") == "reagent":
            rid = e.get("id")
            if rid:
                if rid in reagents:
                    reagents[rid].update(e)
                else:
                    reagents[rid] = e
    print(f"  {len(reagents)} reagents loaded")

    # --- Reactions ---
    print("Loading reaction prototypes...")
    reaction_entries = load_yaml_dir([
        "Recipes/Reactions/**/*.yml",
        "_*/Recipes/Reactions/**/*.yml",
    ])
    reactions = {}
    for e in reaction_entries:
        if e.get("type") == "reaction":
            rid = e.get("id")
            if rid:
                reactions[rid] = e
    print(f"  {len(reactions)} reactions loaded")

    # Build product -> reactions index
    product_reactions = defaultdict(list)
    for rid, rxn in reactions.items():
        for prod_id in (rxn.get("products") or {}):
            product_reactions[prod_id].append(rid)

    # --- Build reagent.json ---
    print("Building reagent.json...")
    reagent_json = {}

    for rid, r in sorted(reagents.items()):
        name_ru = resolve_name(locale, rid, r.get("name"))
        desc_ru = resolve_desc(locale, rid, r.get("desc"))
        phys_ru = resolve_phys(locale, r.get("physicalDesc", ""))
        group = r.get("group", "Unknown")
        color = r.get("color", "#ffffff")

        entry = {
            "id": rid,
            "name": name_ru,
            "desc": desc_ru,
            "group": group,
            "color": color,
        }
        if phys_ru:
            entry["physicalDesc"] = phys_ru

        # Recipes that produce this reagent
        rxn_ids = product_reactions.get(rid, [])
        if rxn_ids:
            recipes = []
            for rxn_id in rxn_ids:
                rxn = reactions[rxn_id]
                recipe = {"id": rxn_id, "reactants": {}, "products": {}}
                for react_id, react_data in (rxn.get("reactants") or {}).items():
                    if isinstance(react_data, dict):
                        rr = {
                            "name": resolve_name(locale, react_id),
                            "amount": react_data.get("amount", 1),
                        }
                        if react_data.get("catalyst"):
                            rr["catalyst"] = True
                        recipe["reactants"][react_id] = rr

                for prod_id, prod_amt in (rxn.get("products") or {}).items():
                    recipe["products"][prod_id] = {
                        "name": resolve_name(locale, prod_id),
                        "amount": prod_amt,
                    }

                if rxn.get("minTemp"):
                    recipe["minTemp"] = rxn["minTemp"]
                if rxn.get("maxTemp"):
                    recipe["maxTemp"] = rxn["maxTemp"]
                if rxn.get("requiredMixerCategories"):
                    recipe["mixer"] = rxn["requiredMixerCategories"]

                recipes.append(recipe)
            entry["recipes"] = recipes

        # Metabolisms
        mets = r.get("metabolisms")
        if isinstance(mets, dict) and mets:
            entry["metabolisms"] = {}
            for met_group, met_data in mets.items():
                met_name = loc(locale, f"metabolism-group-{met_group.lower()}") or met_group
                met_entry = {"name": met_name}
                if isinstance(met_data, dict):
                    if met_data.get("metabolismRate") is not None:
                        met_entry["rate"] = met_data["metabolismRate"]
                    effects = met_data.get("effects", [])
                    if effects:
                        met_entry["effects"] = serialize_effects(effects)
                entry["metabolisms"][met_group] = met_entry

        # Plant metabolism
        plant_met = r.get("plantMetabolism")
        if isinstance(plant_met, list) and plant_met:
            entry["plantMetabolism"] = serialize_effects(plant_met)

        # Physical properties
        if r.get("boilingPoint") is not None:
            entry["boilingPoint"] = r["boilingPoint"]
        if r.get("meltingPoint") is not None:
            entry["meltingPoint"] = r["meltingPoint"]
        if r.get("worksOnTheDead"):
            entry["worksOnTheDead"] = True

        reagent_json[rid] = entry

    reagent_path = os.path.join(args.output_dir, "reagent.json")
    with open(reagent_path, "w", encoding="utf-8") as f:
        json.dump(reagent_json, f, ensure_ascii=False, indent=2)
    print(f"  Written {len(reagent_json)} reagents to {reagent_path}")

    # --- Build reaction.json (effect-only reactions) ---
    print("Building reaction.json...")
    reaction_json = {}

    for rid, rxn in sorted(reactions.items()):
        reactants_raw = rxn.get("reactants") or {}
        products_raw = rxn.get("products") or {}
        effects_raw = rxn.get("effects") or []

        entry = {
            "id": rid,
            "reactants": {},
            "products": {},
        }

        for react_id, react_data in reactants_raw.items():
            if isinstance(react_data, dict):
                rr = {
                    "name": resolve_name(locale, react_id),
                    "amount": react_data.get("amount", 1),
                }
                if react_data.get("catalyst"):
                    rr["catalyst"] = True
                entry["reactants"][react_id] = rr

        for prod_id, prod_amt in products_raw.items():
            entry["products"][prod_id] = {
                "name": resolve_name(locale, prod_id),
                "amount": prod_amt,
            }

        if rxn.get("minTemp"):
            entry["minTemp"] = rxn["minTemp"]
        if rxn.get("maxTemp"):
            entry["maxTemp"] = rxn["maxTemp"]
        if rxn.get("requiredMixerCategories"):
            entry["mixer"] = rxn["requiredMixerCategories"]

        if effects_raw:
            entry["effects"] = serialize_effects(effects_raw)

        entry["hasProducts"] = bool(products_raw)
        entry["hasEffects"] = bool(effects_raw)

        reaction_json[rid] = entry

    reaction_path = os.path.join(args.output_dir, "reaction.json")
    with open(reaction_path, "w", encoding="utf-8") as f:
        json.dump(reaction_json, f, ensure_ascii=False, indent=2)
    print(f"  Written {len(reaction_json)} reactions to {reaction_path}")

    # --- Stats ---
    groups = defaultdict(int)
    for r in reagent_json.values():
        groups[r["group"]] += 1
    print("\nReagents by group:")
    for g, c in sorted(groups.items(), key=lambda x: -x[1]):
        print(f"  {g}: {c}")

    with_recipe = sum(1 for r in reagent_json.values() if r.get("recipes"))
    effect_only = sum(1 for r in reaction_json.values() if r.get("hasEffects") and not r.get("hasProducts"))
    print(f"\nReagents with recipes: {with_recipe}")
    print(f"Effect-only reactions: {effect_only}")
    print(f"\nDone!")


if __name__ == "__main__":
    main()
