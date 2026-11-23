extends Node3D

const Layout = preload("res://city/layout.gd")
const Batch = preload("res://city/mesh_batch.gd")
const FacadeGeometry = preload("res://city/facade_geometry.gd")
const Streets = preload("res://city/streets.gd")
const Buildings = preload("res://city/buildings.gd")
const StreetDetail = preload("res://city/street_detail.gd")
const StreetModels = preload("res://city/street_models.gd")
const LeafAtlas = preload("res://city/leaf_atlas.gd")
const TreeModels = preload("res://city/tree_models.gd")
const Trees = preload("res://city/trees.gd")
const Landscape = preload("res://city/landscape.gd")
const FlyCamera = preload("res://city/fly_camera.gd")

signal city_built

@export var interactive := false
@export var city_seed := 246733

var fly_camera: FlyCamera
var previous_eye := Vector3.INF
var layout: Layout
var streets: Streets
var buildings: Buildings
var street_detail: StreetDetail
var ground_material: ShaderMaterial
var facade_material: ShaderMaterial
var facade_templates: Array[ArrayMesh]
var facade_lods: Array[FacadeGeometry.Lod] = []
var canopy_material: ShaderMaterial
var tree_templates: Array[TreeModels.Model]
var tree_groups: Array[Trees.Group] = []
var vegetation_eye := Vector3(1e10, 1e10, 1e10)
var street_material: ShaderMaterial
var signal_templates: Array[ArrayMesh]
var solid_material: StandardMaterial3D
var lamp_material: StandardMaterial3D
var blocks: Node3D
var hint: Label
var building_count := 0
var build_complete := false


func _ready() -> void:
	var start := Time.get_ticks_msec()
	layout = Layout.new(city_seed)
	streets = Streets.new(layout)
	buildings = Buildings.new(layout)
	street_detail = StreetDetail.new(layout)
	ground_material = _shader(preload("res://shaders/ground.gdshader"))
	ground_material.set_shader_parameter("beach_coast", game_coast())
	ground_material.set_shader_parameter("beach_albedo", preload("res://assets/sand/aerial_beach_01_diff.jpg"))
	ground_material.set_shader_parameter("beach_normal", preload("res://assets/sand/aerial_beach_01_nor_gl.jpg"))
	facade_material = _shader(preload("res://shaders/facade.gdshader"))
	FacadeGeometry.configure_distant_material(facade_material)
	facade_templates = FacadeGeometry.create_templates(_shader(preload("res://shaders/facade_geometry.gdshader")))
	canopy_material = _shader(preload("res://shaders/canopy.gdshader"))
	canopy_material.set_shader_parameter("leaf_cluster", LeafAtlas.create())
	var bark := _shader(preload("res://shaders/tree_bark.gdshader"))
	# CC0 scans from Poly Haven — Bark Brown 01 and Palm Tree Bark.
	bark.set_shader_parameter("oak_color", preload("res://assets/vegetation/bark_brown_01-diffuse.jpg"))
	bark.set_shader_parameter("oak_normal", preload("res://assets/vegetation/bark_brown_01-nor_gl.jpg"))
	bark.set_shader_parameter("palm_color", preload("res://assets/vegetation/palm_tree_bark-diffuse.jpg"))
	bark.set_shader_parameter("palm_normal", preload("res://assets/vegetation/palm_tree_bark-nor_gl.jpg"))
	tree_templates = TreeModels.templates(bark, _shader(preload("res://shaders/leaves.gdshader")), canopy_material)
	street_material = _shader(preload("res://shaders/street_models.gdshader"))
	signal_templates = StreetModels.signal_templates(street_material)
	solid_material = StandardMaterial3D.new()
	solid_material.vertex_color_use_as_albedo = true
	solid_material.vertex_color_is_srgb = true
	solid_material.roughness = 0.76
	lamp_material = solid_material.duplicate()
	lamp_material.shading_mode = BaseMaterial3D.SHADING_MODE_UNSHADED
	_light()
	Landscape.new().build(self, layout, ground_material, solid_material, interactive)
	if not interactive:
		fly_camera = FlyCamera.new()
		fly_camera.name = "FlyCamera"
		add_child(fly_camera)
		fly_camera.setup(layout)
		_hint()
	blocks = Node3D.new()
	blocks.name = "Districts"
	add_child(blocks)
	var next := _chunk_order()
	for index in mini(3, next.size()):
		_build_chunk(next.pop_front())
	print("First districts generated in %d ms" % (Time.get_ticks_msec() - start))
	while not next.is_empty():
		await get_tree().process_frame
		_build_chunk(next.pop_front())
	build_complete = true
	print("Generated %d blocks and %d buildings in %d ms" % [
		Layout.BLOCKS * Layout.BLOCKS, building_count, Time.get_ticks_msec() - start])
	city_built.emit()


func _process(_delta: float) -> void:
	var camera := get_viewport().get_camera_3d()
	if camera == null:
		return
	var eye := camera.global_position
	if eye.distance_squared_to(previous_eye) < 1.0:
		return
	previous_eye = eye
	for facade in facade_lods:
		facade.update(eye)
	if eye.distance_squared_to(vegetation_eye) >= 4.0:
		vegetation_eye = eye
		for group in tree_groups:
			group.update(eye)


func _chunk_order() -> Array[Vector2i]:
	var cells: Array[Vector2i] = []
	for j in range(0, Layout.BLOCKS, Layout.CHUNK_SIZE):
		for i in range(0, Layout.BLOCKS, Layout.CHUNK_SIZE):
			cells.append(Vector2i(i, j))
	cells.sort_custom(func(a: Vector2i, b: Vector2i) -> bool:
		return _priority(a) < _priority(b))
	return cells


func _priority(cell: Vector2i) -> float:
	var offset := Vector2(cell) - Vector2(4, 4)
	return absf(offset.x) * 2.35 + absf(offset.y) + maxf(offset.y, 0.0) * 0.8


func _build_chunk(cell: Vector2i) -> void:
	var center := layout.map_point(Vector3(layout.x[cell.x + 1], 0, layout.z[cell.y + 1]))
	var chunk := Node3D.new()
	chunk.name = "Blocks_%d_%d" % [cell.x, cell.y]
	chunk.position = center
	blocks.add_child(chunk)
	var signs := Node3D.new()
	signs.name = "Road signs"
	signs.position = center
	var ground := Batch.new(center)
	var paint := Batch.new(center)
	var furniture := Batch.new(center)
	var shrubs := Batch.new(center)
	var trees := Trees.new(center)
	var lamps := Batch.new(center)
	var signal_poses: Array[Array] = [[], []]
	for mesh in [ground, paint, furniture, shrubs, lamps]:
		mesh.warp = layout.map_point
	for j in range(cell.y, cell.y + Layout.CHUNK_SIZE):
		for i in range(cell.x, cell.x + Layout.CHUNK_SIZE):
			streets.build_cell(i, j, ground, paint, furniture, signs, signal_poses)
			street_detail.build_cell(i, j, ground, paint, furniture, trees, shrubs, lamps)
			if layout.park(i, j):
				street_detail.park(i, j, ground, furniture, trees)
			else:
				building_count += buildings.build_cell(i, j, ground, _building_lot.bind(chunk))
	signs.position = Vector3.ZERO
	chunk.add_child(signs)
	var sidewalks := ground.attach(chunk, "Raised curbs and sidewalks", ground_material, false)
	if interactive and sidewalks != null:
		sidewalks.create_trimesh_collision()
	paint.attach(chunk, "Road markings", ground_material, false, 350.0)
	furniture.attach(chunk, "Street furniture and trunks", street_material)
	StreetModels.attach_signals(chunk, signal_templates, signal_poses)
	shrubs.attach(chunk, "Shrub foliage", canopy_material)
	tree_groups.append_array(trees.attach(chunk, tree_templates, _eye()))
	lamps.attach(chunk, "Signal lights", lamp_material, false, 270.0)


func _eye() -> Vector3:
	var camera := get_viewport().get_camera_3d()
	return camera.global_position if camera != null else layout.map_point(Vector3(layout.x[-1], 2, 0))


func _building_lot(point: Vector3, make_building: Callable, chunk: Node3D) -> void:
	var lot := Node3D.new()
	lot.position = point - chunk.position
	chunk.add_child(lot)
	var solids := StaticBody3D.new()
	solids.collision_mask = 0
	lot.add_child(solids)
	var signs := Node3D.new()
	signs.position = point
	lot.add_child(signs)
	var detailed := absf(point.z) < 105.0 or point.x > layout.bounds.end.x - 190.0
	var walls := FacadeGeometry.new(point, detailed)
	var trim := Batch.new(point)
	var details := Batch.new(point)
	make_building.call(walls, trim, details, solids, signs)
	signs.position = Vector3.ZERO
	if signs.get_child_count() == 0:
		signs.queue_free()
	var facade := walls.attach(lot, facade_templates, facade_material)
	if facade != null and facade.near != null:
		facade.update(_eye())
		facade_lods.append(facade)
	trim.attach(lot, "Roof and trim", solid_material)
	details.attach(lot, "Balconies and railings", solid_material, true, 110.0)
	if interactive:
		lot.add_to_group("flood_building")


func game_coast() -> float:
	return layout.bounds.end.x + 42.0


func game_road() -> PackedVector3Array:
	var route := PackedVector3Array()
	for step in 161:
		var x := lerpf(layout.x[-1], layout.x[0], step / 160.0)
		route.append(layout.map_point(Vector3(x, 0, layout.z[Layout.CENTER])))
	return route


func game_palms(center_z: float) -> void:
	var coast := game_coast()
	var root := Node3D.new()
	root.name = "Beach palms"
	add_child(root)
	var batch := Trees.new(Vector3.ZERO)
	var rng := layout.random(4, 5, 240)
	for index in range(-9, 10):
		var z := center_z + index * 13.1 + rng.randf_range(-2.0, 2.0)
		if absf(z - (center_z - 17.0)) < 11.0 or absf(z - (center_z + 8.0)) < 5.0:
			continue
		batch.add_world(Vector3(coast - 34.7 + rng.randf_range(-2.6, 1.2), -0.14, z), rng.randf_range(5.4, 8.9), true, rng)
	tree_groups.append_array(batch.attach(root, tree_templates, _eye()))


func game_district_trees(parent: Node3D, origin: Vector3, points: PackedVector3Array, heights: PackedFloat32Array, district: int) -> void:
	var cluster := Node3D.new()
	cluster.name = "Street and garden trees"
	cluster.position = origin
	parent.add_child(cluster)
	var batch := Trees.new(origin)
	var rng := layout.random(district, 5, 448)
	for index in points.size():
		batch.add_world(points[index], heights[index], index % 7 == 0, rng)
	tree_groups.append_array(batch.attach(cluster, tree_templates, _eye()))


func _shader(shader: Shader) -> ShaderMaterial:
	var material := ShaderMaterial.new()
	material.shader = shader
	return material


func _light() -> void:
	var sun_direction := Vector3(-0.87, 0.39, 0.30).normalized()
	var sky_material := _shader(preload("res://shaders/coastal_sky.gdshader"))
	sky_material.set_shader_parameter("sun_direction", sun_direction)
	var sky := Sky.new()
	sky.sky_material = sky_material
	sky.radiance_size = Sky.RADIANCE_SIZE_128
	sky.process_mode = Sky.PROCESS_MODE_INCREMENTAL
	var environment := Environment.new()
	environment.background_mode = Environment.BG_SKY
	environment.sky = sky
	environment.ambient_light_source = Environment.AMBIENT_SOURCE_COLOR
	environment.ambient_light_color = Color("#cbdbf3")
	environment.ambient_light_energy = 0.83
	environment.ambient_light_sky_contribution = 0.25
	environment.reflected_light_source = Environment.REFLECTION_SOURCE_SKY
	environment.tonemap_mode = Environment.TONE_MAPPER_AGX
	environment.tonemap_exposure = 0.98
	environment.adjustment_enabled = true
	environment.adjustment_contrast = 1.08
	environment.adjustment_saturation = 1.14
	environment.fog_enabled = true
	environment.fog_light_color = Color("#9aadb8")
	environment.fog_density = 0.00054
	environment.fog_sky_affect = 0.0
	environment.ssao_enabled = true
	environment.ssao_radius = 1.75
	environment.ssao_intensity = 2.05
	environment.ssao_power = 1.38
	environment.glow_enabled = true
	environment.glow_bloom = 0.026
	environment.glow_intensity = 0.30
	var world := WorldEnvironment.new()
	world.name = "Coastal afternoon"
	world.environment = environment
	add_child(world)
	var sun := DirectionalLight3D.new()
	sun.name = "Late afternoon sun"
	sun.light_color = Color("#ffd6a5")
	sun.light_energy = 2.03
	sun.shadow_enabled = true
	sun.directional_shadow_max_distance = 285.0
	sun.directional_shadow_split_1 = 0.095
	sun.directional_shadow_split_2 = 0.25
	sun.directional_shadow_split_3 = 0.50
	sun.directional_shadow_blend_splits = true
	sun.shadow_bias = 0.032
	sun.shadow_normal_bias = 0.56
	add_child(sun)
	sun.basis = Basis.looking_at(-sun_direction)


func _hint() -> void:
	var hud := CanvasLayer.new()
	add_child(hud)
	hint = Label.new()
	hint.text = "WASD  Полёт    Мышь  Обзор    Q / E  Высота    Shift  Быстрее    F1–F3  Ракурсы    Esc  Курсор"
	hint.add_theme_font_size_override("font_size", 15)
	hint.add_theme_color_override("font_color", Color("#eee7d7"))
	hint.add_theme_color_override("font_shadow_color", Color(0, 0, 0, 0.75))
	hint.add_theme_constant_override("shadow_offset_y", 2)
	hint.mouse_filter = Control.MOUSE_FILTER_IGNORE
	hud.add_child(hint)
	hint.set_anchors_and_offsets_preset(Control.PRESET_CENTER_BOTTOM)
	hint.offset_top = -42
	hint.offset_bottom = -18
	hint.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	create_tween().tween_property(hint, "modulate:a", 0.0, 0.7).set_delay(7.5)
