extends RefCounted

var vertices := PackedVector3Array()
var normals := PackedVector3Array()
var colors := PackedColorArray()
var uvs := PackedVector2Array()
var uv2s := PackedVector2Array()
var indices := PackedInt32Array()
var origin: Vector3
var placement := Transform3D.IDENTITY
var warp := Callable()


func _init(chunk_origin := Vector3.ZERO) -> void:
	origin = chunk_origin


func _vertex(point: Vector3, normal: Vector3, tint: Color, uv: Vector2, data: Vector2) -> void:
	if warp.is_valid():
		point = warp.call(point)
	else:
		point = placement * point
		normal = placement.basis * normal
	vertices.append(point - origin)
	normals.append(normal)
	colors.append(tint)
	uvs.append(uv)
	uv2s.append(data)


func add_vertex(point: Vector3, normal: Vector3, tint: Color, uv: Vector2, data := Vector2.ZERO) -> int:
	var index := vertices.size()
	_vertex(point, normal, tint, uv, data)
	return index


func face(a: int, b: int, c: int) -> void:
	var normal := (vertices[b] - vertices[a]).cross(vertices[c] - vertices[a])
	var reverse := normal.dot(normals[a] + normals[b] + normals[c]) > 0.0
	indices.append(a)
	indices.append(c if reverse else b)
	indices.append(b if reverse else c)


func quad(a: Vector3, b: Vector3, c: Vector3, d: Vector3, normal: Vector3, tint: Color,
		uv_rect := Rect2(0, 0, 1, 1), data := Vector2.ZERO) -> void:
	var start := vertices.size()
	_vertex(a, normal, tint, uv_rect.position, data)
	_vertex(b, normal, tint, Vector2(uv_rect.end.x, uv_rect.position.y), data)
	_vertex(c, normal, tint, uv_rect.end, data)
	_vertex(d, normal, tint, Vector2(uv_rect.position.x, uv_rect.end.y), data)
	var reverse := (b - a).cross(c - a).dot(normal) > 0.0
	if warp.is_valid() and absf(normal.y) < 0.95:
		var surface := (vertices[start + 1] - vertices[start]).cross(vertices[start + 2] - vertices[start])
		var adjusted := surface.normalized() * (1.0 if reverse else -1.0)
		for corner in 4:
			normals[start + corner] = adjusted
	indices.append(start)
	indices.append(start + (2 if reverse else 1))
	indices.append(start + (1 if reverse else 2))
	indices.append(start)
	indices.append(start + (3 if reverse else 2))
	indices.append(start + (2 if reverse else 3))


func triangle(a: Vector3, b: Vector3, c: Vector3, normal: Vector3, tint: Color, data := Vector2.ZERO) -> void:
	smooth_triangle(a, b, c, normal, normal, normal, tint, tint, tint, data)


func smooth_triangle(a: Vector3, b: Vector3, c: Vector3, na: Vector3, nb: Vector3, nc: Vector3,
		ca: Color, cb: Color, cc: Color, data := Vector2.ZERO) -> void:
	var start := vertices.size()
	_vertex(a, na, ca, Vector2.ZERO, data)
	_vertex(b, nb, cb, Vector2.ZERO, data)
	_vertex(c, nc, cc, Vector2.ZERO, data)
	var reverse := (b - a).cross(c - a).dot(na + nb + nc) > 0.0
	indices.append(start)
	indices.append(start + (2 if reverse else 1))
	indices.append(start + (1 if reverse else 2))


func floor_rect(rect: Rect2, elevation: float, tint: Color, data := Vector2.ZERO) -> void:
	if rect.size.x <= 0.0 or rect.size.y <= 0.0:
		return
	quad(Vector3(rect.position.x, elevation, rect.position.y),
		Vector3(rect.end.x, elevation, rect.position.y), Vector3(rect.end.x, elevation, rect.end.y),
		Vector3(rect.position.x, elevation, rect.end.y), Vector3.UP, tint, Rect2(0, 0, 1, 1), data)


func walls(rect: Rect2, base: float, top: float, tint: Color, floor_count := 1.0,
		bay_width := 1000.0, data := Vector2.ZERO) -> void:
	var nw := Vector3(rect.position.x, base, rect.position.y)
	var ne := Vector3(rect.end.x, base, rect.position.y)
	var se := Vector3(rect.end.x, base, rect.end.y)
	var sw := Vector3(rect.position.x, base, rect.end.y)
	var up := Vector3.UP * (top - base)
	var across := maxf(1.0, roundf(rect.size.x / bay_width))
	var along := maxf(1.0, roundf(rect.size.y / bay_width))
	var horizontal := Rect2(0, 0, across, floor_count)
	var vertical := Rect2(0, 0, along, floor_count)
	quad(nw, ne, ne + up, nw + up, Vector3.FORWARD, tint, horizontal, data)
	quad(ne, se, se + up, ne + up, Vector3.RIGHT, tint, vertical, data + Vector2(0, 1))
	quad(se, sw, sw + up, se + up, Vector3.BACK, tint, horizontal, data + Vector2(0, 2))
	quad(sw, nw, nw + up, sw + up, Vector3.LEFT, tint, vertical, data + Vector2(0, 3))


func outline(points: PackedVector2Array, base: float, top: float, tint: Color,
		floors: int, bay_width: float, data: Vector2) -> void:
	for edge in points.size():
		var p := points[edge]
		var q := points[(edge + 1) % points.size()]
		var outward := Vector3(q.y - p.y, 0, p.x - q.x).normalized()
		var uv := Rect2(0, 0, maxf(1.0, roundf(p.distance_to(q) / bay_width)), floors)
		quad(Vector3(p.x, base, p.y), Vector3(q.x, base, q.y), Vector3(q.x, top, q.y),
			Vector3(p.x, top, p.y), outward, tint, uv, data + Vector2(0, edge))


func cover(points: PackedVector2Array, y: float, tint: Color) -> void:
	var first := Vector3(points[0].x, y, points[0].y)
	for index in range(1, points.size() - 1):
		triangle(first, Vector3(points[index].x, y, points[index].y),
			Vector3(points[index + 1].x, y, points[index + 1].y), Vector3.UP, tint)


func box(center: Vector3, size: Vector3, tint: Color, bottom := false, data := Vector2.ZERO) -> void:
	var rect := Rect2(Vector2(center.x - size.x * 0.5, center.z - size.z * 0.5), Vector2(size.x, size.z))
	walls(rect, center.y - size.y * 0.5, center.y + size.y * 0.5, tint, 1.0, 1000.0, data)
	floor_rect(rect, center.y + size.y * 0.5, tint.lightened(0.045), data)
	if bottom:
		var y := center.y - size.y * 0.5
		quad(Vector3(rect.position.x, y, rect.position.y), Vector3(rect.end.x, y, rect.position.y),
			Vector3(rect.end.x, y, rect.end.y), Vector3(rect.position.x, y, rect.end.y), Vector3.DOWN, tint,
			Rect2(0, 0, 1, 1), data)


func beam(a: Vector3, b: Vector3, radius_a: float, radius_b: float, tint: Color, sides := 6,
		data := Vector2.ZERO) -> void:
	var axis := (b - a).normalized()
	var reference := Vector3.RIGHT if absf(axis.y) > 0.9 else Vector3.UP
	var u := axis.cross(reference).normalized()
	var v := axis.cross(u)
	for side in sides:
		var t0 := TAU * side / sides
		var t1 := TAU * (side + 1) / sides
		var n0 := u * cos(t0) + v * sin(t0)
		var n1 := u * cos(t1) + v * sin(t1)
		var a0 := a + n0 * radius_a
		var a1 := a + n1 * radius_a
		var b0 := b + n0 * radius_b
		var b1 := b + n1 * radius_b
		quad(a0, a1, b1, b0, (n0 + n1).normalized(), tint, Rect2(float(side) / sides, 0, 1.0 / sides, 1), data)
		if radius_b > 0.0:
			triangle(b, b0, b1, axis, tint, data)


func create_mesh(material: Material, mesh: ArrayMesh = null) -> ArrayMesh:
	if vertices.is_empty():
		return null
	var arrays := []
	arrays.resize(Mesh.ARRAY_MAX)
	arrays[Mesh.ARRAY_VERTEX] = vertices
	arrays[Mesh.ARRAY_NORMAL] = normals
	arrays[Mesh.ARRAY_COLOR] = colors
	arrays[Mesh.ARRAY_TEX_UV] = uvs
	arrays[Mesh.ARRAY_TEX_UV2] = uv2s
	arrays[Mesh.ARRAY_INDEX] = indices
	if mesh == null:
		mesh = ArrayMesh.new()
	mesh.add_surface_from_arrays(Mesh.PRIMITIVE_TRIANGLES, arrays)
	mesh.surface_set_material(mesh.get_surface_count() - 1, material)
	return mesh


func attach(parent: Node3D, label: String, material: Material, shadows := true, end_distance := 0.0) -> MeshInstance3D:
	var mesh := create_mesh(material)
	if mesh == null:
		return null
	var instance := MeshInstance3D.new()
	instance.name = label
	instance.mesh = mesh
	if not shadows:
		instance.cast_shadow = GeometryInstance3D.SHADOW_CASTING_SETTING_OFF
	if end_distance > 0.0:
		instance.visibility_range_end = end_distance
		instance.visibility_range_end_margin = 24.0
		instance.visibility_range_fade_mode = GeometryInstance3D.VISIBILITY_RANGE_FADE_SELF
	parent.add_child(instance)
	return instance
