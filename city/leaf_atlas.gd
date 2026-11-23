extends RefCounted

const SIDE := 144


static func create() -> ImageTexture:
	var texture := Image.create_empty(SIDE * 2, SIDE * 2, false, Image.FORMAT_RGBA8)
	for index in 4:
		var tile := _tile(index)
		texture.blit_rect(tile, Rect2i(0, 0, SIDE, SIDE), Vector2i(index % 2, index >> 1) * SIDE)
	texture.generate_mipmaps()
	return ImageTexture.create_from_image(texture)


static func _tile(index: int) -> Image:
	var atlas := Image.create_empty(SIDE, SIDE, false, Image.FORMAT_RGBA8)
	atlas.fill(Color(0.67, 0.72, 0.59, 0))
	var rng := RandomNumberGenerator.new()
	rng.seed = 21807 + index * 1579
	for branch in 11:
		var angle := branch * 2.39996
		var axis := Vector2(cos(angle), sin(angle))
		var tangent := Vector2(-axis.y, axis.x)
		var start := Vector2(SIDE * 0.5, SIDE * 0.5) + axis * rng.randf_range(3.1, 12.4) + tangent * rng.randf_range(-12.0, 12.0)
		var length := rng.randf_range(34.0, 58.0)
		_leaf(atlas, start + axis * length * 0.45, axis, length * 0.47, 0.95, 0.58)
		for leaf in 7:
			var along := (leaf + 1.0) / 8.0
			var side := 1.0 if leaf % 2 == 0 else -1.0
			var direction := (axis * rng.randf_range(0.61, 1.05) + tangent * side).normalized()
			var leaf_length := rng.randf_range(6.1, 12.6) * (1.04 - along * 0.34)
			var width := leaf_length * rng.randf_range(0.35, 0.51)
			var point := start + axis * length * along + direction * leaf_length * 0.61
			_leaf(atlas, point, direction, leaf_length, width, rng.randf_range(0.60, 0.95))
	return atlas


static func _leaf(atlas: Image, center: Vector2, axis: Vector2, length: float, width: float, value: float) -> void:
	var tangent := Vector2(-axis.y, axis.x)
	var extent := Vector2(absf(axis.x) * length + absf(tangent.x) * width,
		absf(axis.y) * length + absf(tangent.y) * width) + Vector2.ONE
	var left := maxi(0, floori(center.x - extent.x))
	var right := mini(SIDE, ceili(center.x + extent.x))
	var top := maxi(0, floori(center.y - extent.y))
	var bottom := mini(SIDE, ceili(center.y + extent.y))
	for y in range(top, bottom):
		for x in range(left, right):
			var point := Vector2(x + 0.5, y + 0.5) - center
			var u := point.dot(axis) / length
			var v := point.dot(tangent) / width
			var shape := u * u + v * v * (1.0 + 0.82 * absf(u))
			if shape <= 1.0:
				var vein := 1.0 - smoothstep(0.032, 0.19, absf(v) + absf(u) * 0.035)
				var color := clampf(value * (0.78 + u * 0.10 - absf(v) * 0.17) + vein * 0.18, 0.0, 1.0)
				atlas.set_pixel(x, y, Color(color, color * 1.04, color * 0.88, 1.0))
