extends RefCounted

const Models = preload("res://city/tree_models.gd")
const Layout = preload("res://city/layout.gd")


class Group:
	var chunk_origin: Vector3
	var transforms: Array[Transform3D]
	var shades: PackedColorArray
	var distance: float
	var is_near := PackedByteArray()
	var detailed: MultiMesh
	var distant: MultiMesh

	func update(eye: Vector3) -> void:
		var changed := is_near.is_empty()
		is_near.resize(transforms.size())
		var relative_eye := eye - chunk_origin
		for index in transforms.size():
			var previous := is_near[index] == 1
			var threshold := distance + (6.0 if previous else 0.0)
			var delta := transforms[index].origin - relative_eye
			delta.y += transforms[index].basis.y.y * 4.7
			var close := delta.length_squared() < threshold * threshold
			if close != previous:
				changed = true
			is_near[index] = int(close)
		if not changed:
			return
		var near_count := 0
		var far_count := 0
		for index in transforms.size():
			if is_near[index] == 1:
				detailed.set_instance_transform(near_count, transforms[index])
				detailed.set_instance_color(near_count, shades[index])
				near_count += 1
			else:
				distant.set_instance_transform(far_count, transforms[index])
				distant.set_instance_color(far_count, shades[index])
				far_count += 1
		detailed.visible_instance_count = near_count
		distant.visible_instance_count = far_count


var origin: Vector3
var transforms: Array[Array] = [[], [], [], []]
var shades: Array[PackedColorArray] = [PackedColorArray(), PackedColorArray(), PackedColorArray(), PackedColorArray()]


func _init(chunk_origin: Vector3) -> void:
	origin = chunk_origin


func add(layout: Layout, point: Vector3, height: float, palm: bool, rng: RandomNumberGenerator) -> void:
	add_world(layout.map_point(point), height, palm, rng)


func add_world(point: Vector3, height: float, palm: bool, rng: RandomNumberGenerator) -> void:
	var index := rng.randi_range(0, 1) + (0 if palm else 2)
	var scale := height / (Models.PALM_HEIGHT if palm else Models.TREE_HEIGHT)
	var width := pow(scale, 0.45) * rng.randf_range(0.88, 1.10)
	if palm:
		width = minf(width, 1.055)
	var pivot := point - origin
	var turn := Basis(Vector3.UP, rng.randf() * TAU).scaled_local(Vector3(width, scale, width))
	transforms[index].append(Transform3D(turn, pivot))
	var brightness := rng.randf_range(0.88, 1.0)
	var coolness := rng.randf_range(0.91, 1.0)
	var colors := shades[index]
	colors.append(Color(brightness * coolness, brightness, brightness * lerpf(0.90, 1.0, coolness), 1.0))
	shades[index] = colors


func attach(parent: Node3D, templates: Array[Models.Model], eye: Vector3) -> Array[Group]:
	var groups: Array[Group] = []
	for index in templates.size():
		var poses: Array[Transform3D] = []
		poses.assign(transforms[index])
		if poses.is_empty():
			continue
		var model := templates[index]
		var group := Group.new()
		group.chunk_origin = origin
		group.transforms = poses
		group.shades = shades[index]
		group.distance = model.detail_distance
		group.detailed = _make_instance(parent, model.label + " detailed", model.detailed, poses)
		group.distant = _make_instance(parent, model.label + " distant", model.distant, poses)
		group.update(eye)
		groups.append(group)
	return groups


func _make_instance(parent: Node3D, title: String, mesh: ArrayMesh, poses: Array[Transform3D]) -> MultiMesh:
	var bounds := poses[0] * mesh.get_aabb()
	for index in range(1, poses.size()):
		bounds = bounds.merge(poses[index] * mesh.get_aabb())
	var batch := MultiMesh.new()
	batch.transform_format = MultiMesh.TRANSFORM_3D
	batch.use_colors = true
	batch.mesh = mesh
	batch.instance_count = poses.size()
	batch.visible_instance_count = 0
	batch.custom_aabb = bounds.grow(0.83)
	var instance := MultiMeshInstance3D.new()
	instance.name = title
	instance.multimesh = batch
	parent.add_child(instance)
	return batch
