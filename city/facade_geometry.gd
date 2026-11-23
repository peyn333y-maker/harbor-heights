extends RefCounted

const Batch = preload("res://city/mesh_batch.gd")

enum Surface { BRICK, PANEL, CONCRETE, STONE, PLASTER, FRAME, GLASS, SHOP_GLASS, BRONZE }

const OPENINGS := [
	Rect2(-0.29, 0.19, 0.58, 0.67),
	Rect2(-0.457, 0.135, 0.914, 0.81),
	Rect2(-0.398, 0.102, 0.796, 0.704),
	Rect2(-0.292, 0.205, 0.584, 0.664),
	Rect2(-0.424, 0.129, 0.848, 0.675),
	Rect2(),
	Rect2(-0.407, 0.014, 0.814, 0.794),
]
const DEPTHS := [0.36, 0.26, 0.54, 0.43, 0.67, 0.20, 0.85]
const BAY_NAMES := ["Brick", "Curtain wall", "Concrete", "Stone", "Shop window", "Solid wall", "Entrance"]
const STORE := 4
const SOLID := 5
const ENTRANCE := 6
const CORNER_STORE := 7
const FLOATS_PER_INSTANCE := 20


class Lod:
	var near: Node3D
	var far: MeshInstance3D
	var minimum: Vector3
	var maximum: Vector3

	func update(eye: Vector3) -> void:
		var gap := eye - eye.clamp(minimum, maximum)
		var cutoff := 208.0 if near.visible else 174.0
		var show_geometry := gap.length_squared() < cutoff * cutoff
		if show_geometry != near.visible or far.visible == show_geometry:
			near.visible = show_geometry
			far.visible = not show_geometry


var _distant: Batch
var _detailed: bool
var _buffers: Array[PackedFloat32Array] = []


func _init(origin: Vector3, detailed := true) -> void:
	_detailed = detailed
	_distant = Batch.new(origin)
	for style in OPENINGS.size():
		_buffers.append(PackedFloat32Array())


func place(pose: Transform3D) -> void:
	_distant.placement = pose


func walls(rect: Rect2, base: float, top: float, tint: Color, floors: int,
		bay_width: float, data: Vector2) -> void:
	outline(PackedVector2Array([rect.position, Vector2(rect.end.x, rect.position.y),
		rect.end, Vector2(rect.position.x, rect.end.y)]), base, top, tint, floors, bay_width, data)


func outline(points: PackedVector2Array, base: float, top: float, tint: Color,
		floors: int, bay_width: float, data: Vector2, entrance_edge := -1) -> void:
	var style := int(data.x)
	var story := (top - base) / floors
	var linear_tint := tint.srgb_to_linear()
	for edge in points.size():
		var p := points[edge]
		var delta := points[(edge + 1) % points.size()] - p
		var span := delta.length()
		var bays := 3 if edge == entrance_edge else maxi(1, roundi(span / bay_width))
		var direction := Vector3(delta.x, 0.0, delta.y)
		var step := _distant.placement.basis * direction / bays
		var outward := Vector3.UP.cross(step).normalized()
		var plane_normal := Vector3.UP.cross(direction).normalized()
		var row_origin := _distant.placement * Vector3(p.x, base, p.y) - _distant.origin
		var face_seed := int(data.y) + edge
		var q := p + delta
		var distant_style := CORNER_STORE if edge == entrance_edge else style
		_distant.quad(Vector3(p.x, base, p.y), Vector3(q.x, base, q.y), Vector3(q.x, top, q.y),
			Vector3(p.x, top, p.y), plane_normal, tint, Rect2(0, 0, bays, floors), Vector2(distant_style, face_seed))
		if not _detailed:
			continue
		for bay in bays:
			var is_entry := style == STORE and posmod(bay + face_seed, 4) == 0
			if edge == entrance_edge:
				is_entry = bay == 1
			var variant := ENTRANCE if is_entry else style
			var opening: Rect2 = OPENINGS[variant]
			var depth: float = DEPTHS[variant]
			var position := row_origin + step * (bay + 0.5)
			var buffer := _buffers[variant]
			var pane_width := maxf(0.6, span / bays * opening.size.x)
			var pane_height := maxf(0.6, story * opening.size.y)
			for floor_index in floors:
				# MultiMesh stores the transform by row, followed by color and custom data.
				buffer.append(-step.x)
				buffer.append(0.0)
				buffer.append(outward.x * depth)
				buffer.append(position.x)
				buffer.append(0.0)
				buffer.append(story)
				buffer.append(0.0)
				buffer.append(position.y + floor_index * story)
				buffer.append(-step.z)
				buffer.append(0.0)
				buffer.append(outward.z * depth)
				buffer.append(position.z)
				buffer.append(linear_tint.r)
				buffer.append(linear_tint.g)
				buffer.append(linear_tint.b)
				buffer.append(1.0)
				buffer.append(float(posmod(face_seed * 193 + bay * 79 + floor_index * 141, 7919)))
				buffer.append(pane_width)
				buffer.append(pane_height)
				buffer.append(0.0)
			_buffers[variant] = buffer


func attach(parent: Node3D, templates: Array[ArrayMesh], distant_material: Material) -> Lod:
	var far := _distant.attach(parent, "Distant building facades", distant_material)
	if far == null:
		return null
	if not _detailed:
		return null
	var near := Node3D.new()
	near.name = "Window and wall geometry"
	parent.add_child(near)
	for style in templates.size():
		var buffer := _buffers[style]
		if buffer.is_empty():
			continue
		var instances := MultiMesh.new()
		instances.transform_format = MultiMesh.TRANSFORM_3D
		instances.use_colors = true
		instances.use_custom_data = true
		instances.mesh = templates[style]
		@warning_ignore("integer_division")
		instances.instance_count = buffer.size() / FLOATS_PER_INSTANCE
		instances.buffer = buffer
		var mesh := MultiMeshInstance3D.new()
		mesh.name = BAY_NAMES[style]
		mesh.multimesh = instances
		near.add_child(mesh)
	var bounds := far.mesh.get_aabb()
	var lod := Lod.new()
	lod.near = near
	lod.far = far
	lod.minimum = parent.global_position + bounds.position
	lod.maximum = parent.global_position + bounds.end
	return lod


static func create_templates(material: Material) -> Array[ArrayMesh]:
	var templates: Array[ArrayMesh] = []
	for style in OPENINGS.size():
		templates.append(_template(style).create_mesh(material))
	return templates


static func configure_distant_material(material: ShaderMaterial) -> void:
	var openings := PackedVector4Array()
	for rect: Rect2 in OPENINGS:
		openings.append(Vector4(rect.position.x + 0.5, rect.position.y, rect.end.x + 0.5, rect.end.y))
	material.set_shader_parameter("openings", openings)


static func _template(style: int) -> Batch:
	var mesh := Batch.new()
	var panel := Rect2(-0.5, 0, 1, 1)
	if style == SOLID:
		_panel(mesh, panel, 0, Surface.STONE)
		return mesh

	var opening: Rect2 = OPENINGS[style]
	var wall: int = [Surface.BRICK, Surface.PANEL, Surface.CONCRETE, Surface.STONE,
		Surface.PLASTER, Surface.STONE, Surface.PLASTER][style]
	var shop := style == STORE or style == ENTRANCE
	var reveal := opening.grow(-0.010 if shop else -0.016)
	var frame := reveal.grow(-0.017 if style == 1 else -0.022)
	var pane := frame.grow(-0.006 if style == 1 else -0.010)
	_ring(mesh, panel, opening, 0.0, 0.0, wall)
	_ring(mesh, opening, reveal, 0.0, -0.83, wall)
	var frame_metal := Surface.BRONZE if shop else Surface.FRAME
	_ring(mesh, reveal, frame, -0.83, -0.86, frame_metal)
	_ring(mesh, frame, pane, -0.86, -1.0, frame_metal)
	_panel(mesh, pane, -1.0, Surface.SHOP_GLASS if shop else Surface.GLASS)

	if style in [0, 3]:
		var sill := Rect2(opening.position.x - 0.038, opening.position.y - 0.043,
			opening.size.x + 0.076, 0.049)
		_prism(mesh, sill, -0.12, 0.53, Surface.STONE)
		var lintel := Rect2(opening.position.x - 0.020, opening.end.y,
			opening.size.x + 0.040, 0.047 if style == 0 else 0.061)
		_prism(mesh, lintel, -0.08, 0.28, wall)
		_crossbar(mesh, pane, lerpf(pane.position.y, pane.end.y, 0.66), 0.018, frame_metal)
		if style == 3:
			_prism(mesh, Rect2(-0.499, 0.01, 0.998, 0.066), -0.02, 0.20, wall)
	else:
		if style in [1, 2, STORE, ENTRANCE]:
			var mullion := Rect2(-0.009, pane.position.y, 0.018, pane.size.y)
			_prism(mesh, mullion, -1.014, -0.75, frame_metal)
		if style == 1:
			_prism(mesh, Rect2(-0.499, 0.089, 0.998, 0.019), -0.04, 0.28, frame_metal)
		elif shop:
			_crossbar(mesh, pane, lerpf(pane.position.y, pane.end.y, 0.74), 0.017, frame_metal)
			if style == ENTRANCE:
				_prism(mesh, Rect2(0.034, 0.222, 0.010, 0.067), -0.999, -0.79, Surface.BRONZE)
				_prism(mesh, Rect2(-0.044, 0.222, 0.010, 0.067), -0.999, -0.79, Surface.BRONZE)
	return mesh


static func _crossbar(mesh: Batch, pane: Rect2, center_y: float, thickness: float, surface: int) -> void:
	var bar := Rect2(pane.position.x, center_y - thickness * 0.5, pane.size.x, thickness)
	var middle := 0.009 if surface == Surface.BRONZE else 0.0
	if middle > 0.0:
		_prism(mesh, Rect2(bar.position.x, bar.position.y, -middle - bar.position.x, thickness),
			-1.012, -0.74, surface)
		_prism(mesh, Rect2(middle, bar.position.y, bar.end.x - middle, thickness),
			-1.012, -0.74, surface)
	else:
		_prism(mesh, bar, -1.012, -0.74, surface)


static func _corners(rect: Rect2, depth: float) -> PackedVector3Array:
	return PackedVector3Array([Vector3(rect.position.x, rect.position.y, depth),
		Vector3(rect.end.x, rect.position.y, depth), Vector3(rect.end.x, rect.end.y, depth),
		Vector3(rect.position.x, rect.end.y, depth)])


static func _panel(mesh: Batch, rect: Rect2, depth: float, surface: int) -> void:
	var corners := _corners(rect, depth)
	mesh.quad(corners[0], corners[1], corners[2], corners[3], Vector3.BACK,
		Color.WHITE, Rect2(0, 0, 1, 1), Vector2(surface, 0))


static func _ring(mesh: Batch, outer: Rect2, inner: Rect2, start: float, finish: float, surface: int) -> void:
	var a := _corners(outer, start)
	var b := _corners(inner, finish)
	for side in 4:
		var next := (side + 1) % 4
		var normal := (a[next] - a[side]).cross(b[side] - a[side]).normalized()
		mesh.quad(a[side], a[next], b[next], b[side], normal, Color.WHITE,
			Rect2(0, 0, 1, 1), Vector2(surface, 0))


static func _prism(mesh: Batch, rect: Rect2, back: float, front: float, surface: int) -> void:
	_ring(mesh, rect, rect, back, front, surface)
	_panel(mesh, rect, front, surface)
