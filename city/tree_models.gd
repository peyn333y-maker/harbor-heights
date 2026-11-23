extends RefCounted

const Batch = preload("res://city/mesh_batch.gd")
const PALM_HEIGHT := 8.0
const TREE_HEIGHT := 6.8
const PALM_BARK := Color("#8b7c67")
const HARDWOOD_BARK := Color("#71634f")
const ANGLE_STEP := 2.399963


class Model:
	var label: String
	var detailed: ArrayMesh
	var distant: ArrayMesh
	var detail_distance: float


static func templates(bark: Material, leaves: Material, canopy: Material) -> Array[Model]:
	var all: Array[Model] = []
	for index in 4:
		var palm := index < 2
		var model := Model.new()
		model.label = ["Canary date palm", "Coastal date palm", "Broad-canopy oak", "Coastal evergreen"][index]
		model.detail_distance = 46.0 if palm else 30.0
		var wood := Batch.new()
		var distant_wood := Batch.new()
		var near := Batch.new()
		var far := Batch.new()
		if palm:
			_palm(index, wood, distant_wood, near, far)
		else:
			_hardwood(index - 2, wood, distant_wood, near, far)
		model.detailed = near.create_mesh(leaves)
		wood.create_mesh(bark, model.detailed)
		model.distant = far.create_mesh(leaves if palm else canopy)
		distant_wood.create_mesh(bark, model.distant)
		all.append(model)
	return all


static func _rng(seed_value: int) -> RandomNumberGenerator:
	var rng := RandomNumberGenerator.new()
	rng.seed = seed_value
	return rng


static func _palm(variant: int, wood: Batch, distant_wood: Batch, near: Batch, far: Batch) -> void:
	var rng := _rng(2401 + variant * 1709)
	var radius := 0.237 if variant == 0 else 0.211
	var lean := Vector3(0.19, 0, -0.30) if variant == 0 else Vector3(-0.61, 0, 0.44)
	var path := PackedVector3Array()
	var radii := PackedFloat32Array()
	var silhouette := PackedVector3Array()
	var silhouette_radii := PackedFloat32Array()
	for index in 25:
		var t := index / 24.0
		path.append(_palm_axis(t, lean))
		radii.append(_palm_radius(t, radius))
		if index % 3 == 0:
			silhouette.append(path[-1])
			silhouette_radii.append(radii[-1])
	_tube(wood, path, radii, 18, PALM_BARK, 1, variant + 1.5)
	_tube(distant_wood, silhouette, silhouette_radii, 10, PALM_BARK, 1, variant + 1.5)
	wood.beam(path[-1] - Vector3.UP * 0.12, path[-1] + Vector3.UP * 0.17, radius * 0.98,
		radius * 0.58, Color("#67543e"), 14, Vector2(2, 0))
	_leaf_bases(wood, lean, radius, rng)
	var rotation := rng.randf() * TAU
	for frond in 31:
		var tier := frond / 30.0
		var angle := rotation + ANGLE_STEP * frond + rng.randf_range(-0.17, 0.17)
		var direction := Vector3(cos(angle), 0, sin(angle))
		var length := lerpf(4.18, 2.20, tier) * rng.randf_range(0.83, 1.16)
		var lift := lerpf(0.82, 2.42, tier) * rng.randf_range(0.78, 1.16)
		var droop := lerpf(2.08, 0.11, tier)
		var base := path[-1] + direction * radius * 0.49 + Vector3.UP * lerpf(-0.39, 0.13, tier)
		var tint := Color("#556b32").lerp(Color("#8b9b49"), rng.randf_range(0.10, 0.68))
		if frond < 5:
			tint = tint.lerp(Color("#94804a"), rng.randf_range(0.20, 0.47))
		var curve := Vector4(length, lift, droop, rng.randf_range(-0.66, 0.72))
		_frond(near, base, direction, curve, angle, tint, 27, true, rng)
		if frond % 2 == 0:
			_frond(far, base, direction, curve, angle, tint, 12, false, rng)


static func _palm_axis(t: float, lean: Vector3) -> Vector3:
	return Vector3.UP * PALM_HEIGHT * t + lean * t * t + Vector3(0.12 * sin(t * PI * 1.45), 0, 0.07 * sin(t * TAU))


static func _palm_radius(t: float, radius: float) -> float:
	return radius * (1.07 - t * 0.34 + 0.31 * exp(-t * 24.0) + 0.27 * smoothstep(0.83, 0.98, t))


static func _leaf_bases(mesh: Batch, lean: Vector3, radius: float, rng: RandomNumberGenerator) -> void:
	for row in 33:
		var row_t := 0.041 + row * 0.0298
		var phase := float(row % 2) * 0.21 + rng.randf_range(-0.09, 0.09)
		for slot in 13:
			var t := row_t + rng.randf_range(-0.0066, 0.0066)
			var center := _palm_axis(t, lean)
			var r := _palm_radius(t, radius)
			var upper := smoothstep(0.73, 0.99, t)
			var angle := TAU * slot / 13.0 + phase + rng.randf_range(-0.085, 0.085)
			var n := Vector3(cos(angle), 0, sin(angle))
			var side := Vector3.UP.cross(n)
			var extent := rng.randf_range(0.063, 0.115) + upper * 0.023
			var width := r * rng.randf_range(0.19, 0.276)
			var scale := rng.randf_range(0.86, 1.12)
			var ridge := center + n * (r + lerpf(0.024, 0.063, upper) * scale) + Vector3.UP * extent * 0.26
			var heel := center + n * (r - 0.003) - Vector3.UP * extent
			var lip := center + n * (r + 0.011) + Vector3.UP * extent * 0.70
			var left := center + n * (r - 0.003) - side * width + Vector3.UP * extent * 0.16
			var right := center + n * (r - 0.003) + side * width
			var tint := PALM_BARK.lerp(Color("#65533e"), 0.04 + upper * 0.32 + rng.randf_range(0, 0.16))
			mesh.triangle(heel, left, ridge, n, tint.darkened(0.057), Vector2(2, 0))
			mesh.triangle(heel, ridge, right, n, tint.darkened(0.044), Vector2(2, 0))
			mesh.triangle(left, lip, ridge, n, tint, Vector2(2, 0))
			mesh.triangle(ridge, lip, right, n, tint.lightened(0.065), Vector2(2, 0))


static func _frond(mesh: Batch, base: Vector3, direction: Vector3, curve: Vector4, phase: float,
		tint: Color, stations: int, detailed: bool, rng: RandomNumberGenerator) -> void:
	var side := direction.cross(Vector3.UP)
	var last_left := -1
	var last_right := -1
	for station in stations + 1:
		var t := float(station) / stations
		var spine := base + direction * curve.x * t + side * curve.w * t * t
		spine.y += sin(t * PI * 0.78) * curve.y - t * t * curve.z
		var stem := lerpf(0.030, 0.0025, t)
		var flex := Vector2(t, phase)
		var stem_tint := tint.lerp(Color("#a29361"), 0.36)
		var left := mesh.add_vertex(spine - side * stem, Vector3.UP, stem_tint, Vector2(0.45, 0), flex)
		var right := mesh.add_vertex(spine + side * stem, Vector3.UP, stem_tint, Vector2(0.55, 0), flex)
		if station > 0:
			mesh.face(last_left, last_right, right)
			mesh.face(last_left, right, left)
		last_left = left
		last_right = right
		if station < 2 or station == stations:
			continue
		for sign_value: float in [-1.0, 1.0]:
			var width := curve.x * pow(sin(t * PI), 0.82) * rng.randf_range(0.158, 0.221)
			var root := spine + side * stem * sign_value * 0.54
			var tip := root + side * sign_value * width + direction * width * rng.randf_range(0.37, 0.68)
			tip.y -= width * lerpf(0.40, 0.66, t) * rng.randf_range(0.78, 1.17)
			var normal := (Vector3.UP + side * sign_value * 0.36).normalized()
			var blade_width := curve.x * rng.randf_range(0.009, 0.0145)
			var leaf_tint := tint.lerp(Color("#9aa35c"), rng.randf_range(0.04, 0.34))
			_blade(mesh, root, tip, normal, blade_width * (1.0 if detailed else 1.65),
				width * 0.087, leaf_tint, flex, detailed)


static func _hardwood(variant: int, wood: Batch, distant_wood: Batch, near: Batch, far: Batch) -> void:
	var rng := _rng(17891 + variant * 661)
	var bark := HARDWOOD_BARK.lerp(Color("#878176"), variant * 0.44)
	var green := Color("#526b38").lerp(Color("#637f46"), variant * 0.75)
	var h := TREE_HEIGHT
	var trunk := PackedVector3Array([
		Vector3(0, -0.09, 0), Vector3(0.025, 0.43, 0.04), Vector3(0.075, h * 0.29, -0.035),
		Vector3(-0.11, h * 0.54, 0.13), Vector3(0.17, h * 0.72, 0.24), Vector3(0.38, h * 0.90, 0.08),
		Vector3(0.49, h * 0.99, 0.12),
	])
	var trunk_radii := PackedFloat32Array([0.305, 0.238, 0.188, 0.139, 0.091, 0.042, 0.013])
	_tube(wood, trunk, trunk_radii, 16, bark, 0, 2.4)
	_tube(distant_wood, trunk, trunk_radii, 10, bark, 0, 2.4, false)
	for root in 5:
		var angle := root * ANGLE_STEP + rng.randf_range(-0.35, 0.35)
		var axis := Vector3(cos(angle), 0, sin(angle))
		var run := rng.randf_range(0.23, 0.35)
		_tube(wood, PackedVector3Array([Vector3.UP * 0.35 + axis * 0.075,
			axis * run * 0.75 + Vector3.UP * 0.04, axis * run - Vector3.UP * 0.04]),
			PackedFloat32Array([0.10, 0.068, 0.022]), 7, bark, 0, root + 0.2)

	var rotation := rng.randf() * TAU
	for branch in 9:
		var t := branch / 8.0
		var angle := rotation + branch * ANGLE_STEP + rng.randf_range(-0.26, 0.26)
		var direction := Vector3(cos(angle), 0, sin(angle))
		var side := Vector3.UP.cross(direction)
		var start := _curve_point(trunk, 2.02 + t * 2.36)
		var reach := lerpf(3.10, 1.69, t) * lerpf(1.0, 0.91, variant) * rng.randf_range(0.89, 1.19)
		var altitude := lerpf(1.42, 2.43, t) * rng.randf_range(0.88, 1.1)
		var end := start + direction * reach + Vector3.UP * altitude
		var elbow := start.lerp(end, 0.43) - Vector3.UP * 0.27 + side * rng.randf_range(-0.23, 0.20)
		var curve := PackedVector3Array([start, start.lerp(elbow, 0.52) + Vector3.UP * 0.14, elbow,
			elbow.lerp(end, 0.54), end])
		var size := lerpf(0.128, 0.053, t)
		var branch_radii := PackedFloat32Array([size, size * 0.74, size * 0.52, size * 0.31, 0.013])
		_tube(wood, curve, branch_radii, 9, bark, 0, angle)
		_tube(distant_wood, curve, branch_radii, 6, bark, 0, angle, false)
		for shoot in 9:
			var offset := float(shoot) / 8.0
			var anchor := _curve_point(curve, 2.0 + offset * 2.0)
			var yaw := angle + (1.0 if shoot % 2 == 0 else -1.0) * rng.randf_range(0.42, 1.46)
			var axis := Vector3(cos(yaw), rng.randf_range(-0.15, 0.77), sin(yaw)).normalized()
			var length := rng.randf_range(0.70, 1.29) * (1.13 - offset * 0.20)
			_spray(wood, near, far, anchor, axis, length, green, bark, rng)
	for shoot in 7:
		var angle := shoot * ANGLE_STEP
		var axis := Vector3(cos(angle), 0.61, sin(angle)).normalized()
		_spray(wood, near, far, trunk[-2].lerp(trunk[-1], shoot * 0.14), axis, 0.82, green, bark, rng)


static func _spray(wood: Batch, near: Batch, far: Batch, base: Vector3, axis: Vector3,
		length: float, green: Color, bark: Color, rng: RandomNumberGenerator) -> void:
	var side := axis.cross(Vector3.UP).normalized()
	var end := base + axis * length
	_tube(wood, PackedVector3Array([base, base.lerp(end, 0.53) + Vector3.UP * 0.055, end]),
		PackedFloat32Array([0.024, 0.013, 0.0045]), 5, bark, 0, base.x)
	for ply in 5:
		var fraction := (ply + 1.0) * 0.158
		for sign_value: float in [-1.0, 1.0]:
			var root := base.lerp(end, fraction) + Vector3.UP * sin(fraction * PI) * 0.051
			var reach := rng.randf_range(0.20, 0.48) * length
			var lateral := side * sign_value
			var twig := root + lateral * reach + axis * reach * 0.58 + Vector3.UP * rng.randf_range(-0.067, 0.16)
			wood.beam(root, twig, 0.0075, 0.0027, bark.lightened(0.065), 4, Vector2(3, 0))
			for leaf_index in 4:
				var t := (leaf_index + 1.0) * 0.22
				var start := root.lerp(twig, t)
				var facing := (lateral * rng.randf_range(0.42, 0.96) + axis * rng.randf_range(-0.6, 0.61)
					+ Vector3.UP * rng.randf_range(-0.63, 0.54)).normalized()
				var span := rng.randf_range(0.125, 0.272)
				var tip := start + facing * span
				var upward := (Vector3.UP + axis * rng.randf_range(-0.42, 0.51) + lateral * rng.randf_range(-0.56, 0.46)).normalized()
				var tint := green.lerp(Color("#a0aa66"), rng.randf_range(0.04, 0.40))
				_blade(near, start, tip, upward, span * rng.randf_range(0.196, 0.30), span * 0.155,
					tint, Vector2(0, rng.randf() * TAU), true)
	for layer in 4:
		var height := (layer - 1.5) * 0.22
		var normal := (Vector3.UP * height + side.rotated(Vector3.UP, layer * 1.66)).normalized()
		var right := Vector3.UP.cross(normal).normalized().rotated(normal, rng.randf_range(-0.58, 0.58))
		var up := normal.cross(right)
		var center := base.lerp(end, 0.48 + height * 0.39) + side * height * length
		var w := right * (0.26 + length * rng.randf_range(0.38, 0.66))
		var h := up * (0.26 + length * rng.randf_range(0.39, 0.66))
		far.quad(center - w - h, center + w - h, center + w + h, center - w + h, normal,
			green.lightened(rng.randf_range(0, 0.17)), Rect2(0, 0, 1, 1), Vector2(rng.randi_range(0, 3), rng.randf() * TAU))


static func _blade(mesh: Batch, base: Vector3, tip: Vector3, up_hint: Vector3, width: float,
		ridge: float, tint: Color, flex: Vector2, detailed: bool) -> void:
	var along := (tip - base).normalized()
	var across := along.cross(up_hint).normalized()
	var normal := across.cross(along).normalized()
	var root := mesh.add_vertex(base, normal, tint.darkened(0.16), Vector2(0.5, 0.0), flex)
	var previous_l := -1
	var previous_m := -1
	var previous_r := -1
	var stations := 2 if detailed else 1
	for station in stations:
		var t := 0.34 if station == 0 else 0.69
		var center := base.lerp(tip, t) + normal * ridge * (1.0 if station == 0 else 0.64)
		var span := width * (1.0 if station == 0 else 0.79)
		var edge := normal * ridge * 0.42
		var left_normal := (normal - across * 0.24).normalized()
		var right_normal := (normal + across * 0.24).normalized()
		var left := mesh.add_vertex(center - across * span - edge, left_normal, tint, Vector2(0.0, t), flex)
		var mid := mesh.add_vertex(center, normal, tint.lightened(0.035), Vector2(0.5, t), flex)
		var right := mesh.add_vertex(center + across * span - edge, right_normal, tint.darkened(0.04), Vector2(1.0, t), flex)
		if station == 0:
			mesh.face(root, left, mid)
			mesh.face(root, mid, right)
		else:
			mesh.face(previous_l, left, mid)
			mesh.face(previous_l, mid, previous_m)
			mesh.face(previous_m, mid, right)
			mesh.face(previous_m, right, previous_r)
		previous_l = left
		previous_m = mid
		previous_r = right
	var point := mesh.add_vertex(tip, normal, tint.lightened(0.045), Vector2(0.5, 1.0), flex)
	mesh.face(previous_l, point, previous_m)
	mesh.face(previous_m, point, previous_r)


static func _tube(mesh: Batch, path: PackedVector3Array, radii: PackedFloat32Array, sides: int,
		tint: Color, bark_kind: int, phase: float, refined := true) -> void:
	if refined and bark_kind == 0 and radii[0] >= 0.045:
		var smooth := PackedVector3Array()
		var thickness := PackedFloat32Array()
		for segment in path.size() - 1:
			var steps := clampi(ceili(path[segment].distance_to(path[segment + 1]) / 0.26), 2, 8)
			for step in steps:
				var t := float(step) / steps
				smooth.append(_curve_point(path, float(segment) + t))
				thickness.append(lerpf(radii[segment], radii[segment + 1], t))
		smooth.append(path[-1])
		thickness.append(radii[-1])
		path = smooth
		radii = thickness
	var distance := 0.0
	var material := Vector2(bark_kind, 0)
	var heading := (path[-1] - path[0]).normalized()
	var reference := Vector3.FORWARD if absf(heading.z) < 0.74 else Vector3.RIGHT
	for ring in path.size():
		var endpoint := path[ring]
		if ring > 0:
			distance += endpoint.distance_to(path[ring - 1])
		var tangent := (path[mini(ring + 1, path.size() - 1)] - path[maxi(ring - 1, 0)]).normalized()
		var right := tangent.cross(reference).normalized()
		var back := tangent.cross(right)
		var first := mesh.vertices.size()
		for side in sides + 1:
			var ratio := float(side) / sides
			var angle := ratio * TAU
			var unit := right * cos(angle) + back * sin(angle)
			var grain := sin(angle * 5 + phase) * 0.046 + sin(angle * 9 + phase - distance * 0.42) * 0.025
			var flare := sin(angle * 6 + phase) * maxf(0.0, 0.46 - distance) * (0.043 if bark_kind == 1 else 0.18)
			var radius := radii[ring] * (1.0 + grain) + maxf(0.0, flare)
			var color := tint.lerp(Color("#a0937a"), clampf(0.11 + grain * 1.67 + distance * 0.018, 0.03, 0.35))
			mesh.add_vertex(endpoint + unit * radius, unit, color, Vector2(ratio, distance), material)
		if ring == 0:
			continue
		for side in sides:
			var a := first - (sides + 1) + side
			var b := first + side
			mesh.face(a, a + 1, b + 1)
			mesh.face(a, b + 1, b)


static func _curve_point(path: PackedVector3Array, position: float) -> Vector3:
	var i := mini(int(position), path.size() - 2)
	return path[i].cubic_interpolate(path[i + 1], path[maxi(0, i - 1)], path[mini(path.size() - 1, i + 2)],
		position - i)
