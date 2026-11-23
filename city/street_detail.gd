extends RefCounted

const Layout = preload("res://city/layout.gd")
const Batch = preload("res://city/mesh_batch.gd")
const Models = preload("res://city/street_models.gd")
const Trees = preload("res://city/trees.gd")
const CONCRETE := Color("#aaa092")
const METAL := Color("#555952")
const SOIL := Color("#605140")
const LAWN := Color("#6f7147")

var layout: Layout


func _init(plan: Layout) -> void:
	layout = plan


func build_cell(i: int, j: int, ground: Batch, paint: Batch, street: Batch, trees: Trees,
		shrubs: Batch, lamps: Batch) -> void:
	var rng := layout.random(i, j, 51)
	var rect := layout.block(i, j)
	var p := rect.position
	var q := rect.end
	if layout.roundabout(i, j):
		_roundabout_trees(i, j, trees, shrubs)
	_greenway_trees(i, j, true, trees)
	_greenway_trees(j, i, false, trees)
	var on_boulevard := i == 4 or i == 5
	var street_x := q.x - 1.25 if i == 4 else p.x + 1.25
	var center := rect.get_center()
	if on_boulevard:
		var extension := Vector3.RIGHT if i == 4 else Vector3.LEFT
		for offset in [13.0, rect.size.y - 13.0]:
			Models.lamp(street, lamps, Vector3(street_x, Layout.CURB, p.y + offset), extension)
		for offset in [5.7, rect.size.y * 0.49, rect.size.y - 5.7]:
			var point := Vector3(street_x + (-0.70 if i == 4 else 0.70), Layout.CURB, p.y + offset)
			_tree_pit(point, ground, street)
			Models.tree_grate(street, point)
			trees.add(layout, point, rng.randf_range(7.2, 12.2), true, rng)
		if (i == 4 and j == 5) or (i == 5 and j == 4):
			var stop := Vector3(street_x - extension.x * 1.36, Layout.CURB, p.y + 26.0)
			_shelter(stop, extension, street, lamps)
		if i == Layout.CENTER:
			var median_start := layout.z[j] + 26.8 if layout.roundabout(i, j) else p.y + 8.8
			var median_end := layout.z[j + 1] - 26.8 if layout.roundabout(i, j + 1) else q.y - 8.8
			for offset in [12.3, 25.0, rect.size.y * 0.54, rect.size.y - 12.0]:
				for cluster in 3:
					var point := Vector3(rng.randf_range(-0.23, 0.23), 0.24, p.y + offset + cluster * 0.48)
					if point.z >= median_start and point.z <= median_end:
						_crown(point, rng.randf_range(0.23, 0.44), rng, shrubs, Color("#747e4c"))
	else:
		Models.lamp(street, lamps, Vector3(p.x + 1.15, Layout.CURB, center.y + 7.0), Vector3.LEFT)
		for point in [Vector3(p.x + 2.2, Layout.CURB, p.y + 12.4), Vector3(q.x - 2.1, Layout.CURB, q.y - 10.5)]:
			_tree_pit(point, ground, street)
			trees.add(layout, point, rng.randf_range(5.0, 8.8), false, rng)
	if not layout.pedestrian_link(i, j, true):
		var manhole := Vector3(layout.x[i] + layout.half_width(i, true) * 0.43, 0.004, center.y - 8.6)
		street.beam(manhole, manhole + Vector3.UP * 0.017, 0.37, 0.35, Color("#444443"), 12)
		for k in 4:
			var grille := manhole + Vector3((k - 1.5) * 0.12, 0.024, 0)
			street.box(grille, Vector3(0.027, 0.008, 0.39), Color("#292a2a"))
	var gutter := Vector3(p.x - 0.28, 0.016, p.y + 7.5)
	street.box(gutter, Vector3(0.47, 0.024, 0.77), Color("#555350"))
	for line in 5:
		street.box(gutter + Vector3(0, 0.014, (line - 2.0) * 0.116), Vector3(0.36, 0.009, 0.045), Color("#2f2f2f"))
	if i in [4, 5] and j in [3, 4, 5, 6]:
		var side := -1.0 if i == 4 else 1.0
		for k in 2:
			var start := p.y + 18.0 + k * 6.55
			var edge := street_x - side * 2.2
			paint.floor_rect(Rect2(edge - 0.055, start, 0.11, 3.45), 0.011, Color("#b1ac9d"), Vector2(2, 0))
			paint.floor_rect(Rect2(minf(edge, edge + side * 0.75), start, 0.75, 0.11),
				0.011, Color("#b1ac9d"), Vector2(2, 0))
		Models.bench(street, Vector3(street_x + side * 1.65, Layout.CURB, center.y - 6.0),
			Vector3(-side, 0, 0), Color("#896b4e"))
		Models.hydrant(street, Vector3(street_x - side * 0.52, Layout.CURB, p.y + 8.65), Vector3(-side, 0, 0))
		var shade := Color("#686f5b") if i == 4 else Color("#536661")
		Models.litter_bin(street, Vector3(street_x + side * 1.19, Layout.CURB, center.y + 8.0), shade)
		var first_pad := layout.z[j] + 21.5 if layout.roundabout(Layout.CENTER, j) else p.y + 4.15
		var last_pad := layout.z[j + 1] - 21.5 if layout.roundabout(Layout.CENTER, j + 1) else q.y - 4.15
		for on_end in [first_pad, last_pad]:
			var pad := Rect2(street_x + side * 0.88 - 0.42, on_end - 0.92, 0.84, 1.84)
			ground.floor_rect(pad, Layout.CURB + 0.008, Color("#bd9c55"), Vector2(5, 0))


func _roundabout_trees(i: int, j: int, trees: Trees, shrubs: Batch) -> void:
	var rng := layout.random(i, j, 201)
	var center := Vector3(layout.x[i], 0.26, layout.z[j])
	for tree in 3:
		var angle := float(tree) * TAU / 3.0 + 0.32
		var point := center + Vector3(cos(angle), 0, sin(angle)) * 2.72
		trees.add(layout, point, 7.5 + tree * 1.18, true, rng)
	for shrub in 18:
		var angle := float(shrub) * TAU / 18.0
		var point := center + Vector3(cos(angle), 0.25, sin(angle)) * rng.randf_range(4.72, 5.66)
		_crown(point, rng.randf_range(0.35, 0.59), rng, shrubs, Color("#79834f"))


func _greenway_trees(road: int, segment: int, northbound: bool, trees: Trees) -> void:
	if not layout.pedestrian_link(road, segment, northbound):
		return
	var rng := layout.random(road, segment, 201 + int(northbound))
	var across := layout.x if northbound else layout.z
	var along := layout.z if northbound else layout.x
	var axis := across[road]
	for index in 3:
		var crossing := axis + (3.39 if index % 2 == 0 else -3.39)
		var point := lerpf(along[segment], along[segment + 1], (index + 1.0) / 4.0)
		var base := Layout.CURB + 0.056
		var root := Vector3(crossing, base, point) if northbound else Vector3(point, base, crossing)
		trees.add(layout, root, rng.randf_range(4.9, 6.8), false, rng)


func park(i: int, j: int, ground: Batch, street: Batch, trees: Trees) -> void:
	var rng := layout.random(i, j, 118)
	var park_rect := layout.block(i, j).grow(-5.6)
	var center := park_rect.get_center()
	var path := 4.2
	for sx in [-1.0, 1.0]:
		for sz in [-1.0, 1.0]:
			var min_x: float = park_rect.position.x if sx < 0 else center.x + path * 0.5
			var max_x: float = center.x - path * 0.5 if sx < 0 else park_rect.end.x
			var min_z: float = park_rect.position.y if sz < 0 else center.y + path * 0.5
			var max_z: float = center.y - path * 0.5 if sz < 0 else park_rect.end.y
			var lawn := Rect2(min_x, min_z, max_x - min_x, max_z - min_z)
			ground.walls(lawn, Layout.CURB, 0.30, CONCRETE)
			ground.floor_rect(lawn, 0.30, LAWN, Vector2(3, 0))
			for k in 3:
				var x := rng.randf_range(min_x + 4.1, max_x - 4.1)
				var z := lerpf(min_z + 4.4, max_z - 4.4, (k + rng.randf_range(0.2, 0.8)) / 3.0)
				trees.add(layout, Vector3(x, 0.30, z), rng.randf_range(5.6, 10.2), false, rng)
			Models.bench(street, Vector3(center.x + sx * 1.45, Layout.CURB, lerpf(min_z, max_z, 0.66)),
				Vector3(-sx, 0, 0), Color("#8d7356"))


func _tree_pit(point: Vector3, ground: Batch, street: Batch) -> void:
	var rect := Rect2(point.x - 0.84, point.z - 0.84, 1.68, 1.68)
	ground.floor_rect(rect, Layout.CURB + 0.012, SOIL, Vector2(4, 0))
	for sign_value in [-1.0, 1.0]:
		street.box(point + Vector3(sign_value * 0.84, 0.046, 0), Vector3(0.12, 0.092, 1.80), CONCRETE)
		street.box(point + Vector3(0, 0.046, sign_value * 0.84), Vector3(1.54, 0.092, 0.12), CONCRETE)


func _shelter(point: Vector3, front: Vector3, mesh: Batch, lamps: Batch) -> void:
	var edge := point - front * 0.85
	var canopy := Color("#4c6665")
	for z in [-1.96, 1.96]:
		mesh.box(edge + Vector3(0, 1.48, z), Vector3(0.13, 2.96, 0.13), METAL)
		mesh.box(point + front * 0.58 + Vector3(0, 1.44, z), Vector3(0.088, 2.88, 0.088), METAL)
	mesh.box(point + Vector3.UP * 2.93, Vector3(1.78, 0.18, 4.30), canopy, true)
	mesh.box(edge + Vector3.UP * 1.08, Vector3(0.11, 1.71, 4.0), canopy)
	mesh.box(edge + front * 0.45 + Vector3.UP * 0.49, Vector3(0.41, 0.12, 3.13), Color("#9c8266"), true)
	lamps.box(point + Vector3.UP * 2.824, Vector3(0.09, 0.021, 2.75), Color("#f5e6bd"), true)


func _crown(center: Vector3, radius: float, rng: RandomNumberGenerator, foliage: Batch, tint: Color) -> void:
	var phase := rng.randf_range(0.0, TAU)
	for spray in 8:
		var angle := phase + spray * 2.39996
		var tilt := rng.randf_range(-0.83, 0.83)
		var horizontal := sqrt(1.0 - tilt * tilt)
		var n := Vector3(cos(angle) * horizontal, tilt, sin(angle) * horizontal)
		var roll := rng.randf_range(-0.73, 0.73)
		var side := Vector3.UP.cross(n).normalized().rotated(n, roll)
		var up := n.cross(side)
		var inner := spray % 3 == 0
		var point := center + n * radius * (0.18 if inner else rng.randf_range(0.32, 0.67))
		var w := side * radius * rng.randf_range(0.58, 0.86)
		var h := up * radius * rng.randf_range(0.61, 0.91)
		var leaves := tint.darkened(0.14) if inner else tint.lightened(rng.randf_range(0.02, 0.16))
		foliage.quad(point - w - h, point + w - h, point + w + h, point - w + h, n, leaves,
			Rect2(0, 0, 1, 1), Vector2(rng.randi_range(0, 3), angle))

