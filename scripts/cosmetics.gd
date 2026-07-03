extends Node
## Global cosmetics catalog + the local player's current look.
## Autoloaded as "Cosmetics". This is the backbone for character customization
## and, later, the cosmetics shop / monetization: every item carries a rarity
## and (eventually) a price. The customization screen reads/writes `selection`,
## and the in-game player will tint its mesh from get_color() calls.

# Rarity → accent color (Fortnite-style tiers for the shop).
const RARITY := {
	"Common":    Color(0.62, 0.64, 0.70),
	"Rare":      Color(0.30, 0.62, 1.00),
	"Epic":      Color(0.72, 0.36, 1.00),
	"Legendary": Color(1.00, 0.78, 0.24),
}

# Each category holds a list of items. Items carry a display name, a rarity,
# and a payload the preview understands (a color, or a style/shape id).
var catalog := {
	"hair_style": [
		{"name": "Long",      "rarity": "Common",    "style": "long"},
		{"name": "Short",     "rarity": "Common",    "style": "short"},
		{"name": "Twin-tails","rarity": "Rare",      "style": "twin"},
		{"name": "Ponytail",  "rarity": "Rare",      "style": "pony"},
	],
	"hair_color": [
		{"name": "Raven",     "rarity": "Common",    "color": Color(0.12, 0.11, 0.14)},
		{"name": "Chestnut",  "rarity": "Common",    "color": Color(0.36, 0.22, 0.12)},
		{"name": "Blonde",    "rarity": "Rare",      "color": Color(0.92, 0.80, 0.42)},
		{"name": "Sakura",    "rarity": "Epic",      "color": Color(1.00, 0.55, 0.78)},
		{"name": "Ocean",     "rarity": "Epic",      "color": Color(0.35, 0.62, 0.95)},
		{"name": "Ghost",     "rarity": "Legendary", "color": Color(0.90, 0.92, 0.98)},
	],
	"eyes": [
		{"name": "Brown",     "rarity": "Common",    "color": Color(0.42, 0.26, 0.14)},
		{"name": "Sky",       "rarity": "Common",    "color": Color(0.40, 0.70, 0.95)},
		{"name": "Emerald",   "rarity": "Rare",      "color": Color(0.25, 0.75, 0.45)},
		{"name": "Amethyst",  "rarity": "Epic",      "color": Color(0.66, 0.40, 0.95)},
		{"name": "Yandere",   "rarity": "Legendary", "color": Color(0.95, 0.20, 0.25)},
	],
	"skin": [
		{"name": "Fair",      "rarity": "Common",    "color": Color(0.98, 0.87, 0.80)},
		{"name": "Warm",      "rarity": "Common",    "color": Color(0.93, 0.78, 0.66)},
		{"name": "Tan",       "rarity": "Common",    "color": Color(0.80, 0.62, 0.48)},
		{"name": "Deep",      "rarity": "Common",    "color": Color(0.52, 0.37, 0.28)},
	],
	"uniform": [
		{"name": "Navy",      "rarity": "Common",    "color": Color(0.16, 0.20, 0.34)},
		{"name": "Noir",      "rarity": "Common",    "color": Color(0.10, 0.10, 0.12)},
		{"name": "Crimson",   "rarity": "Rare",      "color": Color(0.55, 0.12, 0.16)},
		{"name": "Mint",      "rarity": "Epic",      "color": Color(0.40, 0.70, 0.58)},
		{"name": "Royal",     "rarity": "Legendary", "color": Color(0.35, 0.20, 0.62)},
	],
	"accessory": [
		{"name": "None",      "rarity": "Common",    "acc": "none"},
		{"name": "Ribbon",    "rarity": "Rare",      "acc": "bow"},
		{"name": "Glasses",   "rarity": "Rare",      "acc": "glasses"},
		{"name": "Headset",   "rarity": "Epic",      "acc": "headphones"},
		{"name": "Mask",      "rarity": "Legendary", "acc": "mask"},
	],
}

# Ordered list of categories as the UI should present them.
const CATEGORY_ORDER := ["hair_style", "hair_color", "eyes", "skin", "uniform", "accessory"]
const CATEGORY_LABEL := {
	"hair_style": "HAIR STYLE",
	"hair_color": "HAIR COLOR",
	"eyes":       "EYES",
	"skin":       "SKIN TONE",
	"uniform":    "UNIFORM",
	"accessory":  "ACCESSORY",
}

# Currently selected index per category.
var selection := {
	"hair_style": 0,
	"hair_color": 0,
	"eyes":       0,
	"skin":       0,
	"uniform":    0,
	"accessory":  0,
}

func item(category: String) -> Dictionary:
	var list: Array = catalog[category]
	var idx: int = clampi(selection.get(category, 0), 0, list.size() - 1)
	return list[idx]

func get_color(category: String) -> Color:
	return item(category).get("color", Color.WHITE)

func get_style() -> String:
	return item("hair_style").get("style", "long")

func get_accessory() -> String:
	return item("accessory").get("acc", "none")

func rarity_color(rarity: String) -> Color:
	return RARITY.get(rarity, Color.GRAY)
