#!/usr/bin/env python3
# SPDX-License-Identifier: AGPL-3.0-or-later
"""
Extracts cooking data (microwave recipes, food sequence elements, metamorph recipes)
from SS14 prototypes and locale files, outputting JSON suitable for wiki import.

Usage:
    python3 tools/extract_cooking.py [--output-dir ./wiki_data]

Outputs:
    microwave_recipes.json  - All microwave meal recipes with ingredients, groups, localized result names
    food_sequences.json     - Food sequence elements with tags, sprites, localized names
    metamorph_recipes.json  - Metamorphosis recipes with rules and tag requirements
    food_entities.json      - Food entity data (name, desc, reagents, flavors)
"""

import yaml
import glob
import os
import json
import argparse
from collections import defaultdict

BASE = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
PROTO = os.path.join(BASE, "Resources", "Prototypes")
LOCALE = os.path.join(BASE, "Resources", "Locale", "ru-RU")


# --- YAML loader that handles custom tags like !type:SequenceLength ---

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


def loc_get(locale, key):
    """Resolve locale key, return None if not found."""
    v = locale.get(key)
    return v if v and v != key else None


def resolve_entity_name(locale, entity_id):
    """Try to resolve Russian name for an entity."""
    v = loc_get(locale, f"ent-{entity_id}")
    if v:
        return v
    return entity_id


def resolve_entity_desc(locale, entity_id):
    """Try to resolve Russian description for an entity."""
    v = loc_get(locale, f"ent-{entity_id}.desc")
    if v:
        return v
    return ""


def resolve_loc_key(locale, key):
    """Resolve an arbitrary locale key."""
    if not key:
        return ""
    v = loc_get(locale, key)
    return v or key


# --- Metamorph rule serialization ---

def _clean_type(raw):
    """Strip 'type:' prefix from YAML custom tag names."""
    if raw.startswith("type:"):
        return raw[5:]
    return raw


def serialize_metamorph_rules(rules):
    """Convert metamorph rule list to a JSON-friendly structure."""
    if not isinstance(rules, list):
        return []
    result = []
    for rule in rules:
        if not isinstance(rule, dict):
            continue
        raw_type = rule.get("_type", "Unknown")
        rtype = _clean_type(raw_type)
        entry = {"type": rtype}

        if rtype == "SequenceLength":
            r = rule.get("range", {})
            if isinstance(r, dict):
                entry["min"] = r.get("min", 0)
                entry["max"] = r.get("max", 0)

        elif rtype == "IngredientsWithTags":
            entry["tags"] = rule.get("tags", [])
            count = rule.get("count", {})
            if isinstance(count, dict):
                entry["countMin"] = count.get("min", 0)
                entry["countMax"] = count.get("max", 0)

        elif rtype == "LastElementHasTags":
            entry["tags"] = rule.get("tags", [])

        elif rtype == "FoodHasReagent":
            entry["reagent"] = rule.get("reagent", "")
            count = rule.get("count", {})
            if isinstance(count, dict):
                entry["countMin"] = count.get("min", 0)
                entry["countMax"] = count.get("max", 0)

        else:
            # Dump remaining fields
            for k, v in rule.items():
                if k not in ("_type",) and not k.startswith("_"):
                    try:
                        json.dumps(v)
                        entry[k] = v
                    except (TypeError, ValueError):
                        entry[k] = str(v)

        result.append(entry)
    return result


# --- Main ---

def main():
    parser = argparse.ArgumentParser(description="Extract cooking data for wiki")
    parser.add_argument("--output-dir", default=os.path.join(BASE, "wiki_data"),
                        help="Output directory for JSON files")
    args = parser.parse_args()

    os.makedirs(args.output_dir, exist_ok=True)

    print("Loading locale files...")
    locale = load_locale()
    print(f"  {len(locale)} locale keys loaded")

    # --- Microwave Meal Recipes ---
    print("Loading microwave meal recipes...")
    recipe_entries = load_yaml_dir([
        "Recipes/Cooking/**/*.yml",
        "_*/Recipes/Cooking/**/*.yml",
    ])
    recipes = {}
    for e in recipe_entries:
        if e.get("type") == "microwaveMealRecipe":
            rid = e.get("id")
            if rid:
                recipes[rid] = e
    print(f"  {len(recipes)} microwave recipes loaded")

    # --- Food Sequence Elements ---
    print("Loading food sequence elements...")
    seq_entries = load_yaml_dir([
        "Recipes/Cooking/**/*.yml",
        "_*/Recipes/Cooking/**/*.yml",
        "_*/Cooking/**/*.yml",
    ])
    sequences = {}
    for e in seq_entries:
        if e.get("type") == "foodSequenceElement":
            sid = e.get("id")
            if sid:
                sequences[sid] = e
    print(f"  {len(sequences)} food sequence elements loaded")

    # --- Metamorph Recipes ---
    print("Loading metamorph recipes...")
    metamorph_entries = load_yaml_dir([
        "Recipes/Cooking/**/*.yml",
        "_*/Recipes/Cooking/**/*.yml",
        "_*/Cooking/**/*.yml",
    ])
    metamorphs = {}
    for e in metamorph_entries:
        if e.get("type") == "metamorphRecipe":
            mid = e.get("id")
            if mid:
                metamorphs[mid] = e
    print(f"  {len(metamorphs)} metamorph recipes loaded")

    # --- Food Entity Prototypes ---
    print("Loading food entity prototypes...")
    entity_entries = load_yaml_dir([
        "Entities/Objects/Consumable/Food/**/*.yml",
        "_*/Entities/Objects/Consumable/Food/**/*.yml",
    ])
    food_entities = {}
    for e in entity_entries:
        if e.get("type") == "entity":
            eid = e.get("id")
            if eid:
                food_entities[eid] = e
    print(f"  {len(food_entities)} food entities loaded")

    # --- Collect all result entity IDs from recipes ---
    result_entity_ids = set()
    for r in recipes.values():
        result_id = r.get("result")
        if result_id:
            result_entity_ids.add(result_id)
    for m in metamorphs.values():
        result_id = m.get("result")
        if result_id:
            result_entity_ids.add(result_id)

    # --- Build microwave_recipes.json ---
    print("Building microwave_recipes.json...")
    recipes_json = {}

    for rid, r in sorted(recipes.items()):
        result_id = r.get("result", "")
        result_name = resolve_entity_name(locale, result_id)
        result_desc = resolve_entity_desc(locale, result_id)
        group = r.get("group", "Unknown")
        cook_time = r.get("time", 5)
        secret = r.get("secretRecipe", False)

        entry = {
            "id": rid,
            "name": r.get("name", rid),
            "result": result_id,
            "resultName": result_name,
            "group": group,
            "cookTime": cook_time,
        }

        if result_desc:
            entry["resultDesc"] = result_desc

        if secret:
            entry["secret"] = True

        # Solid ingredients
        solids = r.get("solids") or {}
        if solids:
            entry["solids"] = {}
            for solid_id, amount in solids.items():
                solid_name = resolve_entity_name(locale, solid_id)
                entry["solids"][solid_id] = {
                    "name": solid_name,
                    "amount": amount,
                }

        # Reagent ingredients
        reagents = r.get("reagents") or {}
        if reagents:
            entry["reagents"] = {}
            for reagent_id, amount in reagents.items():
                # Reagent names use reagent-name-{kebab} pattern
                reagent_name = loc_get(locale, f"reagent-name-{reagent_id.lower()}")
                if not reagent_name:
                    reagent_name = reagent_id
                entry["reagents"][reagent_id] = {
                    "name": reagent_name,
                    "amount": amount,
                }

        recipes_json[rid] = entry

    recipe_path = os.path.join(args.output_dir, "microwave_recipes.json")
    with open(recipe_path, "w", encoding="utf-8") as f:
        json.dump(recipes_json, f, ensure_ascii=False, indent=2)
    print(f"  Written {len(recipes_json)} recipes to {recipe_path}")

    # --- Build food_sequences.json ---
    print("Building food_sequences.json...")
    sequences_json = {}

    for sid, s in sorted(sequences.items()):
        name_key = s.get("name", "")
        name_ru = resolve_loc_key(locale, name_key) if name_key else sid

        entry = {
            "id": sid,
            "name": name_ru,
        }

        if s.get("final"):
            entry["final"] = True

        tags = s.get("tags") or []
        if tags:
            entry["tags"] = tags

        # Sprites info
        sprites = s.get("sprites") or []
        if sprites:
            sprite_list = []
            for sp in sprites:
                if isinstance(sp, dict):
                    sprite_info = {}
                    if "sprite" in sp:
                        sprite_info["sprite"] = sp["sprite"]
                    if "state" in sp:
                        sprite_info["state"] = sp["state"]
                    if sprite_info:
                        sprite_list.append(sprite_info)
            if sprite_list:
                entry["sprites"] = sprite_list

        # Scale
        scale = s.get("scale")
        if scale:
            entry["scale"] = scale

        sequences_json[sid] = entry

    seq_path = os.path.join(args.output_dir, "food_sequences.json")
    with open(seq_path, "w", encoding="utf-8") as f:
        json.dump(sequences_json, f, ensure_ascii=False, indent=2)
    print(f"  Written {len(sequences_json)} sequence elements to {seq_path}")

    # --- Build metamorph_recipes.json ---
    print("Building metamorph_recipes.json...")
    metamorphs_json = {}

    for mid, m in sorted(metamorphs.items()):
        result_id = m.get("result", "")
        result_name = resolve_entity_name(locale, result_id)
        key = m.get("key", "")

        entry = {
            "id": mid,
            "key": key,
            "result": result_id,
            "resultName": result_name,
        }

        rules = m.get("rules") or []
        if rules:
            entry["rules"] = serialize_metamorph_rules(rules)

        metamorphs_json[mid] = entry

    metamorph_path = os.path.join(args.output_dir, "metamorph_recipes.json")
    with open(metamorph_path, "w", encoding="utf-8") as f:
        json.dump(metamorphs_json, f, ensure_ascii=False, indent=2)
    print(f"  Written {len(metamorphs_json)} metamorph recipes to {metamorph_path}")

    # --- Build food_entities.json (only entities referenced by recipes) ---
    print("Building food_entities.json...")
    food_json = {}

    for eid in sorted(result_entity_ids):
        name_ru = resolve_entity_name(locale, eid)
        desc_ru = resolve_entity_desc(locale, eid)

        entry = {
            "id": eid,
            "name": name_ru,
        }
        if desc_ru:
            entry["desc"] = desc_ru

        # Extract data from entity prototype if available
        entity = food_entities.get(eid)
        if entity:
            # Parent
            parent = entity.get("parent")
            if parent:
                if isinstance(parent, list):
                    entry["parent"] = parent
                else:
                    entry["parent"] = parent

            # Components data
            components = entity.get("components") or []
            for comp in components:
                if not isinstance(comp, dict):
                    continue
                ctype = comp.get("type", "")

                # Extract reagent content from SolutionContainerManager
                if ctype == "SolutionContainerManager":
                    solutions = comp.get("solutions") or {}
                    for sol_name, sol_data in solutions.items():
                        if isinstance(sol_data, dict):
                            reagents = sol_data.get("reagents") or []
                            if reagents:
                                entry_reagents = {}
                                for r in reagents:
                                    if isinstance(r, dict):
                                        r_id = r.get("ReagentId", "")
                                        r_qty = r.get("Quantity", 0)
                                        if r_id:
                                            r_name = loc_get(locale, f"reagent-name-{r_id.lower()}") or r_id
                                            entry_reagents[r_id] = {
                                                "name": r_name,
                                                "quantity": r_qty,
                                            }
                                if entry_reagents:
                                    entry["reagents"] = entry_reagents

                # Extract flavor profile
                if ctype == "FlavorProfile":
                    flavors = comp.get("flavors") or []
                    if flavors:
                        entry["flavors"] = flavors

                # Extract sprite info
                if ctype == "Sprite":
                    sprite = comp.get("sprite")
                    state = comp.get("state")
                    if sprite:
                        entry["sprite"] = sprite
                    if state:
                        entry["spriteState"] = state

        food_json[eid] = entry

    food_path = os.path.join(args.output_dir, "food_entities.json")
    with open(food_path, "w", encoding="utf-8") as f:
        json.dump(food_json, f, ensure_ascii=False, indent=2)
    print(f"  Written {len(food_json)} food entities to {food_path}")

    # --- Stats ---
    groups = defaultdict(int)
    for r in recipes_json.values():
        groups[r["group"]] += 1
    print("\nMicrowave recipes by group:")
    for g, c in sorted(groups.items(), key=lambda x: -x[1]):
        print(f"  {g}: {c}")

    metamorph_keys = defaultdict(int)
    for m in metamorphs_json.values():
        metamorph_keys[m["key"]] += 1
    print("\nMetamorph recipes by key:")
    for k, c in sorted(metamorph_keys.items(), key=lambda x: -x[1]):
        print(f"  {k}: {c}")

    seq_tags = defaultdict(int)
    for s in sequences_json.values():
        for t in s.get("tags", []):
            seq_tags[t] += 1
    print("\nFood sequence element tags:")
    for t, c in sorted(seq_tags.items(), key=lambda x: -x[1]):
        print(f"  {t}: {c}")

    with_solids = sum(1 for r in recipes_json.values() if r.get("solids"))
    with_reagents = sum(1 for r in recipes_json.values() if r.get("reagents"))
    secret = sum(1 for r in recipes_json.values() if r.get("secret"))
    print(f"\nRecipes with solid ingredients: {with_solids}")
    print(f"Recipes with reagent ingredients: {with_reagents}")
    print(f"Secret recipes: {secret}")
    print(f"Food entities referenced by recipes: {len(food_json)}")
    print(f"\nDone!")


if __name__ == "__main__":
    main()
