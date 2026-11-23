extends RefCounted

const Layout = preload("res://city/layout.gd")
const Batch = preload("res://city/mesh_batch.gd")
const FacadeGeometry = preload("res://city/facade_geometry.gd")
const ROOF := Color("#645e56")
const IRON := Color("#4a4843")
const BRICK := [Color("#ae634c"), Color("#925241"), Color("#a97e63"), Color("#b88866")]
const GLASS := [Color("#4e6878"), Color("#5a7580"), Color("#758582"), Color("#676575")]
const CONCRETE := [Color("#d0bea4"), Color("#aaa187"), Color("#c6aa8b"), Color("#aab1a5")]
const STONE := [Color("#c7b69d"), Color("#bfa982"), Color("#ada188"), Color("#bfaa93")]
const AWNINGS := [Color("#6d2828"), Color("#35574f"), Color("#344a60"), Color("#ae8450")]
const SHOPS := ["MARLOW COFFEE", "SALT & STONE", "CYPRESS MARKET", "PACIFIC RECORDS",
	"ORCHID HOTEL", "BAY CITY BOOKS", "SUNDOWN DELI", "PORT SUPPLY"]

var layout: Layout
var placement := Transform3D.IDENTITY


func _init(plan: Layout) -> void:
	layout = plan


func build_cell(i: int, j: int, ground: Batch, attach_lot: Callable) -> int:
	var rng := layout.random(i, j, 132)
	var block := layout.block(i, j)
	var parcel := block.grow(-Layout.SIDEWALK)
	var along_x := (i + j) % 3 == 0 and absf(i - Layout.CENTER + 0.5) > 0.6
	var depth := parcel.size.y if along_x else parcel.size.x
	var stretch := parcel.size.x if along_x else parcel.size.y
	var middle := depth * rng.randf_range(0.46, 0.54)
	var alley := 4.8
	var alley_rect := _lot(parcel, along_x, 0.0, stretch, middle - alley * 0.5, alley)
	ground.floor_rect(alley_rect, Layout.CURB + 0.006, Color("#706a60"), Vector2(4, 0))
	var count := 0
	var distance := Vector2(i - 4.48, j - 3.25).length()
	for side in 2:
		var positive := side == 1
		var facing := Vector3.BACK if along_x else Vector3.RIGHT
		if not positive:
			facing = -facing
		var start := 0.0
		var lot_count := clampi(roundi(stretch / 25.0), 2, 4)
		for index in lot_count:
			var end := stretch if index == lot_count - 1 else stretch * float(index + 1) / lot_count + rng.randf_range(-2.75, 2.75)
			var front_setback := rng.randf_range(0.65, 2.75)
			var near_edge := middle + alley * 0.5 + rng.randf_range(0.55, 1.4) if positive else front_setback
			var deep_edge := depth - front_setback if positive else middle - alley * 0.5 - rng.randf_range(0.55, 1.4)
			var gap := rng.randf_range(0.80, 1.65)
			var footprint := _lot(parcel, along_x, start + gap, end - start - gap * 2.0, near_edge, deep_edge - near_edge)
			placement = layout.building_pose(footprint.get_center(), facing)
			footprint = layout.fit_building(footprint, placement)
			start = end
			var style := rng.randi_range(0, 3)
			var floors := rng.randi_range(2, 5)
			var setback_floor := 0
			if distance < 2.75 and rng.randf() < 0.41:
				floors = rng.randi_range(9, 21)
				style = 1 if rng.randf() < 0.70 else 3
				setback_floor = rng.randi_range(2, 4)
			elif distance < 4.0:
				floors += rng.randi_range(0, 4)
			elif j >= 7 and i >= 6:
				style = rng.randi_range(0, 1) * 2
				floors = rng.randi_range(1, 3)
			var on_boulevard := (i == 4 and positive) or (i == 5 and not positive)
			if on_boulevard and (j == 4 or j == 5):
				var choice: Array[int] = [0, 3, 2, 0]
				style = choice[(index + j + side) % choice.size()]
				floors = rng.randi_range(3, 6)
				setback_floor = 0
			if (i == 4 and j == 3 and positive and index == 1) or (i == 5 and j == 2 and not positive and index == 1):
				style = 3 if i == 4 else 1
				floors = 24 if i == 4 else 32
				setback_floor = 4
			var display_sign := on_boulevard and j >= 3 and j <= 6
			var hotel := i == 4 and j == 4 and positive and index == lot_count - 1
			var center := placement * Vector3(footprint.get_center().x, 0, footprint.get_center().y)
			attach_lot.call(center, func(walls: FacadeGeometry, trim: Batch, details: Batch, solids: StaticBody3D, signs: Node3D) -> void:
				walls.place(placement)
				trim.placement = placement
				details.placement = placement
				if hotel:
					_corner_hotel(footprint, rng, walls, trim, details, solids, signs)
				else:
					_building(footprint, facing, style, floors, setback_floor, display_sign,
						rng, walls, trim, details, solids, signs))
			count += 1
	return count


func _lot(parcel: Rect2, along_x: bool, along: float, width: float, depth_origin: float,
		depth: float) -> Rect2:
	var p := Vector2(along, depth_origin) if along_x else Vector2(depth_origin, along)
	var size := Vector2(width, depth) if along_x else Vector2(depth, width)
	return Rect2(parcel.position + p, size)


func _building(rect: Rect2, front: Vector3, style: int, floors: int, setback_floor: int,
		show_sign: bool, rng: RandomNumberGenerator, facades: FacadeGeometry, trim: Batch,
		details: Batch, solids: StaticBody3D, signs: Node3D) -> void:
	var palette: Array = [BRICK, GLASS, CONCRETE, STONE][style]
	var tint: Color = palette[rng.randi_range(0, palette.size() - 1)].lerp(Color("#a89887"), rng.randf_range(0, 0.14))
	var belt := tint.lightened(0.16 if style != 1 else 0.06)
	var salt := float(rng.randi_range(1, 700))
	var ground := Layout.CURB
	var base_top := ground + (4.55 if style != 1 else 5.45)
	var story := [3.28, 3.60, 3.16, 3.48][style] as float
	var bay := [3.55, 3.25, 4.65, 3.75][style] as float
	var podium_floors := setback_floor if setback_floor > 0 else floors
	var podium_top := base_top + story * podium_floors

	facades.walls(rect, ground, base_top, tint.darkened(0.17), 1, 3.65, Vector2(4.0, salt))
	facades.walls(rect, base_top, podium_top, tint, podium_floors, bay, Vector2(style, salt))
	trim.floor_rect(rect, podium_top, ROOF)
	_solid(solids, rect, ground, podium_top)
	_belt(trim, rect, base_top, 0.19, 0.28, belt)
	_parapet(trim, rect, podium_top, 0.63 if style != 1 else 0.36, belt)
	if style == 0 or style == 3:
		_belt(trim, rect, podium_top - 0.29, 0.19, 0.24, belt)
		_belt(details, rect, base_top + story * 0.94, 0.075, 0.08, belt.darkened(0.06))
	var roof := podium_top
	var roof_rect := rect
	if setback_floor > 0:
		var upper := rect.grow(-minf(3.75, minf(rect.size.x, rect.size.y) * 0.17))
		var upper_stories := floors - setback_floor
		var crown_stories := 0
		if style == 1 and upper_stories >= 10 and int(salt) % 3 != 0:
			crown_stories = maxi(3, roundi(upper_stories * 0.28))
			upper_stories -= crown_stories
		roof = podium_top + upper_stories * story
		facades.walls(upper, podium_top, roof, tint, upper_stories, bay, Vector2(style, salt + 19.0))
		trim.floor_rect(upper, roof, ROOF)
		_parapet(trim, upper, roof, 0.53, belt)
		_solid(solids, upper, podium_top, roof)
		roof_rect = upper
		if crown_stories > 0:
			var step_in := minf(1.30, minf(upper.size.x, upper.size.y) * 0.093)
			var high := upper.grow(-step_in)
			high.position.x += 0.32 * step_in
			var crown_top := roof + story * crown_stories
			facades.walls(high, roof, crown_top, tint.darkened(0.16), crown_stories, bay, Vector2(style, salt + 33.0))
			trim.floor_rect(high, crown_top, ROOF)
			_parapet(trim, high, crown_top, 0.43, belt)
			_solid(solids, high, roof, crown_top)
			roof = crown_top
			roof_rect = high
		if floors >= 20:
			roof = _crown(roof_rect, roof, tint, belt, style, facades, trim, solids)
			roof_rect = roof_rect.grow(-2.3)
	if style in [2, 3] and floors < 10:
		_balconies(rect, front, base_top, story, mini(floors, 8), bay, rng, details, belt)
	if style == 0 and floors >= 3 and rng.randf() < 0.62:
		_fire_escape(rect, -front, base_top, story, mini(floors, 7), details)
	_mechanicals(roof_rect, roof, rng, trim, details)
	if show_sign or (style != 1 and rng.randf() < 0.24):
		_storefront(rect, front, base_top, tint, rng, details, signs if show_sign else null)


func _chamfer(rect: Rect2, cut: float) -> PackedVector2Array:
	return PackedVector2Array([rect.position, Vector2(rect.end.x, rect.position.y),
		rect.end - Vector2(0, cut), rect.end - Vector2(cut, 0), Vector2(rect.position.x, rect.end.y)])


func _corner_hotel(rect: Rect2, rng: RandomNumberGenerator, walls: FacadeGeometry, trim: Batch,
		details: Batch, solids: StaticBody3D, signs: Node3D) -> void:
	var cut := 4.25
	var outline := _chamfer(rect, cut)
	var brick := Color("#a77350")
	var limestone := Color("#c9ad7f")
	var brass := Color("#8b6844")
	var front := Vector3(1, 0, 1).normalized()
	var tangent := Vector3(1, 0, -1).normalized()
	var corner := rect.end - Vector2(cut, cut) * 0.5
	var door := Vector3(corner.x, Layout.CURB, corner.y)
	var podium := 5.18
	var top := 22.9
	walls.outline(outline, Layout.CURB, podium, Color("#917658"), 1, 3.15, Vector2(4, 84), 2)
	walls.outline(outline, podium, top, brick, 5, 3.24, Vector2(0, 121))
	trim.cover(outline, top, ROOF)
	_convex_solid(solids, outline, Layout.CURB, top)

	for height in [podium, 8.70, 15.75, 22.47, top + 0.15]:
		var belt := _chamfer(rect.grow(0.20), cut + 0.116)
		trim.outline(belt, height - 0.21, height, limestone, 1, 4.0, Vector2.ZERO)
		trim.cover(belt, height, limestone)
	for edge in [-1.0, 1.0]:
		var rib: Vector3 = door + tangent * edge * 2.67
		details.beam(rib + Vector3.UP * 4.92, rib + Vector3.UP * 23.02, 0.13, 0.10, limestone, 6)
		details.beam(rib, rib + Vector3.UP * 4.66, 0.26, 0.23, limestone, 8)
	var crown := rect.grow(-3.55)
	walls.walls(crown, top + 0.15, top + 2.64, limestone, 1, 1000.0, Vector2(5, 78))
	trim.floor_rect(crown, top + 2.64, ROOF)
	_parapet(trim, crown, top + 2.64, 0.37, limestone)
	_solid(solids, crown, top + 0.15, top + 2.64)
	_mechanicals(crown, top + 2.64, rng, trim, details)

	# A chamfered entrance and canopy address the diagonal crossing.
	var a := door + Vector3.UP * 4.18 - tangent * 2.89
	var b := door + Vector3.UP * 4.18 + tangent * 2.89
	var projection := front * 2.01
	var fascia_height := Vector3.UP * 0.56
	var edge_color := Color("#64523c")
	details.quad(a, b, b + projection, a + projection, Vector3.DOWN, edge_color)
	details.quad(a + fascia_height, b + fascia_height, b + projection + fascia_height,
		a + projection + fascia_height, Vector3.UP, brass)
	details.quad(a, a + projection, a + projection + fascia_height, a + fascia_height, -tangent, brass)
	details.quad(b, b + projection, b + projection + fascia_height, b + fascia_height, tangent, brass)
	details.quad(a + projection, b + projection, b + projection + fascia_height,
		a + projection + fascia_height, front, edge_color)
	var title := (a + b) * 0.5 + projection + front * 0.018 + fascia_height * 0.5
	_sign(signs, title, front, "M A R L O W   H O T E L", 0.0048, Color("#fff0c7"))
	for lamp in 5:
		details.beam(title - front * 0.79 - fascia_height * 0.50 + tangent * (lamp - 2.0) * 1.02,
			title - front * 0.79 - fascia_height * 0.66 + tangent * (lamp - 2.0) * 1.02,
			0.14, 0.13, Color("#c8a373"), 8)
	var plaque := door + front * 0.245 + Vector3.UP * 10.9 + tangent * 1.87
	var plaque_side := tangent * 0.56
	var plaque_height := Vector3.UP * 3.62
	details.quad(plaque - plaque_side - plaque_height, plaque + plaque_side - plaque_height,
		plaque + plaque_side + plaque_height, plaque - plaque_side + plaque_height, front, edge_color)
	var sign := Label3D.new()
	sign.text = "M\nA\nR\nL\nO\nW"
	sign.position = placement * (plaque + front * 0.01) - signs.position
	var heading := placement.basis * front
	sign.rotation.y = atan2(heading.x, heading.z)
	sign.font_size = 100
	sign.pixel_size = 0.009
	sign.modulate = Color("#e2bf7c")
	sign.outline_size = 6
	sign.outline_modulate = Color("#62402c")
	sign.double_sided = false
	signs.add_child(sign)


func _convex_solid(body: StaticBody3D, outline: PackedVector2Array, base: float, top: float) -> void:
	var points := PackedVector3Array()
	var center := body.get_parent_node_3d().global_position
	for point in outline:
		points.append(placement * Vector3(point.x, base, point.y) - center)
		points.append(placement * Vector3(point.x, top, point.y) - center)
	var shape := ConvexPolygonShape3D.new()
	shape.points = points
	var collision := CollisionShape3D.new()
	collision.shape = shape
	body.add_child(collision)


func _crown(rect: Rect2, base: float, tint: Color, belt: Color, style: int,
		facades: FacadeGeometry, trim: Batch, solids: StaticBody3D) -> float:
	var first := rect.grow(-1.08)
	var second := rect.grow(-2.3)
	var top := base + 6.0
	facades.walls(first, base, base + 3.25, tint, 1, 3.0, Vector2(style, 53))
	facades.walls(second, base + 3.25, top, tint, 1, 3.0, Vector2(style, 17))
	trim.floor_rect(first, base + 3.25, belt)
	trim.floor_rect(second, top, ROOF)
	_belt(trim, second, top, 0.16, 0.23, belt)
	_solid(solids, first, base, base + 3.25)
	_solid(solids, second, base + 3.25, top)
	if style == 3:
		var middle := second.get_center()
		var point := Vector3(middle.x, top, middle.y)
		trim.beam(point, point + Vector3.UP * 9.8, 0.34, 0.065, belt, 8)
	return top


func _belt(mesh: Batch, rect: Rect2, top: float, projection: float, height: float, tint: Color) -> void:
	var outer := rect.grow(projection)
	mesh.walls(outer, top - height, top, tint)
	var x := outer.position.x
	var z := outer.position.y
	var right := outer.end.x
	var back := outer.end.y
	mesh.floor_rect(Rect2(x, z, right - x, projection), top, tint)
	mesh.floor_rect(Rect2(x, rect.end.y, right - x, projection), top, tint)
	mesh.floor_rect(Rect2(x, rect.position.y, projection, rect.size.y), top, tint)
	mesh.floor_rect(Rect2(rect.end.x, rect.position.y, projection, rect.size.y), top, tint)


func _parapet(mesh: Batch, rect: Rect2, base: float, height: float, tint: Color) -> void:
	var c := rect.get_center()
	var width := 0.23
	for side in [-1.0, 1.0]:
		mesh.box(Vector3(c.x, base + height * 0.5, c.y + (rect.size.y - width) * 0.5 * side),
			Vector3(rect.size.x, height, width), tint)
		mesh.box(Vector3(c.x + (rect.size.x - width) * 0.5 * side, base + height * 0.5, c.y),
			Vector3(width, height, rect.size.y - width * 2.02), tint)


func _wall_point(rect: Rect2, front: Vector3, y: float, offset := 0.0) -> Vector3:
	var c := rect.get_center()
	var span := rect.size.x if absf(front.x) > 0.5 else rect.size.y
	return Vector3(c.x, y, c.y) + front * (span * 0.5 + offset)


func _oriented_size(front: Vector3, width: float, height: float, depth: float) -> Vector3:
	return Vector3(depth, height, width) if absf(front.x) > 0.5 else Vector3(width, height, depth)


func _balconies(rect: Rect2, front: Vector3, base: float, story: float, floors: int, bay_width: float,
		rng: RandomNumberGenerator, mesh: Batch, tint: Color) -> void:
	var across := front.cross(Vector3.UP)
	var frontage := rect.size.y if absf(front.x) > 0.5 else rect.size.x
	var section := maxi(1, roundi(frontage / bay_width))
	var phase := rng.randi_range(0, 1)
	for floor_index in floors:
		var level := base + story * floor_index
		for slot in section:
			if (slot + phase) % 3 == 2:
				continue
			var offset := across * ((slot + 0.5) / section - 0.5) * frontage
			var platform := _wall_point(rect, front, level + 0.16, 0.65) + offset
			var width := minf(4.2, frontage / section * 0.89)
			mesh.box(platform, _oriented_size(front, width, 0.27, 1.28), tint, true)
			var railing := platform + front * 0.60 + Vector3.UP * 0.82
			mesh.box(railing, _oriented_size(front, width, 0.065, 0.073), IRON, true)
			for picket in 5:
				var point := railing + across * width * (picket / 4.0 - 0.5) - Vector3.UP * 0.36
				mesh.box(point, Vector3(0.048, 0.76, 0.048), IRON)


func _fire_escape(rect: Rect2, front: Vector3, base: float, story: float, floors: int,
		mesh: Batch) -> void:
	var across := front.cross(Vector3.UP)
	for tier in floors:
		var level := base + tier * story + 0.36
		var platform := _wall_point(rect, front, level, 0.66)
		mesh.box(platform, _oriented_size(front, 3.8, 0.13, 1.24), IRON, true)
		var rail := platform + front * 0.57
		mesh.box(rail + Vector3.UP * 0.82, _oriented_size(front, 3.8, 0.06, 0.06), IRON)
		for picket in 7:
			mesh.box(rail + across * (picket - 3.0) * 0.61 + Vector3.UP * 0.42,
				Vector3(0.035, 0.84, 0.035), IRON)
		if tier == 0:
			continue
		var sign := -1.0 if tier % 2 == 0 else 1.0
		var top := platform + across * sign * 1.45 + front * 0.23
		var bottom := top - across * sign * 2.9 - Vector3.UP * story
		for step in 10:
			mesh.box(bottom.lerp(top, step / 10.0), _oriented_size(front, 0.305, 0.06, 0.64), IRON, true)


func _mechanicals(rect: Rect2, roof: float, rng: RandomNumberGenerator, trim: Batch, mesh: Batch) -> void:
	var c := rect.get_center()
	var center := Vector3(c.x, roof, c.y)
	var hvac := center + Vector3(rng.randf_range(-1, 1), 0.58, rng.randf_range(-1, 1))
	trim.box(hvac, Vector3(3.25, 1.16, 2.55), Color("#969591"))
	for vent in 6:
		mesh.box(hvac + Vector3(0, 0.60, (vent - 2.5) * 0.24), Vector3(2.78, 0.065, 0.065), IRON)
	if rect.size.x > 12.0 and rect.size.y > 12.0 and rng.randf() < 0.42:
		var penthouse := center + Vector3(rect.size.x * 0.21, 0.84, rect.size.y * 0.21)
		trim.box(penthouse, Vector3(4.1, 1.68, 3.45), Color("#aca290"))
		trim.box(penthouse + Vector3.UP * 0.89, Vector3(4.35, 0.12, 3.70), Color("#756f63"))
		trim.beam(penthouse + Vector3(1.0, 0.89, 0), penthouse + Vector3(1.0, 2.63, 0), 0.12, 0.11, IRON, 6)
	if rng.randf() < 0.26:
		var tank := center + Vector3(rect.size.x * 0.27, 0, -rect.size.y * 0.22)
		var pipe := Color("#7d7260")
		trim.beam(tank, tank + Vector3.UP * 0.8, 0.76, 0.86, IRON, 10)
		trim.beam(tank + Vector3.UP * 0.8, tank + Vector3.UP * 3.03, 1.15, 1.15, pipe, 12)
		trim.beam(tank + Vector3.UP * 3.03, tank + Vector3.UP * 3.50, 1.19, 0.26, pipe.darkened(0.15), 12)


func _storefront(rect: Rect2, front: Vector3, top: float, tint: Color, rng: RandomNumberGenerator,
		mesh: Batch, signs: Node3D) -> void:
	var frontage := rect.size.y if absf(front.x) > 0.5 else rect.size.x
	var width := minf(11.8, frontage - 2.1)
	var point := _wall_point(rect, front, top - 0.86)
	var across := front.cross(Vector3.UP)
	var cloth: Color = AWNINGS[rng.randi_range(0, AWNINGS.size() - 1)]
	var a := point - across * width * 0.5
	var b := point + across * width * 0.5
	var overhang := front * 1.32 - Vector3.UP * 0.53
	mesh.quad(a, b, b + overhang, a + overhang, (Vector3.UP + front * 0.40).normalized(), cloth)
	mesh.quad(a, b, b + overhang, a + overhang, (-Vector3.UP - front * 0.40).normalized(), cloth.darkened(0.18))
	mesh.box(point + overhang - Vector3.UP * 0.10, _oriented_size(front, width, 0.22, 0.065), cloth, true)
	if signs == null:
		return
	var board := point + Vector3.UP * 0.43 + front * 0.095
	mesh.box(board, _oriented_size(front, width * 0.90, 0.67, 0.18), tint.darkened(0.63))
	_sign(signs, board + front * 0.096, front, SHOPS[rng.randi_range(0, SHOPS.size() - 1)],
		0.0047, Color("#ebd9b9"))


func _sign(signs: Node3D, point: Vector3, front: Vector3, title: String, scale: float, tint: Color) -> void:
	var label := Label3D.new()
	label.text = title
	label.position = placement * point - signs.position
	var heading := placement.basis * front
	label.rotation.y = atan2(heading.x, heading.z)
	label.font_size = 98
	label.pixel_size = scale
	label.modulate = tint
	label.outline_size = 0
	label.double_sided = false
	label.alpha_cut = Label3D.ALPHA_CUT_DISCARD
	signs.add_child(label)


func _solid(body: StaticBody3D, rect: Rect2, base: float, top: float) -> void:
	var shape := BoxShape3D.new()
	shape.size = Vector3(rect.size.x, top - base, rect.size.y)
	var collision := CollisionShape3D.new()
	var center := rect.get_center()
	collision.position = placement * Vector3(center.x, (base + top) * 0.5, center.y) - body.get_parent_node_3d().global_position
	collision.basis = placement.basis
	collision.shape = shape
	body.add_child(collision)
