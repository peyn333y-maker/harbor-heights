extends RefCounted

const Layout = preload("res://city/layout.gd")
const Batch = preload("res://city/mesh_batch.gd")
const CONCRETE := Color("#a59b8c")
const CURBSTONE := Color("#c2b7a1")
const WHITE := Color("#ddd9c7")
const YELLOW := Color("#e2b252")
const METAL := Color("#6d7472")
const PAINT := Vector2(2, 0)
const PAVING := Vector2(1, 0)

var layout: Layout


func _init(plan: Layout) -> void:
	layout = plan


func build_cell(i: int, j: int, ground: Batch, paint: Batch, props: Batch, signs: Node3D,
		signal_poses: Array[Array]) -> void:
	var corner := [Vector2i.ZERO, Vector2i.RIGHT, Vector2i.ONE, Vector2i.DOWN].find(Layout.ROUNDABOUT - Vector2i(i, j))
	_paving(ground, layout.block(i, j), 3.65, Layout.CURB, CONCRETE, PAVING, corner)
	_segment(paint, ground, true, i, j)
	_segment(paint, ground, false, j, i)
	if i == Layout.BLOCKS - 1:
		_segment(paint, ground, true, i + 1, j)
	if j == Layout.BLOCKS - 1:
		_segment(paint, ground, false, j + 1, i)
	if layout.roundabout(i, j):
		_roundabout(ground, paint, props, Vector2(layout.x[i], layout.z[j]))
	elif i > 0 and j > 0 and (layout.road_class(i, true) > 0 or layout.road_class(j, false) > 0):
		_signals(i, j, props, signs, signal_poses)


func _rounded(rect: Rect2, radius: float, ramps: bool, circle_corner: int, inset: float) -> PackedVector2Array:
	var points := PackedVector2Array()
	var corners := [rect.position, Vector2(rect.end.x, rect.position.y),
		rect.end, Vector2(rect.position.x, rect.end.y)]
	var signs: Array[Vector2] = [
		Vector2(1, 1), Vector2(-1, 1), Vector2(-1, -1), Vector2(1, -1),
	]
	var centers := PackedVector2Array()
	var radii := PackedFloat32Array()
	for corner in 4:
		var size := 9.75 if corner == circle_corner else radius
		centers.append(corners[corner] + signs[corner] * size)
		radii.append(size - inset)
	for corner in 4:
		var subdivisions := 9 if corner == circle_corner else 5
		for slice in subdivisions + 1:
			var angle: float = PI + (corner + float(slice) / subdivisions) * PI * 0.5
			points.append(centers[corner] + Vector2(cos(angle), sin(angle)) * radii[corner])
		var angle := PI + (corner + 1) * PI * 0.5
		var next_corner := (corner + 1) % 4
		var last := points[-1]
		var next := centers[next_corner] + Vector2(cos(angle), sin(angle)) * radii[next_corner]
		var length := centers[corner].distance_to(centers[next_corner])
		var parts := ceili(length / (2.8 if circle_corner >= 0 else 4.3))
		var distances := PackedFloat32Array()
		for part in range(1, parts):
			distances.append(length * part / parts)
		if ramps:
			distances.append_array(PackedFloat32Array([1.25, 3.05, length - 3.05, length - 1.25]))
			distances.sort()
		var previous := 0.0
		for distance in distances:
			if distance - previous > 0.12 and distance < length - 0.12:
				points.append(last.lerp(next, distance / length))
				previous = distance
	return points


func _curb_level(point: Vector2, rect: Rect2, height: float, circle_corner: int) -> float:
	var from_x := minf(absf(point.x - rect.position.x), absf(point.x - rect.end.x))
	var from_z := minf(absf(point.y - rect.position.y), absf(point.y - rect.end.y))
	var distance := maxf(from_x, from_z)
	var east := point.x > rect.get_center().x
	var south := point.y > rect.get_center().y
	var corner := (2 if east else 3) if south else int(east)
	if corner == circle_corner:
		var center := Vector2(layout.x[Layout.ROUNDABOUT.x], layout.z[Layout.ROUNDABOUT.y])
		var along := absf(point.y - center.y) if from_x < from_z else absf(point.x - center.x)
		return lerpf(0.028, height, smoothstep(1.30, 2.85, absf(along - 21.5)))
	return lerpf(0.028, height, smoothstep(0.92, 2.42, absf(distance - 4.07)))


func _paving(mesh: Batch, rect: Rect2, radius: float, height := Layout.CURB,
		inside_tint := CONCRETE, inside_type := PAVING, circle_corner := -1) -> void:
	var ramps := rect.size.x > 12.0 and rect.size.y > 12.0
	var rim := _rounded(rect, radius, ramps, circle_corner, 0.0)
	var inner := _rounded(rect, radius, ramps, circle_corner, 0.19)
	var walk := _rounded(rect, radius, true, circle_corner, 1.47) if ramps else inner
	var stone := Vector2(4, 0)
	for edge in rim.size():
		var next := (edge + 1) % rim.size()
		var p := rim[edge]
		var q := rim[next]
		var a := Vector3(p.x, _curb_level(p, rect, height, circle_corner) if ramps else height, p.y)
		var b := Vector3(q.x, _curb_level(q, rect, height, circle_corner) if ramps else height, q.y)
		var c := Vector3(inner[next].x, b.y, inner[next].y)
		var d := Vector3(inner[edge].x, a.y, inner[edge].y)
		var outward := Vector3(q.y - p.y, 0, p.x - q.x).normalized()
		mesh.quad(a, b, c, d, Vector3.UP, CURBSTONE, Rect2(0, 0, 1, 1), stone)
		mesh.quad(a, b, Vector3(b.x, 0, b.z), Vector3(a.x, 0, a.z),
			outward, CURBSTONE.darkened(0.16), Rect2(0, 0, 1, 1), stone)
		var u := Vector3(walk[edge].x, height, walk[edge].y)
		var v := Vector3(walk[next].x, height, walk[next].y)
		if ramps:
			mesh.quad(d, c, v, u, Vector3.UP, inside_tint, Rect2(0, 0, 1, 1), inside_type)
	# A long, narrow median can bend far enough for a center fan to cross its edge.
	for index in walk.size():
		walk[index] = layout.map_xy(walk[index])
	var triangles := Geometry2D.triangulate_polygon(walk)
	var warp := mesh.warp
	mesh.warp = Callable()
	for index in range(0, triangles.size(), 3):
		var a := walk[triangles[index]]
		var b := walk[triangles[index + 1]]
		var c := walk[triangles[index + 2]]
		mesh.triangle(Vector3(a.x, height, a.y), Vector3(b.x, height, b.y),
			Vector3(c.x, height, c.y), Vector3.UP, inside_tint, inside_type)
	mesh.warp = warp


func _strip(mesh: Batch, northbound: bool, across: float, start: float, finish: float,
		width: float, tint: Color, elevation := 0.012) -> void:
	var parts := maxi(1, ceili((finish - start) / 3.2))
	for part in parts:
		var low := lerpf(start, finish, float(part) / parts)
		var high := lerpf(start, finish, float(part + 1) / parts)
		mesh.floor_rect(_road_rect(northbound, across - width * 0.5, low, width, high - low),
			elevation, tint, PAINT)


func _road_rect(northbound: bool, across: float, along: float, width: float, length: float) -> Rect2:
	if not northbound:
		return Rect2(along, across, length, width)
	return Rect2(across, along, width, length)


func _segment(paint: Batch, ground: Batch, northbound: bool, road: int, segment: int) -> void:
	var along := layout.z if northbound else layout.x
	var across := layout.x if northbound else layout.z
	var axis := across[road]
	var class_id := layout.road_class(road, northbound)
	var width := layout.half_width(road, northbound)
	var start_junction := Vector2i(road, segment) if northbound else Vector2i(segment, road)
	var end_junction := start_junction + (Vector2i.DOWN if northbound else Vector2i.RIGHT)
	var round_start := start_junction == Layout.ROUNDABOUT
	var round_end := end_junction == Layout.ROUNDABOUT
	var round_edge := Layout.CIRCULATION_RADIUS + 3.0
	var entry := along[segment] + (round_edge if round_start else layout.half_width(segment, not northbound))
	var exit := along[segment + 1] - (round_edge if round_end else layout.half_width(segment + 1, not northbound))
	if layout.pedestrian_link(road, segment, northbound):
		_greenway(ground, northbound, axis, width, entry, exit)
		return
	var start := entry + 8.0
	var finish := exit - 8.0

	if class_id == 2:
		var median := Rect2(axis - 0.82, start + 0.4, 1.64, finish - start - 0.8)
		_paving(ground, median, 0.80, 0.14, Color("#666849"), Vector2(3, 0))
		for offset in [-1.18, 1.18]:
			_strip(paint, northbound, axis + offset, start, finish, 0.13, YELLOW)
	elif class_id == 1:
		_strip(paint, northbound, axis - 0.14, start, finish, 0.12, YELLOW)
		_strip(paint, northbound, axis + 0.14, start, finish, 0.12, YELLOW)
	else:
		_dashes(paint, northbound, axis, start, finish, 2.40, 7.3, 0.12, YELLOW)

	if class_id > 0:
		var lane: float = 4.46 if class_id == 2 else 3.35
		for side in [-1.0, 1.0]:
			_dashes(paint, northbound, axis + lane * side, start, finish, 3.1, 8.1, 0.13, WHITE)
			_strip(paint, northbound, axis + (width - 2.20) * side, start, finish, 0.105, WHITE.darkened(0.14))
			var direction: float = -side if northbound else side
			_arrow(paint, northbound, axis + lane * 0.64 * side,
				entry + 10.3 if direction < 0 else exit - 10.3, direction)

	var first_crossing := class_id > 0 or layout.road_class(segment, not northbound) > 0
	var second_crossing := class_id > 0 or layout.road_class(segment + 1, not northbound) > 0
	if segment > 0 and (first_crossing or round_start):
		_crossing(paint, northbound, axis, width, entry, 1.0, round_start)
	if segment < Layout.BLOCKS - 1 and (second_crossing or round_end):
		_crossing(paint, northbound, axis, width, exit, -1.0, round_end)


func _greenway(ground: Batch, northbound: bool, axis: float, width: float, start: float, finish: float) -> void:
	var path := _road_rect(northbound, axis - width, start, width * 2.0, finish - start)
	_paving(ground, path, 0.85)
	for side in [-1.0, 1.0]:
		var from: float = axis + side * 3.39 - 1.34
		var lawn := _road_rect(northbound, from, start + 4.2, 2.68, finish - start - 8.4)
		_paving(ground, lawn, 1.12, Layout.CURB + 0.056, Color("#77764f"), Vector2(3, 0))


func _roundabout(ground: Batch, paint: Batch, props: Batch, center: Vector2) -> void:
	const SECTIONS := 80
	var cap := Vector3(center.x, 0.26, center.y)
	var tile := Vector2(4, 0)
	for section in SECTIONS:
		var a := Vector3(cos(TAU * section / SECTIONS), 0, sin(TAU * section / SECTIONS))
		var b := Vector3(cos(TAU * (section + 1) / SECTIONS), 0, sin(TAU * (section + 1) / SECTIONS))
		var foot := Vector3(center.x, 0.007, center.y)
		var lip := Vector3(center.x, 0.052, center.y)
		var lawn := cap
		ground.quad(foot + a * 8.70, foot + b * 8.70, lip + b * 7.27, lip + a * 7.27,
			Vector3.UP, Color("#9e8f80"), Rect2(0, 0, 1, 1), tile)
		ground.quad(lip + a * 7.27, lip + b * 7.27, lawn + b * 7.04, lawn + a * 7.04,
			(a + b).normalized(), CURBSTONE, Rect2(0, 0, 1, 1), tile)
		ground.quad(lawn + a * 7.04, lawn + b * 7.04, lawn + b * 6.69, lawn + a * 6.69,
			Vector3.UP, CURBSTONE, Rect2(0, 0, 1, 1), tile)
		ground.triangle(cap, lawn + a * 6.69, lawn + b * 6.69, Vector3.UP, Color("#737851"), Vector2(3, 0))
		if section % 4 < 2:
			var line := Vector3(center.x, 0.015, center.y)
			paint.quad(line + a * 9.25, line + b * 9.25, line + b * 9.38, line + a * 9.38,
				Vector3.UP, WHITE, Rect2(0, 0, 1, 1), PAINT)
	for northbound: bool in [true, false]:
		var axis := center.x if northbound else center.y
		var along := center.y if northbound else center.x
		var road_index := Layout.ROUNDABOUT.x if northbound else Layout.ROUNDABOUT.y
		var width := layout.half_width(road_index, northbound)
		for side in [-1.0, 1.0]:
			var low: float = along + (15.7 if side > 0.0 else -19.3)
			var splitter := _road_rect(northbound, axis - 0.64, low, 1.28, 3.6)
			_paving(ground, splitter, 0.62, 0.13, Color("#b1a59a"), tile)
			var inbound_side: float = side if northbound else -side
			var across := axis + inbound_side * (width + 1.3)
			var pole := _road_point(northbound, across, along + side * 25.3)
			pole.y = Layout.CURB
			var front := Vector3(0, 0, side) if northbound else Vector3(side, 0, 0)
			_yield_sign(props, pole, front)


func _yield_sign(mesh: Batch, pole: Vector3, front: Vector3) -> void:
	mesh.beam(pole, pole + Vector3.UP * 2.68, 0.04, 0.033, METAL, 6)
	var center := pole + Vector3.UP * 2.48
	var side := front.cross(Vector3.UP)
	for layer in 3:
		var scale := 0.64 if layer == 2 else 1.0
		var depth: float = [-0.036, 0.039, 0.042][layer]
		var point := center + front * depth
		var left := point + (Vector3.UP * 0.25 - side * 0.39) * scale
		var right := point + (Vector3.UP * 0.25 + side * 0.39) * scale
		var tip := point - Vector3.UP * 0.43 * scale
		var tint: Color = [METAL, Color("#b65343"), Color("#f0e5cf")][layer]
		mesh.triangle(left, right, tip, -front if layer == 0 else front, tint)


func _dashes(mesh: Batch, northbound: bool, across: float, from: float, to: float,
		length: float, interval: float, width: float, tint: Color) -> void:
	var start := from + 0.65
	while start + length <= to:
		_strip(mesh, northbound, across, start, start + length, width, tint)
		start += interval


func _road_point(northbound: bool, across: float, along: float) -> Vector3:
	if northbound:
		return Vector3(across, 0.013, along)
	return Vector3(along, 0.013, across)


func _arrow(mesh: Batch, northbound: bool, across: float, along: float, direction: float) -> void:
	var tail := along - 0.85 * direction
	var shoulder := along + 0.48 * direction
	_strip(mesh, northbound, across, minf(tail, shoulder), maxf(tail, shoulder), 0.16, WHITE, 0.013)
	var tip := _road_point(northbound, across, along + 1.38 * direction)
	var left := _road_point(northbound, across - 0.47, shoulder)
	var right := _road_point(northbound, across + 0.47, shoulder)
	mesh.triangle(left, right, tip, Vector3.UP, WHITE, PAINT)


func _crossing(mesh: Batch, northbound: bool, across: float, width: float, edge: float, side: float,
		at_roundabout: bool) -> void:
	var low := minf(edge + side * 2.0, edge + side * 5.25)
	var high := maxf(edge + side * 2.0, edge + side * 5.25)
	var count := floori((width - 0.43) * 2.0 / 0.78)
	for index in count:
		var offset := (index - (count - 1) * 0.5) * 0.78
		_strip(mesh, northbound, across + offset, low, high, 0.40, WHITE)
	var inbound := (side > 0.0) == northbound
	var lane_side := 1.0 if inbound else -1.0
	if at_roundabout:
		var teeth := floori((width - 1.1) / 0.72)
		for index in teeth:
			var x := across + lane_side * (0.83 + index * 0.72)
			var a := _road_point(northbound, x - 0.24, edge - side * 0.7)
			var b := _road_point(northbound, x + 0.24, edge - side * 0.7)
			var tip := _road_point(northbound, x, edge - side * 0.08)
			mesh.triangle(a, b, tip, Vector3.UP, WHITE, PAINT)
		return
	var stop := edge + 6.50 * side
	_strip(mesh, northbound, across + lane_side * (width + 0.90) * 0.5,
		stop - 0.15, stop + 0.15, width - 1.40, WHITE)


func _signals(i: int, j: int, props: Batch, signs: Node3D, signal_poses: Array[Array]) -> void:
	for sx in [-1.0, 1.0]:
		for sz in [-1.0, 1.0]:
			var position := Vector3(layout.x[i] + sx * (layout.half_width(i, true) + 2.0), Layout.CURB,
				layout.z[j] + sz * (layout.half_width(j, false) + 3.8))
			var high := position + Vector3.UP * 6.8
			var northbound: bool = sx != sz
			var arm := Vector3(-sx * 5.1, 0, 0) if northbound else Vector3(0, 0, -sz * 5.1)
			var front := Vector3(0, 0, -sz) if northbound else Vector3(-sx, 0, 0)
			var base := layout.map_point(position)
			var right := (base - layout.map_point(position + arm)) / arm.length()
			var forward := (layout.map_point(position + front) - layout.map_point(position - front)) * 0.5
			var pose := Transform3D(Basis(right, Vector3.UP, forward), base - props.origin)
			signal_poses[int(northbound)].append(pose)
			if i == Layout.CENTER and j in [4, 5, 6]:
				var name := ["CYPRESS AVE", "MARLOW AVE", "BENTON ST"][j - 4] as String
				_street_sign(props, signs, high + arm * 0.43 - Vector3.UP * 0.40, front,
					name if northbound else "CRESCENT BLVD")


func _street_sign(mesh: Batch, signs: Node3D, point: Vector3, front: Vector3, title: String) -> void:
	var size := Vector3(3.58, 0.52, 0.088) if front.z != 0.0 else Vector3(0.088, 0.52, 3.58)
	mesh.box(point, size, Color("#33604c"), true)
	var label := Label3D.new()
	var tangent := layout.map_heading(point, front.cross(Vector3.UP))
	var heading := Vector3.UP.cross(tangent)
	label.position = layout.map_point(point) + heading * 0.072 - signs.position
	label.rotation.y = atan2(heading.x, heading.z)
	label.text = title
	label.font_size = 84
	label.pixel_size = 0.00435
	label.modulate = Color("#e4e3cc")
	label.outline_size = 0
	label.double_sided = false
	label.alpha_cut = Label3D.ALPHA_CUT_DISCARD
	signs.add_child(label)
