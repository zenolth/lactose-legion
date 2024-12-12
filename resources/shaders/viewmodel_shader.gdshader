shader_type spatial;
render_mode depth_draw_opaque,cull_back;

uniform vec4 albedo : source_color = vec4(1.0,1.0,1.0,1.0);
uniform sampler2D texture_albedo : source_color, filter_nearest_mipmap, repeat_enable;

global uniform float fov;
global uniform float depth_multiplier;
const float M_PI = 3.14159265359;

void vertex() {
	// recreate the camera projection matrix with our custom fov value 
	float scale = 1.0 / tan(fov * 0.5 * M_PI / -180.0); 
	PROJECTION_MATRIX[0][0] = scale / (-VIEWPORT_SIZE.x / VIEWPORT_SIZE.y);
	PROJECTION_MATRIX[1][1] = scale;
}

void fragment() {
	vec4 albedo_tex = texture(texture_albedo,UV);
	ALBEDO = albedo.rgb * albedo_tex.rgb;
	DEPTH = 1.0 - (1.0 - FRAGCOORD.z) * depth_multiplier;
}