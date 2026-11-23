extends RefCounted

const BLOCKS := 10
const CENTER := 5
const CHUNK_SIZE := 2
const CURB := 0.18
const SIDEWALK := 4.2
const ROUNDABOUT := Vector2i(5, 4)
const CIRCULATION_RADIUS := 15.0

var x: PackedFloat32Array
var z: PackedFloat32Array
var plan_bounds: Rect2
var bounds: Rect2
var seed_value: int
var pedestrian_links: Dictionary[Vector3i, bool] = {}


func _init(city_seed: int) -> void:
	seed_value = city_seed
	var rng := random(0, 0, 11)
	x = _axis(rng, 71.0, 91.0)
	z = _axis(rng, 81.0, 108.0)
	plan_bounds = Rect2(Vector2(x[0] - 8.0, z[0] - 8.0),
		Vector2(x[-1] - x[0] + 16.0, z[-1] - z[0] + 16.0))
	var perimeter := sample_rect(plan_bounds, 9.0)
	bounds = Rect2(map_xy(perimeter[0]), Vector2.ZERO)
	for point in perimeter:
		bounds = bounds.expand(map_xy(point))
	_choose_pedestrian_links(rng)


func map_xy(point: Vector2) -> Vector2:
	# The two bends are continuous across junctions and vary in strength by district.
	var corridor := -59.0 * sin(point.y * 0.0087) + 10.0 * sin(point.y * 0.017)
	var east := point.x + corridor * (0.76 + 0.31 * cos(point.x * 0.0072 + 0.64))
	var shore := 37.0 * sin(east * 0.0094) - 9.0 * sin(east * 0.019)
	return Vector2(east, point.y + shore * (0.72 + 0.28 * cos(point.y * 0.0078 - 0.58)))


func map_point(point: Vector3) -> Vector3:
	var mapped := map_xy(Vector2(point.x, point.z))
	return Vector3(mapped.x, point.y, mapped.y)


func map_heading(point: Vector3, direction: Vector3) -> Vector3:
	return (map_point(point + direction) - map_point(point - direction)).normalized()


static func sample_rect(rect: Rect2, spacing: float) -> PackedVector2Array:
	var corners := PackedVector2Array([rect.position, Vector2(rect.end.x, rect.position.y),
		rect.end, Vector2(rect.position.x, rect.end.y)])
	var points := PackedVector2Array()
	for edge in corners.size():
		var a := corners[edge]
		var b := corners[(edge + 1) % corners.size()]
		var parts := maxi(1, ceili(a.distance_to(b) / spacing))
		for part in parts:
			points.append(a.lerp(b, float(part) / parts))
	return points


func building_pose(center: Vector2, front: Vector3) -> Transform3D:
	var along_x := absf(front.z) > 0.5
	var point := Vector3(center.x, 0, center.y)
	var heading := map_heading(point, Vector3.RIGHT if along_x else Vector3.BACK)
	var right := heading if along_x else Vector3.UP.cross(heading)
	var back := right.cross(Vector3.UP)
	var basis := Basis(right, Vector3.UP, back)
	return Transform3D(basis, map_point(point) - basis * point)


func fit_building(rect: Rect2, pose: Transform3D) -> Rect2:
	var center := rect.get_center()
	var half := rect.size * 0.5
	var rim := sample_rect(rect, 5.0)
	var inverse := pose.affine_inverse()
	for index in rim.size():
		var point := rim[index]
		var local := inverse * map_point(Vector3(point.x, 0, point.y))
		rim[index] = Vector2(local.x, local.z) - center
	var scale := 1.0
	for index in rim.size():
		var a := rim[index]
		var edge := rim[(index + 1) % rim.size()] - a
		var normal := Vector2(edge.y, -edge.x).normalized()
		var limit := (normal.dot(a) - 0.06) / (normal.abs().dot(half))
		scale = minf(scale, limit)
	return Rect2(center - half * scale, half * (2.0 * scale))


func roundabout(i: int, j: int) -> bool:
	return Vector2i(i, j) == ROUNDABOUT


func pedestrian_link(road: int, segment: int, northbound: bool) -> bool:
	return pedestrian_links.has(Vector3i(road, segment, int(northbound)))


func _choose_pedestrian_links(rng: RandomNumberGenerator) -> void:
	var candidates: Array[Vector3i] = []
	for direction in 2:
		var northbound := direction == 1
		for road in range(1, BLOCKS):
			if road_class(road, northbound) > 0:
				continue
			for segment in range(1, BLOCKS - 1):
				if road_class(segment, not northbound) == 0 and road_class(segment + 1, not northbound) == 0:
					candidates.append(Vector3i(road, segment, direction))
	for index in range(candidates.size() - 1, 0, -1):
		var other := rng.randi_range(0, index)
		var candidate := candidates[index]
		candidates[index] = candidates[other]
		candidates[other] = candidate
	# Close at most one arm per junction, preserving connections to the surrounding streets.
	var occupied: Dictionary[Vector2i, bool] = {}
	for candidate in candidates:
		var northbound := candidate.z == 1
		var first := Vector2i(candidate.x, candidate.y) if northbound else Vector2i(candidate.y, candidate.x)
		var second := first + (Vector2i.DOWN if northbound else Vector2i.RIGHT)
		if occupied.has(first) or occupied.has(second):
			continue
		occupied[first] = true
		occupied[second] = true
		pedestrian_links[candidate] = true
		if pedestrian_links.size() == 12:
			break


func _axis(rng: RandomNumberGenerator, minimum: float, maximum: float) -> PackedFloat32Array:
	var coordinates := PackedFloat32Array()
	coordinates.resize(BLOCKS + 1)
	for index in range(CENTER - 1, -1, -1):
		coordinates[index] = coordinates[index + 1] - rng.randf_range(minimum, maximum)
	for index in range(CENTER + 1, BLOCKS + 1):
		coordinates[index] = coordinates[index - 1] + rng.randf_range(minimum, maximum)
	return coordinates


func random(i: int, j: int, salt: int) -> RandomNumberGenerator:
	var rng := RandomNumberGenerator.new()
	rng.seed = hash(Vector4i(seed_value, i, j, salt))
	return rng


func road_class(index: int, northbound: bool) -> int:
	if northbound and index == CENTER:
		return 2
	if index == 2 or index == 8 or (not northbound and index == CENTER):
		return 1
	return 0


func half_width(index: int, northbound: bool) -> float:
	return [5.5, 8.6, 10.9][road_class(index, northbound)]


func block(i: int, j: int) -> Rect2:
	var minimum := Vector2(x[i] + half_width(i, true), z[j] + half_width(j, false))
	var maximum := Vector2(x[i + 1] - half_width(i + 1, true), z[j + 1] - half_width(j + 1, false))
	return Rect2(minimum, maximum - minimum)


func park(i: int, j: int) -> bool:
	return Vector2i(i, j) in [Vector2i(4, 2), Vector2i(6, 5), Vector2i(2, 6),
		Vector2i(7, 3), Vector2i(3, 8), Vector2i(8, 8), Vector2i(1, 3)]
