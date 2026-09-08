PIXELKILN ICONS - VOL. 2 / FREE SAMPLE
======================================
45 icons  |  by Pixelkiln  |  z3er1n.itch.io

A free slice of PIXELKILN ICONS VOL. 2, which is 381 16x16 pixel
icons across 14 categories:
https://z3er1n.itch.io/icons-vol2

These 45 icons are the same files that ship in the paid pack - not
lower-resolution, not watermarked, not re-drawn. Use them in anything,
commercially, forever, with no attribution required. If you never buy
the pack they are still yours.

WHY THESE 45
------------
Because a sample that shows one third of a set is useless, everything
here is COMPLETE:

  * the whole six-tier material ladder on one weapon - wood, stone,
    iron, gold, crystal, obsidian - so you can see what an upgrade
    path actually looks like rather than three rungs of it
  * hearts and stars in all three states (full / half / empty). Two
    of the three cannot draw a health bar
  * a chest that opens as well as closes
  * iron ore AND the ingot it smelts into

Held back for the paid pack: crafting stations, doors and gates, and
the Oakheart-matched subset - the three sets Vol. 2's v1.1 added.

WHAT'S IN THE BOX
-----------------
assets/
  1x/ 2x/ 4x/      every icon as a single PNG, per scale
    adventure/(2)    a wooden chest, closed and open
    armor/(4)        helmet, boots, ring, amulet - four equipment slots
    food/(3)         apple, bread, cooked meat
    magic/(3)        scroll, orb, rune
    potions/(4)      four liquid colours
    quest/(2)        the "!" and "?" questgiver markers
    resources/(4)    iron ore and the ingot it smelts into, ruby, coin
    status/(6)       hearts and stars, full / half / empty
    tools/(4)        pickaxe, key, lit torch, map
    ui/(7)           four arrows, tick, cross, cog
    weapons/(6)      the complete six-tier sword ladder
  sheets/          per-category 2x spritesheets + atlas_2x.json
godot_project/     runnable Godot 4 project - press play
engine/unity/      pre-sliced sheets + .meta
LICENSE.txt        plain-language licence (commercial use OK)

THE ENGINE INTEGRATION IS IN THE SAMPLE TOO
-------------------------------------------
This is the part worth trying, and it is the reason this sample is not
just a folder of PNGs. Vol. 2 does not hand you a sheet and a JSON and
leave the slicing to you.

GODOT 4
  godot_project/ opens and runs - press play for a browsable grid of
  every icon in this sample. Inside it:

    pixelkiln_icons/<category>_2x.png       the sheet
    pixelkiln_icons/<category>/<name>.tres  one AtlasTexture per icon

  Copy pixelkiln_icons/ to the ROOT of your project (keep the folder
  name - the res:// paths inside each .tres are relative to it), then:

    sprite.texture = load("res://pixelkiln_icons/weapons/sword_iron.tres")
    button.icon    = load("res://pixelkiln_icons/ui/check_gold.tres")

  project.godot already sets nearest-neighbour filtering. Copying the
  folder into an existing project instead? Set Rendering > Textures >
  Canvas Textures > Default Texture Filter to Nearest, or the icons
  will be blurry.

UNITY
  engine/unity/ holds the same sheets with a .meta beside each one,
  already set to Sprite (2D and UI), Multiple, Point filter, every
  rect filled in and named. Drag the PNG *and* its .meta in together
  and the sheet arrives pre-sliced.

  Copy only the PNG and Unity writes a fresh .meta, giving you one
  un-sliced sprite. The .meta is the integration.

  The sample's GUIDs are namespaced separately from the paid pack's,
  so installing both into one project does not collide.

ANY OTHER ENGINE
  sheets/atlas_2x.json maps every icon name to {x, y, w, h} on its
  category sheet, top-left origin. (Unity measures from the BOTTOM
  left; if you roll your own rects from this file, flip them:
  y_unity = sheet_height - y - h.)

THE FULL PACK
-------------
381 icons, 14 categories, same Godot and Unity integration
across all of them:

  https://z3er1n.itch.io/icons-vol2

Weapons, armour and tools there ship in all six material tiers, and
the oakheart/ set is drawn in the exact colour ramps of the Oakheart
tilesets so an icon dropped on an Oakheart floor shades like the
furniture next to it.

Updates are free for everyone who owns it. Nobody has requested an
icon yet - v1.1 came from auditing what the pack was missing - so if
there is something you need, the itch comments are open.

Icons are 16x16 at 1x with a uniform dark outline, and sit in the slot
and panel sprites from PIXELKILN UI PACK VOL. 1. Use nearest-neighbour
/ point filtering.

Made with AI assistance (graphics), as disclosed on the itch.io page.

(c) 2026 Pixelkiln
