extends Control
## Pixelkiln Icons Vol. 2 - browsable contact sheet.
##
## Every icon below is an AtlasTexture resource loaded straight from
## res://pixelkiln_icons/<category>/<name>.tres. Nothing is sliced at runtime.

const CATS := ["adventure", "armor", "food", "magic", "potions", "quest", "resources", "status", "tools", "ui", "weapons"]


func _ready() -> void:
	var bg := ColorRect.new()
	bg.color = Color(0.078, 0.063, 0.09)
	bg.set_anchors_preset(Control.PRESET_FULL_RECT)
	add_child(bg)

	var scroll := ScrollContainer.new()
	scroll.set_anchors_preset(Control.PRESET_FULL_RECT)
	add_child(scroll)

	var col := VBoxContainer.new()
	col.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	scroll.add_child(col)

	for cat in CATS:
		var head := Label.new()
		head.text = cat.to_upper()
		head.add_theme_color_override("font_color", Color(0.941, 0.725, 0.306))
		col.add_child(head)

		var grid := GridContainer.new()
		grid.columns = 16
		col.add_child(grid)

		var d := DirAccess.open("res://pixelkiln_icons/%s" % cat)
		if d == null:
			continue
		d.list_dir_begin()
		var f := d.get_next()
		var names: Array = []
		while f != "":
			if f.ends_with(".tres"):
				names.append(f.get_basename())
			f = d.get_next()
		d.list_dir_end()
		names.sort()
		for n in names:
			var tex := load("res://pixelkiln_icons/%s/%s.tres" % [cat, n])
			var r := TextureRect.new()
			r.texture = tex
			r.custom_minimum_size = Vector2(32, 32)
			r.tooltip_text = "%s/%s" % [cat, n]
			grid.add_child(r)
