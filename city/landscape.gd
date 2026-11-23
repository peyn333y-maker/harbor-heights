extends RefCounted

const Layout = preload("res://city/layout.gd")
const Batch = preload("res://city/mesh_batch.gd")
const WATER = preload("res://shaders/water.gdshader")


func build(parent: Node3D, layout: Layout, ground_material: Material, land_material: Material, interactive := false) -> void:
	var size := layout.bounds
	var coast := size.end.x + 42.0
	var plain := Batch.new()
	plain.floor_rect(Rect2(-7200, -7000, coast + 7168, 14000), -0.14, Color("#79734f"), Vector2(3, 0))
	_city_asphalt(plain, layout)
	_shore(plain, coast)
	var terrain := plain.attach(parent, "Terrain and asphalt", ground_material, false)
	if interactive:
		terrain.create_trimesh_collision()
	var promenade := Batch.new()
	_waterfront(promenade, coast, size, interactive)
	var walk := promenade.attach(parent, "Waterfront walk and piers", land_material)
	if interactive:
		walk.create_trimesh_collision()
	if not interactive:
		var water := Batch.new()
		water.floor_rect(Rect2(coast - 12.0, -7100.0, 7500.0, 14200.0), -0.37, Color.WHITE)
		var water_material := ShaderMaterial.new()
		water_material.shader = WATER
		water.attach(parent, "Harbor", water_material, false)

	var north := Batch.new()
	_hills(north, Rect2(-1210.0, size.position.y - 750.0, coast + 1190.0, 702.0),
		layout.seed_value + 1, 115.0)
	var north_mesh := north.attach(parent, "Northern foothills", land_material, true)
	if interactive:
		north_mesh.create_trimesh_collision()
	var west := Batch.new()
	var west_edge := -4100.0 if interactive else size.position.x - 615.0
	_hills(west, Rect2(west_edge, size.position.y - 455.0, 590.0, size.size.y + 1110.0),
		layout.seed_value + 2, 82.0)
	west.attach(parent, "Western foothills", land_material, true)


func _city_asphalt(mesh: Batch, layout: Layout) -> void:
	var perimeter := Layout.sample_rect(layout.plan_bounds, 8.0)
	for index in perimeter.size():
		perimeter[index] = layout.map_xy(perimeter[index])
	var triangles := Geometry2D.triangulate_polygon(perimeter)
	var tint := Color("#535654")
	for index in range(0, triangles.size(), 3):
		var a := perimeter[triangles[index]]
		var b := perimeter[triangles[index + 1]]
		var c := perimeter[triangles[index + 2]]
		mesh.triangle(Vector3(a.x, 0, a.y), Vector3(b.x, 0, b.y), Vector3(c.x, 0, c.y), Vector3.UP, tint)


func _shore(mesh: Batch, coast: float) -> void:
	for step in 280:
		var z := -7000.0 + step * 50.0
		var end := z + 50.0
		var rim := _shore_x(coast, z)
		var next := _shore_x(coast, end)
		mesh.quad(Vector3(coast - 32.0, -0.14, z), Vector3(rim, -0.26, z),
			Vector3(next, -0.26, end), Vector3(coast - 32.0, -0.14, end), Vector3.UP,
			Color("#e0c69a"), Rect2(0, 0, 1, 1), Vector2(6, 0))
		mesh.quad(Vector3(rim, -0.26, z), Vector3(rim + 5.0, -0.58, z),
			Vector3(next + 5.0, -0.58, end), Vector3(next, -0.26, end), Vector3.UP,
			Color("#a58d68"), Rect2(0, 0, 1, 1), Vector2(6, 0))
		mesh.quad(Vector3(rim + 5, -0.58, z), Vector3(rim + 32, -5.4, z),
			Vector3(next + 32, -5.4, end), Vector3(next + 5, -0.58, end), Vector3.UP,
			Color("#85876d"), Rect2(0, 0, 1, 1), Vector2(6, 0))
		mesh.quad(Vector3(rim + 32, -5.4, z), Vector3(rim + 210, -14, z),
			Vector3(next + 210, -14, end), Vector3(next + 32, -5.4, end), Vector3.UP,
			Color("#556956"), Rect2(0, 0, 1, 1), Vector2(6, 0))


func _shore_x(coast: float, z: float) -> float:
	return coast + sin(z * 0.0043 + 0.7) * 6.3 + sin(z * 0.0092) * 1.8


func _waterfront(mesh: Batch, coast: float, bounds: Rect2, interactive: bool) -> void:
	var from := bounds.position.y - 17.0
	var to := bounds.end.y + 17.0
	var walk := Rect2(coast - 33.0, from, 4.6, to - from)
	var wood := Color("#9a8369")
	var railing := Color("#665f54")
	mesh.floor_rect(walk, -0.078, Color("#b29e85"))
	var pier_z := Vector2(lerpf(from, to, 0.27), lerpf(from, to, 0.73))
	for section in [Vector2(from + 1.0, pier_z.x - 2.7), Vector2(pier_z.x + 2.7, pier_z.y - 2.7),
			Vector2(pier_z.y + 2.7, to - 1.0)]:
		var length: float = section.y - section.x
		var posts := ceili(length / 6.8)
		if interactive:
			continue
		for index in posts + 1:
			var point := Vector3(coast - 28.66, -0.075, lerpf(section.x, section.y, float(index) / posts))
			mesh.box(point + Vector3.UP * 0.42, Vector3(0.11, 0.84, 0.11), railing)
		mesh.box(Vector3(coast - 28.66, 0.70, (section.x + section.y) * 0.5), Vector3(0.092, 0.09, length), railing)
	for slot in 2:
		var z := pier_z[slot]
		var span := 49.0 + slot * 15.0
		mesh.box(Vector3(coast - 17.8 + span * 0.5, 0.19, z), Vector3(span, 0.23, 5.4), wood, true)
		for pair in floori(span / 5.4):
			for side in [-1.0, 1.0]:
				var position := Vector3(coast - 16.0 + pair * 5.4, -0.92, z + side * 2.04)
				mesh.beam(position, position + Vector3.UP * 2.65, 0.21, 0.18, railing, 7)
		mesh.box(Vector3(coast - 14.8 + span, 0.19, z), Vector3(6.0, 0.23, 9.1), wood, true)


func _hills(mesh: Batch, area: Rect2, world_seed: int, height: float) -> void:
	const COLUMNS := 60
	const ROWS := 34
	var noise := FastNoiseLite.new()
	noise.seed = world_seed
	noise.noise_type = FastNoiseLite.TYPE_SIMPLEX_SMOOTH
	noise.frequency = 0.0033
	noise.fractal_octaves = 3
	noise.fractal_gain = 0.32
	var nodes := PackedVector3Array()
	var normals := PackedVector3Array()
	var colors := PackedColorArray()
	for z in ROWS + 1:
		for x in COLUMNS + 1:
			var uv := Vector2(float(x) / COLUMNS, float(z) / ROWS)
			var point := area.position + area.size * uv
			var edge := minf(minf(uv.x, 1.0 - uv.x), minf(uv.y, 1.0 - uv.y))
			var sample := noise.get_noise_2dv(point)
			var peak := pow(clampf(0.60 + sample * 0.71, 0.0, 1.0), 1.4)
			var elevation := peak * height * smoothstep(0.035, 0.34, edge) * 1.45 - 0.18
			nodes.append(Vector3(point.x, elevation, point.y))
			colors.append(Color("#727656").lerp(Color("#a09875"), clampf(sample * 0.9 + 0.30, 0, 1)))
	var stride := COLUMNS + 1
	for z in ROWS + 1:
		for x in COLUMNS + 1:
			var horizontal := nodes[z * stride + mini(x + 1, COLUMNS)] - nodes[z * stride + maxi(x - 1, 0)]
			var vertical := nodes[mini(z + 1, ROWS) * stride + x] - nodes[maxi(z - 1, 0) * stride + x]
			normals.append(vertical.cross(horizontal).normalized())
	for z in ROWS:
		for x in COLUMNS:
			var a := x + z * stride
			var b := a + 1
			var c := a + stride
			var d := c + 1
			mesh.smooth_triangle(nodes[a], nodes[b], nodes[d], normals[a], normals[b], normals[d], colors[a], colors[b], colors[d])
			mesh.smooth_triangle(nodes[a], nodes[d], nodes[c], normals[a], normals[d], normals[c], colors[a], colors[d], colors[c])

