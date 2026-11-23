extends RefCounted

const Batch = preload("res://city/mesh_batch.gd")
const GALVANIZED := Color("#868986")
const IRON := Color("#3e4a43")
const BLACK := Color("#2d302c")
const OCHRE := Color("#b89042")
const PAINT := Vector2(2, 0)


static func signal_templates(material: Material) -> Array[ArrayMesh]:
	var templates: Array[ArrayMesh] = []
	for state in [0, 2]:
		var mesh := Batch.new()
		_signal_mast(mesh, Vector3.ZERO, Vector3.LEFT * 5.1, Vector3.BACK, state)
		templates.append(mesh.create_mesh(material))
	return templates


static func attach_signals(parent: Node3D, templates: Array[ArrayMesh], placements: Array[Array]) -> void:
	for state in placements.size():
		var poses := placements[state]
		if poses.is_empty():
			continue
		var batch := MultiMesh.new()
		batch.transform_format = MultiMesh.TRANSFORM_3D
		batch.mesh = templates[state]
		batch.instance_count = poses.size()
		for index in poses.size():
			batch.set_instance_transform(index, poses[index])
		var instance := MultiMeshInstance3D.new()
		instance.name = "Red traffic signals" if state == 0 else "Green traffic signals"
		instance.multimesh = batch
		instance.visibility_range_end = 360.0
		instance.visibility_range_end_margin = 22.0
		parent.add_child(instance)


static func _signal_mast(mesh: Batch, point: Vector3, arm: Vector3, front: Vector3, state: int) -> void:
	var high := point + Vector3.UP * 6.8
	mesh.box(point + Vector3.UP * 0.034, Vector3(0.48, 0.068, 0.48), GALVANIZED)
	mesh.beam(point + Vector3.UP * 0.066, point + Vector3.UP * 0.36, 0.185, 0.156, GALVANIZED, 10)
	mesh.beam(point + Vector3.UP * 0.355, high, 0.143, 0.085, GALVANIZED, 10)
	_anchor_bolts(mesh, point + Vector3.UP * 0.067, 0.183, 0.024)
	var arm_root := high - Vector3.UP * 0.26
	var wrist := arm_root + arm * 0.14 + Vector3.UP * 0.074
	var end := high + arm
	mesh.beam(arm_root, wrist, 0.121, 0.098, GALVANIZED, 9)
	mesh.beam(wrist, end, 0.097, 0.060, GALVANIZED, 9)
	mesh.beam(high - Vector3.UP * 0.78, wrist, 0.041, 0.031, GALVANIZED, 6)
	mesh.beam(end - arm.normalized() * 0.13, end + arm.normalized() * 0.012, 0.076, 0.070, GALVANIZED.darkened(0.17), 8)
	var plate := Transform3D(Basis(Vector3.UP.cross(front), Vector3.UP, front),
		point + Vector3.UP * 1.21 + front * 0.137)
	_case(mesh, plate, Vector3(0.123, 0.208, 0.022), 0.013, GALVANIZED.darkened(0.13))
	mesh.beam(plate.origin + front * 0.015, plate.origin + front * 0.025, 0.033, 0.027, BLACK, 9)
	mesh.beam(end, end - Vector3.UP * 0.22, 0.053, 0.045, GALVANIZED, 8)
	_signal_head(mesh, end - Vector3.UP * 0.75, front, state)


static func _signal_head(mesh: Batch, center: Vector3, front: Vector3, state: int) -> void:
	var right := Vector3.UP.cross(front)
	var pose := Transform3D(Basis(right, Vector3.UP, front), center)
	_case(mesh, pose.translated(-front * 0.087), Vector3(0.49, 1.135, 0.052), 0.047, BLACK)
	_case(mesh, pose, Vector3(0.329, 1.034, 0.226), 0.028, OCHRE, PAINT)
	for index in 3:
		var base := center + Vector3.UP * (0.321 - index * 0.321)
		_case(mesh, Transform3D(pose.basis, base + front * 0.119),
			Vector3(0.292, 0.295, 0.022), 0.023, BLACK)
		var bezel := base + front * 0.122
		_profile(mesh, bezel, front, PackedVector2Array([
			Vector2(0.108, 0), Vector2(0.116, 0.015), Vector2(0.108, 0.032), Vector2(0.093, 0.032),
			Vector2(0.093, 0.019),
		]), BLACK.lightened(0.08), 12)
		_visor(mesh, bezel, pose.basis)
		var tint: Color = [Color("#ed442c"), Color("#ffb33e"), Color("#49e48d")][index]
		var lens := Vector2(3, 1.0 if index == state else 0.0)
		_profile(mesh, bezel, front, PackedVector2Array([
			Vector2(0.092, 0.021), Vector2(0.092, 0.029), Vector2(0.064, 0.042), Vector2(0, 0.047),
		]), tint, 14, lens)
		for side in [-1.0, 1.0]:
			var bolt: Vector3 = base + right * side * 0.126 + Vector3.UP * 0.118 + front * 0.131
			mesh.beam(bolt, bolt + front * 0.009, 0.008, 0.007, GALVANIZED.darkened(0.30), 5)


static func _visor(mesh: Batch, root: Vector3, frame: Basis) -> void:
	const STEPS := 9
	var a := Vector3.ZERO
	var b := Vector3.ZERO
	var c := Vector3.ZERO
	var d := Vector3.ZERO
	for index in STEPS + 1:
		var angle := lerpf(-0.38, PI + 0.38, float(index) / STEPS)
		var radial := frame.x * cos(angle) + frame.y * sin(angle)
		var projection := 0.084 + 0.110 * maxf(0.0, sin(angle))
		var back_outer := root + radial * 0.121 + frame.z * 0.018
		var tip_outer := root + radial * 0.116 + frame.z * projection
		var tip_inner := root + radial * 0.109 + frame.z * (projection - 0.001)
		var back_inner := root + radial * 0.111 + frame.z * 0.018
		if index > 0:
			mesh.quad(a, back_outer, tip_outer, b, radial, BLACK)
			mesh.quad(d, c, tip_inner, back_inner, -radial, BLACK.darkened(0.28))
			mesh.quad(b, tip_outer, tip_inner, c, frame.z, BLACK.lightened(0.09))
		if index == 0 or index == STEPS:
			var rim_normal := radial.cross(frame.z).normalized() * (1.0 if index == 0 else -1.0)
			mesh.quad(back_outer, tip_outer, tip_inner, back_inner, rim_normal, BLACK.lightened(0.04))
		a = back_outer
		b = tip_outer
		c = tip_inner
		d = back_inner


static func bench(mesh: Batch, point: Vector3, front: Vector3, timber: Color) -> void:
	var right := Vector3.UP.cross(front)
	var pose := Transform3D(Basis(right, Vector3.UP, front), point)
	var grain_axis := right
	if mesh.warp.is_valid():
		var opposite: Vector3 = mesh.warp.call(point - right)
		var along: Vector3 = mesh.warp.call(point + right)
		grain_axis = (along - opposite).normalized()
	var wood := Vector2(1.0, atan2(grain_axis.z, grain_axis.x))
	for end in [-0.70, 0.70]:
		var side: Vector3 = right * end
		var foot := point + side
		_case(mesh, Transform3D(pose.basis, foot + Vector3.UP * 0.021),
			Vector3(0.15, 0.043, 0.52), 0.025, IRON)
		for offset in [-0.21, 0.21]:
			var bolt := foot + front * float(offset) + Vector3.UP * 0.043
			mesh.beam(bolt, bolt + Vector3.UP * 0.014, 0.018, 0.016, GALVANIZED, 6)
		var rear := foot - front * 0.19 + Vector3.UP * 0.042
		var front_foot := foot + front * 0.195 + Vector3.UP * 0.042
		var rail := foot + Vector3.UP * 0.414
		mesh.beam(front_foot, rail + front * 0.086, 0.033, 0.045, IRON, 8)
		mesh.beam(rear, rail - front * 0.13, 0.037, 0.050, IRON, 8)
		mesh.beam(rail - front * 0.22, rail + front * 0.24, 0.036, 0.034, IRON, 8)
		var back_bottom := rail - front * 0.22
		var back_top := foot + Vector3.UP * 0.891 - front * 0.346
		mesh.beam(back_bottom, back_top, 0.034, 0.027, IRON, 8)
		var elbow := foot + Vector3.UP * 0.644 + front * 0.147
		var shoulder := foot + Vector3.UP * 0.670 - front * 0.258
		var joint := elbow.lerp(shoulder, 0.33) + Vector3.UP * 0.029
		mesh.beam(rail + front * 0.205, elbow + Vector3.UP * 0.014, 0.026, 0.025, IRON, 8)
		mesh.beam(elbow - Vector3.UP * 0.012, joint - front * 0.015, 0.025, 0.024, IRON, 8)
		mesh.beam(joint + front * 0.015, shoulder, 0.024, 0.023, IRON, 8)
	for row in 5:
		var z := (row - 2.0) * 0.098
		var plank := point + Vector3.UP * (0.466 - z * 0.073) + front * z
		_case(mesh, Transform3D(pose.basis, plank), Vector3(1.93, 0.043, 0.085),
			0.015, timber.lightened(row * 0.023), wood)
		for end in [-0.70, 0.70]:
			var screw := plank + right * float(end) + Vector3.UP * 0.021
			mesh.beam(screw, screw + Vector3.UP * 0.004, 0.010, 0.009, BLACK, 6)
	var lean := Basis(right, -0.23) * pose.basis
	for row in 4:
		var h := 0.57 + row * 0.093
		var plank := point + Vector3.UP * h - front * (0.211 + row * 0.022)
		_case(mesh, Transform3D(lean, plank), Vector3(1.93, 0.076, 0.038),
			0.013, timber.lightened((3 - row) * 0.020), wood)
		for end in [-0.70, 0.70]:
			var screw := plank + right * float(end) + lean.z * 0.019
			mesh.beam(screw, screw + lean.z * 0.004, 0.010, 0.009, BLACK, 6)


static func hydrant(mesh: Batch, point: Vector3, front: Vector3) -> void:
	var red := Color("#a34834")
	_profile(mesh, point, Vector3.UP, PackedVector2Array([
		Vector2(0.235, 0), Vector2(0.235, 0.046), Vector2(0.203, 0.067), Vector2(0.201, 0.102),
		Vector2(0.147, 0.112), Vector2(0.145, 0.585), Vector2(0.183, 0.607), Vector2(0.188, 0.655),
		Vector2(0.137, 0.704), Vector2(0.078, 0.732), Vector2(0, 0.735),
	]), red, 12, PAINT)
	_anchor_bolts(mesh, point + Vector3.UP * 0.077, 0.140, 0.017)
	var side := front.cross(Vector3.UP)
	for direction in [side, -side, front]:
		var nozzle := point + Vector3.UP * 0.422
		var large: bool = direction == front
		var ring := 0.110 if large else 0.084
		_profile(mesh, nozzle, direction, PackedVector2Array([
			Vector2(ring * 0.92, 0.12), Vector2(ring * 0.92, 0.216), Vector2(ring * 1.11, 0.221),
			Vector2(ring * 1.11, 0.255), Vector2(ring * 0.78, 0.271), Vector2(0, 0.271),
		]), red.lightened(0.08), 10, PAINT)
		mesh.beam(nozzle + direction * 0.27, nozzle + direction * 0.291, ring * 0.37, ring * 0.37,
			red.darkened(0.15), 6, PAINT)
	mesh.beam(point + Vector3.UP * 0.735, point + Vector3.UP * 0.776, 0.052, 0.047, red.darkened(0.18), 5, PAINT)


static func litter_bin(mesh: Batch, point: Vector3, tint: Color) -> void:
	_profile(mesh, point, Vector3.UP, PackedVector2Array([
		Vector2(0.253, 0.0), Vector2(0.253, 0.059), Vector2(0.282, 0.095), Vector2(0.307, 0.70),
		Vector2(0.319, 0.722), Vector2(0.319, 0.788), Vector2(0.285, 0.812), Vector2(0.205, 0.807),
		Vector2(0.190, 0.775), Vector2(0.190, 0.697), Vector2(0, 0.697),
	]), tint, 16, PAINT)
	for slat in 16:
		var direction := Vector3(cos(TAU * slat / 16), 0, sin(TAU * slat / 16))
		var low := point + direction * 0.281 + Vector3.UP * 0.132
		var high := point + direction * 0.305 + Vector3.UP * 0.689
		mesh.beam(low, high, 0.016, 0.017, tint.lightened(0.068), 5, PAINT)


static func tree_grate(mesh: Batch, point: Vector3) -> void:
	var iron := Color("#55544a")
	_profile(mesh, point, Vector3.UP, PackedVector2Array([
		Vector2(0.73, 0.012), Vector2(0.73, 0.041), Vector2(0.69, 0.052), Vector2(0.673, 0.041),
		Vector2(0.673, 0.014),
	]), iron, 22)
	_profile(mesh, point, Vector3.UP, PackedVector2Array([
		Vector2(0.419, 0.015), Vector2(0.419, 0.042), Vector2(0.384, 0.051), Vector2(0.376, 0.04),
		Vector2(0.376, 0.014),
	]), iron, 16)
	for spoke in 16:
		var direction := Vector3(cos(TAU * spoke / 16), 0, sin(TAU * spoke / 16))
		var y := Vector3.UP * 0.039
		mesh.beam(point + y + direction * 0.405, point + y + direction * 0.689, 0.009, 0.014, iron, 4)


static func lamp(mesh: Batch, emissive: Batch, point: Vector3, front: Vector3) -> void:
	var base := point + Vector3.UP * 0.047
	var high := point + Vector3.UP * 7.35
	var lamp_color := Color("#737a74")
	mesh.box(point + Vector3.UP * 0.026, Vector3(0.40, 0.052, 0.40), lamp_color)
	_anchor_bolts(mesh, point + Vector3.UP * 0.051, 0.151, 0.019)
	mesh.beam(base, base + Vector3.UP * 0.38, 0.161, 0.132, lamp_color, 10, PAINT)
	mesh.beam(base + Vector3.UP * 0.38, high, 0.114, 0.052, lamp_color, 10, PAINT)
	var pose := Transform3D(Basis(Vector3.UP.cross(front), Vector3.UP, front),
		point + Vector3.UP * 0.88 + front * 0.113)
	_case(mesh, pose, Vector3(0.094, 0.24, 0.028), 0.012, lamp_color.darkened(0.13), PAINT)
	var bend := high + front * 0.38 + Vector3.UP * 0.55
	var end := bend + front * 1.73 + Vector3.UP * 0.10
	mesh.beam(high, bend, 0.056, 0.047, lamp_color, 8)
	mesh.beam(bend, end, 0.047, 0.039, lamp_color, 8)
	var head := Transform3D(pose.basis, end + front * 0.19)
	_case(mesh, head, Vector3(0.264, 0.148, 0.635), 0.063, lamp_color.darkened(0.13), PAINT)
	var diffuser := Transform3D(pose.basis, head.origin - Vector3.UP * 0.084)
	_case(emissive, diffuser, Vector3(0.19, 0.024, 0.44), 0.039, Color("#ded6b8"))


static func _anchor_bolts(mesh: Batch, point: Vector3, spread: float, radius: float) -> void:
	for x in [-spread, spread]:
		for z in [-spread, spread]:
			var bolt := point + Vector3(x, 0.0, z)
			mesh.beam(bolt, bolt + Vector3.UP * 0.025, radius, radius, GALVANIZED.darkened(0.18), 6)


static func _case(mesh: Batch, pose: Transform3D, size: Vector3, chamfer: float,
		tint: Color, data := Vector2.ZERO) -> void:
	var x := size.x * 0.5
	var y := size.y * 0.5
	var z := size.z * 0.5
	var edge := minf(chamfer, minf(x, y) * 0.83)
	var points := PackedVector2Array([
		Vector2(-x + edge, -y), Vector2(x - edge, -y), Vector2(x, -y + edge),
		Vector2(x, y - edge), Vector2(x - edge, y), Vector2(-x + edge, y),
		Vector2(-x, y - edge), Vector2(-x, -y + edge),
	])
	var back := PackedVector3Array()
	var front := PackedVector3Array()
	for p in points:
		back.append(pose * Vector3(p.x, p.y, -z))
		front.append(pose * Vector3(p.x, p.y, z))
	for side in points.size():
		var next := (side + 1) % points.size()
		var normal := (back[next] - back[side]).cross(pose.basis.z).normalized()
		mesh.quad(back[side], back[next], front[next], front[side], normal, tint,
			Rect2(0, 0, 1, 1), data)
	for index in range(1, points.size() - 1):
		mesh.triangle(front[0], front[index], front[index + 1], pose.basis.z, tint, data)
		mesh.triangle(back[0], back[index], back[index + 1], -pose.basis.z, tint.darkened(0.04), data)


static func _profile(mesh: Batch, center: Vector3, axis: Vector3, profile: PackedVector2Array,
		tint: Color, sides: int, data := Vector2.ZERO) -> void:
	var u := axis.cross(Vector3.RIGHT if absf(axis.y) > 0.9 else Vector3.UP).normalized()
	var v := axis.cross(u)
	for side in sides:
		var angle_a := TAU * side / sides
		var angle_b := TAU * (side + 1) / sides
		var u0 := u * cos(angle_a) + v * sin(angle_a)
		var u1 := u * cos(angle_b) + v * sin(angle_b)
		var radial := (u0 + u1).normalized()
		for section in profile.size() - 1:
			var a := profile[section]
			var b := profile[section + 1]
			var delta := b - a
			var normal := (radial * delta.y - axis * delta.x).normalized()
			var p := center + axis * a.y
			var q := center + axis * b.y
			if is_zero_approx(b.x):
				mesh.triangle(p + u0 * a.x, p + u1 * a.x, q, normal, tint, data)
			elif is_zero_approx(a.x):
				mesh.triangle(p, q + u1 * b.x, q + u0 * b.x, normal, tint, data)
			else:
				mesh.quad(p + u0 * a.x, p + u1 * a.x, q + u1 * b.x, q + u0 * b.x, normal, tint,
					Rect2(float(side) / sides, a.y, 1.0 / sides, delta.y), data)
