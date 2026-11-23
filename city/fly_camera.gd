extends CharacterBody3D

const Layout = preload("res://city/layout.gd")

var camera: Camera3D
var limits: Rect2
var layout: Layout
var view := 0
var yaw := 0.0
var pitch := 0.0


func _ready() -> void:
	collision_layer = 0
	collision_mask = 1
	motion_mode = MOTION_MODE_FLOATING
	var collider := CollisionShape3D.new()
	var sphere := SphereShape3D.new()
	sphere.radius = 0.34
	collider.shape = sphere
	add_child(collider)
	camera = Camera3D.new()
	camera.fov = 66.0
	camera.near = 0.12
	camera.far = 6100.0
	add_child(camera)
	camera.make_current()


func setup(plan: Layout) -> void:
	layout = plan
	limits = plan.bounds.grow(132.0)
	set_view(0)
	if DisplayServer.get_name() != "headless":
		Input.mouse_mode = Input.MOUSE_MODE_CAPTURED


func set_view(index: int) -> void:
	view = posmod(index, 3)
	velocity = Vector3.ZERO
	var targets := [Vector3(-13.0, 8.0, -70.0), Vector3(0.0, 0.0, layout.z[4] - 18.0), Vector3(64.0, 7.0, -150.0)]
	var positions := [Vector3(8.4, 2.4, 20.0), Vector3(-6.0, 57.0, layout.z[4] + 75.0), Vector3(-248.0, 265.0, 255.0)]
	global_position = layout.map_point(positions[view])
	var direction := (layout.map_point(targets[view]) - global_position).normalized()
	yaw = atan2(-direction.x, -direction.z)
	pitch = asin(direction.y)
	_apply_rotation()
	reset_physics_interpolation()


func _unhandled_input(event: InputEvent) -> void:
	if event is InputEventMouseMotion and Input.mouse_mode == Input.MOUSE_MODE_CAPTURED:
		yaw = wrapf(yaw - event.screen_relative.x * 0.0022, -PI, PI)
		pitch = clampf(pitch - event.screen_relative.y * 0.0022, -1.50, 1.50)
		_apply_rotation()
	elif event is InputEventMouseButton and event.pressed and event.button_index == MOUSE_BUTTON_LEFT:
		Input.mouse_mode = Input.MOUSE_MODE_CAPTURED
	elif event is InputEventKey and event.pressed and not event.echo:
		match event.physical_keycode:
			KEY_ESCAPE:
				Input.mouse_mode = Input.MOUSE_MODE_VISIBLE
			KEY_F1, KEY_F2, KEY_F3:
				set_view(event.physical_keycode - KEY_F1)


func _apply_rotation() -> void:
	rotation.y = yaw
	camera.rotation.x = pitch


func _physics_process(delta: float) -> void:
	if Input.mouse_mode != Input.MOUSE_MODE_CAPTURED:
		velocity = Vector3.ZERO
		return
	var forward := float(Input.is_physical_key_pressed(KEY_W)) - float(Input.is_physical_key_pressed(KEY_S))
	var strafe := float(Input.is_physical_key_pressed(KEY_D)) - float(Input.is_physical_key_pressed(KEY_A))
	var rise := float(Input.is_physical_key_pressed(KEY_E) or Input.is_physical_key_pressed(KEY_SPACE)) - float(Input.is_physical_key_pressed(KEY_Q) or Input.is_physical_key_pressed(KEY_CTRL))
	var direction := -camera.global_basis.z * forward + global_basis.x * strafe + Vector3.UP * rise
	var speed := 36.0 if Input.is_physical_key_pressed(KEY_SHIFT) else 9.5
	velocity = velocity.lerp(direction.limit_length() * speed, 1.0 - exp(-delta * 10.0))
	move_and_slide()
	position = Vector3(clampf(position.x, limits.position.x, limits.end.x),
		clampf(position.y, 1.72, 320.0), clampf(position.z, limits.position.y, limits.end.y))
